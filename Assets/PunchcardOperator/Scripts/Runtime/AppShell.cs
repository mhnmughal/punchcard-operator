using System;
using UnityEngine;
using UnityEngine.EventSystems;
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
        /// <summary>Which Menu view to show on load: "control", "vault" or "manual".</summary>
        public static string MenuTarget = "control";

        private static LevelDatabase _db;
        public static LevelDatabase Database =>
            _db != null ? _db : (_db = Resources.Load<LevelDatabase>("LevelDatabase"));

        public static PunchCardLevelDefinition PendingLevel => Database != null ? Database.ById(PendingLevelId) : null;
    }

    /// <summary>Industrial control-panel palette + UGUI builders with consistent
    /// styling and tactile press feedback.</summary>
    public static class Theme
    {
        public static readonly Color Panel = new Color32(0x1E, 0x22, 0x28, 0xFF);
        public static readonly Color PanelDark = new Color32(0x14, 0x17, 0x1B, 0xFF);
        public static readonly Color PanelLight = new Color32(0x2C, 0x32, 0x3B, 0xFF);
        public static readonly Color Ink = new Color32(0xE7, 0xE2, 0xD4, 0xFF);
        public static readonly Color InkDim = new Color32(0x8A, 0x8F, 0x8C, 0xFF);
        public static readonly Color Amber = new Color32(0xF2, 0xA5, 0x3A, 0xFF);
        public static readonly Color Blue = new Color32(0x4F, 0xA9, 0xD6, 0xFF);
        public static readonly Color Good = new Color32(0x6E, 0xC5, 0x7A, 0xFF);
        public static readonly Color Bad = new Color32(0xD9, 0x5A, 0x4A, 0xFF);
        public static readonly Color Card = new Color32(0xC9, 0xBE, 0x9E, 0xFF);
        public static readonly Color CardEdge = new Color32(0x9A, 0x8C, 0x66, 0xFF);
        public static readonly Color Edge = new Color32(0x3A, 0x40, 0x4A, 0xFF);
        public static readonly Color Rivet = new Color32(0x10, 0x12, 0x15, 0xFF);

        // type scale
        public const float Display = 64f, H1 = 40f, H2 = 26f, Body = 21f, Small = 16f;

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

        public static Image Raised(string name, Transform parent, Color color, Color? edge = null)
        {
            var img = Panelled(name, parent, color);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = edge ?? Edge;
            o.effectDistance = new Vector2(2, -2);
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
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;

            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.4f);
            o.effectDistance = new Vector2(0, -2);

            var label = Label(name + "_Label", img.transform, text, H2, fg);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = new Vector2(8, 4); lrt.offsetMax = new Vector2(-8, -4);

            img.gameObject.AddComponent<ButtonFX>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void Bolts(Transform parent, float inset = 12f)
        {
            var corners = new[]
            {
                (new Vector2(0, 0), new Vector2(inset, inset)),
                (new Vector2(0, 1), new Vector2(inset, -inset)),
                (new Vector2(1, 0), new Vector2(-inset, inset)),
                (new Vector2(1, 1), new Vector2(-inset, -inset)),
            };
            foreach (var (a, p) in corners)
            {
                var b = Panelled("bolt", parent, Rivet);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = a; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(9, 9); rt.anchoredPosition = p;
                b.raycastTarget = false;
                var o = b.gameObject.AddComponent<Outline>();
                o.effectColor = Edge; o.effectDistance = new Vector2(1, 1);
            }
        }
    }

    /// <summary>Tactile button feel: scale dip + click on press, subtle lift on hover.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform _rt;
        private Vector3 _base;
        private Selectable _sel;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _base = _rt.localScale;
            _sel = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel != null && !_sel.interactable) return;
            _rt.localScale = _base * 0.955f;
            Sfx.Play("ui", 1.1f);
        }

        public void OnPointerUp(PointerEventData e) => _rt.localScale = _base;
        public void OnPointerExit(PointerEventData e) => _rt.localScale = _base;
        private void OnDisable() { if (_rt != null) _rt.localScale = _base; }
    }
}
