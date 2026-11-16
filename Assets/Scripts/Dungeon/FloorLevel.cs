using UnityEngine;

namespace OnLinces
{
    /// Número de piso y salas del nivel.
    public sealed class FloorLevel : MonoBehaviour
    {
        [SerializeField] int floorNumber = 1;
        [SerializeField] FloorData data;
        [Tooltip("De la entrada a la salida")]
        [SerializeField] RoomShell[] rooms;

        public int FloorNumber => floorNumber;
        public FloorData Data => data;
        public RoomShell[] Rooms => rooms;

        void Awake()
        {
            // por si se abre la escena directo
            if (floorNumber > 0) GameManager.Instance.Run.Floor = floorNumber;
        }
    }
}
