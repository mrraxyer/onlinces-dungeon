using UnityEngine;

namespace OnLinces
{
    /// Ajusta el rect al área segura (sin notch).
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int appliedScreen;

        void OnEnable() => Apply();

        void Update()
        {
            // al girar
            if (Screen.safeArea != applied || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y) Apply();
        }

        void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            applied = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);

            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rt.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
