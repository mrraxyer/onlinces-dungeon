using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Dueño único del desenfoque de pantalla (shader BlackoutBlur del Renderer2D).
    /// Lo piden el apagón y el golpe; gana el que pida más.
    public sealed class ScreenBlur : MonoBehaviour
    {
        static readonly int CenterId = Shader.PropertyToID("_BlackoutCenter");
        static readonly int RadiiId = Shader.PropertyToID("_BlackoutRadii");
        static readonly int BlurId = Shader.PropertyToID("_BlackoutBlur");
        static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

        // golpe: el jugador nítido, el resto desenfocado; se apaga solo
        const float HitTime = 0.35f, HitInner = 0.06f, HitOuter = 0.3f, HitSize = 0.014f;

        static ScreenBlur instance;
        ScriptableRendererFeature feature;
        float blackout, blackoutSize;
        Vector2 blackoutCenter, blackoutRadii;
        float hitT = -1f;
        Transform player;

        static ScreenBlur Instance
        {
            get
            {
                if (instance) return instance;
                var go = new GameObject("ScreenBlur");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenBlur>();
                return instance;
            }
        }

        /// El apagón: cuánto, dónde (viewport) y radios en alturas de pantalla.
        public static void SetBlackout(ScriptableRendererFeature f, float amount, Vector2 center, Vector2 radii, float size)
        {
            if (amount <= 0.01f && instance == null) { Apply(f, 0f); return; }
            var s = Instance;
            if (f) s.feature = f;
            s.blackout = amount;
            s.blackoutCenter = center;
            s.blackoutRadii = radii;
            s.blackoutSize = size;
        }

        /// Pulso de desenfoque al recibir un golpe.
        public static void Hit(Transform who)
        {
            var s = Instance;
            s.player = who;
            s.hitT = 0f;
        }

        void LateUpdate()
        {
            if (feature == null) feature = FindFeature();
            float hit = 0f;
            if (hitT >= 0f)
            {
                hitT += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(hitT / HitTime);
                hit = (1f - p) * (1f - p); // de golpe y se va suave
                if (p >= 1f) hitT = -1f;
            }

            if (hit > blackout && player && Camera.main)
            {
                Shader.SetGlobalVector(CenterId, Camera.main.WorldToViewportPoint(player.position + Vector3.up * 0.4f));
                Shader.SetGlobalVector(RadiiId, new Vector4(HitInner, HitOuter));
                Shader.SetGlobalFloat(BlurSizeId, HitSize);
                Apply(feature, hit);
            }
            else
            {
                if (blackout > 0.01f)
                {
                    Shader.SetGlobalVector(CenterId, blackoutCenter);
                    Shader.SetGlobalVector(RadiiId, new Vector4(blackoutRadii.x, blackoutRadii.y));
                    Shader.SetGlobalFloat(BlurSizeId, blackoutSize);
                }
                Apply(feature, blackout);
            }
        }

        static void Apply(ScriptableRendererFeature f, float amount)
        {
            if (f) f.SetActive(amount > 0.01f);
            Shader.SetGlobalFloat(BlurId, amount);
        }

        // el feature vive en el asset del Renderer2D, que ya está cargado por el pipeline
        static ScriptableRendererFeature FindFeature()
        {
            foreach (var f in Resources.FindObjectsOfTypeAll<ScriptableRendererFeature>())
                if (f.name == "BlackoutBlur") return f;
            return null;
        }

        // el feature vive en un asset: no dejarlo prendido al salir
        void OnApplicationQuit() => Apply(feature, 0f);
    }
}
