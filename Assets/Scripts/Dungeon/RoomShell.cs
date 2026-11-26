using UnityEngine;

namespace OnLinces
{
    /// Tamaño de una sala (para el minimapa).
    public sealed class RoomShell : MonoBehaviour
    {
        [Tooltip("Tiles, impar")]
        public int width = 21;
        [Tooltip("Tiles")]
        public int height = 15;
    }
}
