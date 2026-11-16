using UnityEngine;

namespace OnLinces
{
    public enum WeaponKind { Gun, Melee }
    public enum Rarity { Comun, PocoComun, Rara, Epica, Legendaria }

    /// Datos de un arma (px y segundos).
    [CreateAssetMenu(menuName = "OnLinces/Weapon", fileName = "Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        public string displayName = "print()";
        [TextArea] public string description = "Confiable. Nunca te falla.";
        public Rarity rarity = Rarity.Comun;
        [Tooltip("Solo Gun está hecho")]
        public WeaponKind kind = WeaponKind.Gun;

        [Header("Arte")]
        public Sprite icon;
        [Tooltip("Uno al azar por tiro")]
        public Sprite[] projectileSprites;

        [Header("Disparo")]
        public float damage = 2f;
        [Tooltip("s entre disparos")]
        public float fireDelay = 0.3f;
        [Tooltip("px/s")]
        public float projectileSpeedPx = 260f;
        [Tooltip("Grados")]
        public float spreadDegrees = 4f;
        [Tooltip("Balas por disparo")]
        public int count = 1;
        [Tooltip("Vida de la bala en s")]
        public float range = 0.7f;
        [Tooltip("Sin usar")]
        public int energyCost;
    }
}
