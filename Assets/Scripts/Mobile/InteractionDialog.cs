using UnityEngine;

namespace OnLinces
{
    /// El diálogo sigue al interactuable, dentro de la pantalla. El aviso es el botón para usarlo.
    public sealed class InteractionDialog : MonoBehaviour
    {
        const float AboveObject = 0.95f; // unidades del mundo
        const float Gap = 3f;
        const float Margin = 6f;

        [SerializeField] RectTransform prompt;
        [SerializeField] RectTransform info;

        RectTransform area;
        Camera cam;

        void Awake() => area = (RectTransform)transform;

        /// Botón del aviso.
        public void Use() => PlayerInteractor.Tap();

        void LateUpdate()
        {
            var target = PlayerInteractor.Current;
            if (target == null || prompt == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector2 screen = cam.WorldToScreenPoint(target.Position + Vector3.up * AboveObject);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var p)) return;

            bool hasInfo = info != null && info.gameObject.activeSelf;
            float promptH = prompt.rect.height;
            float infoH = hasInfo ? info.rect.height + Gap : 0f;
            float halfW = Mathf.Max(prompt.rect.width, hasInfo ? info.rect.width : 0f) * 0.5f;

            var r = area.rect;
            float x = Mathf.Clamp(p.x, r.xMin + halfW + Margin, r.xMax - halfW - Margin);
            float y = Mathf.Clamp(p.y, r.yMin + Margin, r.yMax - promptH - infoH - Margin);

            prompt.anchoredPosition = new Vector2(x, y);
            if (hasInfo) info.anchoredPosition = new Vector2(x, y + promptH + Gap);
        }
    }
}
