using UnityEngine;

namespace OnLinces
{
    /// Sigue al jugador, suavizado y alineado al píxel.
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Más alto = sigue más rápido")]
        [SerializeField] float sharpness = 10f;

        Vector3 smooth;
        static float shakePx;

        /// Sacudida de cámara (traslación aleatoria que se apaga sola). En píxeles del juego.
        public static void Shake(float px) => shakePx = Mathf.Max(shakePx, px);

        void Start() => smooth = transform.position;

        void LateUpdate()
        {
            if (target == null) return;
            var goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            smooth = Vector3.Lerp(smooth, goal, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
            var shake = shakePx > 0f ? Random.insideUnitCircle * shakePx : Vector2.zero;
            shakePx = Mathf.MoveTowards(shakePx, 0f, 30f * Time.deltaTime);
            transform.position = new Vector3(
                Mathf.Round(smooth.x * Units.PPU + shake.x) / Units.PPU,
                Mathf.Round(smooth.y * Units.PPU + shake.y) / Units.PPU,
                smooth.z);
        }
    }
}
