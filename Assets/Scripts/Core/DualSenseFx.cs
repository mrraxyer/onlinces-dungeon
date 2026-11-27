using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace OnLinces
{
    /// DualSense (PS5) por USB y Bluetooth: vibración, gatillo R2 adaptativo, barra de luz y LEDs de jugador.
    /// Unity solo manda vibración y luz por USB y no tiene gatillos ni LEDs, así que aquí se arma el reporte HID completo.
    /// Cada envío lleva todo el estado del mando; solo se manda cuando algo cambió.
    public static class DualSenseFx
    {
        /// Efecto de un gatillo. Zonas 0-9 a lo largo del recorrido, fuerza 1-8, frecuencia en Hz.
        public readonly struct Trigger : IEquatable<Trigger>
        {
            public readonly TriggerProfile.Mode mode;
            public readonly int start, end, strength, endStrength, frequency;

            public Trigger(TriggerProfile.Mode mode, int start = 0, int end = 9, int strength = 1, int endStrength = 1, int frequency = 0)
            {
                this.mode = mode;
                this.start = Mathf.Clamp(start, 0, 9);
                this.end = Mathf.Clamp(end, 1, 9);
                this.strength = Mathf.Clamp(strength, 1, 8);
                this.endStrength = Mathf.Clamp(endStrength, 1, 8);
                this.frequency = Mathf.Clamp(frequency, 0, 255);
            }

            public static Trigger From(TriggerProfile p) => new(p.mode, p.start, p.end, p.strength, p.endStrength, p.frequency);
            public static readonly Trigger Off = new(TriggerProfile.Mode.Off);
            /// Trabado en todo el recorrido.
            public static readonly Trigger Locked = new(TriggerProfile.Mode.Feedback, 0, 9, 8);

            /// Mismo efecto, más duro (energía baja).
            public Trigger Harder(int extra) => new(mode, start, end, strength + extra, endStrength + extra, frequency);

            public bool Equals(Trigger o) => mode == o.mode && start == o.start && end == o.end && strength == o.strength && endStrength == o.endStrength && frequency == o.frequency;
            public override bool Equals(object o) => o is Trigger t && Equals(t);
            public override int GetHashCode() => HashCode.Combine(mode, start, end, strength, endStrength, frequency);
            public static bool operator ==(Trigger a, Trigger b) => a.Equals(b);
            public static bool operator !=(Trigger a, Trigger b) => !a.Equals(b);
        }

        sealed class State
        {
            public byte low, high, r, g, b, leds;
            public Trigger trigger = Trigger.Off, left = Trigger.Off;
            public byte seq;
            public bool bluetooth, sent, dirty;
            public int reportSize;
        }

        static readonly Dictionary<DualSenseGamepadHID, State> pads = new();
        static Flusher flusher;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pads.Clear();
            flusher = null;
            Application.quitting -= ResetAll;
            Application.quitting += ResetAll;
        }

        public static bool Handles(Gamepad pad) => pad is DualSenseGamepadHID ds && ds.added;

        public static void SetMotors(Gamepad pad, float low, float high) =>
            Change(pad, s => { s.low = Byte(low); s.high = Byte(high); });

        public static void SetLight(Gamepad pad, Color c) =>
            Change(pad, s => { s.r = Byte(c.r); s.g = Byte(c.g); s.b = Byte(c.b); });

        /// 1 = LED del centro; 2 = los dos de en medio (como en la PS5).
        public static void SetPlayer(Gamepad pad, int player) =>
            Change(pad, s => s.leds = player switch { 1 => 0x04, 2 => 0x0A, 3 => 0x15, 4 => 0x1B, _ => 0 });

        /// R2.
        public static void SetTrigger(Gamepad pad, Trigger t) => Change(pad, s => s.trigger = t);
        /// L2.
        public static void SetLeftTrigger(Gamepad pad, Trigger t) => Change(pad, s => s.left = t);

        /// Gatillos sueltos y motores apagados (al salir: si no, R2 se queda duro fuera del juego).
        public static void ResetAll()
        {
            foreach (var kv in pads)
            {
                kv.Value.low = kv.Value.high = 0;
                kv.Value.trigger = kv.Value.left = Trigger.Off;
                if (kv.Key.added) Send(kv.Key, kv.Value);
            }
        }

        static void Change(Gamepad pad, Action<State> edit)
        {
            if (pad is not DualSenseGamepadHID ds || !ds.added) return;
            if (!pads.TryGetValue(ds, out var s)) pads[ds] = s = NewState(ds);
            var before = (s.low, s.high, s.r, s.g, s.b, s.leds, s.trigger, s.left);
            edit(s);
            if (s.sent && before == (s.low, s.high, s.r, s.g, s.b, s.leds, s.trigger, s.left)) return;
            // un solo paquete por frame con todo lo que cambió: por Bluetooth, varios seguidos pueden fallar
            if (!Application.isPlaying) { Send(ds, s); return; }
            s.dirty = true;
            if (flusher == null)
            {
                flusher = new GameObject("DualSenseFx").AddComponent<Flusher>();
                UnityEngine.Object.DontDestroyOnLoad(flusher.gameObject);
                flusher.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        // manda al final del frame; si el mando no lo aceptó, reintenta en el siguiente
        sealed class Flusher : MonoBehaviour
        {
            void LateUpdate()
            {
                foreach (var kv in pads)
                    if (kv.Value.dirty && kv.Key.added && Send(kv.Key, kv.Value)) kv.Value.dirty = false;
            }

            // al salir de Play en el editor (en el juego lo cubre Application.quitting)
            void OnDestroy() => ResetAll();
        }

        static State NewState(InputDevice pad)
        {
            int size = 0;
            try { size = HID.HIDDeviceDescriptor.FromJson(pad.description.capabilities).outputReportSize; }
            catch (Exception) { /* sin descriptor: se asume USB */ }
            // por USB el reporte de salida mide 48; por Bluetooth, 78 o más
            bool bt = size >= BtLength;
            return new State { bluetooth = bt, reportSize = bt ? Mathf.Min(size, MaxReport) : UsbLength };
        }

        // ---------------- reporte HID ----------------
        const int UsbLength = 48, BtLength = 78, MaxReport = 600;

        [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.BaseCommandSize + MaxReport)]
        struct OutputCommand : IInputDeviceCommandInfo
        {
            [FieldOffset(0)] public InputDeviceCommand baseCommand;
            public FourCC typeStatic => new('H', 'I', 'D', 'O');
        }

        static bool Send(DualSenseGamepadHID pad, State s)
        {
            var cmd = new OutputCommand { baseCommand = new InputDeviceCommand(new FourCC('H', 'I', 'D', 'O'), InputDeviceCommand.BaseCommandSize + s.reportSize) };
            var report = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref cmd, 1)).Slice(InputDeviceCommand.BaseCommandSize, s.reportSize);

            // por Bluetooth el bloque común va 2 bytes más adelante (secuencia + etiqueta) y cierra con CRC32
            int o;
            if (s.bluetooth)
            {
                report[0] = 0x31;
                report[1] = (byte)((s.seq++ & 0xF) << 4);
                report[2] = 0x10;
                o = 3;
            }
            else
            {
                report[0] = 0x02;
                o = 1;
            }
            report[o + 0] = 0x01 | 0x02 | 0x04 | 0x08; // vibración clásica, sin háptica HD, gatillo derecho, gatillo izquierdo
            report[o + 1] = 0x04 | 0x10;               // barra de luz, LEDs de jugador
            report[o + 2] = s.high;                    // motor derecho (chico)
            report[o + 3] = s.low;                     // motor izquierdo (grande)
            WriteTrigger(report.Slice(o + 10, 11), s.trigger);    // R2
            WriteTrigger(report.Slice(o + 21, 11), s.left);       // L2 (habilidad)
            // firmware reciente: la vibración clásica también necesita este bit (el viejo lo ignora)
            report[o + 38] = 0x04;
            if (!s.sent)
            {
                report[o + 38] |= 0x02; // la primera vez: apaga la luz azul de encendido para poder pintarla
                report[o + 41] = 0x02;
            }
            report[o + 43] = s.leds;
            report[o + 44] = s.r;
            report[o + 45] = s.g;
            report[o + 46] = s.b;

            if (s.bluetooth)
            {
                uint crc = Crc32(report.Slice(0, BtLength - 4));
                report[BtLength - 4] = (byte)crc;
                report[BtLength - 3] = (byte)(crc >> 8);
                report[BtLength - 2] = (byte)(crc >> 16);
                report[BtLength - 1] = (byte)(crc >> 24);
            }
            bool ok = pad.ExecuteCommand(ref cmd) >= 0;
            if (ok) s.sent = true; // hasta el primero aceptado se repite el apagado de la luz azul
            return ok;
        }

        // efecto de gatillo: modo + 10 parámetros
        static void WriteTrigger(Span<byte> t, Trigger e)
        {
            t.Clear();
            switch (e.mode)
            {
                case TriggerProfile.Mode.Weapon: Weapon(t, e.start, e.end, e.strength); break;
                case TriggerProfile.Mode.Feedback: Zones(t, 0x21, e.start, e.end, e.strength, e.strength); break;
                case TriggerProfile.Mode.Slope: Zones(t, 0x21, e.start, e.end, e.strength, e.endStrength); break;
                case TriggerProfile.Mode.Vibration: Zones(t, 0x26, e.start, e.end, e.strength, e.strength); t[9] = (byte)Mathf.Max(1, e.frequency); break;
                default: t[0] = 0x05; break;
            }
        }

        // el modo Weapon del mando solo acepta inicio 2-7 y fin hasta 8
        static void Weapon(Span<byte> t, int start, int end, int strength)
        {
            start = Mathf.Clamp(start, 2, 7);
            end = Mathf.Clamp(end, start + 1, 8);
            int zones = (1 << start) | (1 << end);
            t[0] = 0x25;
            t[1] = (byte)zones;
            t[2] = (byte)(zones >> 8);
            t[3] = (byte)(strength - 1);
        }

        /// Fuerza (o amplitud) por zona desde start: pasa de s0 a s1 hasta end y sigue en s1 hasta el fondo.
        /// 0x21 = resistencia, 0x26 = vibración; cada zona usa 3 bits.
        static void Zones(Span<byte> t, byte mode, int start, int end, int s0, int s1)
        {
            uint active = 0, values = 0;
            for (int i = start; i < 10; i++)
            {
                float k = end > start ? Mathf.Clamp01((i - start) / (float)(end - start)) : 1f;
                int v = Mathf.RoundToInt(Mathf.Lerp(s0, s1, k));
                active |= 1u << i;
                values |= (uint)(Mathf.Clamp(v, 1, 8) - 1) << (3 * i);
            }
            t[0] = mode;
            t[1] = (byte)active;
            t[2] = (byte)(active >> 8);
            for (int k = 0; k < 4; k++) t[3 + k] = (byte)(values >> (8 * k));
        }

        static byte Byte(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);

        // CRC-32 estándar, sembrado con el byte 0xA2 que el DualSense espera en reportes de salida por Bluetooth
        static uint Crc32(ReadOnlySpan<byte> data, bool seed = true)
        {
            uint crc = 0xFFFFFFFF;
            if (seed) crc = Step(crc, 0xA2);
            foreach (var b in data) crc = Step(crc, b);
            return ~crc;
        }

        static uint Step(uint c, byte b)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c >> 1) ^ (0xEDB88320u & (uint)-(int)(c & 1));
            return c;
        }

        /// Comprobación del CRC con el valor de referencia ("123456789" sin semilla = 0xCBF43926).
        internal static bool SelfTest() => Crc32(System.Text.Encoding.ASCII.GetBytes("123456789"), seed: false) == 0xCBF43926u;
    }
}
