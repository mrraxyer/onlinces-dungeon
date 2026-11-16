using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Adapta el HUD de la escena al teléfono (en PC no cambia nada).
    public static class MobileHud
    {
        const string SafeName = "SafeArea";
        const float InfoWidth = 250f;

        // desde arriba a la derecha, a la izquierda del botón de pausa
        static readonly Vector2 MinimapCorner = new(-66f, -8f);
        static readonly Vector2 FeedbackCorner = new(-66f, -8f);
        static readonly Vector2 FeedbackCornerWithMinimap = new(-104f, -8f);
        static readonly Vector2 ToastBottom = new(-55f, 12f); // hueco entre palancas
        static readonly Color TapColor = new(1f, 0.86f, 0.5f);

        /// Más alto = HUD más chico.
        public static void Apply(float referenceHeight, Canvas touchCanvas)
        {
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>())
            {
                var canvas = scaler.GetComponent<Canvas>();
                if (canvas == null || canvas == touchCanvas || !canvas.isRootCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (canvas.transform.Find(SafeName) != null) continue; // ya hecho

                Proportional(scaler, referenceHeight);
                var safe = WrapInSafeArea(canvas.transform);

                var minimap = canvas.GetComponentInChildren<Minimap>(true);
                if (minimap != null) ToTopRight((RectTransform)minimap.transform, MinimapCorner);

                if (safe.Find("Feedback") is RectTransform feedback)
                    ToTopRight(feedback, minimap != null ? FeedbackCornerWithMinimap : FeedbackCorner);

                var prompt = safe.Find("PromptLabelPanel") as RectTransform;
                var info = safe.Find("InfoPanel") as RectTransform;
                if (info != null) info.sizeDelta = new Vector2(InfoWidth, info.sizeDelta.y);
                if (prompt != null)
                {
                    safe.gameObject.AddComponent<InteractionDialog>().Init(prompt, info);
                    MakeTappable(canvas, prompt, touchCanvas);
                }

                if (safe.Find("ToastLabelPanel") is RectTransform toast)
                {
                    toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
                    toast.pivot = new Vector2(0.5f, 0f);
                    toast.anchoredPosition = ToastBottom;
                }
            }
        }

        /// Escala proporcional; siempre caben 2h x h.
        public static void Proportional(CanvasScaler scaler, float referenceHeight)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(referenceHeight * 2f, referenceHeight);
        }

        /// El aviso se vuelve botón; el resto del HUD deja pasar los toques a las palancas.
        static void MakeTappable(Canvas canvas, RectTransform prompt, Canvas touchCanvas)
        {
            if (touchCanvas != null) canvas.sortingOrder = touchCanvas.sortingOrder + 1;
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            var img = prompt.GetComponent<Image>();
            img.raycastTarget = true;
            img.color = TapColor;
            var button = prompt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.6f, 0.3f);
            button.colors = colors;
            button.onClick.AddListener(PlayerInteractor.Tap);

            // más grande para el dedo
            if (prompt.GetComponent<HorizontalLayoutGroup>() is { } layout)
                layout.padding = new RectOffset(layout.padding.left + 6, layout.padding.right + 6, layout.padding.top + 4, layout.padding.bottom + 4);
        }

        /// Mete los hijos del Canvas en un área segura.
        static RectTransform WrapInSafeArea(Transform canvas)
        {
            var children = new List<Transform>();
            foreach (Transform c in canvas) children.Add(c);

            var go = new GameObject(SafeName, typeof(RectTransform)) { layer = canvas.gameObject.layer };
            var safe = (RectTransform)go.transform;
            safe.SetParent(canvas, false);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;

            foreach (var c in children) c.SetParent(safe, false);
            go.AddComponent<SafeArea>();
            return safe;
        }

        static void ToTopRight(RectTransform rt, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
            rt.anchoredPosition = position;
        }
    }
}
