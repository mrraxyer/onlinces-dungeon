using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OnLinces
{
    [Serializable]
    public struct EnemyChance
    {
        public Enemy prefab;
        [Tooltip("Peso: más alto = sale más seguido")]
        public int weight;
    }

    /// Todo lo que cambia de un piso a otro. Create > OnLinces > Floor.
    [CreateAssetMenu(menuName = "OnLinces/Floor", fileName = "Floor")]
    public sealed class FloorData : ScriptableObject
    {
        public string displayName = "Laboratorio de cómputo";

        [Tooltip("8 tiles en este orden: piso A, B, C, D, pared arriba, pared frente, pared frente con pantalla, pasillo. " +
                 "Las salas se dibujan con los del piso 1 y se cambian por estos.")]
        public TileBase[] tiles = new TileBase[8];
        public Color background = new(0.047f, 0.039f, 0.07f);

        [Header("Enemigos (Godot: FLOORS en game_data.gd)")]
        public EnemyChance[] enemies;
    }
}
