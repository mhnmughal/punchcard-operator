using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PunchcardOperator
{
    /// <summary>Startup screen: a short machine power-on, then routes to Operator
    /// Training on a first launch or to the Control Room otherwise. Tap to skip.</summary>
    public class BootController : MonoBehaviour
    {
        private RectTransform _root;
        private bool _done;

        private void Start()
        {
            _root = (RectTransform)transform;
            var bg = Theme.Panelled("bg", _root, Theme.PanelDark);
            Theme.Stretch((RectTransform)bg.transform);
            bg.gameObject.AddComponent<Button>().onClick.AddListener(Skip);

            var logo = Theme.Label("logo", _root, "PUNCHCARD\nOPERATOR", 72, Theme.Amber);
            var lrt = (RectTransform)logo.transform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(900, 240); lrt.anchoredPosition = new Vector2(0, 60);
            logo.alpha = 0f;

            var lamps = Theme.Rect("lamps", _root);
            lamps.anchorMin = lamps.anchorMax = lamps.pivot = new Vector2(0.5f, 0.5f);
            lamps.sizeDelta = new Vector2(360, 26); lamps.anchoredPosition = new Vector2(0, -110);
            var hlg = lamps.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14; hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            var dots = new Image[5];
            for (int i = 0; i < 5; i++)
                dots[i] = Theme.Panelled($"d{i}", lamps, new Color32(0x33, 0x2A, 0x22, 0xFF));

            var status = Theme.Label("status", _root, "", Theme.Small, Theme.InkDim);
            ((RectTransform)status.transform).Set(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(600, 30), new Vector2(0, 60));

            Fade.In(0.4f);
            StartCoroutine(Sequence(logo, dots, status));
        }

        private IEnumerator Sequence(TextMeshProUGUI logo, Image[] dots, TextMeshProUGUI status)
        {
            yield return new WaitForSecondsRealtime(0.15f);
            float t = 0;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; logo.alpha = Mathf.Clamp01(t / 0.5f); yield return null; }
            logo.alpha = 1f;

            status.text = "MACHINE POWER-ON";
            for (int i = 0; i < dots.Length; i++)
            {
                Sfx.Play("reader", 0.7f + i * 0.12f);
                dots[i].color = Theme.Good;
                yield return new WaitForSecondsRealtime(0.14f);
            }
            status.text = "SYSTEM READY";
            Sfx.Play("certified", 0.9f);
            yield return new WaitForSecondsRealtime(0.5f);
            Route();
        }

        private void Skip()
        {
            Sfx.Play("ui");
            Route();
        }

        private void Route()
        {
            if (_done) return;
            _done = true;
            if (!SaveSystem.Data.tutorialComplete)
            {
                AppState.PendingLevelId = "00";
                Fade.ToScene("Game");
            }
            else
            {
                AppState.MenuTarget = "control";
                Fade.ToScene("Menu");
            }
        }
    }
}
