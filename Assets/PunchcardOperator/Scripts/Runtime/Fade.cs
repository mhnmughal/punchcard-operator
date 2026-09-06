using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PunchcardOperator
{
    /// <summary>Persistent full-screen fade used for every scene change, so screens
    /// cut to black and back rather than popping. Auto-created on first use.</summary>
    public class Fade : MonoBehaviour
    {
        private static Fade _instance;
        private CanvasGroup _group;
        private Image _img;

        /// <summary>Tests set this false to skip the visual fade and load scenes synchronously.</summary>
        public static bool Enabled = true;

        private static Fade Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("~Fade");
                DontDestroyOnLoad(go);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32760;
                _instance = go.AddComponent<Fade>();
                _instance._group = go.AddComponent<CanvasGroup>();
                _instance._group.blocksRaycasts = false;
                var imgGo = new GameObject("black", typeof(RectTransform));
                imgGo.transform.SetParent(go.transform, false);
                var rt = (RectTransform)imgGo.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                _instance._img = imgGo.AddComponent<Image>();
                _instance._img.color = new Color(0.02f, 0.025f, 0.03f);
                _instance._group.alpha = 0f;
                return _instance;
            }
        }

        /// <summary>Call from a scene controller's Start to fade up from black.</summary>
        public static void In(float duration = 0.35f)
        {
            if (!Enabled) return;
            var f = Instance;
            f.StopAllCoroutines();
            f._group.blocksRaycasts = true;
            f.StartCoroutine(f.Do(f._group.alpha <= 0f ? 1f : f._group.alpha, 0f, duration, () => f._group.blocksRaycasts = false));
        }

        /// <summary>Fade to black, then load the scene (which should fade itself back in).</summary>
        public static void ToScene(string scene, float duration = 0.3f)
        {
            if (!Enabled) { SceneManager.LoadScene(scene); return; }
            var f = Instance;
            f.StopAllCoroutines();
            f._group.blocksRaycasts = true;
            f.StartCoroutine(f.Do(f._group.alpha, 1f, duration, () => SceneManager.LoadScene(scene)));
        }

        private IEnumerator Do(float from, float to, float dur, System.Action then)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.SmoothStep(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            _group.alpha = to;
            then?.Invoke();
        }
    }
}
