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
            var safe = Screen.safeArea;
            _lastSafe = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;

            if (min.x < 0 || min.y < 0 || max.x > 1 || max.y > 1) return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
