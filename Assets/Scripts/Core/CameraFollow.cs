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
    }
}
