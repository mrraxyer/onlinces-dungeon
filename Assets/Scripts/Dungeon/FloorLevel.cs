using UnityEngine;

namespace OnLinces
{
    /// Va en cada escena de nivel (Floor1..3). El nivel está dibujado a mano en la escena;
    /// este componente solo sabe qué piso es y qué salas tiene (para el minimapa).
    /// Al darle Play directo a un nivel desde el editor, ajusta el número de piso.
    public sealed class FloorLevel : MonoBehaviour
    {
        [SerializeField] int floorNumber = 1;
        [SerializeField] FloorData data;
        [Tooltip("Salas en orden, de la entrada a la salida")]
        [SerializeField] RoomShell[] rooms;

        public int FloorNumber => floorNumber;
        public FloorData Data => data;
        public RoomShell[] Rooms => rooms;

        void Awake()
        {
            // Si entras directo a esta escena (Play en el editor), el piso queda bien
            GameManager.Instance.Run.Floor = floorNumber;
        }
    }
}
