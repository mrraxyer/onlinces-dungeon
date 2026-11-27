using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Ícono, nombre y recarga de la habilidad.
    public sealed class SkillHUD : MonoBehaviour
    {
        static readonly Color Dimmed = new(0.3f, 0.3f, 0.3f, 1f);
        static readonly Color NameColor = new Color32(0xff, 0xd8, 0x4a, 255);

        [SerializeField] PlayerSkill skill;
        [SerializeField] Image icon;
        [Tooltip("Image Filled radial")]
        [SerializeField] Image cooldownCover;
        [SerializeField] TMP_Text nameLabel;

        TMP_Text counter;

        // Se crea en runtime para no tocar el HUD de cada escena de piso.
        void Awake()
        {
            nameLabel.color = NameColor; // el texto de escena era oscuro para el fondo claro de diálogo

            // Línea blanca de arriba, igual que el accent de WeaponHUD.
            var line = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            var lrt = (RectTransform)line.transform;
            lrt.SetParent(transform, false);
            lrt.SetAsFirstSibling();
            lrt.anchorMin = new Vector2(0f, 1f);
            lrt.anchorMax = Vector2.one;
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -1f);
            lrt.sizeDelta = new Vector2(-4f, 1f);
            line.GetComponent<Image>().raycastTarget = false;

            var go = new GameObject("CooldownCounter", typeof(RectTransform));
            go.transform.SetParent(cooldownCover.transform, false); // encima del overlay radial
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            counter = go.AddComponent<TextMeshProUGUI>();
            counter.font = nameLabel.font;
            counter.alignment = TextAlignmentOptions.Center;
            counter.enableAutoSizing = true;
            counter.fontSizeMin = 8;
            counter.fontSizeMax = 200;
            counter.color = Color.white;
            counter.outlineWidth = 0.2f;
            counter.outlineColor = Color.black;
            counter.raycastTarget = false;
            counter.enabled = false;
        }

        void Update()
        {
            if (skill == null) return;
            var r = skill.Role;
            if (r == null) return;
            if (icon.sprite != r.icon) icon.sprite = r.icon;
            cooldownCover.fillAmount = skill.CooldownFraction;
            bool ready = skill.Ready;
            icon.color = ready ? Color.white : Dimmed;
            counter.enabled = !ready;
            if (!ready) counter.text = Mathf.CeilToInt(skill.CooldownFraction * r.cooldown).ToString();
            string key = TouchControls.Active ? "" : "  " + TouchControls.Key("key_space", "xb_rb", "ps_r1");
            nameLabel.text = r.skillName + key;
        }
    }
}
