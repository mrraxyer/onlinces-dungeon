using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Número de daño que sube y se desvanece (tweening con ease-out).
    public sealed class DamageNumber : MonoBehaviour
    {
        const float Rise = 0.45f, RiseUnits = 0.42f, FadeDelay = 0.25f, FadeTime = 0.3f;
        static readonly Color Big = new(1f, 0.85f, 0.3f), Small = Color.white;

        TextMeshPro text;
        Vector3 start;
        float dx, t;

        public static void Spawn(Vector3 position, float damage)
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null || damage <= 0f) return;
            var go = new GameObject("DamageNumber");
            go.transform.position = position;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.fontSize = 11f / Units.PPU * 10f; // 1 px de fuente = 1 px del juego
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
            tmp.text = damage >= 10f || Mathf.Approximately(damage, Mathf.Round(damage)) ? Mathf.RoundToInt(damage).ToString() : damage.ToString("0.#");
            tmp.color = damage >= 5f ? Big : Small;
            tmp.sortingOrder = 300;
            var n = go.AddComponent<DamageNumber>();
            n.text = tmp;
            n.start = position;
            n.dx = Random.Range(-6f, 6f) / Units.PPU;
        }

        void Update()
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Rise);
            float e = 1f - (1f - p) * (1f - p); // ease-out cuadrático
            var pos = start + new Vector3(dx * e, RiseUnits * e, 0f);
            transform.position = new Vector3(Mathf.Round(pos.x * Units.PPU) / Units.PPU, Mathf.Round(pos.y * Units.PPU) / Units.PPU, 0f);
            var c = text.color;
            c.a = 1f - Mathf.Clamp01((t - FadeDelay) / FadeTime);
            text.color = c;
            if (t >= FadeDelay + FadeTime) Destroy(gameObject);
        }
    }
}
