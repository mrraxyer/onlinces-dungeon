using UnityEngine;
using UnityEngine.Tilemaps;

namespace OnLinces
{
    /// Caja de una sala: tamaño, puertas y tilemaps (para el minimapa).
    public sealed class RoomShell : MonoBehaviour
    {
        [Tooltip("Tiles, impar")]
        public int width = 21;
        [Tooltip("Tiles")]
        public int height = 15;
        public Door bottomDoor;
        public Door topDoor;
        public Tilemap floorMap;
        public Tilemap wallMap;
        [Tooltip("Solo sala de inicio")]
        public Transform playerSpawn;
    }
}
