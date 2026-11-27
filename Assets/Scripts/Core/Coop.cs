using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Jugadores elegidos en el título. Sin configurar (se entra directo a una escena) = un jugador con todos los dispositivos.
    public static class Coop
    {
        public static bool Configured;
        /// Mando del jugador 1; null = teclado y ratón.
        public static Gamepad P1Pad;
        /// Mando del jugador 2; null = juega uno solo.
        public static Gamepad P2Pad;
        public static Mascot P2Mascot;

        public static bool HasGuest => P2Pad != null && P2Pad.added && !TouchControls.Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Configured = false; P1Pad = null; P2Pad = null; P2Mascot = Mascot.On; }

        /// Ícono del botón según el mando (Xbox o PlayStation).
        public static string PadKey(Gamepad pad, string xbox, string ps) =>
            $"<sprite=\"Buttons\" name=\"{(pad is UnityEngine.InputSystem.DualShock.DualShockGamepad ? ps : xbox)}\">";
    }
}
