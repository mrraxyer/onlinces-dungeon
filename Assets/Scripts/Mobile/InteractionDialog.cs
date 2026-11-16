using UnityEngine;

namespace OnLinces
{
    /// Solo en teléfono (lo agrega MobileHud): pone el diálogo encima de lo que tienes cerca
    /// (cofre, ítem, puesto, portal...) y lo sigue. Abajo va el aviso "USAR: ..." que se toca para usarlo,
    /// y arriba de él la descripción. Nunca se sale de la pantalla.
    public sealed class InteractionDialog : MonoBehaviour
    {
        const float AboveObject = 0.95f; // unidades del mundo arriba del objeto
        const float Gap = 3f;
        const float Margin = 6f;

        RectTransform area, prompt, info;
        Camera cam;

        public void Init(RectTransform prompt, RectTransform info)
        {
            area = (RectTransform)transform;
            this.prompt = prompt;
            this.info = info;
            foreach (var rt in new[] { prompt, info })
            {
                if (rt == null) continue;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0f);
            }
        }

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
