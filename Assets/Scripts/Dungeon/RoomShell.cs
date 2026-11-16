using UnityEngine;
using UnityEngine.Tilemaps;

namespace OnLinces
{
    /// La "caja" de una sala hecha a mano (prefab). El FloorBuilder las encadena de abajo hacia arriba.
    /// Reglas para dibujar una sala nueva en el editor:
    ///  · La fila de pared de abajo está en y = 0 y la de arriba en y = height - 1.
    ///  · Las puertas van en la columna x = 0 (abajo y arriba).
    ///  · Las celdas van de x = -(width-1)/2 a (width-1)/2 (ancho impar).
    public sealed class RoomShell : MonoBehaviour
    {
        [Tooltip("Ancho en tiles (impar)")]
        public int width = 21;
        [Tooltip("Alto en tiles")]
        public int height = 15;
        public Door bottomDoor;
        public Door topDoor;
        public Tilemap floorMap;
        public Tilemap wallMap;
        [Tooltip("Dónde aparece el jugador (solo sala de inicio)")]
        public Transform playerSpawn;

        public Room Combat => GetComponentInChildren<Room>();

        /// Quita una puerta y tapa el hueco con pared (sala de inicio sin puerta abajo, última sin puerta arriba).
        public void SealDoor(bool top, TileBase wallTile)
        {
            var door = top ? topDoor : bottomDoor;
            if (door == null) return;
            door.gameObject.SetActive(false);
            var cell = new Vector3Int(0, top ? height - 1 : 0, 0);
            floorMap.SetTile(cell, null);
            wallMap.SetTile(cell, wallTile);
            if (top) topDoor = null; else bottomDoor = null;
        }
    }
}
