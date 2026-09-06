using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PunchcardOperator;

namespace PunchcardOperator.Tests
{
    /// <summary>Drives every stage through the real Game scene + GameController flow:
    /// load the scene, apply the intended edit sequence through the same CardEditSession
    /// the Tool Dock buttons use, pull the Load and Execute lever, and assert the
    /// result overlay, rating, save and unlock. This is the automated backbone of the
    /// 13/13 manual QA pass — it exercises the player-facing flow, not the simulator.</summary>
    [TestFixture]
    public class CampaignPlaythroughTests
    {
        private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;

        // edit script per level: (tool, a, b, op, repeat)
        private static readonly Dictionary<string, System.Action<CardEditSession>> Scripts = new()
        {
            ["00"] = s => Assert.IsTrue(s.Punch(1, CardOp.Move, 1, out var e), e),
            ["01"] = s => Assert.IsTrue(s.Punch(2, CardOp.TurnLeft, 1, out var e), e),
            ["02"] = s => { Assert.IsTrue(s.Punch(1, CardOp.Move, 1, out var e1), e1); Assert.IsTrue(s.Punch(5, CardOp.Move, 1, out var e2), e2); },
            ["03"] = s => Assert.IsTrue(s.Patch(3, CardOp.TurnLeft, 1, out var e), e),
            ["04"] = s => { Assert.IsTrue(s.Patch(5, CardOp.Move, 1, out var e1), e1); Assert.IsTrue(s.Punch(7, CardOp.TurnLeft, 1, out var e2), e2); },
            ["05"] = s => Assert.IsTrue(s.Shift(1, 2, out var e), e),
            ["06"] = s => Assert.IsTrue(s.Swap(1, 3, out var e), e),
            ["07"] = s => Assert.IsTrue(s.Flip(out var e), e),
            ["08"] = s => { Assert.IsTrue(s.Shift(2, 3, out var e1), e1); Assert.IsTrue(s.Patch(5, CardOp.Move, 1, out var e2), e2); },
            ["09"] = s => { Assert.IsTrue(s.Copy(0, 1, out var e1), e1); Assert.IsTrue(s.Copy(0, 2, out var e2), e2); Assert.IsTrue(s.Copy(0, 3, out var e3), e3); },
            ["10"] = s => Assert.IsTrue(s.Patch(1, CardOp.Repeat, 4, out var e), e),
            ["11"] = s => Assert.IsTrue(s.Punch(4, CardOp.TurnLeft, 1, out var e), e),
            ["12"] = s => { Assert.IsTrue(s.Patch(1, CardOp.Repeat, 5, out var e1), e1); Assert.IsTrue(s.Swap(2, 3, out var e2), e2); },
        };

        private static readonly string[] Order = { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.ResetProgress();
            yield return null;
        }

        [TearDown]
        public void TearDown() => SaveSystem.ResetProgress();

        [UnityTest]
        public IEnumerator EveryStage_SolvesThroughTheGameScene()
        {
            var db = Resources.Load<LevelDatabase>("LevelDatabase");
            Assert.IsNotNull(db, "LevelDatabase missing from Resources");

            foreach (var id in Order)
            {
                var def = db.ById(id);
                Assert.IsNotNull(def, $"level {id} missing");

                AppState.PendingLevelId = id;
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null; yield return null; // Start + DeferredBoard

                var gc = Object.FindFirstObjectByType<GameController>();
                Assert.IsNotNull(gc, $"[{id}] no GameController in scene");

                var session = (CardEditSession)gc.GetType().GetField("_session", BF).GetValue(gc);
                Assert.IsNotNull(session, $"[{id}] no edit session");

                // sanity: UI actually built
                var cardList = gc.GetType().GetField("_cardList", BF).GetValue(gc) as RectTransform;
                var toolRow = gc.GetType().GetField("_toolRow", BF).GetValue(gc) as RectTransform;
                Assert.Greater(cardList.childCount, 0, $"[{id}] card rows not rendered");
                if (!def.isTutorial)
                    Assert.AreEqual(def.availableTools.Count, toolRow.childCount, $"[{id}] tool dock count mismatch");

                // apply the intended solution through the real edit API
                Scripts[id](session);

                // tutorial needs its gate flag; set it the way OnRowTapped/picker would
                if (def.isTutorial)
                    gc.GetType().GetField("_tutorialPunchDone", BF).SetValue(gc, true);

                var onLever = gc.GetType().GetMethod("OnLever", BF);
                onLever.Invoke(gc, null);   // LOAD
                onLever.Invoke(gc, null);   // EXECUTE

                // wait out the execute coroutine
                float t = 0;
                object phase = null;
                while (t < 12f)
                {
                    phase = gc.GetType().GetField("_phase", BF).GetValue(gc);
                    if (phase.ToString() == "Result") break;
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.AreEqual("Result", phase.ToString(), $"[{id}] execution never reached Result");

                if (id == "12")
                {
                    float ct = 0;
                    while (ct < 12f)
                    {
                        bool seen = false;
                        foreach (var lbl in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                            if (lbl.text.Contains("SYSTEM CERTIFIED")) seen = true;
                        if (seen) break;
                        ct += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                // gather all on-screen text (result panel for 00-11, certified finale for 12)
                string text = "";
                foreach (var lbl in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                    text += lbl.text + " | ";
                bool win = id == "12"
                    ? text.Contains("SYSTEM CERTIFIED")
                    : (text.Contains("ACCEPTED") || text.Contains("TRAINING COMPLETE"));
                Assert.IsTrue(win, $"[{id}] success not shown on screen: {text}");

                if (def.isTutorial)
                {
                    Assert.IsTrue(SaveSystem.Data.tutorialComplete, "[00] tutorial completion not saved");
                }
                else
                {
                    int editCount = session.EditCount;
                    var expected = RatingRules.Evaluate(true, editCount, def.compactEditTarget, def.perfectEditTarget);
                    Assert.AreEqual(LevelRating.PerfectProgram, expected,
                        $"[{id}] intended solution ({editCount} edits) is not PERFECT (compact {def.compactEditTarget}, perfect {def.perfectEditTarget})");
                    Assert.AreEqual(LevelRating.PerfectProgram, ProgressionManager.BestRating(id),
                        $"[{id}] rating not persisted");
                    Assert.GreaterOrEqual(SaveSystem.Data.highestUnlockedIndex, def.orderIndex + 1,
                        $"[{id}] next level not unlocked");
                }
            }

            Assert.IsTrue(SaveSystem.Data.systemCertified, "System Certified not set after Level 12");
        }
    }
}
