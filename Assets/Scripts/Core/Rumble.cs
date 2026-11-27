using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Vibración clásica (dos motores) en Xbox y PlayStation. Se apaga en Pausa > Opciones > Vibración.
    /// low = motor grande (golpe sordo), high = motor chico (zumbido).
    public static class Rumble
    {
        static int enabled = -1; // -1 = sin leer de PlayerPrefs
        public static bool Enabled => (enabled < 0 ? enabled = PlayerPrefs.GetInt("rumble", 1) : enabled) == 1;

        struct Pulse { public float low, high, until; }
        static readonly Dictionary<Gamepad, Pulse> active = new();
        static Runner runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            runner = null;
            enabled = -1;
        }

        public static void SetEnabled(bool on)
        {
            enabled = on ? 1 : 0;
            PlayerPrefs.SetInt("rumble", on ? 1 : 0);
            PlayerPrefs.Save();
            if (!on) StopAll();
        }

        /// El mando de ese jugador (null si juega con teclado o táctil).
        public static Gamepad PadOf(PlayerController p) =>
            p == null ? null : p.IsGuest ? Coop.P2Pad : Coop.Configured ? Coop.P1Pad : TouchControls.PadActive ? Gamepad.current : null;

        public static void Play(PlayerController p, float low, float high, float seconds) => Play(PadOf(p), low, high, seconds);

        /// A todos los jugadores con mando (explosiones y golpes del jefe).
        public static void PlayAll(float low, float high, float seconds)
        {
            foreach (var h in PlayerHealth.All)
                if (h && h.TryGetComponent<PlayerController>(out var p)) Play(p, low, high, seconds);
        }

        public static void Play(Gamepad pad, float low, float high, float seconds)
        {
            if (!Enabled || pad == null || !pad.added || Time.timeScale == 0f) return;
            // si ya vibra, se queda con lo más fuerte y lo más largo
            float until = Time.unscaledTime + seconds;
            if (active.TryGetValue(pad, out var cur))
            {
                low = Mathf.Max(low, cur.low);
                high = Mathf.Max(high, cur.high);
                until = Mathf.Max(until, cur.until);
            }
            active[pad] = new Pulse { low = low, high = high, until = until };
            Motors(pad, Mathf.Clamp01(low), Mathf.Clamp01(high));
            if (runner == null)
            {
                runner = new GameObject("Rumble").AddComponent<Runner>();
                Object.DontDestroyOnLoad(runner.gameObject);
                runner.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        // el DualSense va por DualSenseFx: Unity solo le manda vibración por USB y pisaría la luz y los gatillos
        static void Motors(Gamepad pad, float low, float high)
        {
            if (DualSenseFx.Handles(pad)) DualSenseFx.SetMotors(pad, low, high);
            else pad.SetMotorSpeeds(low, high);
        }

        static void StopAll()
        {
            foreach (var pad in active.Keys)
                if (pad.added) Motors(pad, 0f, 0f);
            active.Clear();
        }

        // apaga cada mando al terminar su pulso, al pausar y al salir (si no, se queda vibrando)
        sealed class Runner : MonoBehaviour
        {
            readonly List<Gamepad> done = new();

            void Update()
            {
                if (active.Count == 0) return;
                if (Time.timeScale == 0f) { StopAll(); return; }
                done.Clear();
                foreach (var kv in active)
                    if (Time.unscaledTime >= kv.Value.until || !kv.Key.added) done.Add(kv.Key);
                foreach (var pad in done)
                {
                    if (pad.added) Motors(pad, 0f, 0f);
                    active.Remove(pad);
                }
            }

            void OnApplicationFocus(bool focus) { if (!focus) StopAll(); }
            void OnDestroy() => StopAll();
        }
    }
}
