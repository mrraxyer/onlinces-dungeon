using UnityEngine;

namespace OnLinces
{
    /// Zona de salida (BoxCollider2D con Is Trigger): al pisarla pasas al siguiente piso.
    public sealed class FloorExit : MonoBehaviour
    {
        bool used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.GetComponentInParent<PlayerHealth>() == null) return;
            // Al recargar la escena, el jugador de la escena vieja existe un instante más: se ignora.
            if (other.gameObject.scene != gameObject.scene) return;
            used = true;
            SceneFlow.CompleteFloor();
        }
    }
}
