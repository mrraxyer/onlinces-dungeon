using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OnLinces
{
    [Serializable]
    public struct EnemyChance
    {
        public Enemy prefab;
        [Tooltip("Más alto = más seguido")]
        public int weight;
    }

    /// Datos de un piso.
    [CreateAssetMenu(menuName = "OnLinces/Floor", fileName = "Floor")]
    public sealed class FloorData : ScriptableObject
    {
        public string displayName = "Laboratorio de cómputo";

        [Tooltip("Piso A-D, pared arriba, frente, frente con pantalla, pasillo")]
        public TileBase[] tiles = new TileBase[8];
        public Color background = new(0.047f, 0.039f, 0.07f);

        [Header("Enemigos")]
        public EnemyChance[] enemies;
    }
}
