using UnityEngine;

namespace OnLinces
{
    public enum SkillKind { DeployDoble, JuntaGeneral, Suplencia, FirewallTotal }

    /// Un puesto de la mesa directiva (o Miembro). Cambia la vestimenta de On/Off, la velocidad y la habilidad.
    /// Create > OnLinces > Role. Valores de ROLES y KITS en game_data.gd (Godot).
    [CreateAssetMenu(menuName = "OnLinces/Role", fileName = "Role")]
    public sealed class RoleData : ScriptableObject
    {
        [Header("Puesto")]
        public string displayName = "Miembro";
        [Tooltip("Quién lo tiene en el club (vacío para Miembro)")]
        public string person;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Vestimenta")]
        public CharacterSkin onSkin;
        public CharacterSkin offSkin;

        [Header("Stats")]
        [Tooltip("Píxeles por segundo (Godot: speed x 1.6)")]
        public float speedPx = 184f;
        [Tooltip("Medios corazones")]
        public int maxHp = 6;

        [Header("Habilidad (Espacio)")]
        public SkillKind skill;
        public string skillName = "Deploy doble";
        [TextArea] public string skillDescription = "5 s disparando con tus dos armas a la vez.";
        public float cooldown = 14f;
        [Tooltip("Cuánto dura (0 = instantánea)")]
        public float duration = 5f;

        public CharacterSkin SkinFor(Mascot m) => m == Mascot.Off ? offSkin : onSkin;
    }
}
