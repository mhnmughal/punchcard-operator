using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using PunchcardOperator;

namespace PunchcardOperator.Tests
{
    /// <summary>Loads Menu + Game at a spread of mobile landscape resolutions and
    /// asserts layout invariants: nothing critical renders off-screen, tap targets
    /// stay large enough, the gameplay bands (Status Rail / board / Tool Dock) don't
    /// overlap, and the console stays clean.</summary>
    public class ResponsiveLayoutTests
    {
        private static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(2400, 1080), // 20:9 ultra-tall phone, landscape
            new Vector2Int(2340, 1080), // 19.5:9
            new Vector2Int(1920, 1080), // 16:9
            new Vector2Int(2048, 1536), // 4:3 tablet
            new Vector2Int(1334, 750),  // small legacy phone
        };

        private readonly List<string> _errors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _errors.Clear();
            Application.logMessageReceived += OnLog;
            SaveSystem.ResetProgress();
            Fade.Enabled = false;
            Time.timeScale = 1f;
            SaveSystem.Data.tutorialComplete = true;
            SaveSystem.Data.highestUnlockedIndex = 12;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            SetRes(1920, 1080);
            SaveSystem.ResetProgress();
            Fade.Enabled = false;
            Time.timeScale = 1f;
        }

        private static void SetRes(int w, int h)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)w, (uint)h, "test");
#else
            Screen.SetResolution(w, h, false);
#endif
        }

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add(msg);
        }

        [UnityTest]
        public IEnumerator Menu_LaysOutAtAllResolutions()
        {
            yield return CheckScene("Menu", null);
        }

        [UnityTest]
        public IEnumerator Game_LaysOutAtAllResolutions()
        {
            // 04 has the tallest card (10 rows); 11 is dual channel with a wide board
            yield return CheckScene("Game", "04");
            yield return CheckScene("Game", "11");
        }

        private IEnumerator CheckScene(string scene, string levelId)
        {
            if (levelId != null) AppState.PendingLevelId = levelId;

            foreach (var res in Resolutions)
            {
                SetRes(res.x, res.y);
                yield return SceneManager.LoadSceneAsync(scene);
                for (int i = 0; i < 4; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;

                string tag = $"{scene}{(levelId != null ? "/" + levelId : "")} @ {res.x}x{res.y}";
                var sb = new StringBuilder();

                float w = Screen.width, h = Screen.height;
                var screenRect = new Rect(0, 0, w, h);

                foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
                {
                    if (!g.isActiveAndEnabled) continue;
                    var rt = g.rectTransform;
                    var corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    var r = Rect.MinMaxRect(
                        Mathf.Min(corners[0].x, corners[2].x), Mathf.Min(corners[0].y, corners[2].y),
                        Mathf.Max(corners[0].x, corners[2].x), Mathf.Max(corners[0].y, corners[2].y));

                    // fully off screen?
                    if (r.xMax < 1f || r.xMin > w - 1f || r.yMax < 1f || r.yMin > h - 1f)
                    {
                        // small transient labels (Toast/coach) start empty & off — ignore empty text
                        var t = g as TMP_Text;
                        if (t != null && string.IsNullOrWhiteSpace(t.text)) continue;
                        sb.AppendLine($"  OFF-SCREEN: {Path(g.transform)}  rect={r}");
                        continue;
                    }
                    // significant clipping past an edge (>10px) for a text/button
                    float bleed = Mathf.Max(-r.xMin, -r.yMin, r.xMax - w, r.yMax - h);
                    if (bleed > 12f && (g is TMP_Text || g.GetComponent<Button>() != null))
                        sb.AppendLine($"  CLIPPED {bleed:0}px: {Path(g.transform)}");
                }

                foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                {
                    if (!b.isActiveAndEnabled || !b.interactable) continue;
                    var rt = (RectTransform)b.transform;
                    var size = rt.rect.size * LossyScale(rt);
                    if (Mathf.Min(size.x, size.y) < 30f)
                        sb.AppendLine($"  TAP TARGET {size.x:0}x{size.y:0}: {Path(b.transform)}");
                }

                if (scene == "Game")
                    CheckGameBands(sb);

                if (_errors.Count > 0)
                    sb.AppendLine("  CONSOLE ERROR: " + _errors[0]);

                Assert.IsEmpty(sb.ToString(), $"[{tag}]\n{sb}");
            }
        }

        private void CheckGameBands(StringBuilder sb)
        {
            var rail = GameObject.Find("Canvas/Root/StatusRail");
            var board = GameObject.Find("Canvas/Root/BoardArea");
            var dock = GameObject.Find("Canvas/Root/ToolDock");
            var card = GameObject.Find("Canvas/Root/CardPanel");
            if (rail == null || board == null || dock == null || card == null)
            {
                sb.AppendLine("  MISSING BAND (rail/board/dock/card)");
                return;
            }
            Rect R(GameObject go) { var c = new Vector3[4]; ((RectTransform)go.transform).GetWorldCorners(c);
                return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            var rr = R(rail); var br = R(board); var dr = R(dock); var cr = R(card);
            // board must sit strictly between the rail (top) and dock (bottom)
            if (br.yMax > rr.yMin + 2f) sb.AppendLine("  board overlaps Status Rail");
            if (br.yMin < dr.yMax - 2f) sb.AppendLine("  board overlaps Tool Dock");
            if (cr.yMax > rr.yMin + 2f) sb.AppendLine("  card panel overlaps Status Rail");
            // board and card panel side by side, not overlapping horizontally
            if (br.xMax > cr.xMin + 2f) sb.AppendLine("  board overlaps card panel");
        }

        private static Vector2 LossyScale(RectTransform rt) =>
            new Vector2(rt.lossyScale.x, rt.lossyScale.y);

        private static string Path(Transform t)
        {
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
