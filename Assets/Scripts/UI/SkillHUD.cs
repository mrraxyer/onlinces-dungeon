using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Ícono, nombre y recarga de la habilidad.
    public sealed class SkillHUD : MonoBehaviour
    {
        [SerializeField] PlayerSkill skill;
        [SerializeField] Image icon;
        [Tooltip("Image Filled radial")]
        [SerializeField] Image cooldownCover;
        [SerializeField] TMP_Text nameLabel;

        void Update()
        {
            if (skill == null) return;
            var r = skill.Role;
            if (r == null) return;
            if (icon.sprite != r.icon) icon.sprite = r.icon;
            cooldownCover.fillAmount = skill.CooldownFraction;
            string key = TouchControls.Active ? "" : "  [Espacio]";
            nameLabel.text = skill.Ready ? r.skillName + key : $"{r.skillName}  ({Mathf.CeilToInt(skill.CooldownFraction * r.cooldown)} s)";
        }
    }
}
