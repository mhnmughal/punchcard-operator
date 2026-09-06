using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>Drives one stage: builds the machine floor + punch card, handles card
    /// editing with the Tool Dock, loads and executes the card, animates the reader,
    /// scores the run and persists progress. Attach to a full-screen RectTransform that
    /// sits under a Canvas (with a SafeAreaPanel above it).</summary>
    public class GameController : MonoBehaviour
    {
        private enum Phase { Editing, Executing, Result }

        private RectTransform _root;
        private PunchCardLevelDefinition _def;
        private CardEditSession _session;
        private Phase _phase;

        // chrome
        private RectTransform _boardArea, _cardList, _toolRow;
        private TextMeshProUGUI _objective, _editCounter, _toast, _coach;
        private RectTransform _coachRing;
        private GameObject _coachBar;
        private Button _leverBtn;
        private TextMeshProUGUI _leverLabel;
        private RectTransform _overlay;

        // board
        private float _cell;
        private Vector2 _boardOrigin;
        private readonly Dictionary<Vector2Int, Image> _cells = new Dictionary<Vector2Int, Image>();
        private RectTransform _carrierA, _carrierB;

        // card
        private readonly List<CardRowWidget> _rowWidgets = new List<CardRowWidget>();

        // interaction
        private CardTool _tool;
        private bool _hasTool;
        private int _firstRow = -1;
        private bool _loaded;
        private float _toastUntil;

        // tutorial
        private int _coachStep;
        private bool _tutorialPunchDone;

        private class CardRowWidget
        {
            public int index;
            public Button button;
            public Image background;
            public TextMeshProUGUI opText;
            public Image hole;
            public TextMeshProUGUI lockGlyph;
        }

        private void Start()
        {
            _root = (RectTransform)transform;
            _def = AppState.PendingLevel;
            if (_def == null)
            {
                Theme.Label("NoLevel", _root, "NO LEVEL DATA\n(run Punchcard Operator/Rebuild All Levels)", 34, Theme.Bad);
                return;
            }
            _session = new CardEditSession(_def.initialCard);
            BuildChrome();
            RebuildCard();
            RebuildToolDock();
            _phase = Phase.Editing;
            RefreshLever();
            StartCoroutine(DeferredBoard());
            Fade.In();
        }

        private IEnumerator DeferredBoard()
        {
            yield return null; // let the canvas resolve BoardArea's size
            Canvas.ForceUpdateCanvases();
            RebuildBoard();
            if (_def.isTutorial) AdvanceCoach(0);
        }

        private void Update()
        {
            if (_toast != null && Time.unscaledTime > _toastUntil && _toast.text.Length > 0)
                _toast.text = "";
            if (_coachRing != null && _coachRing.gameObject.activeSelf)
            {
                float s = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
                _coachRing.localScale = new Vector3(s, s, 1f);
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && _def != null)
                SetPaused(!_paused);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && !_paused && _def != null && _phase == Phase.Editing) SetPaused(true);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && !_paused && _def != null && _phase == Phase.Editing) SetPaused(true);
        }

        // ---------------------------------------------------------------- pause

        private bool _paused;
        private GameObject _pauseOverlay;

        private void SetPaused(bool p)
        {
            if (p == _paused || _def == null) return;
            if (p && _phase == Phase.Executing) return; // don't pause mid-run
            _paused = p;
            Time.timeScale = p ? 0f : 1f;
            Sfx.Play("ui");
            if (!p) { if (_pauseOverlay != null) Destroy(_pauseOverlay); _pauseOverlay = null; return; }

            var dim = Theme.Panelled("PauseOverlay", _root, new Color(0.02f, 0.025f, 0.03f, 0.82f));
            _pauseOverlay = dim.gameObject;
            Theme.Stretch((RectTransform)dim.transform);
            dim.gameObject.AddComponent<Button>();
            dim.transform.SetAsLastSibling();

            var panel = Theme.Raised("PausePanel", dim.transform, Theme.Panel);
            ((RectTransform)panel.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 520), Vector2.zero);
            Theme.Bolts(panel.transform);
            Theme.Label("h", panel.transform, "PAUSED", Theme.H1, Theme.Amber, TextAlignmentOptions.Top)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-40, 56), new Vector2(0, -24));

            var col = Theme.Rect("col", panel.transform);
            col.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 300), new Vector2(0, -14));
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = vlg.childForceExpandWidth = true;

            Theme.TextButton("Resume", col, "RESUME", Theme.Good, Theme.PanelDark, () => SetPaused(false));
            Theme.TextButton("Restart", col, "RESTART PROGRAM", Theme.PanelLight, Theme.Ink, () =>
            {
                Time.timeScale = 1f;
                LoadLevel(_def.id);
            });

            // sound slider
            var srow = Theme.Raised("Snd", col, Theme.PanelDark);
            Theme.Label("sl", srow.transform, "SOUND", Theme.Small, Theme.Ink, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(16, 0));
            var track = Theme.Panelled("track", srow.transform, Theme.Panel);
            ((RectTransform)track.transform).Set(new Vector2(0.42f, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-24, 16), new Vector2(-12, 0));
            var sGo = new GameObject("slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(track.transform, false);
            Theme.Stretch((RectTransform)sGo.transform);
            var fill = Theme.Panelled("fill", sGo.transform, Theme.Amber);
            Theme.Stretch((RectTransform)fill.transform);
            var s = sGo.GetComponent<Slider>();
            s.minValue = 0; s.maxValue = 1; s.value = SaveSystem.Data.sfxVolume;
            s.targetGraphic = fill; s.fillRect = (RectTransform)fill.transform;
            s.onValueChanged.AddListener(v => { SaveSystem.Data.sfxVolume = v; SaveSystem.Data.masterVolume = 1f; SaveSystem.Save(); });

            Theme.TextButton("Quit", col, "QUIT TO CARD VAULT", Theme.PanelLight, Theme.InkDim, () =>
            {
                Time.timeScale = 1f;
                AppState.MenuTarget = _def.isTutorial ? "manual" : "vault";
                Fade.ToScene("Menu");
            });
        }

        private void OnDestroy()
        {
            if (_paused) Time.timeScale = 1f;
        }

        // ---------------------------------------------------------------- chrome

        private Image _lampPower, _lampReader, _lampRun;
        private RectTransform _punchHead;

        private static void Bolts(Transform parent, float inset = 10f)
        {
            var corners = new[]
            {
                (new Vector2(0, 0), new Vector2(inset, inset)),
                (new Vector2(0, 1), new Vector2(inset, -inset)),
                (new Vector2(1, 0), new Vector2(-inset, inset)),
                (new Vector2(1, 1), new Vector2(-inset, -inset)),
            };
            foreach (var (anchor, pos) in corners)
            {
                var b = Theme.Panelled("bolt", parent, new Color32(0x10, 0x12, 0x15, 0xFF));
                ((RectTransform)b.transform).Set(anchor, anchor, new Vector2(0.5f, 0.5f), new Vector2(9, 9), pos);
                var o = b.gameObject.AddComponent<Outline>();
                o.effectColor = new Color32(0x3E, 0x44, 0x4E, 0xFF); o.effectDistance = new Vector2(1, 1);
                b.raycastTarget = false;
            }
        }

        private static void Raise(Image img, Color? edge = null)
        {
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = edge ?? new Color32(0x3A, 0x40, 0x4A, 0xFF);
            o.effectDistance = new Vector2(2, -2);
        }

        private Image Lamp(Transform parent, string label, float xFromRight)
        {
            var housing = Theme.Panelled($"lamp_{label}", parent, Theme.PanelDark);
            ((RectTransform)housing.transform).Set(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(92, 24), new Vector2(xFromRight, -60));
            var dot = Theme.Panelled("d", housing.transform, new Color32(0x33, 0x2A, 0x22, 0xFF));
            ((RectTransform)dot.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(11, 11), new Vector2(10, 0));
            Theme.Label("t", housing.transform, label, 12, Theme.InkDim, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-26, 0), new Vector2(26, 0));
            housing.raycastTarget = false;
            return dot;
        }

        private void BuildChrome()
        {
            var bg = Theme.Panelled("Backplate", _root, Theme.PanelDark);
            Theme.Stretch((RectTransform)bg.transform);
            // faint machine-panel seams
            for (int i = 1; i < 4; i++)
            {
                var seam = Theme.Panelled("seam", bg.transform, new Color(0, 0, 0, 0.18f));
                ((RectTransform)seam.transform).Set(new Vector2(i / 4f, 0), new Vector2(i / 4f, 1), new Vector2(0.5f, 0.5f), new Vector2(2, 0), Vector2.zero);
                seam.raycastTarget = false;
            }

            // Status Rail (top)
            var rail = Theme.Panelled("StatusRail", _root, Theme.Panel);
            Raise(rail);
            Bolts(rail.transform, 12f);
            var accent = Theme.Panelled("railAccent", rail.transform, Theme.Amber);
            ((RectTransform)accent.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-8, 3), new Vector2(0, 0));
            accent.raycastTarget = false;
            var rrt = (RectTransform)rail.transform;
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.sizeDelta = new Vector2(0, 132);
            rrt.anchoredPosition = Vector2.zero;

            _lampPower = Lamp(rail.transform, "PWR", -314);
            _lampReader = Lamp(rail.transform, "RDR", -214);
            _lampRun = Lamp(rail.transform, "RUN", -114);
            _lampPower.color = Theme.Good; // powered on

            var title = Theme.Label("Title", rail.transform, $"PROGRAM {_def.id}  —  {_def.displayName}", 30, Theme.Amber, TextAlignmentOptions.TopLeft);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(-40, 40); trt.anchoredPosition = new Vector2(20, -12);

            _objective = Theme.Label("Objective", rail.transform, _def.machineObjective, 21, Theme.Ink, TextAlignmentOptions.TopLeft);
            var ort = (RectTransform)_objective.transform;
            ort.anchorMin = new Vector2(0, 0); ort.anchorMax = new Vector2(1, 1); ort.offsetMin = new Vector2(20, 8); ort.offsetMax = new Vector2(-430, -50);

            _editCounter = Theme.Label("EditCounter", rail.transform, "", 20, Theme.InkDim, TextAlignmentOptions.TopRight);
            var crt = (RectTransform)_editCounter.transform;
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(1, 1);
            crt.sizeDelta = new Vector2(520, 28); crt.anchoredPosition = new Vector2(-20, -12);

            Theme.TextButton("BackBtn", rail.transform, "PAUSE", Theme.PanelLight, Theme.Ink, () => SetPaused(true))
                .GetComponent<RectTransform>().Set(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(170, 44), new Vector2(-20, 10));

            // Board area (left ~62%) — recessed machine housing
            _boardArea = Theme.Rect("BoardArea", _root);
            _boardArea.anchorMin = new Vector2(0, 0); _boardArea.anchorMax = new Vector2(0.62f, 1);
            _boardArea.offsetMin = new Vector2(20, 190); _boardArea.offsetMax = new Vector2(-10, -150);
            var floor = _boardArea.gameObject.AddComponent<Image>();
            floor.color = new Color32(0x1A, 0x1E, 0x23, 0xFF);
            Raise(floor, new Color32(0x0C, 0x0E, 0x11, 0xFF));
            Bolts(_boardArea, 14f);
            Theme.Label("FloorTag", _boardArea, "AUTOMATION FLOOR", 14, Theme.InkDim, TextAlignmentOptions.BottomLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(-24, 20), new Vector2(16, 8));

            // Card panel (right) — the card reader
            var cardPanel = Theme.Panelled("CardPanel", _root, Theme.PanelLight);
            var cprt = (RectTransform)cardPanel.transform;
            cprt.anchorMin = new Vector2(0.62f, 0); cprt.anchorMax = new Vector2(1, 1);
            cprt.offsetMin = new Vector2(10, 190); cprt.offsetMax = new Vector2(-20, -150);
            Raise(cardPanel);
            Bolts(cardPanel.transform, 12f);

            var readerHead = Theme.Panelled("ReaderHead", cardPanel.transform, Theme.PanelDark);
            ((RectTransform)readerHead.transform).Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 52), new Vector2(0, 0));
            Theme.Label("CardHdr", readerHead.transform, "CARD READER", 18, Theme.Amber, TextAlignmentOptions.Left)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-24, 0), new Vector2(18, 0));
            var slotMouth = Theme.Panelled("slot", readerHead.transform, new Color32(0x0C, 0x0E, 0x11, 0xFF));
            ((RectTransform)slotMouth.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-24, 6), new Vector2(0, 0));

            // punch-head assembly — drops onto a row when you punch
            _punchHead = Theme.Rect("PunchHead", cardPanel.transform);
            _punchHead.Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(46, 120), new Vector2(0, 40));
            var phBar = _punchHead.gameObject.AddComponent<Image>(); phBar.color = Theme.Amber; phBar.raycastTarget = false;
            var phTip = Theme.Panelled("tip", _punchHead, Theme.PanelDark);
            ((RectTransform)phTip.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(20, 16), new Vector2(0, 0));
            _punchHead.gameObject.SetActive(false);

            _cardList = Theme.Rect("CardList", cardPanel.transform);
            _cardList.anchorMin = new Vector2(0, 0); _cardList.anchorMax = new Vector2(1, 1);
            _cardList.offsetMin = new Vector2(14, 14); _cardList.offsetMax = new Vector2(-14, -64);
            var vlg = _cardList.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

            // Tool Dock (bottom) — physical dock
            var dock = Theme.Panelled("ToolDock", _root, Theme.Panel);
            Raise(dock);
            Bolts(dock.transform, 12f);
            var drt = (RectTransform)dock.transform;
            drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(1, 0); drt.pivot = new Vector2(0.5f, 0);
            drt.sizeDelta = new Vector2(0, 168); drt.anchoredPosition = Vector2.zero;
            Theme.Label("DockTag", dock.transform, "TOOL DOCK", 13, Theme.InkDim, TextAlignmentOptions.TopLeft)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, 20), new Vector2(20, -2));

            _toolRow = Theme.Rect("ToolRow", dock.transform);
            _toolRow.anchorMin = new Vector2(0, 0); _toolRow.anchorMax = new Vector2(0.66f, 1);
            _toolRow.offsetMin = new Vector2(20, 16); _toolRow.offsetMax = new Vector2(-10, -22);
            var hlg = _toolRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            _leverBtn = Theme.TextButton("Lever", dock.transform, "LOAD CARD", Theme.Amber, Theme.PanelDark, OnLever);
            ((RectTransform)_leverBtn.transform).Set(new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Theme.Stretch((RectTransform)_leverBtn.transform, 14);
            var knob = Theme.Panelled("knob", _leverBtn.transform, Theme.PanelDark);
            ((RectTransform)knob.transform).Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(40, 14), new Vector2(0, 6));
            knob.raycastTarget = false;
            _leverLabel = _leverBtn.GetComponentInChildren<TextMeshProUGUI>();

            // toast + coach
            _toast = Theme.Label("Toast", _root, "", 22, Theme.Amber);
            ((RectTransform)_toast.transform).Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-40, 34), new Vector2(0, 178));

            var coachBar = Theme.Panelled("CoachBar", _root, Theme.Amber);
            ((RectTransform)coachBar.transform).Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(820, 76), new Vector2(0, -146));
            coachBar.transform.SetAsLastSibling();
            _coach = Theme.Label("CoachText", coachBar.transform, "", 24, Theme.PanelDark, TextAlignmentOptions.Center);
            Theme.Stretch((RectTransform)_coach.transform, 12);
            _coachBar = coachBar.gameObject;
            _coachBar.SetActive(false);

            _coachRing = Theme.Rect("CoachRing", _root);
            var ringImg = _coachRing.gameObject.AddComponent<Image>();
            ringImg.color = new Color(Theme.Amber.r, Theme.Amber.g, Theme.Amber.b, 0.04f);
            ringImg.raycastTarget = false;
            var ringOutline = _coachRing.gameObject.AddComponent<Outline>();
            ringOutline.effectColor = Theme.Amber;
            ringOutline.effectDistance = new Vector2(5, 5);
            _coachRing.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- board

        private void RebuildBoard()
        {
            if (_coachRing != null && _coachRing.IsChildOf(_boardArea))
            {
                _coachRing.SetParent(_root, false);
                _coachRing.gameObject.SetActive(false);
            }
            foreach (Transform c in _boardArea) Destroy(c.gameObject);
            _cells.Clear();

            var g = _def.grid;
            var size = _boardArea.rect.size;
            _cell = Mathf.Floor(Mathf.Min((size.x - 60) / g.width, (size.y - 60) / g.height));
            _cell = Mathf.Max(_cell, 24f);
            float boardW = _cell * g.width, boardH = _cell * g.height;
            _boardOrigin = new Vector2(-boardW / 2f, -boardH / 2f);

            // recessed board frame
            var frame = Theme.Panelled("BoardFrame", _boardArea, Theme.PanelDark);
            ((RectTransform)frame.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(boardW + 28, boardH + 28), Vector2.zero);
            var fo = frame.gameObject.AddComponent<Outline>();
            fo.effectColor = new Color32(0x3A, 0x40, 0x4A, 0xFF); fo.effectDistance = new Vector2(2, 2);

            for (int y = 0; y < g.height; y++)
            for (int x = 0; x < g.width; x++)
            {
                var p = new Vector2Int(x, y);
                bool wall = g.IsWall(p);
                var img = Theme.Panelled($"Cell_{x}_{y}", _boardArea, CellColor(p));
                var rt = (RectTransform)img.transform;
                rt.sizeDelta = new Vector2(_cell - 3, _cell - 3);
                rt.anchoredPosition = CellPos(p);
                if (wall)
                {
                    var wo = img.gameObject.AddComponent<Outline>();
                    wo.effectColor = new Color32(0x0C, 0x0E, 0x11, 0xFF); wo.effectDistance = new Vector2(2, -2);
                    var hatch = Theme.Panelled("x", img.transform, new Color32(0x33, 0x2D, 0x25, 0xFF));
                    ((RectTransform)hatch.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(_cell * 0.34f, _cell * 0.34f), Vector2.zero);
                    hatch.transform.localEulerAngles = new Vector3(0, 0, 45f);
                }
                _cells[p] = img;
            }

            MarkTarget(_def.machineA, Theme.Blue);
            if (_def.DualChannel) MarkTarget(_def.machineB, Theme.Amber);

            _carrierA = MakeCarrier("CarrierA", Theme.Blue);
            PlaceCarrier(_carrierA, _def.machineA.start, _def.machineA.startFacing);
            if (_def.DualChannel)
            {
                _carrierB = MakeCarrier("CarrierB", Theme.Amber);
                PlaceCarrier(_carrierB, _def.machineB.start, _def.machineB.startFacing);
            }
        }

        private Color CellColor(Vector2Int p) => _def.grid.IsWall(p) ? Theme.PanelDark : new Color32(0x25, 0x2A, 0x31, 0xFF);

        private Vector2 CellPos(Vector2Int p) =>
            _boardOrigin + new Vector2((p.x + 0.5f) * _cell, (p.y + 0.5f) * _cell);

        private void MarkTarget(MachineConfig m, Color c)
        {
            if (_cells.TryGetValue(m.target, out var img))
                img.color = Color.Lerp(CellColor(m.target), c, 0.16f);
            var ring = Theme.Panelled("TargetRing", _boardArea, new Color(c.r, c.g, c.b, 0.06f));
            var outline = ring.gameObject.AddComponent<Outline>();
            outline.effectColor = c; outline.effectDistance = new Vector2(3, 3);
            var rt = (RectTransform)ring.transform;
            rt.sizeDelta = new Vector2(_cell - 6, _cell - 6);
            rt.anchoredPosition = CellPos(m.target);
            string tag = _def.DualChannel ? (c == Theme.Blue ? "A" : "B") : "TRAY";
            Theme.Label("T", ring.transform, tag, Mathf.Max(12f, _cell * 0.26f), c).GetComponent<RectTransform>()
                .Set(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private RectTransform MakeCarrier(string name, Color c)
        {
            var img = Theme.Panelled(name, _boardArea, c);
            var rt = (RectTransform)img.transform;
            rt.sizeDelta = new Vector2(_cell * 0.62f, _cell * 0.62f);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.45f); o.effectDistance = new Vector2(2, -3);
            // facing wedge: a small square rotated 45°, poking out the front edge
            var wedge = Theme.Panelled(name + "_dir", img.transform, Theme.PanelDark);
            ((RectTransform)wedge.transform).Set(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(_cell * 0.22f, _cell * 0.22f), new Vector2(0, 2));
            wedge.transform.localEulerAngles = new Vector3(0, 0, 45f);
            if (_def.DualChannel)
                Theme.Label(name + "_id", img.transform, c == Theme.Blue ? "A" : "B", Mathf.Max(10f, _cell * 0.2f), new Color(0,0,0,0.5f))
                    .GetComponent<RectTransform>().Set(new Vector2(1,1), new Vector2(1,1), new Vector2(1,1), new Vector2(18,18), new Vector2(-2,-2));
            return rt;
        }

        private void PlaceCarrier(RectTransform carrier, Vector2Int pos, Facing f)
        {
            carrier.anchoredPosition = CellPos(pos);
            carrier.localEulerAngles = new Vector3(0, 0, FacingAngle(f));
        }

        private static float FacingAngle(Facing f)
        {
            switch (f)
            {
                case Facing.North: return 0;
                case Facing.West: return 90;
                case Facing.South: return 180;
                default: return -90;
            }
        }

        // ---------------------------------------------------------------- card

        private void RebuildCard()
        {
            foreach (Transform c in _cardList) Destroy(c.gameObject);
            _rowWidgets.Clear();
            var card = _session.Card;

            for (int i = 0; i < card.rows.Count; i++)
            {
                int idx = i;
                var rowImg = Theme.Panelled($"Row{i}", _cardList, Theme.Card);
                var btn = rowImg.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnRowTapped(idx));

                var num = Theme.Label("num", rowImg.transform, (i + 1).ToString("00"), 18, new Color32(0x5A, 0x4E, 0x36, 0xFF), TextAlignmentOptions.Left);
                ((RectTransform)num.transform).Set(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(46, 0), new Vector2(10, 0));

                var hole = Theme.Panelled("hole", rowImg.transform, Theme.PanelDark);
                ((RectTransform)hole.transform).Set(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 24), new Vector2(64, 0));

                var op = Theme.Label("op", rowImg.transform, "", 26, Theme.PanelDark, TextAlignmentOptions.Left);
                ((RectTransform)op.transform).Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(-110, 0), new Vector2(100, 0));

                var lockG = Theme.Label("lock", rowImg.transform, "LOCKED", 13, Theme.CardEdge, TextAlignmentOptions.Right);
                ((RectTransform)lockG.transform).Set(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(84, 0), new Vector2(-10, 0));

                _rowWidgets.Add(new CardRowWidget { index = i, button = btn, background = rowImg, opText = op, hole = hole, lockGlyph = lockG });
            }
            RefreshCardVisuals();
        }

        private void RefreshCardVisuals()
        {
            var card = _session.Card;
            for (int i = 0; i < _rowWidgets.Count; i++)
            {
                var w = _rowWidgets[i];
                var row = card.rows[i];
                w.opText.text = OpText(row);
                w.hole.color = row.IsEmpty ? new Color(0, 0, 0, 0.12f) : Theme.PanelDark;
                w.lockGlyph.gameObject.SetActive(row.locked);
                bool sel = _firstRow == i;
                Color baseC = _def.DualChannel
                    ? Color.Lerp(Theme.Card, row.channel == Channel.A ? Theme.Blue : Theme.Amber, 0.34f)
                    : Theme.Card;
                if (row.locked) baseC = Color.Lerp(baseC, Theme.PanelLight, 0.35f);
                w.background.color = sel ? Color.Lerp(baseC, Theme.Good, 0.5f) : baseC;
            }
            _editCounter.text = _def.isTutorial
                ? $"EDITS {_session.EditCount}"
                : $"EDITS {_session.EditCount}      PERFECT {_def.perfectEditTarget}   COMPACT {_def.compactEditTarget}";
        }

        private static string OpText(CardRow r)
        {
            switch (r.op)
            {
                case CardOp.Move: return "MOVE";
                case CardOp.TurnLeft: return "LEFT";
                case CardOp.TurnRight: return "RIGHT";
                case CardOp.Repeat: return $"REPEAT ×{r.repeatCount}";
                default: return "—";
            }
        }

        // ---------------------------------------------------------------- tools

        private void RebuildToolDock()
        {
            foreach (Transform c in _toolRow) Destroy(c.gameObject);
            foreach (var t in _def.availableTools)
            {
                var tool = t;
                var b = Theme.TextButton($"Tool_{t}", _toolRow, t.ToString().ToUpper(), Theme.PanelLight, Theme.Ink,
                    () => SelectTool(tool));
                b.name = $"Tool_{t}";
            }
            RefreshToolVisuals();
        }

        private void SelectTool(CardTool t)
        {
            if (_phase != Phase.Editing) return;
            _tool = t; _hasTool = true; _firstRow = -1;
            Sfx.Play("ui");
            Toast($"{t.ToString().ToUpper()} SELECTED — tap a card row");
            RefreshToolVisuals();
            RefreshCardVisuals();
            if (_def.isTutorial && t == CardTool.Punch) TryAdvanceCoachByKeyword("PUNCH TOOL");
        }

        private void RefreshToolVisuals()
        {
            foreach (Transform c in _toolRow)
            {
                var img = c.GetComponent<Image>();
                bool sel = _hasTool && c.name == $"Tool_{_tool}";
                img.color = sel ? Theme.Amber : Theme.PanelLight;
                var lbl = c.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.color = sel ? Theme.PanelDark : Theme.Ink;
            }
        }

        private void OnRowTapped(int idx)
        {
            if (_phase != Phase.Editing) return;
            if (!_hasTool) { Toast("PICK A TOOL FROM THE DOCK FIRST"); return; }

            switch (_tool)
            {
                case CardTool.Flip:
                    _session.Flip(out _);
                    Sfx.Play("flip");
                    Toast(_session.Card.flipped ? "CARD FLIPPED — reader runs bottom to top" : "CARD BACK TO NORMAL");
                    AfterEdit();
                    break;
                case CardTool.Punch:
                case CardTool.Patch:
                    ShowOpPicker(idx);
                    break;
                case CardTool.Shift:
                case CardTool.Swap:
                case CardTool.Copy:
                    if (_firstRow < 0) { _firstRow = idx; Toast("NOW TAP THE SECOND ROW"); RefreshCardVisuals(); }
                    else { ApplyTwoRow(_firstRow, idx); _firstRow = -1; }
                    break;
            }
        }

        private void ApplyTwoRow(int a, int b)
        {
            bool ok; string err;
            switch (_tool)
            {
                case CardTool.Shift: ok = _session.Shift(a, b, out err); if (ok) Sfx.Play("shift"); break;
                case CardTool.Swap: ok = _session.Swap(a, b, out err); if (ok) Sfx.Play("swap"); break;
                default: ok = _session.Copy(a, b, out err); if (ok) Sfx.Play("copy"); break;
            }
            if (!ok) { Sfx.Play("error"); Toast(err); }
            AfterEdit();
        }

        private RectTransform _opPicker;

        private void ShowOpPicker(int idx)
        {
            if (_opPicker != null) Destroy(_opPicker.gameObject);
            var panel = Theme.Panelled("OpPicker", _root, Theme.PanelDark);
            _opPicker = (RectTransform)panel.transform;
            _opPicker.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 360), Vector2.zero);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.Amber; outline.effectDistance = new Vector2(2, 2);
            Theme.Label("h", panel.transform, $"{_tool.ToString().ToUpper()}  ROW {idx + 1:00}", 24, Theme.Amber, TextAlignmentOptions.Top)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(0, -12));

            var grid = Theme.Rect("grid", panel.transform);
            grid.Set(new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Theme.Stretch(grid, 20); grid.offsetMax = new Vector2(-20, -56);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(228, 66); g.spacing = new Vector2(12, 12);

            int repeat = 2;
            void Mk(string label, CardOp op)
            {
                Theme.TextButton(label, grid, label, Theme.PanelLight, Theme.Ink, () =>
                {
                    bool ok; string err;
                    if (_tool == CardTool.Punch) ok = _session.Punch(idx, op, repeat, out err);
                    else ok = _session.Patch(idx, op, repeat, out err);
                    if (ok)
                    {
                        Sfx.Play(_tool == CardTool.Punch ? "punch" : "patch");
                        StartCoroutine(PunchHeadDrop(idx));
                        if (_def.isTutorial && _tool == CardTool.Punch) { _tutorialPunchDone = true; TryAdvanceCoachByKeyword("PUNCH MOVE"); }
                    }
                    else { Sfx.Play("error"); Toast(err); }
                    Destroy(_opPicker.gameObject); _opPicker = null;
                    AfterEdit();
                });
            }
            Mk("MOVE", CardOp.Move);
            Mk("LEFT", CardOp.TurnLeft);
            Mk("RIGHT", CardOp.TurnRight);
            bool repeatUsed = _def.availableTools.Contains(CardTool.Punch) || _def.availableTools.Contains(CardTool.Patch);
            if (repeatUsed) Mk("REPEAT ×2", CardOp.Repeat);
            for (int r = 3; r <= 6; r++)
            {
                int rr = r;
                Theme.TextButton($"REPEAT ×{r}", grid, $"REPEAT ×{r}", Theme.PanelLight, Theme.Ink, () =>
                {
                    bool ok; string err;
                    if (_tool == CardTool.Punch) ok = _session.Punch(idx, CardOp.Repeat, rr, out err);
                    else ok = _session.Patch(idx, CardOp.Repeat, rr, out err);
                    if (ok) { Sfx.Play(_tool == CardTool.Punch ? "punch" : "patch"); StartCoroutine(PunchHeadDrop(idx)); }
                    else { Sfx.Play("error"); Toast(err); }
                    Destroy(_opPicker.gameObject); _opPicker = null;
                    AfterEdit();
                });
            }
            Theme.TextButton("CANCEL", panel.transform, "CANCEL", Theme.Bad, Theme.Ink, () => { Destroy(_opPicker.gameObject); _opPicker = null; })
                .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-40, 40), new Vector2(0, 12));
        }

        private IEnumerator PunchHeadDrop(int rowIndex)
        {
            if (_punchHead == null || rowIndex < 0 || rowIndex >= _rowWidgets.Count) yield break;
            var cardPanel = (RectTransform)_punchHead.parent;
            var rowRt = (RectTransform)_rowWidgets[rowIndex].background.transform;

            Vector2 RowLocalTop()
            {
                var world = rowRt.TransformPoint(new Vector3(0, rowRt.rect.height * 0.5f, 0));
                var screen = RectTransformUtility.WorldToScreenPoint(null, world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(cardPanel, screen, null, out var local);
                return local;
            }
            var target = RowLocalTop();
            _punchHead.gameObject.SetActive(true);
            _punchHead.anchoredPosition = new Vector2(target.x, target.y + 130);
            yield return Tween(0.09f, t => _punchHead.anchoredPosition = new Vector2(target.x, Mathf.Lerp(target.y + 130, target.y + 8, t)));
            Sfx.Play("chad", 1.4f);
            _rowWidgets[rowIndex].hole.color = Theme.Amber;
            yield return Tween(0.16f, t => _punchHead.anchoredPosition = new Vector2(target.x, Mathf.Lerp(target.y + 8, target.y + 130, t)));
            _punchHead.gameObject.SetActive(false);
            RefreshCardVisuals();
        }

        private void AfterEdit()
        {
            RefreshCardVisuals();
            RebuildBoardCarriersOnly();
            RefreshLever();
        }

        private void RebuildBoardCarriersOnly()
        {
            if (_carrierA != null) PlaceCarrier(_carrierA, _def.machineA.start, _def.machineA.startFacing);
            if (_carrierB != null) PlaceCarrier(_carrierB, _def.machineB.start, _def.machineB.startFacing);
        }

        // ---------------------------------------------------------------- lever / execute

        private static readonly Color LampOff = new Color32(0x33, 0x2A, 0x22, 0xFF);

        private void RefreshLever()
        {
            if (_lampReader != null) _lampReader.color = _loaded ? Theme.Good : LampOff;
            if (_lampRun != null) _lampRun.color = _phase == Phase.Executing ? Theme.Amber : LampOff;
            if (_phase == Phase.Executing) { _leverBtn.interactable = false; return; }
            _leverBtn.interactable = true;
            _leverLabel.text = _loaded ? "EXECUTE" : "LOAD CARD";
            var img = _leverBtn.GetComponent<Image>();
            img.color = _loaded ? Theme.Good : Theme.Amber;
        }

        private void OnLever()
        {
            if (_phase == Phase.Executing) return;
            if (!_loaded)
            {
                _loaded = true;
                Sfx.Play("load");
                Toast("CARD LOADED INTO READER");
                if (_def.isTutorial) TryAdvanceCoachByKeyword("LOAD");
                RefreshLever();
                return;
            }
            if (_def.isTutorial && !_tutorialPunchDone)
            {
                Toast("PUNCH THE MISSING INSTRUCTION FIRST");
                _loaded = false; RefreshLever();
                return;
            }
            StartCoroutine(ExecuteRoutine());
        }

        private IEnumerator ExecuteRoutine()
        {
            _phase = Phase.Executing;
            RefreshLever();
            Sfx.Play("execute");
            if (_def.isTutorial) TryAdvanceCoachByKeyword("EXECUTE");

            var result = PunchCardSimulator.Run(_def.grid, _def.machineA, _def.machineB, _session.Card);

            var posA = _def.machineA.start; var facA = _def.machineA.startFacing;
            var posB = _def.machineB.start; var facB = _def.machineB.startFacing;

            foreach (var step in result.steps)
            {
                HighlightRow(step.rowIndex);
                Sfx.Play("reader", 1f);
                var carrier = step.channel == Channel.A ? _carrierA : _carrierB;
                var fromPos = step.channel == Channel.A ? posA : posB;
                var fromFac = step.channel == Channel.A ? facA : facB;
                var toPos = step.channel == Channel.A ? step.posA : step.posB;
                var toFac = step.channel == Channel.A ? step.facingA : step.facingB;

                if (carrier != null)
                {
                    if (toFac != fromFac)
                    {
                        Sfx.Play("turn");
                        yield return Tween(0.18f, t =>
                            carrier.localEulerAngles = new Vector3(0, 0, Mathf.LerpAngle(FacingAngle(fromFac), FacingAngle(toFac), t)));
                    }
                    if (toPos != fromPos)
                    {
                        Sfx.Play("step");
                        var a = CellPos(fromPos); var b = CellPos(toPos);
                        yield return Tween(0.16f, t => carrier.anchoredPosition = Vector2.Lerp(a, b, t));
                    }
                }
                if (step.channel == Channel.A) { posA = toPos; facA = toFac; } else { posB = toPos; facB = toFac; }
                yield return new WaitForSecondsRealtime(0.05f);
            }

            ClearRowHighlights();
            if (_lampRun != null) _lampRun.color = LampOff;
            yield return new WaitForSecondsRealtime(0.25f);
            ShowResult(result);
        }

        private IEnumerator Tween(float dur, System.Action<float> apply)
        {
            float t = 0;
            while (t < dur) { t += Time.unscaledDeltaTime; apply(Mathf.Clamp01(t / dur)); yield return null; }
            apply(1f);
        }

        private void HighlightRow(int idx)
        {
            ClearRowHighlights();
            if (idx >= 0 && idx < _rowWidgets.Count)
                _rowWidgets[idx].background.color = Theme.Good;
        }

        private void ClearRowHighlights() => RefreshCardVisuals();

        // ---------------------------------------------------------------- result

        private void ShowResult(ExecutionResult result)
        {
            _phase = Phase.Result;
            if (_overlay != null) Destroy(_overlay.gameObject);
            var dim = Theme.Panelled("ResultOverlay", _root, new Color(0, 0, 0, 0.72f));
            _overlay = (RectTransform)dim.transform;
            Theme.Stretch(_overlay);
            dim.gameObject.AddComponent<Button>(); // swallow clicks

            var panel = Theme.Panelled("ResultPanel", _overlay, Theme.Panel);
            ((RectTransform)panel.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720, 420), Vector2.zero);
            var outline = panel.gameObject.AddComponent<Outline>();

            if (result.success)
            {
                var rating = RatingRules.Evaluate(true, _session.EditCount, _def.compactEditTarget, _def.perfectEditTarget);
                outline.effectColor = Theme.Good; outline.effectDistance = new Vector2(2, 2);
                Sfx.Play("success");

                if (_def.isTutorial)
                {
                    ProgressionManager.CompleteTutorial();
                    Header(panel, "TRAINING COMPLETE", Theme.Good);
                    Body(panel, "You operated the reader end to end. Program 01 is unlocked.");
                    Theme.TextButton("Start01", panel.transform, "START PROGRAM 01", Theme.Good, Theme.PanelDark, () => LoadLevel("01"))
                        .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(360, 60), new Vector2(0, 28));
                }
                else
                {
                    ProgressionManager.RecordCompletion(_def, rating, _session.EditCount, AppState.Database);
                    var next = NextLevelId();
                    if (next == null)
                    {
                        // Level 12 — the campaign finale
                        Destroy(_overlay.gameObject);
                        _overlay = null;
                        StartCoroutine(CertifiedFinale(rating));
                        return;
                    }
                    Header(panel, "PROGRAM ACCEPTED", Theme.Good);
                    Body(panel, $"{RatingRules.Label(rating)}\n{_session.EditCount} edit(s)   -   COMPACT {_def.compactEditTarget}   PERFECT {_def.perfectEditTarget}");
                    Theme.TextButton("Retry", panel.transform, "REPLAY", Theme.PanelLight, Theme.Ink, () => LoadLevel(_def.id))
                        .GetComponent<RectTransform>().Set(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(320, 58), new Vector2(30, 28));
                    Theme.TextButton("Next", panel.transform, "NEXT PROGRAM ›", Theme.Good, Theme.PanelDark, () => LoadLevel(next))
                        .GetComponent<RectTransform>().Set(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(320, 58), new Vector2(-30, 28));
                }
            }
            else
            {
                outline.effectColor = Theme.Bad; outline.effectDistance = new Vector2(2, 2);
                Sfx.Play("error");
                Header(panel, "PROGRAM HALTED", Theme.Bad);
                Body(panel, result.failureReason);
                Theme.TextButton("Retry", panel.transform, "RETRY", Theme.Amber, Theme.PanelDark, () =>
                {
                    Destroy(_overlay.gameObject); _overlay = null;
                    _session.Reset(); _loaded = false; _phase = Phase.Editing;
                    _firstRow = -1;
                    RebuildCard(); RebuildBoard(); RefreshLever();
                }).GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(300, 60), new Vector2(0, 28));
            }
        }

        private IEnumerator CertifiedFinale(LevelRating rating)
        {
            Sfx.Play("execute", 0.7f);
            var dim = Theme.Panelled("CertifiedOverlay", _root, new Color(0.02f, 0.03f, 0.04f, 0f));
            var drt = (RectTransform)dim.transform; Theme.Stretch(drt);
            dim.gameObject.AddComponent<Button>();
            yield return Tween(0.6f, t => dim.color = new Color(0.02f, 0.03f, 0.04f, 0.92f * t));

            // main indicator tower — a column of lamps that light bottom-up
            var tower = Theme.Rect("Tower", drt);
            tower.Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 300), new Vector2(0, 40));
            var lamps = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var l = Theme.Panelled($"lamp{i}", tower, Theme.PanelLight);
                ((RectTransform)l.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(100, 46), new Vector2(0, i * 56 + 8));
                lamps[i] = l;
            }
            for (int i = 0; i < 5; i++)
            {
                Sfx.Play("reader", 0.7f + i * 0.12f);
                yield return Tween(0.16f, t => lamps[i].color = Color.Lerp(Theme.PanelLight, Theme.Good, t));
                yield return new WaitForSecondsRealtime(0.06f);
            }

            var title = Theme.Label("CertTitle", drt, "SYSTEM CERTIFIED", 64, Theme.Good, TextAlignmentOptions.Center);
            ((RectTransform)title.transform).Set(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(1000, 90), new Vector2(0, -120));
            title.alpha = 0f;
            Sfx.Play("certified");
            yield return Tween(0.5f, t => title.alpha = t);

            // program banks illuminate one by one
            var bankRow = Theme.Rect("Banks", drt);
            bankRow.Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(760, 90), new Vector2(0, 200));
            var hlg = bankRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            var plates = new Image[3];
            string[] names = { "BANK A", "BANK B", "BANK C" };
            for (int i = 0; i < 3; i++)
            {
                var p = Theme.Panelled($"bank{i}", bankRow, Theme.PanelDark);
                var lab = Theme.Label("l", p.transform, names[i], 24, Theme.InkDim);
                Theme.Stretch((RectTransform)lab.transform);
                plates[i] = p;
                p.gameObject.AddComponent<PlateTag>().label = lab;
            }
            for (int i = 0; i < 3; i++)
            {
                Sfx.Play("success", 0.8f + i * 0.15f);
                var tag = plates[i].GetComponent<PlateTag>();
                yield return Tween(0.28f, t =>
                {
                    plates[i].color = Color.Lerp(Theme.PanelDark, Theme.Good, t);
                    tag.label.color = Color.Lerp(Theme.InkDim, Theme.PanelDark, t);
                });
            }

            // card reader final sweep
            var sweep = Theme.Panelled("ReaderSweep", drt, new Color(Theme.Good.r, Theme.Good.g, Theme.Good.b, 0.5f));
            var swrt = (RectTransform)sweep.transform;
            swrt.anchorMin = new Vector2(0, 0.5f); swrt.anchorMax = new Vector2(0, 0.5f);
            swrt.pivot = new Vector2(0.5f, 0.5f); swrt.sizeDelta = new Vector2(20, 260);
            Sfx.Play("reader", 1.3f);
            yield return Tween(0.5f, t => swrt.anchoredPosition = new Vector2(Mathf.Lerp(0, Screen.width, t), 60));
            Destroy(sweep.gameObject);

            // master status plate
            var master = Theme.Panelled("MasterPlate", drt, Theme.PanelDark);
            ((RectTransform)master.transform).Set(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560, 96), new Vector2(0, -110));
            var mlab = Theme.Label("ml", master.transform, $"MASTER PROGRAM — {RatingRules.Label(rating)}", 26, Theme.InkDim);
            Theme.Stretch((RectTransform)mlab.transform);
            var mo = master.gameObject.AddComponent<Outline>(); mo.effectColor = Theme.Good; mo.effectDistance = new Vector2(2, 2);
            Sfx.Play("certified", 0.8f);
            yield return Tween(0.4f, t => { master.color = Color.Lerp(Theme.PanelDark, new Color32(0x1b,0x2c,0x1e,0xFF), t); mlab.color = Color.Lerp(Theme.InkDim, Theme.Good, t); });

            yield return new WaitForSecondsRealtime(0.4f);
            Theme.TextButton("Review", drt, "REVIEW PROGRAMS", Theme.PanelLight, Theme.Ink, () => { AppState.MenuTarget = "vault"; GoMenu(); })
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(380, 60), new Vector2(-210, 60));
            Theme.TextButton("Control", drt, "RETURN TO CONTROL ROOM", Theme.Good, Theme.PanelDark, () => { AppState.MenuTarget = "control"; GoMenu(); })
                .GetComponent<RectTransform>().Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(420, 60), new Vector2(210, 60));
        }

        private void GoMenu()
        {
            Sfx.Play("ui");
            if (Application.CanStreamedLevelBeLoaded("Menu")) Fade.ToScene("Menu");
            else LoadLevel("00");
        }

        private class PlateTag : MonoBehaviour { public TextMeshProUGUI label; }

        private void Header(Image panel, string text, Color c) =>
            Theme.Label("h", panel.transform, text, 40, c, TextAlignmentOptions.Top)
                .GetComponent<RectTransform>().Set(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-40, 60), new Vector2(0, -30));

        private void Body(Image panel, string text) =>
            Theme.Label("b", panel.transform, text, 24, Theme.Ink)
                .GetComponent<RectTransform>().Set(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60, 180), new Vector2(0, 10));

        // ---------------------------------------------------------------- flow

        private string NextLevelId()
        {
            var campaign = AppState.Database.Campaign;
            for (int i = 0; i < campaign.Count; i++)
                if (campaign[i].id == _def.id && i + 1 < campaign.Count)
                    return campaign[i + 1].id;
            return null;
        }

        private void LoadLevel(string id)
        {
            AppState.PendingLevelId = id;
            Fade.ToScene(gameObject.scene.name);
        }

        private void GoBack()
        {
            Sfx.Play("ui");
            AppState.MenuTarget = _def.isTutorial ? "manual" : "vault";
            if (Application.CanStreamedLevelBeLoaded("Menu")) Fade.ToScene("Menu");
            else LoadLevel(_def.id);
        }

        private void Toast(string msg)
        {
            if (_toast == null) return;
            _toast.text = msg;
            _toastUntil = Time.unscaledTime + 2.6f;
        }

        // ---------------------------------------------------------------- coach lights

        private void AdvanceCoach(int step)
        {
            if (!_def.isTutorial || _def.tutorialSteps == null || _def.tutorialSteps.Count == 0) return;
            _coachStep = Mathf.Clamp(step, 0, _def.tutorialSteps.Count - 1);
            _coachBar.SetActive(true);
            _coach.text = _def.tutorialSteps[_coachStep];
            PositionCoachRing(_def.tutorialSteps[_coachStep]);
            Sfx.Play("ui", 1.2f);
        }

        private void TryAdvanceCoachByKeyword(string keyword)
        {
            if (!_def.isTutorial) return;
            // advance to the next step whose text follows the current one
            if (_coachStep < _def.tutorialSteps.Count - 1) AdvanceCoach(_coachStep + 1);
            else _coachBar.SetActive(false);
        }

        private void PositionCoachRing(string stepText)
        {
            if (_coachRing == null) return;
            string s = stepText.ToUpper();
            RectTransform target = null;
            if (s.Contains("PUNCH") && s.Contains("TOOL")) target = FindTool(CardTool.Punch);
            else if (s.Contains("PATCH")) target = FindTool(CardTool.Patch);
            else if (s.Contains("EXECUTE") || s.Contains("LEVER")) target = (RectTransform)_leverBtn.transform;
            else if (s.Contains("DROP THE CARD") || s.Contains("READER SLOT") || s.Contains("LOAD")) target = (RectTransform)_leverBtn.transform;
            else if (s.Contains("PUNCH CARD") || s.Contains("ROW")) target = _cardList;
            else if (s.Contains("BOARD") || s.Contains("CARRIER") || s.Contains("TRAY")) target = _boardArea;

            if (target == null) { _coachRing.gameObject.SetActive(false); return; }
            _coachRing.gameObject.SetActive(true);
            _coachRing.SetParent(target, false);
            _coachRing.anchorMin = Vector2.zero; _coachRing.anchorMax = Vector2.one;
            _coachRing.offsetMin = new Vector2(-8, -8); _coachRing.offsetMax = new Vector2(8, 8);
            _coachRing.SetAsLastSibling();
        }

        private RectTransform FindTool(CardTool t)
        {
            var tr = _toolRow.Find($"Tool_{t}");
            return tr as RectTransform;
        }
    }

    internal static class RectExtensions
    {
        public static void Set(this RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = anchoredPos;
        }
    }
}
