using System;
using System.Collections.Generic;
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

    /// <summary>Game UI skin: palette + sprite-backed panels and buttons with a
    /// baked bevel, rounded corners, glow and tactile press feedback.</summary>
    public static class Theme
    {
        // deep instrument palette
        public static readonly Color Base    = new Color32(0x16, 0x1A, 0x21, 0xFF);
        public static readonly Color Panel   = new Color32(0x23, 0x2A, 0x35, 0xFF);
        public static readonly Color PanelDark = new Color32(0x12, 0x15, 0x1B, 0xFF);
        public static readonly Color PanelLight = new Color32(0x33, 0x3D, 0x4B, 0xFF);
        public static readonly Color Ink     = new Color32(0xF1, 0xEE, 0xE4, 0xFF);
        public static readonly Color InkDim  = new Color32(0x94, 0x9C, 0xAA, 0xFF);
        public static readonly Color Amber   = new Color32(0xFF, 0xB0, 0x3A, 0xFF);   // primary accent
        public static readonly Color AmberDeep = new Color32(0xE0, 0x86, 0x1E, 0xFF);
        public static readonly Color Blue    = new Color32(0x53, 0xB4, 0xE6, 0xFF);   // Machine A
        public static readonly Color Good    = new Color32(0x62, 0xD1, 0x7E, 0xFF);
        public static readonly Color Bad     = new Color32(0xF0, 0x5F, 0x52, 0xFF);
        public static readonly Color Card    = new Color32(0xD9, 0xCB, 0xA4, 0xFF);   // manila
        public static readonly Color CardEdge = new Color32(0x8E, 0x7F, 0x58, 0xFF);
        public static readonly Color Edge    = new Color32(0x46, 0x52, 0x64, 0xFF);

        public const float Display = 76f, H1 = 44f, H2 = 27f, Body = 21f, Small = 16f;

        private static Sprite _panel, _button, _inset, _glow, _vignette, _pipFull, _pipEmpty, _lock;
        private static Sprite S(string n) => Resources.Load<Sprite>("UI/" + n);
        public static Sprite PanelSprite  => _panel   ? _panel   : (_panel   = S("panel"));
        public static Sprite ButtonSprite => _button  ? _button  : (_button  = S("button"));
        public static Sprite InsetSprite  => _inset   ? _inset   : (_inset   = S("inset"));
        public static Sprite GlowSprite   => _glow    ? _glow    : (_glow    = S("glow"));
        public static Sprite VignetteSprite => _vignette ? _vignette : (_vignette = S("vignette"));
        public static Sprite PipFull  => _pipFull  ? _pipFull  : (_pipFull  = S("pip_full"));
        public static Sprite PipEmpty => _pipEmpty ? _pipEmpty : (_pipEmpty = S("pip_empty"));
        public static Sprite LockSprite => _lock ? _lock : (_lock = S("lock"));
        private static Sprite _gear;
        public static Sprite GearSprite => _gear ? _gear : (_gear = S("gear"));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Flat filled rounded panel.</summary>
        public static Image Panelled(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PanelSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2.2f;
            img.color = color;
            return img;
        }

        /// <summary>Rounded panel that reads as raised, with a soft drop shadow.</summary>
        public static Image Raised(string name, Transform parent, Color color, Color? edge = null)
        {
            var img = Panelled(name, parent, color);
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.38f);
            sh.effectDistance = new Vector2(0, -6);
            return img;
        }

        public static Image Inset(string name, Transform parent, Color color)
        {
            var img = Panelled(name, parent, color);
            img.sprite = InsetSprite;
            return img;
        }

        public static Image Glow(string name, Transform parent, Color color, float alpha = 0.5f)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GlowSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = new Color(color.r, color.g, color.b, alpha);
            return img;
        }

        public static Image Vignette(Transform parent)
        {
            var rt = Rect("Vignette", parent);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = VignetteSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = new Color(0f, 0f, 0f, 0.55f);
            return img;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.enableVertexGradient = false;
            return t;
        }

        public static Button TextButton(string name, Transform parent, string text, Color bg, Color fg, Action onClick)
            => MakeButton(name, parent, text, bg, fg, onClick, primary: false);

        public static Button PrimaryButton(string name, Transform parent, string text, Action onClick)
            => MakeButton(name, parent, text, Amber, PanelDark, onClick, primary: true);

        private static Button MakeButton(string name, Transform parent, string text, Color bg, Color fg, Action onClick, bool primary)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2.4f;
            img.color = bg;

            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            if (primary)
            {
                var glow = Glow(name + "_Glow", rt, Amber, 0.0f);
                var grt = (RectTransform)glow.transform;
                grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
                grt.offsetMin = new Vector2(-26, -26); grt.offsetMax = new Vector2(26, 26);
                grt.SetAsFirstSibling();
            }

            var label = Label(name + "_Label", rt, text, primary ? H2 + 3 : H2, fg);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10, 4); lrt.offsetMax = new Vector2(-10, -4);
            label.characterSpacing = 3f;
            label.fontStyle = FontStyles.Bold;
            var sh = label.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f);
            sh.effectDistance = new Vector2(0, -2);

            rt.gameObject.AddComponent<ButtonFX>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        /// <summary>A centred row of small rating pips (filled = earned), pinned to the
        /// bottom of the parent.</summary>
        public static void Pips(Transform parent, int filled, int total, float bottom = 14f, float pip = 16f)
        {
            var row = Rect("pips", parent);
            row.anchorMin = new Vector2(0.5f, 0); row.anchorMax = new Vector2(0.5f, 0);
            row.pivot = new Vector2(0.5f, 0);
            row.sizeDelta = new Vector2(total * (pip + 6f), pip);
            row.anchoredPosition = new Vector2(0, bottom);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            for (int i = 0; i < total; i++)
            {
                var img = new GameObject($"pip{i}", typeof(RectTransform)).AddComponent<Image>();
                img.transform.SetParent(row, false);
                img.sprite = i < filled ? PipFull : PipEmpty;
                img.raycastTarget = false;
                img.color = i < filled ? Amber : InkDim;
            }
        }

        // kept for callers; a light no-op now that panels are rounded + shadowed
        public static void Bolts(Transform parent, float inset = 12f) { }
    }

    /// <summary>Tactile button feel: hover lift + glow, press scale-dip + click.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rt;
        private Vector3 _base;
        private Selectable _sel;
        private Image _glow;
        private float _target = 1f, _glowT, _glowTarget;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _base = _rt.localScale;
            _sel = GetComponent<Selectable>();
            var g = transform.Find(name + "_Glow");
            if (g != null) _glow = g.GetComponent<Image>();
        }

        private void Update()
        {
            float s = Mathf.Lerp(_rt.localScale.x / _base.x, _target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            _rt.localScale = _base * s;
            if (_glow != null)
            {
                _glowT = Mathf.Lerp(_glowT, _glowTarget, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                var c = _glow.color; c.a = _glowT * 0.4f; _glow.color = c;
            }
        }

        private bool Usable => _sel == null || _sel.interactable;

        public void OnPointerEnter(PointerEventData e) { if (Usable) { _target = 1.03f; _glowTarget = 1f; } }
        public void OnPointerExit(PointerEventData e)  { _target = 1f; _glowTarget = 0f; }
        public void OnPointerDown(PointerEventData e)  { if (Usable) { _target = 0.955f; Sfx.Play("ui", 1.15f); } }
        public void OnPointerUp(PointerEventData e)    { _target = Usable ? 1.03f : 1f; }
        private void OnDisable() { if (_rt != null) _rt.localScale = _base; _target = 1f; _glowTarget = 0f; }
    }

    /// <summary>Slow-drifting faint punch-card motes + vignette behind a menu screen.</summary>
    public class Backdrop : MonoBehaviour
    {
        private readonly List<RectTransform> _motes = new List<RectTransform>();
        private readonly List<Vector2> _vel = new List<Vector2>();

        private void Start()
        {
            var rt = (RectTransform)transform;
            var fill = rt.gameObject.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            fill.color = Theme.Base;
            fill.raycastTarget = false;

            for (int i = 0; i < 12; i++)
            {
                var m = Theme.Rect("mote", rt);
                var img = m.gameObject.AddComponent<Image>();
                // soft radial glows, not rectangles - never mistaken for a UI panel
                img.sprite = Theme.GlowSprite; img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.color = new Color(0.62f, 0.70f, 0.85f, UnityEngine.Random.Range(0.020f, 0.045f));
                float w = UnityEngine.Random.Range(280f, 620f);
                m.sizeDelta = new Vector2(w, w * 0.8f);
                m.anchorMin = m.anchorMax = new Vector2(0.5f, 0.5f);
                m.anchoredPosition = new Vector2(UnityEngine.Random.Range(-1100f, 1100f), UnityEngine.Random.Range(-800f, 800f));
                _motes.Add(m);
                _vel.Add(new Vector2(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(4f, 10f)));
            }
            Theme.Vignette(rt);
        }

        private void Update()
        {
            for (int i = 0; i < _motes.Count; i++)
            {
                var p = _motes[i].anchoredPosition + _vel[i] * Time.unscaledDeltaTime;
                if (p.y > 900f) p = new Vector2(UnityEngine.Random.Range(-1100f, 1100f), -900f);
                _motes[i].anchoredPosition = p;
            }
        }
    }
}
