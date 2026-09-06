using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PunchcardOperator.EditorTools
{
    /// <summary>Regenerates all 13 level definition assets + the LevelDatabase from code,
    /// so level design lives in one reviewable place and every solution is simulator-checked
    /// the moment it is built. Menu: Punchcard Operator/Rebuild All Levels.</summary>
    public static class PunchcardLevelBuilder
    {
        private const string LevelDir = "Assets/PunchcardOperator/Levels";
        private const string DbPath = "Assets/PunchcardOperator/Resources/LevelDatabase.asset";

        [MenuItem("Punchcard Operator/Rebuild All Levels")]
        public static LevelDatabase RebuildAll()
        {
            Directory.CreateDirectory(LevelDir);
            Directory.CreateDirectory("Assets/PunchcardOperator/Resources");

            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<LevelDatabase>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.levels.Clear();

            foreach (var def in BuildDefs())
            {
                string path = $"{LevelDir}/Level_{def.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<PunchCardLevelDefinition>(path);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(def, existing);
                    Object.DestroyImmediate(def);
                    db.levels.Add(existing);
                }
                else
                {
                    AssetDatabase.CreateAsset(def, path);
                    db.levels.Add(def);
                }
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var report = LevelValidator.Validate(db);
            Debug.Log(LevelValidator.FormatReport(report));
            if (!report.AllValid)
                Debug.LogError($"[Punchcard] Level rebuild produced INVALID data: {report.Summary}");
            else
                Debug.Log($"[Punchcard] Level rebuild OK - {report.Summary}");

            return db;
        }

        // ----- construction helpers -------------------------------------------------

        private static CardRow Row(CardOp op, bool locked = true, Channel ch = Channel.A, int rep = 1)
            => new CardRow { op = op, locked = locked, channel = ch, repeatCount = rep };

        private static PunchCard Card(params CardRow[] rows)
        {
            var c = new PunchCard();
            c.rows.AddRange(rows);
            return c;
        }

        private static PunchCard Flipped(PunchCard c) { c.flipped = true; return c; }

        private static PunchCardLevelDefinition Def(
            string id, string name, ProgramBank bank, int order, bool tutorial,
            GridDef grid, MachineConfig a, MachineConfig b,
            PunchCard initial, PunchCard solution, int solEdits, int compact, int perfect,
            IEnumerable<CardTool> tools, string objective, IEnumerable<string> steps = null)
        {
            var d = ScriptableObject.CreateInstance<PunchCardLevelDefinition>();
            d.id = id; d.displayName = name; d.bank = bank; d.orderIndex = order; d.isTutorial = tutorial;
            d.grid = grid; d.machineA = a; d.machineB = b ?? new MachineConfig { enabled = false };
            d.initialCard = initial; d.solutionCard = solution;
            d.solutionEditCount = solEdits; d.compactEditTarget = compact; d.perfectEditTarget = perfect;
            d.availableTools = new List<CardTool>(tools);
            d.machineObjective = objective;
            d.tutorialSteps = steps != null ? new List<string>(steps) : new List<string>();
            return d;
        }

        private static GridDef G(int w, int h, params Vector2Int[] walls)
            => new GridDef { width = w, height = h, walls = new List<Vector2Int>(walls) };

        private static MachineConfig M(int sx, int sy, Facing f, int tx, int ty, bool enabled = true)
            => new MachineConfig { enabled = enabled, start = new Vector2Int(sx, sy), startFacing = f, target = new Vector2Int(tx, ty) };

        private static readonly CardTool[] PunchOnly = { CardTool.Punch };
        private static readonly CardTool[] PunchPatch = { CardTool.Punch, CardTool.Patch };

        // ----- the campaign -------------------------------------------------------

        private static List<PunchCardLevelDefinition> BuildDefs()
        {
            var list = new List<PunchCardLevelDefinition>();
            CardOp MOVE = CardOp.Move, L = CardOp.TurnLeft, RGT = CardOp.TurnRight, REP = CardOp.Repeat, E = CardOp.Empty;

            // 00 - OPERATOR TRAINING -------------------------------------------------
            list.Add(Def("00", "OPERATOR TRAINING", ProgramBank.None, 0, true,
                G(3, 3),
                M(1, 0, Facing.North, 1, 2), null,
                Card(Row(MOVE), Row(E, locked: false), Row(E, locked: false)),
                Card(Row(MOVE), Row(MOVE), Row(E)),
                1, 1, 1, PunchPatch,
                "Get the carrier onto the lit tray, two steps up.",
                new[]
                {
                    "This card is missing a step. Tap the PUNCH tool to cut a new one.",
                    "Tap the blank row 2 on the card, then pick MOVE.",
                    "The card is ready. Tap LOAD CARD to feed it into the reader.",
                    "Now pull EXECUTE to run the program.",
                    "The carrier steps up onto the lit tray. That is the whole game.",
                }));

            // BANK A - BASIC CONTROL ----------------------------------------------
            // 01 - FIRST PROGRAM : one missing turn on an L-shaped route
            list.Add(Def("01", "FIRST PROGRAM", ProgramBank.A, 0, false,
                G(4, 4),
                M(0, 0, Facing.East, 2, 2), null,
                Card(Row(MOVE), Row(MOVE), Row(E, locked: false), Row(MOVE), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(L), Row(MOVE), Row(MOVE)),
                1, 2, 1, PunchOnly,
                "Two moves east, turn, two moves north. One instruction is missing."));

            // 02 - TURN TABLE : two missing moves around a single turn
            list.Add(Def("02", "TURN TABLE", ProgramBank.A, 1, false,
                G(5, 5),
                M(0, 0, Facing.North, 3, 3), null,
                Card(Row(MOVE), Row(E, locked: false), Row(MOVE), Row(RGT), Row(MOVE), Row(E, locked: false), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(MOVE), Row(RGT), Row(MOVE), Row(MOVE), Row(MOVE)),
                2, 3, 2, PunchOnly,
                "Climb three, turn the table right, run three. Fill both gaps."));

            // 03 - BAD HOLE : one wrongly punched turn, repair with PATCH
            list.Add(Def("03", "BAD HOLE", ProgramBank.A, 2, false,
                G(5, 5),
                M(0, 0, Facing.East, 3, 2), null,
                Card(Row(MOVE), Row(MOVE), Row(MOVE), Row(RGT, locked: false), Row(MOVE), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(MOVE), Row(L), Row(MOVE), Row(MOVE)),
                1, 2, 1, PunchPatch,
                "Row 4 was punched the wrong way and the carrier drives off the board. Patch it."));

            // 04 - CONTROL CHECK : one wrong op + one missing op
            list.Add(Def("04", "CONTROL CHECK", ProgramBank.A, 3, false,
                G(6, 6),
                M(0, 0, Facing.North, 4, 4), null,
                Card(Row(MOVE), Row(MOVE), Row(RGT), Row(MOVE), Row(MOVE),
                     Row(L, locked: false), Row(MOVE), Row(E, locked: false), Row(MOVE), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(RGT), Row(MOVE), Row(MOVE),
                     Row(MOVE), Row(MOVE), Row(L), Row(MOVE), Row(MOVE)),
                2, 3, 2, PunchPatch,
                "One row turns when it should move; one row is blank. Fix both to reach the corner tray."));

            // BANK B - CARD MANIPULATION ----------------------------------------
            // 05 - SHIFT LINE : right instruction, one row too early
            list.Add(Def("05", "SHIFT LINE", ProgramBank.B, 4, false,
                G(5, 5),
                M(0, 0, Facing.East, 2, 2), null,
                Card(Row(MOVE), Row(L, locked: false), Row(MOVE, locked: false), Row(MOVE), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(L), Row(MOVE), Row(MOVE)),
                1, 2, 1, new[] { CardTool.Shift, CardTool.Punch, CardTool.Patch },
                "The turn is punched one row too soon. SHIFT slides a row up or down by one."));

            // 06 - CROSS WIRE : two turns in the wrong order (non-adjacent)
            list.Add(Def("06", "CROSS WIRE", ProgramBank.B, 5, false,
                G(5, 5),
                M(2, 0, Facing.North, 1, 2), null,
                Card(Row(MOVE), Row(RGT, locked: false), Row(MOVE), Row(L, locked: false), Row(MOVE)),
                Card(Row(MOVE), Row(L), Row(MOVE), Row(RGT), Row(MOVE)),
                1, 2, 1, new[] { CardTool.Swap, CardTool.Shift, CardTool.Punch, CardTool.Patch },
                "The left and right turns are wired the wrong way round. SWAP exchanges any two rows."));

            // 07 - REVERSE FEED : the whole card is punched back-to-front
            list.Add(Def("07", "REVERSE FEED", ProgramBank.B, 6, false,
                G(5, 5),
                M(3, 0, Facing.North, 1, 1), null,
                Card(Row(MOVE, locked: false), Row(MOVE, locked: false), Row(L, locked: false), Row(MOVE, locked: false)),
                Flipped(Card(Row(MOVE), Row(MOVE), Row(L), Row(MOVE))),
                1, 1, 1, new[] { CardTool.Flip, CardTool.Punch, CardTool.Patch },
                "This card was fed into the punch upside-down. FLIP makes the reader run it bottom-to-top."));

            // 08 - COMPACT PROGRAM : wrong op + misplaced op; shift+patch beats 3 patches
            list.Add(Def("08", "COMPACT PROGRAM", ProgramBank.B, 7, false,
                G(6, 6),
                M(0, 0, Facing.East, 3, 3), null,
                Card(Row(MOVE), Row(MOVE), Row(L, locked: false), Row(MOVE, locked: false), Row(MOVE),
                     Row(RGT, locked: false), Row(MOVE)),
                Card(Row(MOVE), Row(MOVE), Row(MOVE), Row(L), Row(MOVE), Row(MOVE), Row(MOVE)),
                2, 3, 2, new[] { CardTool.Punch, CardTool.Patch, CardTool.Shift, CardTool.Swap },
                "Solvable with three patches - or two edits if you SHIFT the turn instead of re-punching it."));

            // BANK C - MASTER CONTROL -----------------------------------------
            // 09 - COPY CYCLE : fill a tight card by duplicating the one move you have
            list.Add(Def("09", "COPY CYCLE", ProgramBank.C, 8, false,
                G(6, 6),
                M(0, 0, Facing.East, 4, 0), null,
                Card(Row(MOVE), Row(E, locked: false), Row(E, locked: false), Row(E, locked: false), Row(E, locked: false)),
                Card(Row(MOVE), Row(MOVE), Row(MOVE), Row(MOVE), Row(E)),
                3, 4, 3, new[] { CardTool.Copy, CardTool.Patch },
                "Only one MOVE is punched and there is no PUNCH tool. COPY duplicates a row into a blank one."));

            // 10 - REPEAT LOOP : fix the visible repeat count
            list.Add(Def("10", "REPEAT LOOP", ProgramBank.C, 9, false,
                G(6, 6),
                M(0, 0, Facing.East, 5, 0), null,
                Card(Row(MOVE), Row(REP, locked: false, rep: 2), Row(E, locked: false)),
                Card(Row(MOVE), Row(REP, rep: 4), Row(E)),
                1, 2, 1, new[] { CardTool.Patch, CardTool.Punch },
                "REPEAT re-runs the row above it the number of times shown on the dial. Set the dial so the carrier stops on the tray."));

            // 11 - DUAL CHANNEL : one card, two machines
            list.Add(Def("11", "DUAL CHANNEL", ProgramBank.C, 10, false,
                G(5, 5),
                M(0, 0, Facing.East, 2, 0),
                M(4, 4, Facing.West, 2, 2),
                Card(Row(MOVE, ch: Channel.A), Row(MOVE, ch: Channel.A),
                     Row(MOVE, ch: Channel.B), Row(MOVE, ch: Channel.B),
                     Row(E, locked: false, ch: Channel.B), Row(MOVE, ch: Channel.B), Row(MOVE, ch: Channel.B)),
                Card(Row(MOVE, ch: Channel.A), Row(MOVE, ch: Channel.A),
                     Row(MOVE, ch: Channel.B), Row(MOVE, ch: Channel.B),
                     Row(L, ch: Channel.B), Row(MOVE, ch: Channel.B), Row(MOVE, ch: Channel.B)),
                1, 2, 1, PunchPatch,
                "Blue rows drive MACHINE A, amber rows drive MACHINE B. Machine B is missing its turn - both machines must park on their trays."));

            // 12 - MASTER PROGRAM : dual channel + repeat + a swap
            list.Add(Def("12", "MASTER PROGRAM", ProgramBank.C, 11, false,
                G(7, 7),
                M(0, 0, Facing.East, 6, 0),
                M(0, 6, Facing.East, 6, 6),
                Card(Row(MOVE, ch: Channel.A),
                     Row(REP, locked: false, ch: Channel.A, rep: 4),
                     Row(REP, locked: false, ch: Channel.B, rep: 5),
                     Row(MOVE, locked: false, ch: Channel.B)),
                Card(Row(MOVE, ch: Channel.A),
                     Row(REP, ch: Channel.A, rep: 5),
                     Row(MOVE, ch: Channel.B),
                     Row(REP, ch: Channel.B, rep: 5)),
                2, 3, 2, new[] { CardTool.Patch, CardTool.Swap, CardTool.Punch },
                "Both machines run the full length of the floor on REPEAT. Machine A's dial is short by one; Machine B's REPEAT fires before its MOVE. Patch the dial, swap the amber rows."));

            return list;
        }
    }
}
