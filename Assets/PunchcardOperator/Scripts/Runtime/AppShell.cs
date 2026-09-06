using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>Cross-scene state: which level the player is entering, and a cached
    /// handle to the level database (loaded from Resources so it ships in the build).</summary>
    public static class AppState
    {
        public static string PendingLevelId = "00";
        public static bool ReturnToVaultAfter = true;

        private static LevelDatabase _db;
        public static LevelDatabase Database =>
            _db != null ? _db : (_db = Resources.Load<LevelDatabase>("LevelDatabase"));

        public static PunchCardLevelDefinition PendingLevel => Database != null ? Database.ById(PendingLevelId) : null;
    }

    /// <summary>Industrial control-panel palette + tiny UGUI builders. Dynamic gameplay
    /// content (grid cells, card rows, tool buttons) is built through these; the scene
    /// chrome itself is authored.</summary>
    public static class Theme
    {
        public static readonly Color Panel = new Color32(0x1E, 0x22, 0x28, 0xFF);
        public static readonly Color PanelDark = new Color32(0x14, 0x17, 0x1B, 0xFF);
        public static readonly Color PanelLight = new Color32(0x2C, 0x32, 0x3B, 0xFF);
        public static readonly Color Ink = new Color32(0xE7, 0xE2, 0xD4, 0xFF);
        public static readonly Color InkDim = new Color32(0x8A, 0x8F, 0x8C, 0xFF);
        public static readonly Color Amber = new Color32(0xF2, 0xA5, 0x3A, 0xFF);   // Machine B / warnings
        public static readonly Color Blue = new Color32(0x4F, 0xA9, 0xD6, 0xFF);    // Machine A
        public static readonly Color Good = new Color32(0x6E, 0xC5, 0x7A, 0xFF);
        public static readonly Color Bad = new Color32(0xD9, 0x5A, 0x4A, 0xFF);
        public static readonly Color Card = new Color32(0xC9, 0xBE, 0x9E, 0xFF);    // manila punch card
        public static readonly Color CardEdge = new Color32(0x9A, 0x8C, 0x66, 0xFF);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Panelled(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static Button TextButton(string name, Transform parent, string text, Color bg, Color fg, Action onClick)
        {
            var img = Panelled(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 1);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            var label = Label(name + "_Label", img.transform, text, 30, fg);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
