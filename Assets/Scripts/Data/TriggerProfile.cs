using System;
using UnityEngine;

namespace OnLinces
{
    /// Cómo se siente el gatillo R2 del DualSense con un arma. Zonas 0-9 a lo largo del recorrido, fuerza 1-8.
    [Serializable]
    public sealed class TriggerProfile
    {
        public enum Mode
        {
            [Tooltip("Suelto")] Off,
            [Tooltip("Resiste entre Inicio y Fin y luego truena")] Weapon,
            [Tooltip("Resistencia pareja desde Inicio")] Feedback,
            [Tooltip("Resistencia que pasa de Fuerza a Fuerza final entre Inicio y Fin")] Slope,
            [Tooltip("Vibra desde Inicio a la Frecuencia dada")] Vibration,
        }

        public Mode mode = Mode.Weapon;
        [Range(0, 9)] public int start = 3;
        [Range(1, 9)] public int end = 5;
        [Range(1, 8)] public int strength = 4;
        [Tooltip("Solo Slope")]
        [Range(1, 8)] public int endStrength = 8;
        [Tooltip("Hz de Vibration y de los pulsos al disparar")]
        [Range(1, 255)] public int frequency = 30;

        [Header("Mientras juegas")]
        [Tooltip("Trabado hasta que puede volver a disparar (armas lentas)")]
        public bool lockWhileReloading;
        [Tooltip("Trabado mientras la bala sigue en el aire (boomerang)")]
        public bool lockWhileBulletFlies;
        [Tooltip("s de pulsos en el gatillo justo después de disparar (0 = nada)")]
        public float shotPulseSeconds;
    }
}
