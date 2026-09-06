using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>The menu station: Control Room (hub), Card Vault (physical level
    /// archive), Machine Setup (settings), Operations Manual (rules + training
    /// replay + credits). One scene, cross-faded views.</summary>
    public class MenuController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _controlRoom, _vault, _settings, _manual, _credits;
        private RectTransform _current;
        private GameObject _certPlate;

        private void Start()
        {
            _root = (RectTransform)transform;
            var bg = Theme.Panelled("Backplate", _root, Theme.PanelDark);
            Theme.Stretch((RectTransform)bg.transform);
            for (int i = 1; i < 4; i++)
            {
                var seam = Theme.Panelled("seam", bg.transform, new Color(0, 0, 0, 0.16f));
                ((RectTransform)seam.transform).Set(new Vector2(i / 4f, 0), new Vector2(i / 4f, 1), new Vector2(0.5f, 0.5f), new Vector2(2, 0), Vector2.zero);
                seam.raycastTarget = false;
            }

            BuildControlRoom();
            BuildVault();
            BuildSettings();
            BuildManual();
            BuildCredits();

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
            _credits.gameObject.SetActive(false);
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
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.18f);
                cg.alpha = k; cgf.alpha = 1f - k;
                yield return null;
            }
            cg.alpha = 1f; cgf.alpha = 1f;
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

        private void Header(Transform parent, string title, System.Action back = null)
        {
            var plate = Theme.Raised("HeaderPlate", parent, Theme.Panel);
            ((RectTransform)plate.transform).Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 96), Vector2.zero);
            Theme.Label("h", plate.transform, title, Theme.H1, Theme.Amber, TextAlignmentOptions.Center)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-360, 0), Vector2.zero);
            var rule = Theme.Panelled("rule", plate.transform, Theme.Amber);
            ((RectTransform)rule.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-8, 3), Vector2.zero);
            rule.raycastTarget = false;
            if (back != null)
                Theme.TextButton("Back", plate.transform, "‹ BACK", Theme.PanelLight, Theme.Ink, back)
                    .GetComponent<RectTransform>().Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(210, 52), new Vector2(24, 0));
        }

        // ---------------------------------------------------------------- control room

        private void BuildControlRoom()
        {
            _controlRoom = View("ControlRoom");

            var header = Theme.Raised("HeaderPlate", _controlRoom, Theme.Panel);
            ((RectTransform)header.transform).Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 150), Vector2.zero);
            Theme.Label("Logo", header.transform, "PUNCHCARD OPERATOR", 56, Theme.Amber)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-40, -24), new Vector2(0, 8));
            Theme.Label("Tag", header.transform, "CONTROL ROOM", Theme.H2, Theme.InkDim)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-40, 28), new Vector2(0, 16));
            var rule = Theme.Panelled("Rule", header.transform, Theme.Amber);
            ((RectTransform)rule.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-8, 4), Vector2.zero);
            rule.raycastTarget = false;

            var status = Theme.Raised("StationStatus", _controlRoom, Theme.Panel);
            ((RectTransform)status.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(360, 460), new Vector2(28, -14));
            Theme.Bolts(status.transform);
            Theme.Label("sh", status.transform, "STATION STATUS", Theme.H2, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-40, 30), new Vector2(0, -16));
            var lampCol = Theme.Rect("lamps", status.transform);
            Theme.Stretch(lampCol); lampCol.offsetMin = new Vector2(18, 18); lampCol.offsetMax = new Vector2(-18, -56);
            var lvlg = lampCol.gameObject.AddComponent<VerticalLayoutGroup>();
            lvlg.spacing = 10; lvlg.childControlHeight = lvlg.childControlWidth = true;
            lvlg.childForceExpandHeight = lvlg.childForceExpandWidth = true;
            var camp0 = AppState.Database != null ? AppState.Database.Campaign : new List<PunchCardLevelDefinition>();
            int solved = 0; foreach (var l in camp0) if (ProgressionManager.BestRating(l.id) != LevelRating.None) solved++;
            Lamp(lampCol, "OPERATOR TRAINING", SaveSystem.Data.tutorialComplete);
            Lamp(lampCol, "PROGRAM BANK A", SaveSystem.Data.tutorialComplete);
            Lamp(lampCol, "PROGRAM BANK B", solved >= 4);
            Lamp(lampCol, "PROGRAM BANK C", solved >= 8);
            Lamp(lampCol, $"PROGRAMS FILED   {solved} / 12", solved > 0);
            Lamp(lampCol, "MASTER CERTIFICATION", SaveSystem.Data.systemCertified);

            var console = Theme.Raised("MainConsole", _controlRoom, Theme.PanelDark);
            ((RectTransform)console.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(540, 300), new Vector2(60, 34));
            Theme.Bolts(console.transform);
            Theme.Label("ct", console.transform, "MAIN MACHINE", Theme.Small, Theme.InkDim, TextAlignmentOptions.Top)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-20, 24), new Vector2(0, -12));
            var disc = Theme.Panelled("disc", console.transform, Theme.Panel);
            ((RectTransform)disc.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(176, 176), new Vector2(0, -6));
            var dio = disc.gameObject.AddComponent<Outline>(); dio.effectColor = Theme.Amber; dio.effectDistance = new Vector2(3, 3);
            var hub = Theme.Panelled("hub", disc.transform, Theme.Amber);
            ((RectTransform)hub.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52, 52), Vector2.zero);
            var spoke = Theme.Panelled("spoke", disc.transform, Theme.Amber);
            ((RectTransform)spoke.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 148), Vector2.zero);
            spoke.transform.localEulerAngles = new Vector3(0, 0, 24f);
            spoke.gameObject.AddComponent<Spin>().speed = 8f;

            var col = Theme.Rect("Buttons", _controlRoom);
            col.Set(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(480, 384), new Vector2(-28, 34));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = vlg.childForceExpandWidth = true;
            Theme.TextButton("Enter", col, "ENTER CARD VAULT", Theme.Amber, Theme.PanelDark, () => Show(_vault));
            Theme.TextButton("Manual", col, "OPERATIONS MANUAL", Theme.PanelLight, Theme.Ink, () => Show(_manual));
            Theme.TextButton("Settings", col, "MACHINE SETUP", Theme.PanelLight, Theme.Ink, () => Show(_settings));
            Theme.TextButton("Quit", col, "SHUT DOWN", Theme.PanelLight, Theme.InkDim, Quit);

            var certPlate = Theme.Panelled("CertPlate", _controlRoom, new Color32(0x1B, 0x2C, 0x1E, 0xFF));
            ((RectTransform)certPlate.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(560, 52), new Vector2(0, 20));
            var certLabel = Theme.Label("Cert", certPlate.transform, "MASTER STATUS  —  SYSTEM CERTIFIED", Theme.H2, Theme.Good);
            Theme.Stretch((RectTransform)certLabel.transform);
            var co = certPlate.gameObject.AddComponent<Outline>(); co.effectColor = Theme.Good; co.effectDistance = new Vector2(2, 2);
            _certPlate = certPlate.gameObject;
            _certPlate.SetActive(SaveSystem.Data.systemCertified);
        }

        private void Lamp(Transform parent, string label, bool on)
        {
            var row = Theme.Panelled($"L_{label}", parent, Theme.PanelDark);
            var dot = Theme.Panelled("dot", row.transform, on ? Theme.Good : new Color32(0x33, 0x2A, 0x22, 0xFF));
            ((RectTransform)dot.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, 15), new Vector2(16, 0));
            Theme.Label("t", row.transform, label, Theme.Small, on ? Theme.Ink : Theme.InkDim, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-52, 0), new Vector2(42, 0));
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
            Theme.Stretch(body, 24); body.offsetMax = new Vector2(-24, -120);
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
                Theme.Label("err", body, "NO LEVEL DATABASE — run Punchcard Operator/Rebuild All Levels", Theme.H2, Theme.Bad);
                return;
            }
            Cassette(body, "TRAINING", "Operator Training", new[] { db.Tutorial });
            var camp = db.Campaign;
            Cassette(body, "BANK A", "Basic Control", camp.FindAll(l => l.bank == ProgramBank.A));
            Cassette(body, "BANK B", "Card Manipulation", camp.FindAll(l => l.bank == ProgramBank.B));
            Cassette(body, "BANK C", "Master Control", camp.FindAll(l => l.bank == ProgramBank.C));
            if (_certPlate != null) _certPlate.SetActive(SaveSystem.Data.systemCertified);
        }

        private void Cassette(Transform parent, string code, string name, IList<PunchCardLevelDefinition> levels)
        {
            var group = Theme.Raised($"Grp_{code}", parent, Theme.Panel);
            group.gameObject.AddComponent<LayoutElement>().minHeight = 158;
            Theme.Bolts(group.transform, 10f);

            // spine
            var spine = Theme.Panelled("spine", group.transform, Theme.PanelDark);
            ((RectTransform)spine.transform).Set(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(150, 0), Vector2.zero);
            spine.raycastTarget = false;
            var acc = Theme.Panelled("acc", spine.transform, Theme.Amber);
            ((RectTransform)acc.transform).Set(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(3, 0), Vector2.zero);
            Theme.Label("code", spine.transform, code, Theme.H2, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-24, 30), new Vector2(16, -14));
            Theme.Label("name", spine.transform, name, Theme.Small, Theme.InkDim, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-20, 44), new Vector2(16, -46));

            var strip = Theme.Rect("strip", group.transform);
            strip.Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            strip.offsetMin = new Vector2(164, 16); strip.offsetMax = new Vector2(-16, -16);
            var hlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            foreach (var lvl in levels)
            {
                if (lvl == null) continue;
                bool unlocked = ProgressionManager.IsUnlocked(lvl);
                var rating = ProgressionManager.BestRating(lvl.id);
                string mark = lvl.isTutorial
                    ? (SaveSystem.Data.tutorialComplete ? "COMPLETE" : "START HERE")
                    : rating switch
                    {
                        LevelRating.PerfectProgram => "PERFECT PROGRAM",
                        LevelRating.CompactCard => "COMPACT CARD",
                        LevelRating.Accepted => "ACCEPTED",
                        _ => "READY"
                    };
                var markCol = rating == LevelRating.PerfectProgram ? Theme.Good
                    : rating == LevelRating.None ? Theme.InkDim : Theme.Amber;
                var label = $"<size=140%>{lvl.id}</size>\n{lvl.displayName}\n<size=72%><color=#{ColorUtility.ToHtmlStringRGB(unlocked ? markCol : Theme.InkDim)}>{(unlocked ? mark : "LOCKED")}</color></size>";
                var bg = unlocked ? Theme.PanelLight : Theme.PanelDark;
                var fg = unlocked ? Theme.Ink : Theme.InkDim;
                var btn = Theme.TextButton($"Lvl_{lvl.id}", strip, label, bg, fg, () =>
                {
                    if (!unlocked) { Sfx.Play("error"); return; }
                    Sfx.Play("load", 0.9f);
                    AppState.PendingLevelId = lvl.id;
                    Fade.ToScene("Game");
                });
                var t = btn.GetComponentInChildren<TextMeshProUGUI>();
                t.fontSize = Theme.Small; t.richText = true;
            }
        }

        // ---------------------------------------------------------------- settings

        private void BuildSettings()
        {
            _settings = View("Settings");
            Header(_settings, "MACHINE SETUP", () => Show(_controlRoom));

            var col = Theme.Rect("SCol", _settings);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720, 380), new Vector2(0, 10));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = vlg.childForceExpandWidth = true;

            Slider(col, "MASTER VOLUME", () => SaveSystem.Data.masterVolume, v => SaveSystem.Data.masterVolume = v);
            Slider(col, "SOUND EFFECTS", () => SaveSystem.Data.sfxVolume, v => SaveSystem.Data.sfxVolume = v);
            Theme.TextButton("Reset", col, "RESET ALL PROGRESS", Theme.Bad, Theme.Ink, ConfirmReset);
        }

        private void Slider(Transform parent, string label, System.Func<float> get, System.Action<float> set)
        {
            var row = Theme.Raised($"S_{label}", parent, Theme.Panel);
            Theme.Label("l", row.transform, label, Theme.Body, Theme.Ink, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(20, 0));
            var track = Theme.Panelled("track", row.transform, Theme.PanelDark);
            ((RectTransform)track.transform).Set(new Vector2(0.44f, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30, 20), new Vector2(-15, 0));
            var sGo = new GameObject("slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(track.transform, false);
            Theme.Stretch((RectTransform)sGo.transform);
            var fill = Theme.Panelled("fill", sGo.transform, Theme.Amber);
            Theme.Stretch((RectTransform)fill.transform);
            var s = sGo.GetComponent<Slider>();
            s.minValue = 0; s.maxValue = 1; s.value = get();
            s.targetGraphic = fill; s.fillRect = (RectTransform)fill.transform;
            s.onValueChanged.AddListener(v => { set(v); SaveSystem.Save(); Sfx.Play("shift", 0.9f); });
        }

        private void ConfirmReset()
        {
            if (_settings.Find("ConfirmDim") != null) return;
            var dim = Theme.Panelled("ConfirmDim", _settings, new Color(0, 0, 0, 0.75f));
            Theme.Stretch((RectTransform)dim.transform);
            dim.gameObject.AddComponent<Button>();
            var p = Theme.Raised("ConfirmPanel", dim.transform, Theme.Panel);
            ((RectTransform)p.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640, 300), Vector2.zero);
            var po = p.gameObject.AddComponent<Outline>(); po.effectColor = Theme.Bad; po.effectDistance = new Vector2(2, 2);
            Theme.Label("q", p.transform, "ERASE ALL PROGRESS, RATINGS\nAND TRAINING STATUS?", Theme.H2, Theme.Ink)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-40, 120), new Vector2(0, -30));
            Theme.TextButton("Cancel", p.transform, "CANCEL", Theme.PanelLight, Theme.Ink, () => Destroy(dim.gameObject))
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-24, 62), new Vector2(-112, 26));
            Theme.TextButton("Confirm", p.transform, "ERASE", Theme.Bad, Theme.Ink, () =>
            {
                SaveSystem.ResetProgress();
                Destroy(dim.gameObject);
                Show(_controlRoom);
            }).GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-24, 62), new Vector2(112, 26));
        }

        // ---------------------------------------------------------------- operations manual

        private void BuildManual()
        {
            _manual = View("Manual");
            Header(_manual, "OPERATIONS MANUAL", () => Show(_controlRoom));

            var scroll = Theme.Rect("body", _manual);
            Theme.Stretch(scroll, 28); scroll.offsetMax = new Vector2(-28, -116); scroll.offsetMin = new Vector2(28, 96);

            var cols = Theme.Rect("cols", scroll);
            Theme.Stretch(cols);
            var hlg = cols.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            ManualPanel(cols, "THE MACHINE", new[]
            {
                "A punch card holds one instruction per row. The reader runs them top to bottom.",
                "MOVE  — the carrier steps one cell the way it faces.",
                "LEFT / RIGHT  — the carrier turns in place.",
                "REPEAT  — re-runs the row above it, the number of times on the dial.",
                "Both machines must finish parked on their trays.",
            });
            ManualPanel(cols, "THE TOOL DOCK", new[]
            {
                "PUNCH  — cut an instruction into an empty row.",
                "PATCH  — re-cut a row that was punched wrong.",
                "SHIFT  — slide a row up or down by one.",
                "SWAP  — exchange any two rows.",
                "FLIP  — feed the card so the reader runs it bottom to top.",
                "COPY  — duplicate a row into an empty one.",
            });

            var footer = Theme.Rect("footer", _manual);
            footer.Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(720, 68), new Vector2(0, 22));
            var flg = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            flg.spacing = 16; flg.childControlWidth = flg.childControlHeight = true;
            flg.childForceExpandWidth = flg.childForceExpandHeight = true;
            Theme.TextButton("Replay", footer, "REPLAY TRAINING", Theme.Amber, Theme.PanelDark, () =>
            {
                Sfx.Play("load", 0.9f);
                AppState.PendingLevelId = "00";
                Fade.ToScene("Game");
            });
            Theme.TextButton("Credits", footer, "CREDITS", Theme.PanelLight, Theme.Ink, () => Show(_credits));
        }

        private void ManualPanel(Transform parent, string title, string[] lines)
        {
            var p = Theme.Raised($"M_{title}", parent, Theme.Panel);
            Theme.Label("t", p.transform, title, Theme.H2, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-44, 34), new Vector2(0, -16));
            var bodyText = Theme.Label("b", p.transform, string.Join("\n\n", lines), Theme.Body, Theme.Ink, TextAlignmentOptions.TopLeft);
            var brt = (RectTransform)bodyText.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(22, 22); brt.offsetMax = new Vector2(-22, -58);
        }

        // ---------------------------------------------------------------- credits

        private void BuildCredits()
        {
            _credits = View("Credits");
            Header(_credits, "CREDITS", () => Show(_manual));
            var text =
                "PUNCHCARD OPERATOR\n\n" +
                "Design, code, level design, art and sound\nbuilt as one system for this project.\n\n" +
                "Every sound is synthesised at runtime.\nEvery texture is generated in-engine.\nNo third-party assets. No network. No ads.\n\n" +
                "Type: LiberationSans (SIL Open Font License).\n\n" +
                "Made with Unity.";
            Theme.Label("c", _credits, text, Theme.Body, Theme.Ink)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 460), new Vector2(0, -10));
        }
    }

    /// <summary>Slow continuous rotation, for the Control Room machine disc.</summary>
    public class Spin : MonoBehaviour
    {
        public float speed = 10f;
        private void Update() => transform.Rotate(0, 0, -speed * Time.unscaledDeltaTime);
    }
}
