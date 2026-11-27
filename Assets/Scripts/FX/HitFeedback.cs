using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Reacción al recibir un golpe: el mundo entero brinca en la dirección del impacto,
    /// la cámara tiembla, el juego se congela un instante (hitstop) y los bordes destellan en rojo.
    public sealed class HitFeedback : MonoBehaviour
    {
        const float StopTime = 0.08f, StopScale = 0.05f;
        const float FlashTime = 0.45f, FlashAlpha = 0.8f;
        static readonly Color Red = new(0.85f, 0.08f, 0.15f);

        static HitFeedback instance;
        RawImage vignette;
        float flashT = -1f;
        bool stopping;

        /// strong = el golpe quita vida (si solo pega en el escudo, la reacción es más suave).
        public static void PlayerHit(Vector2 away, bool strong)
        {
            CameraFollow.Kick(away * (strong ? 10f : 5f));
            CameraFollow.Shake(strong ? 7f : 3f);
            if (!strong) return;
            var f = Instance;
            f.flashT = 0f;
            if (!f.stopping && Mathf.Approximately(Time.timeScale, 1f)) f.StartCoroutine(f.HitStop());
        }

        static HitFeedback Instance
        {
            get
            {
                if (instance) return instance;
                var go = new GameObject("HitFeedback");
                DontDestroyOnLoad(go);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 31000; // debajo de la transición de escena
                var img = new GameObject("Vignette").AddComponent<RawImage>();
                img.transform.SetParent(go.transform, false);
                img.rectTransform.anchorMin = Vector2.zero;
                img.rectTransform.anchorMax = Vector2.one;
                img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
                img.texture = VignetteTexture();
                img.color = new Color(Red.r, Red.g, Red.b, 0f);
                img.raycastTarget = false;
                instance = go.AddComponent<HitFeedback>();
                instance.vignette = img;
                return instance;
            }
        }

        /// Viñeta en pixel art: baja resolución, filtro point y la opacidad en 4 escalones.
        static Texture2D VignetteTexture()
        {
            const int W = 120, H = 80;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, v = (y + 0.5f) / H * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v) / Mathf.Sqrt(2f); // 0 centro, 1 esquina
                    float k = Mathf.Clamp01((d - 0.45f) / 0.5f);
                    k = Mathf.Floor(k * 4f) / 4f; // escalones
                    px[y * W + x] = new Color32(255, 255, 255, (byte)(k * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        IEnumerator HitStop()
        {
            stopping = true;
            Time.timeScale = StopScale;
            yield return new WaitForSecondsRealtime(StopTime);
            if (Mathf.Approximately(Time.timeScale, StopScale)) Time.timeScale = 1f; // no pisa la pausa
            stopping = false;
        }

        void Update()
        {
            if (flashT < 0f) return;
            flashT += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(flashT / FlashTime);
            var c = vignette.color;
            c.a = FlashAlpha * (1f - p) * (1f - p); // se apaga con ease-out
            vignette.color = c;
            if (p >= 1f) flashT = -1f;
        }
    }
}
