using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Acomoda el HUD de cada escena cuando se juega en teléfono (lo llama TouchControls al cargar la escena).
    /// Las escenas no cambian: en PC el HUD queda tal como se diseñó en el editor.
    ///  · El HUD se escala por la ALTURA de la pantalla (en un teléfono alargado, por ancho salía enorme
    ///    y los botones de abajo se salían de la pantalla).
    ///  · Todo el HUD se mete al área segura (sin notch ni esquinas redondas).
    ///  · El minimapa sube a la esquina de arriba a la derecha: abajo lo tapa el pulgar que apunta.
    public static class MobileHud
    {
        const string SafeName = "SafeArea";

        // posiciones desde la esquina de arriba a la derecha, en unidades del HUD (a la izquierda del botón de pausa)
        static readonly Vector2 MinimapCorner = new(-66f, -8f);
        static readonly Vector2 FeedbackCorner = new(-66f, -8f);
        static readonly Vector2 FeedbackCornerWithMinimap = new(-104f, -8f);

        /// referenceHeight: alto del HUD en sus unidades. Más alto = HUD más chico.
        public static void Apply(float referenceHeight, Canvas touchCanvas)
        {
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>())
            {
                var canvas = scaler.GetComponent<Canvas>();
                if (canvas == null || canvas == touchCanvas || !canvas.isRootCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                if (canvas.transform.Find(SafeName) != null) continue; // ya acomodado

                Proportional(scaler, referenceHeight);

                var safe = WrapInSafeArea(canvas.transform);

                var minimap = canvas.GetComponentInChildren<Minimap>(true);
                if (minimap != null) ToTopRight((RectTransform)minimap.transform, MinimapCorner);

                if (safe.Find("Feedback") is RectTransform feedback)
                    ToTopRight(feedback, minimap != null ? FeedbackCornerWithMinimap : FeedbackCorner);

                // el diálogo va encima de lo que tienes cerca, y su aviso "USAR: ..." es el botón para usarlo
                var prompt = safe.Find("PromptLabelPanel") as RectTransform;
                var info = safe.Find("InfoPanel") as RectTransform;
                if (info != null) info.sizeDelta = new Vector2(InfoWidth, info.sizeDelta.y);
                if (prompt != null)
                {
                    safe.gameObject.AddComponent<InteractionDialog>().Init(prompt, info);
                    MakeTappable(canvas, prompt, touchCanvas);
                }

                // los mensajes cortos bajan al hueco entre las palancas
                if (safe.Find("ToastLabelPanel") is RectTransform toast)
                {
                    toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0f);
                    toast.pivot = new Vector2(0.5f, 0f);
                    toast.anchoredPosition = ToastBottom;
                }
            }
        }

        /// Escala el Canvas con la pantalla en modo "Expand": todo (textos incluidos) crece y se encoge
        /// en proporción, y siempre caben al menos (2 x referenceHeight) de ancho por referenceHeight de alto.
        /// En teléfonos alargados (20:9) se ve igual que con "match height"; en 16:9 y tablets 4:3 todo
        /// se encoge un poco para que los botones de los lados no se encimen en el centro.
        public static void Proportional(CanvasScaler scaler, float referenceHeight)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(referenceHeight * 2f, referenceHeight);
        }

        const float InfoWidth = 250f;
        static readonly Color TapColor = new(1f, 0.86f, 0.5f); // pergamino dorado: se nota que se toca

        /// Convierte el aviso en botón. El HUD se dibuja encima de las palancas (que ocupan toda la pantalla),
        /// así que todo lo demás del HUD deja de recibir toques para no estorbarlas.
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

            // un poco más grande para el dedo
            if (prompt.GetComponent<HorizontalLayoutGroup>() is { } layout)
                layout.padding = new RectOffset(layout.padding.left + 6, layout.padding.right + 6, layout.padding.top + 4, layout.padding.bottom + 4);
        }
        // el hueco libre entre palancas está un poco a la izquierda (a la derecha hay más botones)
        static readonly Vector2 ToastBottom = new(-55f, 12f);


        /// Crea un hijo que ocupa el área segura y le pasa todo lo que había en el Canvas.
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
