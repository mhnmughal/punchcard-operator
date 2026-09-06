using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using PunchcardOperator;

namespace PunchcardOperator.Tests
{
    /// <summary>Per-stage hands-on-equivalent QA: for every campaign stage this drives
    /// the Game scene through literal UI button clicks — tool buttons, card-row buttons,
    /// op-picker buttons, the Load/Execute lever. It checks a failing run halts with a
    /// reason + RETRY, that an illegal edit is rejected with a toast and costs nothing,
    /// then solves the stage by clicking and confirms the success + PERFECT rating.</summary>
    public class PerStageInputTests
    {
        private const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;

        // one stage's solution as a list of literal UI actions: {"tool",name} / {"row",i} / {"op",label}
        private static readonly Dictionary<string, object[][]> Solve = new()
        {
            ["01"] = new[] { T("Punch"), R(2), O("LEFT") },
            ["02"] = new[] { T("Punch"), R(1), O("MOVE"), T("Punch"), R(5), O("MOVE") },
            ["03"] = new[] { T("Patch"), R(3), O("LEFT") },
            ["04"] = new[] { T("Patch"), R(5), O("MOVE"), T("Punch"), R(7), O("LEFT") },
            ["05"] = new[] { T("Shift"), R(1), R(2) },
            ["06"] = new[] { T("Swap"), R(1), R(3) },
            ["07"] = new[] { T("Flip"), R(0) },
            ["08"] = new[] { T("Shift"), R(2), R(3), T("Patch"), R(5), O("MOVE") },
            ["09"] = new[] { T("Copy"), R(0), R(1), T("Copy"), R(0), R(2), T("Copy"), R(0), R(3) },
            ["10"] = new[] { T("Patch"), R(1), O("REPEAT ×4") },
            ["11"] = new[] { T("Punch"), R(4), O("LEFT") },
            ["12"] = new[] { T("Patch"), R(1), O("REPEAT ×5"), T("Swap"), R(2), R(3) },
        };

        private static object[] T(string tool) => new object[] { "tool", tool };
        private static object[] R(int row) => new object[] { "row", row };
        private static object[] O(string op) => new object[] { "op", op };

        private static readonly int[] ExpectedEdits = { -1, 1, 2, 1, 2, 1, 1, 1, 2, 3, 1, 1, 2 }; // index by stage number

        private GameObject _gc;
        private object _c;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.ResetProgress();
            SaveSystem.Data.tutorialComplete = true;
            SaveSystem.Data.highestUnlockedIndex = 12;
            yield return null;
        }

        [TearDown]
        public void TearDown() => SaveSystem.ResetProgress();

        [UnityTest]
        public IEnumerator EveryCampaignStage_PlayableByClicking(
            [Values("01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12")] string id)
        {
            AppState.PendingLevelId = id;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null; yield return null; yield return null;

            var comp = Object.FindFirstObjectByType<GameController>();
            Assert.IsNotNull(comp, $"[{id}] no GameController");
            _c = comp;

            var toolRow = (RectTransform)F("_toolRow");
            var cardList = (RectTransform)F("_cardList");
            Assert.Greater(cardList.childCount, 0, $"[{id}] card not built");
            Assert.Greater(toolRow.childCount, 0, $"[{id}] tool dock not built");

            // --- 1. the unsolved card must fail with a reason + RETRY ---
            ClickLever(); ClickLever();
            yield return WaitForResult();
            Assert.AreEqual("Result", Phase(), $"[{id}] first run never resolved");
            var overlay = F("_overlay") as RectTransform;
            Assert.IsNotNull(overlay, $"[{id}] no result overlay on failure");
            Assert.IsTrue(ScreenText().Contains("PROGRAM HALTED"), $"[{id}] initial card should fail: {ScreenText()}");
            var retry = FindButton(overlay, "RETRY");
            Assert.IsNotNull(retry, $"[{id}] no RETRY button");
            retry.onClick.Invoke();
            yield return null; yield return null;
            Assert.AreEqual("Editing", Phase(), $"[{id}] RETRY did not return to editing");
            Assert.AreEqual(0, EditCount(), $"[{id}] edit count not reset by RETRY");

            // --- 2. an illegal edit is rejected with a toast, costs nothing ---
            bool hasPunch = HasTool("Punch");
            ClickTool(hasPunch ? "Punch" : "Patch");
            int badRow = hasPunch ? FirstOccupiedRow() : FirstEmptyUnlockedRow();
            ClickRow(badRow);
            var picker = F("_opPicker") as RectTransform;
            if (picker != null) FindButton(picker, "MOVE").onClick.Invoke();
            yield return null;
            Assert.AreEqual(0, EditCount(), $"[{id}] illegal edit changed the card");
            var toast = ((TMP_Text)F("_toast")).text;
            Assert.IsTrue(toast.Length > 0 && (toast.Contains("LOCKED") || toast.Contains("ALREADY") || toast.Contains("USE ")),
                $"[{id}] no rejection toast for the illegal edit (got '{toast}')");

            // --- 3. solve it by clicking ---
            foreach (var step in Solve[id])
            {
                switch ((string)step[0])
                {
                    case "tool": ClickTool((string)step[1]); break;
                    case "row": ClickRow((int)step[1]); break;
                    case "op":
                        var p = F("_opPicker") as RectTransform;
                        Assert.IsNotNull(p, $"[{id}] op picker not open for '{step[1]}'");
                        var b = FindButton(p, (string)step[1]);
                        Assert.IsNotNull(b, $"[{id}] no op button '{step[1]}'");
                        b.onClick.Invoke();
                        break;
                }
                yield return null;
            }
            Assert.AreEqual(ExpectedEdits[int.Parse(id)], EditCount(), $"[{id}] unexpected edit count after solving");

            // --- 4. run it, expect the win ---
            ClickLever(); ClickLever();
            yield return WaitForResult();
            if (id == "12")
            {
                float t = 0;
                while (t < 12f && !ScreenText().Contains("SYSTEM CERTIFIED")) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(ScreenText().Contains("SYSTEM CERTIFIED"), "[12] finale did not show");
            }
            else
            {
                Assert.IsTrue(ScreenText().Contains("ACCEPTED"), $"[{id}] no success: {ScreenText()}");
            }
            Assert.AreEqual(LevelRating.PerfectProgram, ProgressionManager.BestRating(id), $"[{id}] not scored PERFECT");
        }

        // ---- helpers ----

        private object F(string n) => _c.GetType().GetField(n, BF).GetValue(_c);
        private string Phase() => _c.GetType().GetField("_phase", BF).GetValue(_c).ToString();
        private int EditCount() => ((CardEditSession)F("_session")).EditCount;

        private IEnumerator WaitForResult()
        {
            float t = 0;
            while (t < 14f && Phase() != "Result") { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private bool HasTool(string tool) => ((RectTransform)F("_toolRow")).Find($"Tool_{tool}") != null;

        private void ClickTool(string tool)
        {
            var b = ((RectTransform)F("_toolRow")).Find($"Tool_{tool}")?.GetComponent<Button>();
            Assert.IsNotNull(b, $"tool button '{tool}' not found");
            b.onClick.Invoke();
        }

        private void ClickRow(int i)
        {
            var b = ((RectTransform)F("_cardList")).Find($"Row{i}")?.GetComponent<Button>();
            Assert.IsNotNull(b, $"card row {i} not found");
            b.onClick.Invoke();
        }

        private void ClickLever() => ((Button)F("_leverBtn")).onClick.Invoke();

        private int FirstOccupiedRow()
        {
            var card = ((CardEditSession)F("_session")).Card;
            for (int i = 0; i < card.rows.Count; i++)
                if (!card.rows[i].IsEmpty) return i;
            return 0;
        }

        private int FirstEmptyUnlockedRow()
        {
            var card = ((CardEditSession)F("_session")).Card;
            for (int i = 0; i < card.rows.Count; i++)
                if (card.rows[i].IsEmpty && !card.rows[i].locked) return i;
            return 0;
        }

        private static Button FindButton(Transform root, string label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                if (t != null && t.text == label) return b;
            }
            return null;
        }

        private string ScreenText()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) sb.Append(t.text).Append(" | ");
            return sb.ToString();
        }
    }
}
