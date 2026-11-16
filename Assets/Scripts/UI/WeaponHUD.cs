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

        WeaponData shownCurrent, shownOther;
        bool shown, shownTouch;

        void Update()
        {
            var cur = weapons ? weapons.Current : null;
            var other = weapons ? weapons.Other : null;
            bool touch = TouchControls.Active;
            if (shown && cur == shownCurrent && other == shownOther && touch == shownTouch) return;
            shown = true;
            shownCurrent = cur;
            shownOther = other;
            shownTouch = touch;

            panel.SetActive(cur != null);
            if (cur == null) return;

            var color = WeaponData.RarityColor(cur.rarity);
            accent.color = color;
            icon.sprite = cur.icon;
            nameLabel.text = cur.displayName;
            nameLabel.color = color;

            otherRow.SetActive(other != null);
            if (other != null) otherIcon.sprite = other.icon;
            keyLabel.text = touch ? "" : "Q";
        }
    }
}
