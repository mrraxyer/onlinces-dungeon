using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Barra de vida del jefe.
    public sealed class BossBar : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [Tooltip("Image Filled horizontal")]
        [SerializeField] Image fill;

        static BossBar instance;
        Boss boss;
        float shown = 1f;

        void Awake()
        {
            instance = this;
            panel.SetActive(false);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Show(Boss b)
        {
            if (!instance) return;
            instance.boss = b;
            instance.shown = 1f;
            instance.nameLabel.text = b.displayName;
            instance.subtitleLabel.text = b.subtitle;
            instance.panel.SetActive(true);
        }

        public static void Hide()
        {
            if (instance) instance.panel.SetActive(false);
        }

        void Update()
        {
            if (boss == null || !panel.activeSelf) return;
            float target = Mathf.Clamp01(boss.Hp / boss.MaxHp);
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 0.8f);
            fill.fillAmount = shown;
        }
    }
}
