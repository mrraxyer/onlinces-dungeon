using UnityEngine;

namespace OnLinces
{
    public enum WeaponKind { Gun, Melee }
    public enum Rarity { Comun, PocoComun, Rara, Epica, Legendaria }

    /// Un arma. Se crea en Project: clic derecho > Create > OnLinces > Weapon.
    /// Los valores se copian de WEAPONS en game_data.gd (Godot), en píxeles y segundos.
    [CreateAssetMenu(menuName = "OnLinces/Weapon", fileName = "Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        public string displayName = "print()";
        [TextArea] public string description = "Confiable. Nunca te falla.";
        public Rarity rarity = Rarity.Comun;
        [Tooltip("Por ahora solo se implementa Gun (disparo).")]
        public WeaponKind kind = WeaponKind.Gun;

        [Header("Arte")]
        [Tooltip("Cuadro de weapons.png (weapons_0 = print, weapons_1 = for...)")]
        public Sprite icon;
        [Tooltip("Cuadros de tokens.png que dispara. Si hay varios, elige uno al azar en cada tiro.")]
        public Sprite[] projectileSprites;

        [Header("Disparo (valores de Godot)")]
        public float damage = 2f;
        [Tooltip("Segundos entre disparos")]
        public float fireDelay = 0.3f;
        [Tooltip("Velocidad de la bala en píxeles por segundo")]
        public float projectileSpeedPx = 260f;
        [Tooltip("Apertura en grados")]
        public float spreadDegrees = 4f;
        [Tooltip("Balas por disparo")]
        public int count = 1;
        [Tooltip("Segundos que vive la bala (alcance)")]
        public float range = 0.7f;
        [Tooltip("Energía por disparo (todavía no se usa)")]
        public int energyCost;
    }
}
