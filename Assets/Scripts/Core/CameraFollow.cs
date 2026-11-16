using UnityEngine;

namespace OnLinces
{
    /// La cámara sigue al jugador con un poco de suavizado.
    /// La posición final se redondea al píxel del juego (1/32) para que el pixel art no tiemble.
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Más alto = sigue más rápido")]
        [SerializeField] float sharpness = 10f;

        Vector3 smooth;

        void Start() => smooth = transform.position;

        void LateUpdate()
        {
            if (target == null) return;
            var goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            smooth = Vector3.Lerp(smooth, goal, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
            transform.position = new Vector3(
                Mathf.Round(smooth.x * Units.PPU) / Units.PPU,
                Mathf.Round(smooth.y * Units.PPU) / Units.PPU,
                smooth.z);
        }

        /// Para mover la cámara de golpe (al empezar el piso).
        public void SnapTo(Vector3 pos)
        {
            smooth = new Vector3(pos.x, pos.y, transform.position.z);
            transform.position = smooth;
        }
    }
}
