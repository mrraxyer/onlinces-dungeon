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
        [Tooltip("Energía por disparo")]
        public int energyCost;
        [Tooltip("Enemigos extra que atraviesa cada bala")]
        public int pierce;

        [Header("Especial (opcional)")]
        [Tooltip("Boomerang: a media vida regresa a ti y golpea de nuevo")]
        public bool returns;
        [Tooltip("Saltos a enemigos cercanos tras impactar")]
        public int chain;
        [Tooltip("Explota al chocar o al acabar su alcance")]
        public Explosion explosion;
        [Tooltip("Grados/s que gira para perseguir enemigos")]
        public float homingDegPerSec;

        public static string RarityName(Rarity r) => r switch
        {
            Rarity.Comun => "Común",
            Rarity.PocoComun => "Poco común",
            Rarity.Rara => "Rara",
            Rarity.Epica => "Épica",
            _ => "Legendaria",
        };

        public static Color RarityColor(Rarity r) => r switch
        {
            Rarity.Comun => new Color32(0xe8, 0xe6, 0xf5, 255),
            Rarity.PocoComun => new Color32(0x4a, 0xde, 0x80, 255),
            Rarity.Rara => new Color32(0x3b, 0x8c, 0xff, 255),
            Rarity.Epica => new Color32(0xb4, 0x8c, 0xff, 255),
            _ => new Color32(0xff, 0x9a, 0x3c, 255),
        };
    }
}
