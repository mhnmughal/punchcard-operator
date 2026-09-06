using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>Control Room (start screen) + Card Vault (level select, physical archive
    /// grouped by Program Bank) + Operations Manual (tutorial replay) + Settings.
    /// Attach to a full-screen RectTransform under a Canvas + SafeAreaPanel.</summary>
    public class MenuController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _controlRoom, _vault, _settings;

        private void Start()
        {
            _root = (RectTransform)transform;
            var bg = Theme.Panelled("Backplate", _root, Theme.PanelDark);
            Theme.Stretch((RectTransform)bg.transform);
            BuildControlRoom();
            BuildVault();
            BuildSettings();
            Show(AppState.MenuTarget == "vault" ? _vault : _controlRoom);
            AppState.MenuTarget = "control";
        }

        private void Show(RectTransform v)
        {
            _controlRoom.gameObject.SetActive(v == _controlRoom);
            _vault.gameObject.SetActive(v == _vault);
            _settings.gameObject.SetActive(v == _settings);
            if (v == _vault) RefreshVault();
            Sfx.Play("ui");
        }

        // ---------------------------------------------------------------- control room

        private void BuildControlRoom()
        {
            _controlRoom = Theme.Rect("ControlRoom", _root);
            Theme.Stretch(_controlRoom);

            // top header plate
            var header = Theme.Panelled("HeaderPlate", _controlRoom, Theme.Panel);
            ((RectTransform)header.transform).Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 150), new Vector2(0, 0));
            Theme.Label("Logo", header.transform, "PUNCHCARD OPERATOR", 56, Theme.Amber, TextAlignmentOptions.Center)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-40, -24), new Vector2(0, 8));
            Theme.Label("Tag", header.transform, "CONTROL ROOM", 20, Theme.InkDim, TextAlignmentOptions.Center)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-40, 28), new Vector2(0, 14));
            var rule = Theme.Panelled("Rule", header.transform, Theme.Amber);
            ((RectTransform)rule.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-8, 4), new Vector2(0, 0));

            // left status column — station read-outs
            var status = Theme.Panelled("StationStatus", _controlRoom, Theme.Panel);
            ((RectTransform)status.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(360, 440), new Vector2(28, -20));
            Theme.Label("sh", status.transform, "STATION STATUS", 20, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-32, 30), new Vector2(0, -14));
            var lampCol = Theme.Rect("lamps", status.transform);
            lampCol.Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lampCol.offsetMin = new Vector2(18, 18); lampCol.offsetMax = new Vector2(-18, -52);
            var lvlg = lampCol.gameObject.AddComponent<VerticalLayoutGroup>();
            lvlg.spacing = 10; lvlg.childControlHeight = true; lvlg.childControlWidth = true;
            lvlg.childForceExpandHeight = true; lvlg.childForceExpandWidth = true;
            var camp0 = AppState.Database != null ? AppState.Database.Campaign : new List<PunchCardLevelDefinition>();
            int solved = 0; foreach (var l in camp0) if (ProgressionManager.BestRating(l.id) != LevelRating.None) solved++;
            Lamp(lampCol, "OPERATOR TRAINING", SaveSystem.Data.tutorialComplete);
            Lamp(lampCol, "BANK A ONLINE", SaveSystem.Data.tutorialComplete);
            Lamp(lampCol, "BANK B ONLINE", solved >= 4);
            Lamp(lampCol, "BANK C ONLINE", solved >= 8);
            Lamp(lampCol, $"PROGRAMS FILED  {solved} / 12", solved > 0);
            Lamp(lampCol, "MASTER CERTIFICATION", SaveSystem.Data.systemCertified);

            // center console frame
            var console = Theme.Panelled("MainConsole", _controlRoom, Theme.PanelDark);
            ((RectTransform)console.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 300), new Vector2(70, 40));
            var cbo = console.gameObject.AddComponent<UnityEngine.UI.Outline>();
            cbo.effectColor = new Color32(0x3A, 0x40, 0x4A, 0xFF); cbo.effectDistance = new Vector2(2, 2);
            Theme.Label("ct", console.transform, "MAIN MACHINE", 20, Theme.InkDim, TextAlignmentOptions.Top)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-20, 28), new Vector2(0, -10));
            var disc = Theme.Panelled("disc", console.transform, Theme.Panel);
            ((RectTransform)disc.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 180), new Vector2(0, -6));
            var dio = disc.gameObject.AddComponent<UnityEngine.UI.Outline>();
            dio.effectColor = Theme.Amber; dio.effectDistance = new Vector2(3, 3);
            var hub = Theme.Panelled("hub", disc.transform, Theme.Amber);
            ((RectTransform)hub.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54, 54), Vector2.zero);
            var spoke = Theme.Panelled("spoke", disc.transform, Theme.Amber);
            ((RectTransform)spoke.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 150), Vector2.zero);
            spoke.transform.localEulerAngles = new Vector3(0, 0, 30f);

            // nav buttons — right column
            var col = Theme.Rect("Buttons", _controlRoom);
            col.Set(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(480, 360), new Vector2(-28, 40));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

            Theme.TextButton("Enter", col, "ENTER CARD VAULT", Theme.Amber, Theme.PanelDark, () => Show(_vault));
            Theme.TextButton("Manual", col, "OPERATIONS MANUAL", Theme.PanelLight, Theme.Ink, StartTutorial);
            Theme.TextButton("Settings", col, "MACHINE SETUP", Theme.PanelLight, Theme.Ink, () => Show(_settings));
            Theme.TextButton("Quit", col, "SHUT DOWN", Theme.PanelLight, Theme.InkDim, Quit);

            var certPlate = Theme.Panelled("CertPlate", _controlRoom, Theme.PanelDark);
            ((RectTransform)certPlate.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(560, 52), new Vector2(0, 20));
            var certLabel = Theme.Label("Cert", certPlate.transform, "MASTER STATUS — SYSTEM CERTIFIED", 22, Theme.Good);
            Theme.Stretch((RectTransform)certLabel.transform);
            var co = certPlate.gameObject.AddComponent<UnityEngine.UI.Outline>();
            co.effectColor = Theme.Good; co.effectDistance = new Vector2(2, 2);
            _certPlate = certPlate.gameObject;
            _certPlate.SetActive(SaveSystem.Data.systemCertified);
        }

        private GameObject _certPlate;

        private void Lamp(Transform parent, string label, bool on)
        {
            var row = Theme.Panelled($"L_{label}", parent, Theme.PanelDark);
            var dot = Theme.Panelled("dot", row.transform, on ? Theme.Good : new Color32(0x3A, 0x2A, 0x22, 0xFF));
            ((RectTransform)dot.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 16), new Vector2(16, 0));
            Theme.Label("t", row.transform, label, 17, on ? Theme.Ink : Theme.InkDim, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-52, 0), new Vector2(40, 0));
        }

        private void StartTutorial()
        {
            AppState.PendingLevelId = "00";
            SceneManager.LoadScene("Game");
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
            _vault = Theme.Rect("CardVault", _root);
            Theme.Stretch(_vault);

            Theme.Label("VHdr", _vault, "CARD VAULT", 44, Theme.Amber)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 70), new Vector2(0, -30));
            Theme.TextButton("VBack", _vault, "‹ CONTROL ROOM", Theme.PanelLight, Theme.Ink, () => Show(_controlRoom))
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(280, 54), new Vector2(24, -24));

            var body = Theme.Rect("VBody", _vault);
            body.Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Theme.Stretch(body, 24); body.offsetMax = new Vector2(-24, -120);
            var vlg = body.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        }

        private void RefreshVault()
        {
            var body = _vault.Find("VBody");
            foreach (Transform c in body) Destroy(c.gameObject);

            var db = AppState.Database;
            if (db == null)
            {
                Theme.Label("err", body, "NO LEVEL DATABASE — run Punchcard Operator/Rebuild All Levels", 24, Theme.Bad);
                return;
            }

            BankRow(body, "TRAINING", "Operator Training", new[] { db.Tutorial });
            var camp = db.Campaign;
            BankRow(body, "BANK A", "Basic Control", camp.FindAll(l => l.bank == ProgramBank.A));
            BankRow(body, "BANK B", "Card Manipulation", camp.FindAll(l => l.bank == ProgramBank.B));
            BankRow(body, "BANK C", "Master Control", camp.FindAll(l => l.bank == ProgramBank.C));

            if (_certPlate != null) _certPlate.SetActive(SaveSystem.Data.systemCertified);
        }

        private void BankRow(Transform parent, string code, string name, IList<PunchCardLevelDefinition> levels)
        {
            var group = Theme.Panelled($"Grp_{code}", parent, Theme.Panel);
            var grt = (RectTransform)group.transform;
            var le = group.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 150;

            Theme.Label("code", group.transform, code, 24, Theme.Amber, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, 32), new Vector2(18, -12));
            Theme.Label("name", group.transform, name, 18, Theme.InkDim, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(300, 26), new Vector2(18, -44));

            var strip = Theme.Rect("strip", group.transform);
            strip.Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(1, 1);
            strip.offsetMin = new Vector2(18, 14); strip.offsetMax = new Vector2(-18, -76);
            var hlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            foreach (var lvl in levels)
            {
                if (lvl == null) continue;
                bool unlocked = ProgressionManager.IsUnlocked(lvl);
                var rating = ProgressionManager.BestRating(lvl.id);
                string mark = lvl.isTutorial
                    ? (SaveSystem.Data.tutorialComplete ? "COMPLETE" : "START")
                    : rating switch
                    {
                        LevelRating.PerfectProgram => "PERFECT PROGRAM",
                        LevelRating.CompactCard => "COMPACT CARD",
                        LevelRating.Accepted => "ACCEPTED",
                        _ => "READY"
                    };
                var label = $"{lvl.id}\n{lvl.displayName}\n<size=60%>{(unlocked ? mark : "LOCKED")}</size>";
                var bg = unlocked ? Theme.PanelLight : Theme.PanelDark;
                var fg = unlocked ? Theme.Ink : Theme.InkDim;
                var btn = Theme.TextButton($"Lvl_{lvl.id}", strip, label, bg, fg, () =>
                {
                    if (!unlocked) { Sfx.Play("error"); return; }
                    AppState.PendingLevelId = lvl.id;
                    SceneManager.LoadScene("Game");
                });
                var t = btn.GetComponentInChildren<TextMeshProUGUI>();
                t.fontSize = 20; t.richText = true;
            }
        }

        // ---------------------------------------------------------------- settings

        private void BuildSettings()
        {
            _settings = Theme.Rect("Settings", _root);
            Theme.Stretch(_settings);
            Theme.Label("SHdr", _settings, "MACHINE SETUP", 44, Theme.Amber)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 70), new Vector2(0, -30));
            Theme.TextButton("SBack", _settings, "‹ CONTROL ROOM", Theme.PanelLight, Theme.Ink, () => Show(_controlRoom))
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(280, 54), new Vector2(24, -24));

            var col = Theme.Rect("SCol", _settings);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680, 420), Vector2.zero);
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16; vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

            Slider(col, "MASTER VOLUME", () => SaveSystem.Data.masterVolume, v => SaveSystem.Data.masterVolume = v);
            Slider(col, "SFX VOLUME", () => SaveSystem.Data.sfxVolume, v => SaveSystem.Data.sfxVolume = v);
            Slider(col, "MUSIC VOLUME", () => SaveSystem.Data.musicVolume, v => SaveSystem.Data.musicVolume = v);

            Theme.TextButton("Reset", col, "RESET ALL PROGRESS", Theme.Bad, Theme.Ink, ConfirmReset);
        }

        private void Slider(Transform parent, string label, System.Func<float> get, System.Action<float> set)
        {
            var row = Theme.Panelled($"S_{label}", parent, Theme.Panel);
            Theme.Label("l", row.transform, label, 22, Theme.Ink, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(16, 0));
            var sGo = new GameObject("slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(row.transform, false);
            var srt = (RectTransform)sGo.transform;
            srt.anchorMin = new Vector2(0.42f, 0.5f); srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f); srt.sizeDelta = new Vector2(-30, 24); srt.anchoredPosition = new Vector2(-15, 0);
            var fill = Theme.Panelled("fill", sGo.transform, Theme.Amber);
            Theme.Stretch((RectTransform)fill.transform);
            var s = sGo.GetComponent<Slider>();
            s.minValue = 0; s.maxValue = 1; s.value = get();
            s.targetGraphic = fill;
            s.fillRect = (RectTransform)fill.transform;
            s.onValueChanged.AddListener(v => { set(v); SaveSystem.Save(); Sfx.Play("ui"); });
        }

        private void ConfirmReset()
        {
            if (_settings.Find("ConfirmDim") != null) return;
            var dim = Theme.Panelled("ConfirmDim", _settings, new Color(0, 0, 0, 0.7f));
            Theme.Stretch((RectTransform)dim.transform);
            dim.gameObject.AddComponent<Button>();
            var p = Theme.Panelled("ConfirmPanel", dim.transform, Theme.Panel);
            ((RectTransform)p.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620, 300), Vector2.zero);
            Theme.Label("q", p.transform, "ERASE ALL PROGRESS, RATINGS\nAND TRAINING STATUS?", 26, Theme.Ink)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-40, 120), new Vector2(0, -30));
            Theme.TextButton("Cancel", p.transform, "CANCEL", Theme.PanelLight, Theme.Ink, () => Destroy(dim.gameObject))
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-20, 64), new Vector2(-110, 24));
            Theme.TextButton("Confirm", p.transform, "ERASE", Theme.Bad, Theme.Ink, () =>
            {
                SaveSystem.ResetProgress();
                Destroy(dim.gameObject);
                Show(_controlRoom);
            }).GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-20, 64), new Vector2(110, 24));
        }
    }
}
