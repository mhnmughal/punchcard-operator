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
            Show(_controlRoom);
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

            Theme.Label("Logo", _controlRoom, "PUNCHCARD\nOPERATOR", 84, Theme.Amber)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(900, 240), new Vector2(0, -80));
            Theme.Label("Tag", _controlRoom, "CONTROL ROOM", 26, Theme.InkDim)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(600, 40), new Vector2(0, -320));

            var col = Theme.Rect("Buttons", _controlRoom);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 380), new Vector2(0, -40));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18; vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

            Theme.TextButton("Enter", col, "ENTER CARD VAULT", Theme.Amber, Theme.PanelDark, () => Show(_vault));
            Theme.TextButton("Manual", col, "OPERATIONS MANUAL", Theme.PanelLight, Theme.Ink, StartTutorial);
            Theme.TextButton("Settings", col, "MACHINE SETUP", Theme.PanelLight, Theme.Ink, () => Show(_settings));
            Theme.TextButton("Quit", col, "SHUT DOWN", Theme.PanelLight, Theme.InkDim, Quit);

            Theme.Label("Cert", _controlRoom, "", 24, Theme.Good)
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(800, 40), new Vector2(0, 40));
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

            var cert = _controlRoom.Find("Cert").GetComponent<TextMeshProUGUI>();
            cert.text = SaveSystem.Data.systemCertified ? "SYSTEM CERTIFIED" : "";
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
                    ? (SaveSystem.Data.tutorialComplete ? "✔" : "▶")
                    : rating switch
                    {
                        LevelRating.PerfectProgram => "◆◆◆",
                        LevelRating.CompactCard => "◆◆",
                        LevelRating.Accepted => "◆",
                        _ => ""
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
