using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Lo que el DualSense del jugador muestra: color de su personaje en la barra de luz (rojo al recibir daño,
    /// latido rojo con poca vida), LED de jugador 1 o 2 y la resistencia del gatillo R2 según el arma.
    /// Con otro mando no hace nada. Lo agrega PlayerController.
    public sealed class PadFeedback : MonoBehaviour
    {
        static readonly Color OnColor = new(0.25f, 0.55f, 1f), OffColor = new(0.49f, 0.23f, 0.93f), HurtColor = new(1f, 0.08f, 0.05f);
        static Color flashColor;
        static float flashAllUntil;

        PlayerController controller;
        PlayerHealth health;
        PlayerWeapons weapons;
        Gamepad pad;
        int lastHp = -1;
        float hurtUntil;

        /// Destello en todos los mandos (cambio de fase del jefe).
        public static void FlashAll(Color c, float seconds = 0.4f)
        {
            flashColor = c;
            flashAllUntil = Time.unscaledTime + seconds;
        }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            weapons = GetComponent<PlayerWeapons>();
        }

        void Update()
        {
            var p = Rumble.PadOf(controller);
            if (p != pad) { Release(); pad = p; }
            if (!DualSenseFx.Handles(pad)) return;

            if (health && health.Hp < lastHp) hurtUntil = Time.unscaledTime + 0.25f;
            if (health) lastHp = health.Hp;

            DualSenseFx.SetPlayer(pad, controller.IsGuest ? 2 : 1);
            DualSenseFx.SetLight(pad, LightColor());
            DualSenseFx.SetTrigger(pad, TriggerFor());
        }

        // al cambiar de escena o morir el objeto se apaga: R2 queda suelto
        void OnDisable() => Release();

        void Release()
        {
            if (DualSenseFx.Handles(pad)) DualSenseFx.SetTrigger(pad, DualSenseFx.Trigger.Off);
        }

        Color LightColor()
        {
            float now = Time.unscaledTime;
            if (now < hurtUntil) return HurtColor;
            if (now < flashAllUntil) return flashColor;
            var baseColor = controller.Mascot == Mascot.Off ? OffColor : OnColor;
            if (!health) return baseColor;
            if (health.IsDead) return baseColor * 0.1f;
            if (health.Hp * 5 <= health.MaxHp)
            {
                // latido en 5 escalones: así solo se manda un reporte cuando cambia el escalón
                float k = Mathf.Round((0.5f + 0.5f * Mathf.Sin(now * 6f)) * 5f) / 5f;
                return Color.Lerp(HurtColor * 0.2f, HurtColor, k);
            }
            return baseColor;
        }

        DualSenseFx.Trigger TriggerFor()
        {
            var w = weapons ? weapons.Current : null;
            if (!Rumble.Enabled || w == null || (health && health.IsDead) || Time.timeScale == 0f || !controller.enabled)
                return DualSenseFx.Trigger.Off;
            if (w.energyCost > weapons.Energy) return DualSenseFx.Trigger.Locked;
            if (w.count > 1) return DualSenseFx.Trigger.Snap;
            if (w.fireDelay < 0.15f) return DualSenseFx.Trigger.Buzz;
            return DualSenseFx.Trigger.Click;
        }
    }
}
