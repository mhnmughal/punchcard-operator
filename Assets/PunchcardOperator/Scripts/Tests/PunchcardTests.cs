using System.IO;
using NUnit.Framework;
using UnityEngine;
using PunchcardOperator;
using PunchcardOperator.EditorTools;

namespace PunchcardOperator.Tests
{
    public class SimulatorTests
    {
        private static GridDef Grid(int w, int h) => new GridDef { width = w, height = h };
        private static MachineConfig A(int sx, int sy, Facing f, int tx, int ty) =>
            new MachineConfig { enabled = true, start = new Vector2Int(sx, sy), startFacing = f, target = new Vector2Int(tx, ty) };
        private static MachineConfig Off() => new MachineConfig { enabled = false };

        private static PunchCard C(params CardOp[] ops)
        {
            var c = new PunchCard();
            foreach (var o in ops) c.rows.Add(new CardRow { op = o });
            return c;
        }

        [Test]
        public void StraightRun_Succeeds()
        {
            var r = PunchCardSimulator.Run(Grid(4, 4), A(0, 0, Facing.North, 0, 3), Off(),
                C(CardOp.Move, CardOp.Move, CardOp.Move));
            Assert.IsTrue(r.success, r.failureReason);
            Assert.AreEqual(new Vector2Int(0, 3), r.finalA);
        }

        [Test]
        public void Turn_ChangesHeading()
        {
            var r = PunchCardSimulator.Run(Grid(4, 4), A(0, 0, Facing.East, 2, 2), Off(),
                C(CardOp.Move, CardOp.Move, CardOp.TurnLeft, CardOp.Move, CardOp.Move));
            Assert.IsTrue(r.success, r.failureReason);
        }

        [Test]
        public void DriveOffBoard_Fails_WithReason()
        {
            var r = PunchCardSimulator.Run(Grid(3, 3), A(0, 0, Facing.South, 0, 2), Off(), C(CardOp.Move));
            Assert.IsFalse(r.success);
            StringAssert.Contains("OFF THE BOARD", r.failureReason);
        }

        [Test]
        public void Wall_Blocks()
        {
            var g = Grid(3, 3);
            g.walls.Add(new Vector2Int(0, 1));
            var r = PunchCardSimulator.Run(g, A(0, 0, Facing.North, 0, 2), Off(), C(CardOp.Move));
            Assert.IsFalse(r.success);
            StringAssert.Contains("WALL", r.failureReason);
        }

        [Test]
        public void EndingOffTarget_Fails()
        {
            var r = PunchCardSimulator.Run(Grid(4, 4), A(0, 0, Facing.North, 0, 3), Off(), C(CardOp.Move));
            Assert.IsFalse(r.success);
            StringAssert.Contains("DID NOT REACH", r.failureReason);
        }

        [Test]
        public void Repeat_RepeatsPreviousOp()
        {
            var card = new PunchCard();
            card.rows.Add(new CardRow { op = CardOp.Move });
            card.rows.Add(new CardRow { op = CardOp.Repeat, repeatCount = 3 });
            var r = PunchCardSimulator.Run(Grid(6, 2), A(0, 0, Facing.East, 4, 0), Off(), card);
            Assert.IsTrue(r.success, r.failureReason);
        }

        [Test]
        public void Repeat_WithNoPrevious_Fails()
        {
            var card = new PunchCard();
            card.rows.Add(new CardRow { op = CardOp.Repeat, repeatCount = 2 });
            var r = PunchCardSimulator.Run(Grid(4, 4), A(0, 0, Facing.East, 2, 0), Off(), card);
            Assert.IsFalse(r.success);
            StringAssert.Contains("NO PRECEDING", r.failureReason);
        }

        [Test]
        public void Flip_ReversesExecutionOrder()
        {
            var card = C(CardOp.Move, CardOp.Move, CardOp.TurnLeft, CardOp.Move);
            card.flipped = true;
            var r = PunchCardSimulator.Run(Grid(5, 5), A(3, 0, Facing.North, 1, 1), Off(), card);
            Assert.IsTrue(r.success, r.failureReason);
        }

        [Test]
        public void DualChannel_BothMustReach()
        {
            var card = new PunchCard();
            card.rows.Add(new CardRow { op = CardOp.Move, channel = Channel.A });
            card.rows.Add(new CardRow { op = CardOp.Move, channel = Channel.A });
            card.rows.Add(new CardRow { op = CardOp.Move, channel = Channel.B });
            var a = A(0, 0, Facing.East, 2, 0);
            var b = A(4, 4, Facing.West, 2, 4); // needs two moves, card only gives B one
            var r = PunchCardSimulator.Run(Grid(5, 5), a, b, card);
            Assert.IsFalse(r.success);
            Assert.IsTrue(r.machineAReached);
            Assert.IsFalse(r.machineBReached);
        }

        [Test]
        public void DualChannel_Repeat_IsPerChannel()
        {
            var card = new PunchCard();
            card.rows.Add(new CardRow { op = CardOp.Move, channel = Channel.A });
            card.rows.Add(new CardRow { op = CardOp.Move, channel = Channel.B });
            card.rows.Add(new CardRow { op = CardOp.Repeat, repeatCount = 2, channel = Channel.B });
            var a = A(0, 0, Facing.East, 1, 0);
            var b = A(0, 4, Facing.East, 3, 4);
            var r = PunchCardSimulator.Run(Grid(5, 5), a, b, card);
            Assert.IsTrue(r.success, r.failureReason);
        }
    }

    public class CardEditSessionTests
    {
        private static PunchCard Card(params CardOp[] ops)
        {
            var c = new PunchCard();
            foreach (var o in ops) c.rows.Add(new CardRow { op = o });
            return c;
        }

        [Test]
        public void Punch_FillsEmptyRow_CountsOneEdit()
        {
            var s = new CardEditSession(Card(CardOp.Empty, CardOp.Empty));
            Assert.IsTrue(s.Punch(0, CardOp.Move, 1, out _));
            Assert.AreEqual(1, s.EditCount);
            Assert.AreEqual(CardOp.Move, s.Card.rows[0].op);
        }

        [Test]
        public void Punch_OnOccupiedRow_RejectedWithReason()
        {
            var s = new CardEditSession(Card(CardOp.Move));
            Assert.IsFalse(s.Punch(0, CardOp.Move, 1, out string err));
            StringAssert.Contains("ALREADY PUNCHED", err);
            Assert.AreEqual(0, s.EditCount);
        }

        [Test]
        public void Punch_OnLockedRow_Rejected()
        {
            var c = Card(CardOp.Empty);
            c.rows[0].locked = true;
            var s = new CardEditSession(c);
            Assert.IsFalse(s.Punch(0, CardOp.Move, 1, out string err));
            StringAssert.Contains("LOCKED", err);
        }

        [Test]
        public void Shift_SwapsAdjacentRows()
        {
            var s = new CardEditSession(Card(CardOp.Move, CardOp.TurnLeft, CardOp.Move));
            Assert.IsTrue(s.Shift(1, 2, out _));
            Assert.AreEqual(CardOp.Move, s.Card.rows[1].op);
            Assert.AreEqual(CardOp.TurnLeft, s.Card.rows[2].op);
        }

        [Test]
        public void Swap_ExchangesArbitraryRows()
        {
            var s = new CardEditSession(Card(CardOp.TurnRight, CardOp.Move, CardOp.TurnLeft));
            Assert.IsTrue(s.Swap(0, 2, out _));
            Assert.AreEqual(CardOp.TurnLeft, s.Card.rows[0].op);
            Assert.AreEqual(CardOp.TurnRight, s.Card.rows[2].op);
        }

        [Test]
        public void Reset_RestoresInitialAndZeroesCount()
        {
            var s = new CardEditSession(Card(CardOp.Empty));
            s.Punch(0, CardOp.Move, 1, out _);
            s.Reset();
            Assert.AreEqual(0, s.EditCount);
            Assert.AreEqual(CardOp.Empty, s.Card.rows[0].op);
        }
    }

    public class RatingRulesTests
    {
        [Test]
        public void NotSolved_IsNone()
            => Assert.AreEqual(LevelRating.None, RatingRules.Evaluate(false, 0, 3, 2));

        [Test]
        public void WithinPerfect_IsPerfect()
            => Assert.AreEqual(LevelRating.PerfectProgram, RatingRules.Evaluate(true, 2, 3, 2));

        [Test]
        public void WithinCompact_IsCompact()
            => Assert.AreEqual(LevelRating.CompactCard, RatingRules.Evaluate(true, 3, 3, 2));

        [Test]
        public void OverCompact_IsAccepted()
            => Assert.AreEqual(LevelRating.Accepted, RatingRules.Evaluate(true, 9, 3, 2));
    }

    public class SaveSystemTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Application.persistentDataPath, SaveSystem.FileName);
            if (File.Exists(_path)) File.Delete(_path);
            SaveSystem.ResetProgress();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path)) File.Delete(_path);
            SaveSystem.ResetProgress();
        }

        [Test]
        public void FreshLoad_HasSafeDefaults()
        {
            SaveSystem.ResetProgress();
            Assert.IsFalse(SaveSystem.Data.tutorialComplete);
            Assert.AreEqual(0, SaveSystem.Data.highestUnlockedIndex);
        }

        [Test]
        public void CorruptFile_FallsBackToDefaults()
        {
            File.WriteAllText(_path, "{ this is not json ]]");
            var data = SaveSystem.Load();
            Assert.IsNotNull(data);
            Assert.IsNotNull(data.levels);
            Assert.IsFalse(data.tutorialComplete);
        }

        [Test]
        public void RoundTrip_Persists()
        {
            SaveSystem.Data.tutorialComplete = true;
            SaveSystem.Data.highestUnlockedIndex = 3;
            SaveSystem.Save();
            var reloaded = SaveSystem.Load();
            Assert.IsTrue(reloaded.tutorialComplete);
            Assert.AreEqual(3, reloaded.highestUnlockedIndex);
        }

        [Test]
        public void RecordCompletion_NeverDowngradesRating()
        {
            var def = ScriptableObject.CreateInstance<PunchCardLevelDefinition>();
            def.id = "01"; def.isTutorial = false; def.orderIndex = 0;
            ProgressionManager.RecordCompletion(def, LevelRating.PerfectProgram, 2, null);
            ProgressionManager.RecordCompletion(def, LevelRating.Accepted, 9, null);
            Assert.AreEqual(LevelRating.PerfectProgram, ProgressionManager.BestRating("01"));
            Object.DestroyImmediate(def);
        }

        [Test]
        public void TutorialGate_BlocksCampaignUntilComplete()
        {
            var def = ScriptableObject.CreateInstance<PunchCardLevelDefinition>();
            def.id = "01"; def.isTutorial = false; def.orderIndex = 0;
            Assert.IsFalse(ProgressionManager.IsUnlocked(def));
            ProgressionManager.CompleteTutorial();
            Assert.IsTrue(ProgressionManager.IsUnlocked(def));
            Object.DestroyImmediate(def);
        }
    }

    public class CampaignValidationTests
    {
        [Test]
        public void AllThirteenLevels_AreValid()
        {
            var db = PunchcardLevelBuilder.RebuildAll();
            var report = LevelValidator.Validate(db);
            Assert.IsTrue(report.AllValid, LevelValidator.FormatReport(report));
            Assert.AreEqual("13 / 13 VALID", report.Summary);
        }
    }
}
