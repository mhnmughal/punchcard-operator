using UnityEngine;

namespace PunchcardOperator
{
    /// <summary>Keeps a RectTransform inside the device safe area (notches, rounded
    /// corners, home indicator). Re-applies on orientation / resolution change.</summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;

        private void OnEnable()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != _lastSafe || screen != _lastScreen)
                Apply();
        }

        private void Apply()
        {
            if (_rt == null) _rt = (RectTransform)transform;
            _lastSafe = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (!TryComputeAnchors(Screen.safeArea, Screen.width, Screen.height, out var min, out var max))
                return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }

        /// <summary>Pure conversion of a pixel safe-area rect to normalised canvas anchors.
        /// Returns false (and leaves anchors untouched) for a degenerate or out-of-range
        /// input. Exposed for testing.</summary>
        public static bool TryComputeAnchors(Rect safe, int screenW, int screenH, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.one;
            if (screenW <= 0 || screenH <= 0) return false;

            min = new Vector2(safe.xMin / screenW, safe.yMin / screenH);
            max = new Vector2(safe.xMax / screenW, safe.yMax / screenH);

            if (min.x < 0f || min.y < 0f || max.x > 1f || max.y > 1f) return false;
            if (max.x - min.x < 0.5f || max.y - min.y < 0.5f) return false; // implausible inset
            return true;
        }
    }
}
