using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Barra de vida del jefe, con el estilo de StatsHUD. Se arma en código; el color cambia con la fase.
    public sealed class BossBar : MonoBehaviour
    {
        [Tooltip("Panel viejo (pergamino): se oculta y se usa su fuente")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text nameLabel;

        const float W = 260f, Pad = 4f, BarH = 10f;
        static readonly Color Back = new(0.04f, 0.04f, 0.07f, 0.85f);  // igual que StatsHUD
        static readonly Color Empty = new(0.16f, 0.16f, 0.22f, 1f);
        static readonly Color Dim = new(0.62f, 0.6f, 0.72f, 1f);
        static readonly Color[] Phases =
        {
            new Color32(0x5e, 0xf2, 0xff, 255), // fase 1: cian
            new Color32(0xff, 0xb8, 0x4a, 255), // fase 2: ámbar
            new Color32(0xe8, 0x46, 0x5a, 255), // fase 3: rojo
        };

        public static Color PhaseColor(int phase) => Phases[Mathf.Clamp(phase, 1, 3) - 1];

        static BossBar instance;
        Boss boss;
        float shown = 1f, trail = 1f;
        TMP_FontAsset font;
        RectTransform root, barFill, barTrail;
        Image accent, fillImg;
        TMP_Text title, sub, phaseLabel, value;

        void Awake()
        {
            instance = this;
            font = nameLabel ? nameLabel.font : null;
            panel.SetActive(false);
            Build();
            root.gameObject.SetActive(false);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Show(Boss b)
        {
            if (!instance) return;
            instance.boss = b;
            instance.shown = instance.trail = 1f;
            instance.title.text = b.displayName;
            instance.sub.text = b.subtitle;
            instance.root.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (instance) instance.root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (boss == null || !root.gameObject.activeSelf) return;
            float target = Mathf.Clamp01(boss.Hp / boss.MaxHp);
            shown = Mathf.MoveTowards(shown, target, Time.deltaTime * 2f);
            trail = Mathf.MoveTowards(trail, shown, Time.deltaTime * 0.5f); // daño reciente, en blanco
            barFill.anchorMax = new Vector2(shown, 1f);
            barTrail.anchorMax = new Vector2(Mathf.Max(trail, shown), 1f);
            value.text = $"{Mathf.CeilToInt(boss.Hp)}/{Mathf.CeilToInt(boss.MaxHp)}";

            Color c = PhaseColor(boss.Phase);
            if (boss.InTransition)
            {
                // parpadea entre la fase actual y la siguiente
                bool blink = (int)(Time.time * 8f) % 2 == 0;
                c = blink ? PhaseColor(boss.NextPhase) : Color.white;
                phaseLabel.text = "REINICIANDO...";
                value.text = "INMUNE";
            }
            else
            {
                phaseLabel.text = $"FASE {boss.Phase}/3";
                if (boss.Lighthouse) value.text = "INMUNE";
            }
            accent.color = title.color = phaseLabel.color = fillImg.color = c;
        }

        void Build()
        {
            root = Box("TechBar", transform, Back);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(W, 44f);

            accent = Box("Accent", root, Color.white).GetComponent<Image>();
            Stretch(accent.rectTransform, 0f, 2f);

            title = Text(root, 10f, TextAlignmentOptions.Left, FontStyles.Bold);
            Place(title.rectTransform, Pad, 4f, W - Pad * 2, 12f);
            phaseLabel = Text(root, 8f, TextAlignmentOptions.Right, FontStyles.Normal);
            Place(phaseLabel.rectTransform, Pad, 5f, W - Pad * 2, 12f);
            sub = Text(root, 7f, TextAlignmentOptions.Left, FontStyles.Italic);
            sub.color = Dim;
            Place(sub.rectTransform, Pad, 16f, W - Pad * 2, 10f);

            var bar = Box("Bar", root, Empty);
            Place(bar, Pad, 29f, W - Pad * 2, BarH);
            barTrail = Box("Trail", bar, new Color(1f, 1f, 1f, 0.75f));
            barTrail.anchorMin = Vector2.zero;
            barTrail.offsetMin = barTrail.offsetMax = Vector2.zero;
            barFill = Box("Fill", bar, Color.white);
            barFill.anchorMin = Vector2.zero;
            barFill.offsetMin = barFill.offsetMax = Vector2.zero;
            fillImg = barFill.GetComponent<Image>();
            // marcas de fase (66% y 33%)
            foreach (float k in new[] { 1f / 3f, 2f / 3f })
            {
                var tick = Box("Tick", bar, Back);
                tick.anchorMin = tick.anchorMax = new Vector2(k, 0f);
                tick.pivot = new Vector2(0.5f, 0f);
                tick.sizeDelta = new Vector2(1f, BarH);
            }
            value = Text(bar, 8f, TextAlignmentOptions.Center, FontStyles.Normal);
            value.color = Color.white;
        }

        static void Stretch(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, -top - height);
            rt.offsetMax = new Vector2(0f, -top);
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static RectTransform Box(string name, Transform parent, Color? color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            if (color is Color c)
            {
                var img = go.AddComponent<Image>();
                img.color = c;
                img.raycastTarget = false;
            }
            return rt;
        }

        TMP_Text Text(RectTransform parent, float size, TextAlignmentOptions align, FontStyles style)
        {
            var rt = Box("Text", parent, null);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.fontSize = PixelText.Size(size); // la fuente pixel solo se ve nítida en múltiplos de 11
            t.fontStyle = style & ~(FontStyles.Bold | FontStyles.Italic);
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }
    }
}
