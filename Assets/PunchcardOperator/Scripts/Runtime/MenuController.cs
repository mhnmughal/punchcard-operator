using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>The menu: Control Room (main), Card Vault (level select),
    /// Machine Setup (settings), Operations Manual (rules + training replay).
    /// One scene, cross-faded views.</summary>
    public class MenuController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _controlRoom, _vault, _settings, _manual;
        private RectTransform _current;
        private GameObject _certBadge;

        private void Start()
        {
            _root = (RectTransform)transform;
            var bd = Theme.Rect("Backdrop", _root);
            Theme.Stretch(bd);
            bd.gameObject.AddComponent<Backdrop>();

            BuildControlRoom();
            BuildVault();
            BuildSettings();
            BuildManual();

            var target = AppState.MenuTarget switch
            {
                "vault" => _vault,
                "manual" => _manual,
                _ => _controlRoom
            };
            AppState.MenuTarget = "control";
            _controlRoom.gameObject.SetActive(false);
            _vault.gameObject.SetActive(false);
            _settings.gameObject.SetActive(false);
            _manual.gameObject.SetActive(false);
            _current = target;
            target.gameObject.SetActive(true);
            if (target == _vault) RefreshVault();
            Fade.In();
        }

        private void Show(RectTransform v)
        {
            if (v == _current) return;
            Sfx.Play("ui");
            StartCoroutine(Swap(_current, v));
            _current = v;
        }

        private IEnumerator Swap(RectTransform from, RectTransform to)
        {
            if (to == _vault) RefreshVault();
            to.gameObject.SetActive(true);
            var cg = to.GetComponent<CanvasGroup>();
            var cgf = from.GetComponent<CanvasGroup>();
            to.localScale = Vector3.one * 0.98f;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.16f);
                cg.alpha = k; cgf.alpha = 1f - k;
                to.localScale = Vector3.one * Mathf.Lerp(0.98f, 1f, k);
                yield return null;
            }
            cg.alpha = 1f; cgf.alpha = 1f; to.localScale = Vector3.one;
            from.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- shared

        private RectTransform View(string name)
        {
            var v = Theme.Rect(name, _root);
            Theme.Stretch(v);
            v.gameObject.AddComponent<CanvasGroup>();
            return v;
        }

        private void Header(Transform parent, string title, System.Action back)
        {
            Theme.Label("h", parent, title, Theme.H1, Theme.Amber, TextAlignmentOptions.Center)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(900, 64), new Vector2(0, -34));
            Theme.TextButton("Back", parent, "BACK", Theme.PanelLight, Theme.Ink, back)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, 58), new Vector2(30, -28));
        }

        // ---------------------------------------------------------------- control room

        private void BuildControlRoom()
        {
            _controlRoom = View("ControlRoom");

            // decorative machine cogs, low-left, mostly behind - purely atmospheric,
            // kept off the centre axis so they never fight the menu for alignment
            var gearHost = Theme.Rect("gearHost", _controlRoom);
            gearHost.Set(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(300, 300), new Vector2(70, 70));
            Theme.Glow("gglow", gearHost, Theme.Amber, 0.10f)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 420), Vector2.zero);
            var gear = new GameObject("gear", typeof(RectTransform)).AddComponent<Image>();
            gear.transform.SetParent(gearHost, false);
            gear.sprite = Theme.GearSprite; gear.raycastTarget = false;
            gear.color = new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.55f);
            ((RectTransform)gear.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210, 210), Vector2.zero);
            gear.gameObject.AddComponent<Spin>().speed = 12f;
            var gear2 = new GameObject("gear2", typeof(RectTransform)).AddComponent<Image>();
            gear2.transform.SetParent(gearHost, false);
            gear2.sprite = Theme.GearSprite; gear2.raycastTarget = false;
            gear2.color = new Color(Theme.AmberDeep.r, Theme.AmberDeep.g, Theme.AmberDeep.b, 0.5f);
            ((RectTransform)gear2.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(128, 128), new Vector2(124, -100));
            gear2.gameObject.AddComponent<Spin>().speed = -18f;

            // title block, upper-centre
            var logo = Theme.Label("Logo", _controlRoom, "PUNCHCARD\nOPERATOR", Theme.Display, Theme.Ink);
            logo.characterSpacing = 6f; logo.fontStyle = FontStyles.Bold; logo.lineSpacing = -14f;
            ((RectTransform)logo.transform).Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(1100, 230), new Vector2(0, -70));
            var accent = Theme.Panelled("accent", _controlRoom, Theme.Amber);
            ((RectTransform)accent.transform).Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(220, 5), new Vector2(0, -300));
            accent.raycastTarget = false;
            Theme.Label("subtitle", _controlRoom, "PROGRAM THE MACHINE", Theme.Small, Theme.InkDim)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(600, 26), new Vector2(0, -322));

            // primary CTA + secondary buttons, centred column
            var col = Theme.Rect("Buttons", _controlRoom);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 320), new Vector2(0, -30));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            string playLabel; string playTarget = PlayTarget(out playLabel);
            var play = Theme.PrimaryButton("Play", col, playLabel, () =>
            {
                if (playTarget == "vault") { Show(_vault); return; }
                Sfx.Play("load", 0.9f);
                AppState.PendingLevelId = playTarget;
                Fade.ToScene("Game");
            });
            AddLE(play.gameObject, 96);

            AddLE(Theme.TextButton("Vault", col, "CARD VAULT", Theme.PanelLight, Theme.Ink, () => Show(_vault)).gameObject, 66);
            AddLE(Theme.TextButton("Manual", col, "HOW TO PLAY", Theme.PanelLight, Theme.Ink, () => Show(_manual)).gameObject, 66);
            AddLE(Theme.TextButton("Settings", col, "SETTINGS", Theme.PanelLight, Theme.Ink, () => Show(_settings)).gameObject, 66);

            // progress strip, bottom
            var camp = AppState.Database != null ? AppState.Database.Campaign : new List<PunchCardLevelDefinition>();
            int solved = 0; foreach (var l in camp) if (ProgressionManager.BestRating(l.id) != LevelRating.None) solved++;
            var prog = Theme.Raised("Progress", _controlRoom, Theme.Panel);
            ((RectTransform)prog.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(640, 60), new Vector2(0, 28));
            Theme.Label("pl", prog.transform, $"PROGRAMS FILED   {solved} / 12", Theme.Body, Theme.InkDim, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(22, 0));
            var barBg = Theme.Inset("bar", prog.transform, Theme.PanelDark);
            ((RectTransform)barBg.transform).Set(new Vector2(0.58f, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-28, 16), new Vector2(-14, 0));
            var barFill = Theme.Panelled("fill", barBg.transform, Theme.Good);
            ((RectTransform)barFill.transform).Set(new Vector2(0, 0), new Vector2(Mathf.Clamp01(solved / 12f), 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            barFill.raycastTarget = false;

            _certBadge = Theme.Raised("CertBadge", _controlRoom, new Color32(0x1B, 0x30, 0x22, 0xFF)).gameObject;
            ((RectTransform)_certBadge.transform).Set(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(300, 58), new Vector2(-28, -28));
            Theme.Label("cb", _certBadge.transform, "SYSTEM CERTIFIED", Theme.Small, Theme.Good)
                .GetComponent<RectTransform>().Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _certBadge.SetActive(SaveSystem.Data.systemCertified);
        }

        private static void AddLE(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = h; le.preferredHeight = h;
        }

        private string PlayTarget(out string label)
        {
            if (!SaveSystem.Data.tutorialComplete) { label = "BEGIN TRAINING"; return "00"; }
            var camp = AppState.Database != null ? AppState.Database.Campaign : new List<PunchCardLevelDefinition>();
            foreach (var l in camp)
                if (ProgressionManager.IsUnlocked(l) && ProgressionManager.BestRating(l.id) == LevelRating.None)
                { label = "CONTINUE  ›  " + l.id; return l.id; }
            label = "REVIEW PROGRAMS";
            return "vault";
        }

        private void Quit()
        {
            Sfx.Play("ui");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- card vault

        private void BuildVault()
        {
            _vault = View("CardVault");
            Header(_vault, "CARD VAULT", () => Show(_controlRoom));

            var body = Theme.Rect("VBody", _vault);
            Theme.Stretch(body, 26); body.offsetMax = new Vector2(-26, -110); body.offsetMin = new Vector2(26, 26);
            var vlg = body.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        }

        private void RefreshVault()
        {
            var body = _vault.Find("VBody");
            foreach (Transform c in body) Destroy(c.gameObject);
            var db = AppState.Database;
            if (db == null)
            {
                Theme.Label("err", body, "NO LEVEL DATABASE - run Punchcard Operator/Rebuild All Levels", Theme.H2, Theme.Bad);
                return;
            }
            Cassette(body, "TRAINING", new[] { db.Tutorial }, 148);
            var camp = db.Campaign;
            Cassette(body, "BANK A  ·  BASIC CONTROL", camp.FindAll(l => l.bank == ProgramBank.A), 0);
            Cassette(body, "BANK B  ·  CARD MANIPULATION", camp.FindAll(l => l.bank == ProgramBank.B), 0);
            Cassette(body, "BANK C  ·  MASTER CONTROL", camp.FindAll(l => l.bank == ProgramBank.C), 0);
        }

        private void Cassette(Transform parent, string code, IList<PunchCardLevelDefinition> levels, float fixedHeight)
        {
            var group = Theme.Raised($"Grp_{code}", parent, Theme.Panel);
            var le = group.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = fixedHeight > 0 ? fixedHeight : 216f;
            le.flexibleHeight = fixedHeight > 0 ? 0f : 1f;
            Theme.Label("code", group.transform, code, Theme.Small, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-32, 22), new Vector2(22, -10));

            var strip = Theme.Rect("strip", group.transform);
            Theme.Stretch(strip); strip.offsetMin = new Vector2(22, 16); strip.offsetMax = new Vector2(-22, -38);
            var hlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14; hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            foreach (var lvl in levels)
            {
                if (lvl == null) continue;
                bool unlocked = ProgressionManager.IsUnlocked(lvl);
                var rating = ProgressionManager.BestRating(lvl.id);

                var bg = !unlocked ? Theme.PanelDark : rating == LevelRating.PerfectProgram ? new Color32(0x24, 0x33, 0x2A, 0xFF) : Theme.PanelLight;
                var fg = unlocked ? Theme.Ink : Theme.InkDim;
                string title = lvl.isTutorial ? "TRAINING" : lvl.displayName;
                var btn = Theme.TextButton($"Lvl_{lvl.id}", strip, "", bg, fg, () =>
                {
                    if (!unlocked) { Sfx.Play("error"); return; }
                    Sfx.Play("load", 0.9f);
                    AppState.PendingLevelId = lvl.id;
                    Fade.ToScene("Game");
                });
                var brt = (RectTransform)btn.transform;
                Destroy(btn.transform.Find(btn.name + "_Label").gameObject);

                if (lvl.isTutorial)
                {
                    // one wide short tile: "00  OPERATOR TRAINING" centred, status below
                    Theme.Label("num", brt, "00", 44f, Theme.Amber, TextAlignmentOptions.Right)
                        .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(90, 56), new Vector2(-16, 12));
                    Theme.Label("nm", brt, "OPERATOR TRAINING", Theme.H2, fg, TextAlignmentOptions.Left)
                        .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(420, 40), new Vector2(0, 12));
                    Theme.Label("st", brt, SaveSystem.Data.tutorialComplete ? "COMPLETE" : "START HERE", Theme.Small, Theme.Good, TextAlignmentOptions.Center)
                        .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(240, 22), new Vector2(0, 18));
                    continue;
                }

                // number + name centred as a block; status pinned just below it
                Theme.Label("num", brt, lvl.id, 46f, unlocked ? Theme.Amber : Theme.InkDim, TextAlignmentOptions.Center)
                    .GetComponent<RectTransform>().Set(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, 54), new Vector2(0, 30));
                Theme.Label("nm", brt, title, Theme.Small, fg, TextAlignmentOptions.Center)
                    .GetComponent<RectTransform>().Set(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, 22), new Vector2(0, -8));

                if (!unlocked)
                {
                    var lockImg = new GameObject("lock", typeof(RectTransform)).AddComponent<Image>();
                    lockImg.transform.SetParent(brt, false);
                    lockImg.sprite = Theme.LockSprite; lockImg.raycastTarget = false; lockImg.color = Theme.InkDim;
                    ((RectTransform)lockImg.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22, 22), new Vector2(0, -46));
                }
                else
                {
                    int pips = rating == LevelRating.PerfectProgram ? 3 : rating == LevelRating.CompactCard ? 2 : rating == LevelRating.Accepted ? 1 : 0;
                    Theme.Pips(brt, pips, 3, 12f, 15f);
                    var pr = (RectTransform)brt.Find("pips");
                    pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                    pr.pivot = new Vector2(0.5f, 0.5f);
                    pr.anchoredPosition = new Vector2(0, -46);
                }
            }
        }

        // ---------------------------------------------------------------- settings

        private void BuildSettings()
        {
            _settings = View("Settings");
            Header(_settings, "SETTINGS", () => Show(_controlRoom));

            var col = Theme.Rect("SCol", _settings);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760, 340), new Vector2(0, 10));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

            Slider(col, "MASTER VOLUME", () => SaveSystem.Data.masterVolume, v => SaveSystem.Data.masterVolume = v);
            Slider(col, "SOUND EFFECTS", () => SaveSystem.Data.sfxVolume, v => SaveSystem.Data.sfxVolume = v);
            AddLE(Theme.TextButton("Reset", col, "RESET ALL PROGRESS", Theme.Bad, Theme.Ink, ConfirmReset).gameObject, 74);
        }

        private void Slider(Transform parent, string label, System.Func<float> get, System.Action<float> set)
        {
            var row = Theme.Raised($"S_{label}", parent, Theme.Panel);
            AddLE(row.gameObject, 74);
            Theme.Label("l", row.transform, label, Theme.Body, Theme.Ink, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(26, 0));

            var track = Theme.Inset("track", row.transform, Theme.PanelDark);
            var trt = (RectTransform)track.transform;
            trt.anchorMin = new Vector2(0.42f, 0.5f); trt.anchorMax = new Vector2(1, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f); trt.sizeDelta = new Vector2(0, 18);
            trt.offsetMin = new Vector2(0, -9); trt.offsetMax = new Vector2(-26, 9);

            var sGo = new GameObject("slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(track.transform, false);
            Theme.Stretch((RectTransform)sGo.transform);

            var fillArea = Theme.Rect("fillArea", sGo.transform);
            Theme.Stretch(fillArea); fillArea.offsetMin = new Vector2(3, 3); fillArea.offsetMax = new Vector2(-9, -3);
            var fill = Theme.Panelled("fill", fillArea, Theme.Amber);
            Theme.Stretch((RectTransform)fill.transform);

            // slide area inset by the handle half-width so the handle never overhangs
            var slideArea = Theme.Rect("slideArea", sGo.transform);
            Theme.Stretch(slideArea, 0); slideArea.offsetMin = new Vector2(9, 0); slideArea.offsetMax = new Vector2(-9, 0);
            var handle = Theme.Panelled("handle", slideArea, Theme.Ink);
            var hrt = (RectTransform)handle.transform;
            hrt.sizeDelta = new Vector2(16, 34);

            var s = sGo.GetComponent<Slider>();
            s.minValue = 0; s.maxValue = 1; s.value = get();
            s.targetGraphic = fill;
            s.fillRect = (RectTransform)fill.transform;
            s.handleRect = hrt;
            s.onValueChanged.AddListener(v => { set(v); SaveSystem.Save(); Sfx.Play("shift", 0.9f); });
        }

        private void ConfirmReset()
        {
            if (_settings.Find("ConfirmDim") != null) return;
            var dim = Theme.Panelled("ConfirmDim", _settings, new Color(0, 0, 0, 0.78f));
            Theme.Stretch((RectTransform)dim.transform);
            dim.gameObject.AddComponent<Button>();
            var p = Theme.Raised("ConfirmPanel", dim.transform, Theme.Panel);
            ((RectTransform)p.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680, 300), Vector2.zero);
            Theme.Label("q", p.transform, "ERASE ALL PROGRESS, RATINGS\nAND TRAINING STATUS?", Theme.H2, Theme.Ink)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-56, 110), new Vector2(0, -34));

            var cancel = (RectTransform)Theme.TextButton("Cancel", p.transform, "CANCEL", Theme.PanelLight, Theme.Ink,
                () => Destroy(dim.gameObject)).transform;
            cancel.anchorMin = new Vector2(0, 0); cancel.anchorMax = new Vector2(0.5f, 0);
            cancel.pivot = new Vector2(0.5f, 0);
            cancel.offsetMin = new Vector2(30, 28); cancel.offsetMax = new Vector2(-11, 92);

            var confirm = (RectTransform)Theme.TextButton("Confirm", p.transform, "ERASE", Theme.Bad, Theme.Ink, () =>
            {
                SaveSystem.ResetProgress();
                Destroy(dim.gameObject);
                Show(_controlRoom);
            }).transform;
            confirm.anchorMin = new Vector2(0.5f, 0); confirm.anchorMax = new Vector2(1, 0);
            confirm.pivot = new Vector2(0.5f, 0);
            confirm.offsetMin = new Vector2(11, 28); confirm.offsetMax = new Vector2(-30, 92);
        }

        // ---------------------------------------------------------------- how to play

        private void BuildManual()
        {
            _manual = View("Manual");
            Header(_manual, "HOW TO PLAY", () => Show(_controlRoom));

            var cols = Theme.Rect("cols", _manual);
            cols.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1160, 380), new Vector2(0, 44));
            var hlg = cols.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24; hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            ManualPanel(cols, "THE MACHINE", new[]
            {
                "Each card row is one instruction. The reader runs them top to bottom and the carrier crosses the floor.",
                "MOVE  -  step one cell forward.",
                "LEFT / RIGHT  -  turn in place.",
                "REPEAT  -  run the row above it again, the number of times on the dial.",
                "Land the carrier on its tray to win.",
            });
            ManualPanel(cols, "YOUR TOOLS", new[]
            {
                "PUNCH  -  cut an instruction into a blank row.",
                "PATCH  -  fix a row that was punched wrong.",
                "SHIFT  -  nudge a row up or down.",
                "SWAP  -  trade two rows.",
                "FLIP  -  run the card backwards.",
                "COPY  -  duplicate a row.",
            });

            AddLE(Theme.PrimaryButton("Replay", _manual, "REPLAY TRAINING", () =>
            {
                Sfx.Play("load", 0.9f);
                AppState.PendingLevelId = "00";
                Fade.ToScene("Game");
            }).gameObject, 64);
            ((RectTransform)_manual.Find("Replay")).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(360, 64), new Vector2(0, 34));
        }

        private void ManualPanel(Transform parent, string title, string[] lines)
        {
            var p = Theme.Raised($"M_{title}", parent, Theme.Panel);
            Theme.Label("t", p.transform, title, Theme.H2, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-48, 36), new Vector2(0, -18));
            var bodyText = Theme.Label("b", p.transform, string.Join("\n\n", lines), Theme.Body, Theme.Ink, TextAlignmentOptions.TopLeft);
            var brt = (RectTransform)bodyText.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(26, 24); brt.offsetMax = new Vector2(-26, -62);
        }
    }

    /// <summary>Slow continuous rotation.</summary>
    public class Spin : MonoBehaviour
    {
        public float speed = 10f;
        private void Update() => transform.Rotate(0, 0, -speed * Time.unscaledDeltaTime);
    }
}
