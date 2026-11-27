using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Barras VID / ESC / ENE. Se arman en código; ESC solo con escudo.
    public sealed class StatsHUD : MonoBehaviour
    {
        [SerializeField] PlayerHealth player;

        const float RowH = 10f, LabelW = 22f, BarW = 110f, Gap = 2f, Pad = 3f;
        static readonly Color Back = new(0.04f, 0.04f, 0.07f, 0.8f);
        static readonly Color Empty = new(0.16f, 0.16f, 0.22f, 1f);
        static readonly Color HpColor = new Color32(0xe8, 0x46, 0x5a, 255);
        static readonly Color ShieldColor = new Color32(0xb4, 0xb8, 0xc8, 255);
        static readonly Color EnergyColor = new Color32(0x3b, 0x8c, 0xff, 255);

        sealed class Row { public RectTransform root, fill; public TMP_Text value; }

        RectTransform panel;
        Row hp, shield, energy;
        PlayerWeapons weapons;
        TMP_FontAsset font;
        (int, int, int, int, int, int) last = (-1, -1, -1, -1, -1, -1);
        Color back = Back;

        static readonly Color GuestBack = new(0.05f, 0.12f, 0.25f, 0.85f);

        /// Copia de las barras para el jugador 2, debajo del HUD del arma.
        public static void AddFor(PlayerHealth guest)
        {
            var main = FindAnyObjectByType<StatsHUD>();
            if (!main) return; // escenas sin HUD (lobby, práctica)
            var hud = Instantiate(main, main.transform.parent);
            hud.player = guest;
            hud.back = GuestBack;
            ((RectTransform)hud.transform).anchoredPosition = new Vector2(8f, -174f);
        }

        void Start()
        {
            weapons = player.GetComponent<PlayerWeapons>();
            var anyText = GetComponentInParent<Canvas>().GetComponentInChildren<TMP_Text>(true);
            if (anyText) font = anyText.font; // misma fuente que el resto del HUD
            foreach (Transform c in transform) c.gameObject.SetActive(false); // corazones viejos

            panel = Box("Bars", transform, back);
            panel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            panel.anchoredPosition = Vector2.zero;
            hp = MakeRow("VID", HpColor);
            shield = MakeRow("ESC", ShieldColor);
            energy = MakeRow("ENE", EnergyColor);
        }

        void Update()
        {
            // recompilar en Play borra las filas (no se serializan) y Start no vuelve a correr
            if (hp == null) return;
            int en = weapons ? weapons.Energy : 0, maxEn = weapons ? weapons.MaxEnergy : 0;
            var now = (player.Hp, player.MaxHp, player.Shield, player.MaxShield, en, maxEn);
            if (now == last) return;
            last = now;

            float y = Pad;
            Set(hp, player.Hp, player.MaxHp, ref y, true);
            Set(shield, player.Shield, player.MaxShield, ref y, player.HasShield);
            Set(energy, en, maxEn, ref y, weapons);
            panel.sizeDelta = new Vector2(Pad * 2 + LabelW + BarW, y - Gap + Pad);
        }

        static void Set(Row r, int v, int max, ref float y, bool visible)
        {
            r.root.gameObject.SetActive(visible);
            if (!visible) return;
            r.root.anchoredPosition = new Vector2(Pad, -y);
            r.fill.anchorMax = new Vector2(max > 0 ? Mathf.Clamp01((float)v / max) : 0f, 1f);
            r.value.text = $"{v}/{max}";
            y += RowH + Gap;
        }

        Row MakeRow(string label, Color color)
        {
            var root = Box(label, panel, null);
            root.sizeDelta = new Vector2(LabelW + BarW, RowH);
            var labelRt = Box("Label", root, null);
            labelRt.sizeDelta = new Vector2(LabelW, RowH);
            Text(labelRt, label, color, TextAlignmentOptions.Left);

            var bar = Box("Bar", root, Empty);
            bar.anchoredPosition = new Vector2(LabelW, 0f);
            bar.sizeDelta = new Vector2(BarW, RowH);
            var fill = Box("Fill", bar, color);
            fill.anchorMin = Vector2.zero;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            return new Row { root = root, fill = fill, value = Text(bar, "", Color.white, TextAlignmentOptions.Center) };
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

        TMP_Text Text(RectTransform parent, string s, Color color, TextAlignmentOptions align)
        {
            var rt = Box("Text", parent, null);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.text = s;
            t.color = color;
            t.fontSize = PixelText.Base;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }
    }
}
