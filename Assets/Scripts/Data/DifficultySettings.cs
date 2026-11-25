using System;
using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Parámetros de cada dificultad. Archivo: Assets/Data/Resources/Difficulty.asset
    /// (en Resources para que el juego lo cargue sin que una escena lo referencie).
    [CreateAssetMenu(menuName = "OnLinces/Dificultad", fileName = "Difficulty")]
    public sealed class DifficultySettings : ScriptableObject
    {
        public const float HpPerHeart = 2f;

        [Serializable]
        public sealed class Level
        {
            public string displayName = "Media";
            [Tooltip("Multiplica la vida de los enemigos comunes")]
            public float enemyHp = 1f;
            [Tooltip("Multiplica la vida de los jefes")]
            public float bossHp = 1f;
            [Tooltip("Multiplica la velocidad de las balas enemigas")]
            public float bulletSpeed = 1f;
            [Tooltip("Multiplica cuántos enemigos salen por oleada")]
            public float enemyCount = 1f;
            [Tooltip("Medios corazones extra para el jugador")]
            public int playerHalfHearts;
        }

        public Level facil = new() { displayName = "Fácil", enemyHp = 0.7f, bossHp = 0.7f, bulletSpeed = 0.85f, enemyCount = 0.75f, playerHalfHearts = 2 };
        public Level media = new();
        public Level dificil = new() { displayName = "Difícil", enemyHp = 1.35f, bossHp = 1.35f, bulletSpeed = 1.2f, enemyCount = 1.3f };

        [Header("Jefes (en todas las dificultades)")]
        [Tooltip("Vida base de todos los jefes, en corazones (1 corazón = 2 de vida, como el jugador)")]
        public int bossHearts = 33;
        [Tooltip("Vida extra de los jefes por cada piso después del 1 (0.35 = +35%)")]
        public float bossHpPerFloor = 0.35f;

        public Level Get(Difficulty d) => d switch { Difficulty.Facil => facil, Difficulty.Media => media, _ => dificil };

        static DifficultySettings current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;

        /// Si falta el archivo, usa los valores por defecto.
        public static DifficultySettings Current
        {
            get
            {
                if (current == null) current = Resources.Load<DifficultySettings>("Difficulty");
                if (current == null) current = CreateInstance<DifficultySettings>();
                return current;
            }
        }

        /// Texto para la terminal, armado con los valores (no queda desactualizado).
        public string Summary(Difficulty d)
        {
            var l = Get(d);
            static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
            var parts = new List<string>();
            if (!Mathf.Approximately(l.enemyHp, 1f)) parts.Add($"enemigos con {Pct(l.enemyHp)} de vida");
            if (!Mathf.Approximately(l.bossHp, 1f)) parts.Add($"jefes con {Pct(l.bossHp)} de vida");
            if (!Mathf.Approximately(l.bulletSpeed, 1f)) parts.Add($"balas a {Pct(l.bulletSpeed)} de velocidad");
            if (!Mathf.Approximately(l.enemyCount, 1f)) parts.Add($"{Pct(l.enemyCount)} de enemigos");
            if (l.playerHalfHearts > 0) parts.Add($"+{l.playerHalfHearts / 2f:0.#} corazón(es)");
            if (parts.Count == 0) return "La experiencia normal.";
            var s = string.Join(", ", parts) + ".";
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
