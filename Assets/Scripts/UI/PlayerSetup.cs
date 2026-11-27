using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnLinces
{
    /// Entre el título y el lobby: personaje y plataforma de cada jugador. Se arma en código sobre el canvas del título.
    ///  · J1: teclado (flechas/WASD, Enter, Esc), ratón (clic) o su mando (A, B, cruceta).
    ///  · J2: A en otro mando para unirse, izquierda/derecha cambia personaje, B sale.
    public sealed class PlayerSetup : MonoBehaviour
    {
        static readonly Color Back = new(0.04f, 0.04f, 0.07f, 1f);
        static readonly Color Border = new(0.42f, 0.4f, 0.55f, 1f);
        static readonly Color Shade = new(0f, 0f, 0f, 0.7f);
        const string Hi = "#ffd84a", Txt = "#edebf5", Dim = "#9a96b8";
        // "<" y ">" sueltos los leería TMP como etiquetas
        const string L = "<noparse><</noparse>", R = "<noparse>></noparse>";

        enum Row { Mascot, Platform, Start, None }

        Mascot p1Mascot, p2Mascot;
        bool p1UsesPad;
        Gamepad p1Pad, p2Pad;
        Row row = Row.Mascot;
        TMP_Text text;
        readonly List<Row> lineRows = new();
        AudioClip clip;
        EventSystem events;

        /// Lo abre el botón de la mascota. En el teléfono no hay nada que elegir: empieza directo.
        public static void Open(Mascot mascot, AudioClip clickClip)
        {
            if (TouchControls.Active) { Coop.Configured = false; SceneFlow.StartRun(mascot); return; }
            if (FindAnyObjectByType<PlayerSetup>()) return;
            var canvas = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude)
                // solo la escena actual: el canvas de ScreenFade (DontDestroyOnLoad, sin CanvasScaler) lo encogía en la 2ª visita
                .FirstOrDefault(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            if (!canvas) { SceneFlow.StartRun(mascot); return; }

            var go = new GameObject("PlayerSetup", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var s = go.AddComponent<PlayerSetup>();
            s.p1Mascot = mascot;
            s.clip = clickClip;
            // quien hizo clic con el mando juega con ese mando
            var pad = Gamepad.current;
            s.p1UsesPad = pad != null && pad.buttonSouth.isPressed;
            if (s.p1UsesPad) s.p1Pad = pad;
            // la partida anterior deja al J2 listo si su mando sigue conectado
            if (Coop.Configured && Coop.P2Pad != null && Coop.P2Pad.added && Coop.P2Pad != s.p1Pad) { s.p2Pad = Coop.P2Pad; s.p2Mascot = Coop.P2Mascot; }
            s.Build(canvas);
        }

        void Build(Canvas canvas)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // encima de todo el título (sus textos tienen su propio orden de dibujo)
            var own = gameObject.AddComponent<Canvas>();
            own.overrideSorting = true;
            own.sortingOrder = 100;
            gameObject.AddComponent<Image>().color = Shade;

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            var brt = (RectTransform)box.transform;
            brt.SetParent(transform, false);
            brt.sizeDelta = new Vector2(380f, 200f);
            box.GetComponent<Image>().color = Back;
            var border = box.AddComponent<Outline>();
            border.effectColor = Border;
            border.effectDistance = new Vector2(2f, -2f);

            var trt = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            trt.SetParent(brt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 10f);
            trt.offsetMax = new Vector2(-14f, -10f);
            text = trt.gameObject.AddComponent<TextMeshProUGUI>();
            var anyText = canvas.GetComponentInChildren<TMP_Text>(true);
            if (anyText) text.font = anyText.font; // fuente pixel del título
            text.fontSize = PixelText.Base;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            // la navegación del título no debe correr debajo
            events = EventSystem.current;
            if (events) events.enabled = false;
            Refresh();
        }

        void OnDestroy() { if (events) events.enabled = true; }

        void Update()
        {
            var kb = Keyboard.current;
            int move = 0, change = 0;
            bool confirm = false, back = false;

            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) move--;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) move++;
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) change--;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) change++;
                confirm |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                back |= kb.escapeKey.wasPressedThisFrame;
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == p2Pad) { GuestInput(pad); continue; }
                bool mine = pad == p1Pad;
                if (!mine && pad.buttonSouth.wasPressedThisFrame)
                {
                    // con "Mando" el primer mando que pulse A es del J1; los demás se unen como J2
                    if (p1UsesPad && p1Pad == null) p1Pad = pad;
                    else if (p2Pad == null) { p2Pad = pad; p2Mascot = p1Mascot == Mascot.On ? Mascot.Off : Mascot.On; }
                    Click();
                    continue;
                }
                if (!mine) continue;
                if (Pressed(pad.dpad.up, pad.leftStick.up)) move--;
                if (Pressed(pad.dpad.down, pad.leftStick.down)) move++;
                if (Pressed(pad.dpad.left, pad.leftStick.left)) change--;
                if (Pressed(pad.dpad.right, pad.leftStick.right)) change++;
                confirm |= pad.buttonSouth.wasPressedThisFrame;
                back |= pad.buttonEast.wasPressedThisFrame;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                int line = TMP_TextUtilities.FindIntersectingLine(text, mouse.position.ReadValue(), null);
                var hover = line >= 0 && line < lineRows.Count ? lineRows[line] : Row.None;
                if (hover != Row.None && mouse.delta.ReadValue().sqrMagnitude > 0.5f) row = hover;
                if (mouse.leftButton.wasPressedThisFrame && hover != Row.None)
                {
                    row = hover;
                    if (hover == Row.Start) confirm = true; else change++;
                }
            }

            if (back) { Click(); Destroy(gameObject); return; }
            if (move != 0) { row = (Row)Mathf.Clamp((int)row + move, 0, (int)Row.Start); Click(); }
            if (change != 0)
            {
                if (row == Row.Mascot) p1Mascot = Other(p1Mascot);
                else if (row == Row.Platform) { p1UsesPad = !p1UsesPad; p1Pad = null; }
                Click();
            }
            if (confirm && row == Row.Start && CanStart()) { Begin(); return; }
            Refresh();
        }

        void GuestInput(Gamepad pad)
        {
            if (Pressed(pad.dpad.left, pad.leftStick.left) || Pressed(pad.dpad.right, pad.leftStick.right)) { p2Mascot = Other(p2Mascot); Click(); }
            if (pad.buttonEast.wasPressedThisFrame) { p2Pad = null; Click(); }
        }

        static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl a, UnityEngine.InputSystem.Controls.ButtonControl b) =>
            a.wasPressedThisFrame || b.wasPressedThisFrame;

        static Mascot Other(Mascot m) => m == Mascot.On ? Mascot.Off : Mascot.On;

        bool CanStart() => !p1UsesPad || (p1Pad != null && p1Pad.added);

        void Click() { if (clip) Sfx.Play(clip, 0.6f); }

        void Begin()
        {
            Coop.Configured = true;
            Coop.P1Pad = p1UsesPad ? p1Pad : null;
            Coop.P2Pad = p2Pad != null && p2Pad.added ? p2Pad : null;
            Coop.P2Mascot = p2Mascot;
            Click();
            enabled = false;
            SceneFlow.StartRun(p1Mascot);
        }

        void Refresh()
        {
            if (p2Pad != null && !p2Pad.added) p2Pad = null;
            if (p1Pad != null && !p1Pad.added) p1Pad = null;
            lineRows.Clear();
            var sb = new System.Text.StringBuilder();
            void Line(string s, Row r = Row.None) { sb.Append(s).Append('\n'); lineRows.Add(r); }
            string Opt(Row r, string label, string value)
            {
                bool sel = row == r;
                return $"<color={(sel ? Hi : Txt)}>{(sel ? R + " " : "  ")}{label}  {L} {value} {R}</color>";
            }

            Line($"<size={PixelText.Base * 2}>JUGADORES</size>");
            Line("");
            Line($"<color={Hi}>J1</color>  " + Opt(Row.Mascot, "Personaje", $"{p1Mascot} - {PlayerStats.MascotTitle(p1Mascot)}"), Row.Mascot);
            Line(Opt(Row.Platform, "Plataforma", p1UsesPad ? "Mando" : "Teclado y ratón"), Row.Platform);
            Line(p1UsesPad ? (p1Pad != null ? $"<color={Dim}>{p1Pad.displayName}</color>" : $"<color={Hi}>Pulsa A en tu mando</color>") : "");
            Line("");
            if (p2Pad == null) Line($"<color={Hi}>J2</color>  <color={Dim}>Pulsa A en otro mando para unirte</color>");
            else
            {
                Line($"<color={Hi}>J2</color>  <color={Txt}>Personaje  {L} {p2Mascot} - {PlayerStats.MascotTitle(p2Mascot)} {R}</color>");
                Line($"<color={Dim}>{p2Pad.displayName} - izquierda/derecha cambia - B sale</color>");
            }
            Line("");
            bool sel = row == Row.Start;
            string start = CanStart() ? "EMPEZAR" : "EMPEZAR (falta el mando del J1)";
            Line($"<color={(sel ? Hi : CanStart() ? Txt : Dim)}>{(sel ? R + " " : "")}[ {start} ]</color>", Row.Start);
            // los dos botones de atrás: B en Xbox y círculo en PlayStation
            Line($"<color={Dim}>Esc / B / </color><sprite=\"Buttons\" name=\"ps_circle\"><color={Dim}>: volver</color>");
            var s = sb.ToString().TrimEnd('\n');
            if (text.text != s) text.text = s;
        }
    }
}
