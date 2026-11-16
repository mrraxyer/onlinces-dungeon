using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Muestra los corazones del jugador. Va en un objeto del Canvas con Horizontal Layout Group;
    /// "heartTemplate" es una Image hija desactivada que se copia por cada corazón.
    public sealed class HeartsHUD : MonoBehaviour
    {
        [SerializeField] PlayerHealth player;
        [SerializeField] Image heartTemplate;
        [Tooltip("hearts_0 (lleno), hearts_1 (medio), hearts_2 (vacío)")]
        [SerializeField] Sprite full, half, empty;

        readonly List<Image> hearts = new();

        void OnEnable() { if (player) player.Changed += Refresh; }
        void OnDisable() { if (player) player.Changed -= Refresh; }
        void Start() => Refresh();

        void Refresh()
        {
            int count = (player.MaxHp + 1) / 2;
            while (hearts.Count < count)
            {
                var img = Instantiate(heartTemplate, heartTemplate.transform.parent);
                img.gameObject.SetActive(true);
                hearts.Add(img);
            }
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < count);
                int v = player.Hp - i * 2;
                hearts[i].sprite = v >= 2 ? full : v == 1 ? half : empty;
            }
        }
    }
}
