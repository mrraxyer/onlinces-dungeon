using UnityEngine;

namespace OnLinces
{
    /// La cámara sigue al jugador con un poco de suavizado.
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Más alto = sigue más rápido")]
        [SerializeField] float sharpness = 10f;

        void LateUpdate()
        {
            if (target == null) return;
            var goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }
    }
}
