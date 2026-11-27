using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Arma en la mano y la de reserva.
    public sealed class WeaponHUD : MonoBehaviour
    {
        [SerializeField] PlayerWeapons weapons;
        [SerializeField] GameObject panel;
        [Tooltip("Línea de arriba, color de rareza")]
        [SerializeField] Image accent;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;

        [Header("Reserva")]
        [SerializeField] GameObject otherRow;
        [SerializeField] Image otherIcon;
        [SerializeField] TMP_Text keyLabel;

        static readonly Color EnergyColor = new Color32(0x3b, 0x8c, 0xff, 255); // igual que StatsHUD
        static readonly Color Dimmed = new(0.3f, 0.3f, 0.3f, 1f);
        const float LineHeight = 14f;

        WeaponData shownCurrent, shownOther;
        bool shown, shownTouch, shownPad;
        TMP_Text costLabel;
        PlayerController owner;

        /// Copia para el jugador 2, debajo de su habilidad.
        public static void AddFor(PlayerWeapons guest)
        {
            var main = FindAnyObjectByType<WeaponHUD>();
            if (!main) return;
            var hud = Instantiate(main, main.transform.parent);
            hud.weapons = guest;
            ((RectTransform)hud.transform).anchoredPosition = new Vector2(8f, -248f);
        }

        // Línea de costo creada en runtime (mismo estilo que el nombre) para no tocar cada escena de piso.
        void Awake()
        {
            // copia del jugador 2: ya trae la línea de costo y el espacio
            var copied = nameLabel.transform.parent.Find("Cost");
            if (copied) { costLabel = copied.GetComponent<TMP_Text>(); return; }

            costLabel = Instantiate(nameLabel, nameLabel.transform.parent);
            costLabel.name = "Cost";
            costLabel.color = EnergyColor;
            ((RectTransform)costLabel.transform).anchoredPosition += Vector2.down * LineHeight;
            ((RectTransform)otherRow.transform).anchoredPosition += Vector2.down * LineHeight;
            ((RectTransform)transform).sizeDelta += Vector2.up * LineHeight;
        }

        void Update()
        {
            var cur = weapons ? weapons.Current : null;
            // como el cooldown: ícono oscuro si no alcanza la energía
            if (cur != null) icon.color = weapons.Energy >= cur.energyCost ? Color.white : Dimmed;
            var other = weapons ? weapons.Other : null;
            if (!owner && weapons) owner = weapons.GetComponent<PlayerController>();
            bool guest = owner && owner.IsGuest;
            bool touch = TouchControls.Active && !guest;
            if (shown && cur == shownCurrent && other == shownOther && touch == shownTouch && TouchControls.PadActive == shownPad) return;
            shown = true;
            shownCurrent = cur;
            shownOther = other;
            shownTouch = touch;
            shownPad = TouchControls.PadActive;

            panel.SetActive(cur != null);
            if (cur == null) return;

            var color = WeaponData.RarityColor(cur.rarity);
            accent.color = color;
            icon.sprite = cur.icon;
            nameLabel.text = cur.displayName;
            nameLabel.color = color;
            costLabel.text = $"Energía {cur.energyCost}";

            otherRow.SetActive(other != null);
            if (other != null) otherIcon.sprite = other.icon;
            // mando: L1 y R1 (anterior / siguiente); teclado: Q
            bool pad = guest || TouchControls.PadActive;
            keyLabel.text = touch ? "" : pad ? owner.Key("", "xb_lb", "ps_l1") + owner.Key("", "xb_rb", "ps_r1") : owner.Key("key_q", "", "");
        }
    }
}
