using UnityEngine;

namespace OnLinces
{
    public enum SkillKind { DeployDoble, JuntaGeneral, Suplencia, FirewallTotal }

    /// Puesto: vestimenta, stats y habilidad.
    [CreateAssetMenu(menuName = "OnLinces/Role", fileName = "Role")]
    public sealed class RoleData : ScriptableObject
    {
        [Header("Puesto")]
        public string displayName = "Miembro";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Vestimenta")]
        public CharacterSkin onSkin;
        public CharacterSkin offSkin;

        [Header("Stats")]
        [Tooltip("px/s")]
        public float speedPx = 184f;
        [Tooltip("Medios corazones")]
        public int maxHp = 6;

        [Header("Habilidad (Espacio)")]
        public SkillKind skill;
        public string skillName = "Deploy doble";
        [TextArea] public string skillDescription = "5 s disparando con tus dos armas a la vez.";
        public float cooldown = 14f;
        [Tooltip("0 = instantánea")]
        public float duration = 5f;

        public CharacterSkin SkinFor(Mascot m) => m == Mascot.Off ? offSkin : onSkin;
    }
}
