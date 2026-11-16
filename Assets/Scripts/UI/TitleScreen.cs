using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnLinces
{
    /// Animación de la pantalla de inicio (como en Godot):
    ///  · el logo flota suave y debajo dice "D E B U G   D U N G E O N"
    ///  · arriba de cada mascota: su nombre, apodo y ventaja
    ///  · flechas > < que se mecen alrededor de la mascota elegida (la de tu botón con el ratón encima o seleccionado)
    /// Los textos se acomodan sobre los personajes del mundo, así se ven bien en cualquier pantalla.
    public sealed class TitleScreen : MonoBehaviour
    {
        static readonly Color OnColor = new(0.29f, 0.55f, 1f);     // 4b8dff
        static readonly Color OffColor = new(0.37f, 0.95f, 1f);    // 5ff3ff
        static readonly Color Dim = new(0.42f, 0.4f, 0.55f);
        static readonly Color PerkColor = new(0.86f, 0.85f, 0.94f);

        [Header("Mundo")]
        [SerializeField] Transform logo;
        [SerializeField] Transform onCharacter;
        [SerializeField] Transform offCharacter;

        [Header("Textos (en el Canvas)")]
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text onLabel;
        [SerializeField] TMP_Text offLabel;
        [SerializeField] TMP_Text arrowLeft;
        [SerializeField] TMP_Text arrowRight;

        [Header("Botones")]
        [SerializeField] Button onButton;
        [SerializeField] Button offButton;

        [Header("Medidas (unidades del mundo, 1 = 32 px)")]
        [SerializeField] float logoBob = 3f / 32f;
        [SerializeField] float subtitleBelowLogo = 0.85f;
        [SerializeField] float labelAboveFeet = 3.5f;
        [SerializeField] float arrowsAboveFeet = 1.3f;
        [SerializeField] float arrowsApart = 1.55f;

        Camera cam;
        Vector3 logoBase;
        Mascot selected = Mascot.On;
        float t;

        void Start()
        {
            cam = Camera.main;
            logoBase = logo.position;
            subtitle.text = "D E B U G    D U N G E O N";
        }

        void Update()
        {
            t += Time.deltaTime;

            // logo flotando, en píxeles enteros para que no se vea borroso
            float y = Mathf.Round(Mathf.Sin(t * 2f) * logoBob * Units.PPU) / Units.PPU;
            logo.position = logoBase + new Vector3(0f, y, 0f);
            Place(subtitle.rectTransform, logoBase + Vector3.down * subtitleBelowLogo);

            UpdateSelection();
            Label(onLabel, Mascot.On, onCharacter, OnColor);
            Label(offLabel, Mascot.Off, offCharacter, OffColor);

            // flechas que se mecen hacia la mascota elegida
            var ch = selected == Mascot.On ? onCharacter : offCharacter;
            var col = selected == Mascot.On ? OnColor : OffColor;
            float bob = Mathf.Sin(t * 6f) * 2f / Units.PPU;
            var mid = ch.position + Vector3.up * arrowsAboveFeet;
            Place(arrowLeft.rectTransform, mid + Vector3.left * (arrowsApart - bob));
            Place(arrowRight.rectTransform, mid + Vector3.right * (arrowsApart - bob));
            arrowLeft.color = arrowRight.color = col;
        }

        // la elegida es la del botón que tiene el ratón encima o que está seleccionado con teclado/control
        void UpdateSelection()
        {
            if (Mouse.current != null)
            {
                var m = Mouse.current.position.ReadValue();
                if (Over(onButton, m)) selected = Mascot.On;
                else if (Over(offButton, m)) selected = Mascot.Off;
            }
            var focus = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (focus == onButton.gameObject) selected = Mascot.On;
            else if (focus == offButton.gameObject) selected = Mascot.Off;
        }

        static bool Over(Button b, Vector2 screen) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)b.transform, screen, null);

        void Label(TMP_Text label, Mascot m, Transform character, Color color)
        {
            bool sel = m == selected;
            label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(sel ? color : Dim)}><b>{m}</b> - {PlayerStats.MascotTitle(m)}</color>\n" +
                         $"<size=85%><color=#{ColorUtility.ToHtmlStringRGB(sel ? PerkColor : Dim)}>{PlayerStats.MascotPerk(m)}</color></size>";
            Place(label.rectTransform, character.position + Vector3.up * labelAboveFeet);

            // la que no está elegida se ve un poco apagada
            var sr = character.GetComponent<SpriteRenderer>();
            if (sr) sr.color = Color.Lerp(sr.color, sel ? Color.white : new Color(0.62f, 0.62f, 0.72f), Time.deltaTime * 10f);
        }

        /// Pone un texto del Canvas sobre un punto del mundo.
        void Place(RectTransform rt, Vector3 world)
        {
            Vector2 screen = cam.WorldToScreenPoint(world);
            // relativo al padre (en teléfono el Canvas mete todo dentro de un área segura)
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, screen, null, out var local))
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = local;
            }
        }
    }
}
