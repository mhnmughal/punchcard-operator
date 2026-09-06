using System;
using System.Collections.Generic;
using UnityEngine;

namespace PunchcardOperator
{
    // ---------------------------------------------------------------------
    // Punchcard Operator - core data + simulation. Pure C#, no MonoBehaviour.
    // The card holds instruction rows; the machine reader executes them in
    // order and a carrier moves across a grid toward a target.
    // ---------------------------------------------------------------------

    public enum CardOp { Empty = 0, Move = 1, TurnLeft = 2, TurnRight = 3, Repeat = 4 }
    public enum Channel { A = 0, B = 1 }
    public enum Facing { North = 0, East = 1, South = 2, West = 3 }
    public enum CardTool { Punch = 0, Patch = 1, Shift = 2, Swap = 3, Flip = 4, Copy = 5 }
    public enum LevelRating { None = 0, Accepted = 1, CompactCard = 2, PerfectProgram = 3 }
    public enum ProgramBank { None = 0, A = 1, B = 2, C = 3 }

    [Serializable]
    public class CardRow
    {
        public CardOp op = CardOp.Empty;
        [Min(1)] public int repeatCount = 1;
        public Channel channel = Channel.A;
        public bool locked;

        public bool IsEmpty => op == CardOp.Empty;
        public CardRow Clone() => new CardRow { op = op, repeatCount = repeatCount, channel = channel, locked = locked };
    }

    [Serializable]
    public class PunchCard
    {
        public List<CardRow> rows = new List<CardRow>();
        public bool flipped;

        public PunchCard Clone()
        {
            var c = new PunchCard { flipped = flipped };
            for (int i = 0; i < rows.Count; i++) c.rows.Add(rows[i].Clone());
            return c;
        }

        public static PunchCard OfSize(int n)
        {
            var c = new PunchCard();
            for (int i = 0; i < n; i++) c.rows.Add(new CardRow());
            return c;
        }
    }

    [Serializable]
    public class GridDef
    {
        [Min(1)] public int width = 5;
        [Min(1)] public int height = 5;
        public List<Vector2Int> walls = new List<Vector2Int>();

        public bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;

        public bool IsWall(Vector2Int p)
        {
            for (int i = 0; i < walls.Count; i++)
                if (walls[i] == p) return true;
            return false;
        }
    }

    [Serializable]
    public class MachineConfig
    {
        public bool enabled = true;
        public Vector2Int start;
        public Facing startFacing = Facing.North;
        public Vector2Int target;
    }

    public struct SimStep
    {
        public int rowIndex;      // source row in the (unflipped) card
        public Channel channel;
        public Vector2Int posA, posB;
        public Facing facingA, facingB;
        public string note;
    }

    public class ExecutionResult
    {
        public bool success;
        public string failureReason;
        public readonly List<SimStep> steps = new List<SimStep>();
        public Vector2Int finalA, finalB;
        public Facing finalFacingA, finalFacingB;
        public bool machineAReached, machineBReached;
    }

    public static class PunchCardSimulator
    {
        private class Carrier { public Vector2Int pos; public Facing fac; }

        private static Vector2Int Delta(Facing f)
        {
            switch (f)
            {
                case Facing.North: return new Vector2Int(0, 1);
                case Facing.East: return new Vector2Int(1, 0);
                case Facing.South: return new Vector2Int(0, -1);
                default: return new Vector2Int(-1, 0);
            }
        }

        private static Facing Turn(Facing f, int dir) => (Facing)(((int)f + dir + 4) % 4);

        /// <summary>Executes a card. maxOps caps runaway Repeat counts.</summary>
        public static ExecutionResult Run(GridDef grid, MachineConfig a, MachineConfig b, PunchCard card, int maxOps = 4000)
        {
            var res = new ExecutionResult();
            var ca = new Carrier { pos = a.start, fac = a.startFacing };
            var cb = new Carrier { pos = b != null && b.enabled ? b.start : Vector2Int.zero, fac = b != null ? b.startFacing : Facing.North };
            bool dual = b != null && b.enabled;

            int opCount = 0;
            // Previous real instruction, tracked per channel so REPEAT on a Dual Channel
            // card repeats that channel's own last instruction.
            var prevOpByChannel = new[] { CardOp.Empty, CardOp.Empty };

            void Snapshot(int rowIndex, Channel ch, string note)
            {
                res.steps.Add(new SimStep
                {
                    rowIndex = rowIndex,
                    channel = ch,
                    posA = ca.pos, facingA = ca.fac,
                    posB = cb.pos, facingB = cb.fac,
                    note = note
                });
            }

            bool ExecOp(CardOp op, Channel ch, int rowIndex, out string fail)
            {
                fail = null;
                var carrier = ch == Channel.A ? ca : cb;
                switch (op)
                {
                    case CardOp.Empty:
                    case CardOp.Repeat:
                        return true;
                    case CardOp.TurnLeft:
                        carrier.fac = Turn(carrier.fac, -1);
                        Snapshot(rowIndex, ch, "LEFT");
                        return true;
                    case CardOp.TurnRight:
                        carrier.fac = Turn(carrier.fac, +1);
                        Snapshot(rowIndex, ch, "RIGHT");
                        return true;
                    case CardOp.Move:
                        var next = carrier.pos + Delta(carrier.fac);
                        if (!grid.InBounds(next))
                        {
                            fail = $"MACHINE {ch} DROVE OFF THE BOARD (card row {rowIndex + 1})";
                            return false;
                        }
                        if (grid.IsWall(next))
                        {
                            fail = $"MACHINE {ch} HIT A WALL (card row {rowIndex + 1})";
                            return false;
                        }
                        carrier.pos = next;
                        Snapshot(rowIndex, ch, "MOVE");
                        return true;
                    default:
                        return true;
                }
            }

            var order = new List<int>(card.rows.Count);
            for (int i = 0; i < card.rows.Count; i++) order.Add(i);
            if (card.flipped) order.Reverse();

            for (int oi = 0; oi < order.Count; oi++)
            {
                int rowIndex = order[oi];
                var row = card.rows[rowIndex];
                if (row.IsEmpty) continue;

                if (row.op == CardOp.Repeat)
                {
                    var prevOp = prevOpByChannel[(int)row.channel];
                    if (prevOp == CardOp.Empty || prevOp == CardOp.Repeat)
                    {
                        res.failureReason = $"REPEAT ON CARD ROW {rowIndex + 1} HAS NO PRECEDING INSTRUCTION";
                        Finish(res, ca, cb, a, b, dual);
                        return res;
                    }
                    int times = Mathf.Max(0, row.repeatCount);
                    for (int t = 0; t < times; t++)
                    {
                        if (++opCount > maxOps)
                        {
                            res.failureReason = "PROGRAM RAN TOO LONG (check REPEAT counts)";
                            Finish(res, ca, cb, a, b, dual);
                            return res;
                        }
                        if (!ExecOp(prevOp, row.channel, rowIndex, out string rf))
                        {
                            res.failureReason = rf;
                            Finish(res, ca, cb, a, b, dual);
                            return res;
                        }
                    }
                    continue;
                }

                if (++opCount > maxOps)
                {
                    res.failureReason = "PROGRAM RAN TOO LONG (check REPEAT counts)";
                    Finish(res, ca, cb, a, b, dual);
                    return res;
                }

                if (!ExecOp(row.op, row.channel, rowIndex, out string f))
                {
                    res.failureReason = f;
                    Finish(res, ca, cb, a, b, dual);
                    return res;
                }

                prevOpByChannel[(int)row.channel] = row.op;
            }

            Finish(res, ca, cb, a, b, dual);
            if (!res.success && string.IsNullOrEmpty(res.failureReason))
            {
                res.failureReason = !res.machineAReached
                    ? "PROGRAM ENDED - MACHINE A DID NOT REACH ITS TARGET"
                    : "PROGRAM ENDED - MACHINE B DID NOT REACH ITS TARGET";
            }
            return res;
        }

        private static void Finish(ExecutionResult res, Carrier ca, Carrier cb, MachineConfig a, MachineConfig b, bool dual)
        {
            res.finalA = ca.pos; res.finalFacingA = ca.fac;
            res.finalB = cb.pos; res.finalFacingB = cb.fac;
            res.machineAReached = ca.pos == a.target;
            res.machineBReached = !dual || cb.pos == b.target;
            res.success = res.machineAReached && res.machineBReached && string.IsNullOrEmpty(res.failureReason);
        }
    }

    /// <summary>Applies editing tools to a working copy of a card, tracking edit count
    /// and returning a human-readable reason when an edit is rejected.</summary>
    public class CardEditSession
    {
        private readonly PunchCard _initial;
        public PunchCard Card { get; private set; }
        public int EditCount { get; private set; }

        public CardEditSession(PunchCard initial)
        {
            _initial = initial.Clone();
            Card = initial.Clone();
        }

        public void Reset()
        {
            Card = _initial.Clone();
            EditCount = 0;
        }

        private bool RowOk(int row, out string err)
        {
            err = null;
            if (row < 0 || row >= Card.rows.Count) { err = "THAT CARD ROW DOES NOT EXIST"; return false; }
            if (Card.rows[row].locked) { err = $"CARD ROW {row + 1} IS FACTORY-LOCKED"; return false; }
            return true;
        }

        public bool Punch(int row, CardOp op, int repeatCount, out string err)
        {
            if (!RowOk(row, out err)) return false;
            if (op == CardOp.Empty) { err = "PUNCH NEEDS AN INSTRUCTION"; return false; }
            if (!Card.rows[row].IsEmpty) { err = $"CARD ROW {row + 1} IS ALREADY PUNCHED - USE PATCH"; return false; }
            Card.rows[row].op = op;
            Card.rows[row].repeatCount = Mathf.Max(1, repeatCount);
            EditCount++;
            return true;
        }

        public bool Patch(int row, CardOp op, int repeatCount, out string err)
        {
            if (!RowOk(row, out err)) return false;
            if (Card.rows[row].IsEmpty) { err = $"CARD ROW {row + 1} IS EMPTY - USE PUNCH"; return false; }
            Card.rows[row].op = op;
            Card.rows[row].repeatCount = op == CardOp.Repeat ? Mathf.Max(1, repeatCount) : 1;
            EditCount++;
            return true;
        }

        /// <summary>Slides a row up or down by one, exchanging it with its neighbour
        /// (the neighbour may be empty). Adjacent-only - Swap covers arbitrary rows.</summary>
        public bool Shift(int fromRow, int toRow, out string err)
        {
            if (!RowOk(fromRow, out err)) return false;
            if (!RowOk(toRow, out err)) return false;
            if (Mathf.Abs(fromRow - toRow) != 1) { err = "SHIFT ONLY MOVES AN INSTRUCTION ONE ROW"; return false; }
            if (Card.rows[fromRow].IsEmpty) { err = $"CARD ROW {fromRow + 1} IS EMPTY - NOTHING TO SHIFT"; return false; }
            (Card.rows[toRow], Card.rows[fromRow]) = (Card.rows[fromRow], Card.rows[toRow]);
            EditCount++;
            return true;
        }

        public bool Swap(int rowA, int rowB, out string err)
        {
            if (!RowOk(rowA, out err)) return false;
            if (!RowOk(rowB, out err)) return false;
            if (rowA == rowB) { err = "SWAP NEEDS TWO DIFFERENT ROWS"; return false; }
            (Card.rows[rowB], Card.rows[rowA]) = (Card.rows[rowA], Card.rows[rowB]);
            EditCount++;
            return true;
        }

        public bool Flip(out string err)
        {
            err = null;
            Card.flipped = !Card.flipped;
            EditCount++;
            return true;
        }

        public bool Copy(int fromRow, int toRow, out string err)
        {
            if (!RowOk(toRow, out err)) return false;
            if (fromRow < 0 || fromRow >= Card.rows.Count) { err = "SOURCE ROW DOES NOT EXIST"; return false; }
            if (Card.rows[fromRow].IsEmpty) { err = $"CARD ROW {fromRow + 1} IS EMPTY - NOTHING TO COPY"; return false; }
            if (!Card.rows[toRow].IsEmpty) { err = $"CARD ROW {toRow + 1} IS OCCUPIED"; return false; }
            var src = Card.rows[fromRow];
            Card.rows[toRow].op = src.op;
            Card.rows[toRow].repeatCount = src.repeatCount;
            Card.rows[toRow].channel = src.channel;
            EditCount++;
            return true;
        }
    }

    public static class RatingRules
    {
        public static LevelRating Evaluate(bool solved, int editCount, int compactTarget, int perfectTarget)
        {
            if (!solved) return LevelRating.None;
            if (editCount <= perfectTarget) return LevelRating.PerfectProgram;
            if (editCount <= compactTarget) return LevelRating.CompactCard;
            return LevelRating.Accepted;
        }

        public static string Label(LevelRating r)
        {
            switch (r)
            {
                case LevelRating.Accepted: return "ACCEPTED";
                case LevelRating.CompactCard: return "COMPACT CARD";
                case LevelRating.PerfectProgram: return "PERFECT PROGRAM";
                default: return "-";
            }
        }
    }
}
