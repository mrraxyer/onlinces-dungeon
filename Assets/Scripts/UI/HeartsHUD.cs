using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Corazones del jugador.
    public sealed class HeartsHUD : MonoBehaviour
    {
        [SerializeField] PlayerHealth player;
        [Tooltip("Plantilla desactivada")]
        [SerializeField] Image heartTemplate;
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
