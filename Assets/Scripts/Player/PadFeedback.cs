using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Lo que el mando del jugador hace por su cuenta, según el estado del juego:
    /// · cualquier mando: vibración de habilidad lista y de quedarse sin energía;
    /// · DualSense: barra de luz por capas (base, estado, destellos), LED de jugador, R2 según el arma y L2 según la habilidad.
    /// Lo agrega PlayerController.
    public sealed class PadFeedback : MonoBehaviour
    {
        /// Un destello nuevo solo reemplaza a otro de prioridad igual o menor mientras dure.
        public enum Priority { Shot, Event, Hit }

        static readonly Color OnColor = new(0.25f, 0.55f, 1f), OffColor = new(0.49f, 0.23f, 0.93f);
        static readonly Color HurtColor = new(1f, 0.08f, 0.05f), ShieldColor = new(0.3f, 0.95f, 1f), ReadyColor = new(0.3f, 1f, 0.4f);
        public static readonly Color ClearColor = new(0.35f, 1f, 0.45f), GoldColor = new(1f, 0.78f, 0.2f);

        // destello en todos los mandos (jefe, sala limpia); rainbow = barrido de colores (jefe vencido)
        static Color allColor;
        static float allUntil;
        static bool allRainbow;

        PlayerController controller;
        PlayerHealth health;
        PlayerWeapons weapons;
        PlayerSkill skill;
        Gamepad pad;

        Color flashColor;
        float flashUntil, deadSince = -1f, r2PulseUntil, readyTickAt = -1f;
        Priority flashPriority;
        int lastHp = -1, lastEnergy = -1;
        bool wasReady = true, couldShoot = true;

        public static void FlashAll(Color c, float seconds = 0.4f, bool rainbow = false)
        {
            allColor = c;
            allUntil = Time.unscaledTime + seconds;
            allRainbow = rainbow;
        }

        /// Destello solo en el mando de ese jugador.
        public static void Flash(Component player, Color c, float seconds, Priority priority)
        {
            if (player && player.TryGetComponent<PadFeedback>(out var fb)) fb.Flash(c, seconds, priority);
        }

        void Flash(Color c, float seconds, Priority priority)
        {
            if (Time.unscaledTime < flashUntil && priority < flashPriority) return;
            flashColor = c;
            flashUntil = Time.unscaledTime + seconds;
            flashPriority = priority;
        }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            weapons = GetComponent<PlayerWeapons>();
            skill = GetComponent<PlayerSkill>();
        }

        void Update()
        {
            Events();
            var p = Rumble.PadOf(controller);
            if (p != pad) { Release(); pad = p; }
            if (!DualSenseFx.Handles(pad)) return;

            DualSenseFx.SetPlayer(pad, controller.IsGuest ? 2 : 1);
            DualSenseFx.SetLight(pad, LightColor());
            DualSenseFx.SetTrigger(pad, RightTrigger());
            DualSenseFx.SetLeftTrigger(pad, LeftTrigger());
        }

        // cambios de estado que se avisan con vibración (cualquier mando) y destellos
        void Events()
        {
            float now = Time.unscaledTime;
            if (health)
            {
                if (health.Hp < lastHp) Flash(HurtColor, 0.25f, Priority.Hit);
                lastHp = health.Hp;
                if (health.IsDead && deadSince < 0f) deadSince = now;
                if (!health.IsDead) deadSince = -1f;
            }

            // habilidad lista otra vez: doble tic suave y destello verde
            bool ready = skill && skill.Role && skill.Ready;
            if (ready && !wasReady)
            {
                Rumble.Play(controller, 0f, 0.35f, 0.05f);
                readyTickAt = now + 0.12f;
                Flash(ReadyColor, 0.3f, Priority.Event);
            }
            wasReady = ready;
            if (readyTickAt > 0f && now >= readyTickAt) { readyTickAt = -1f; Rumble.Play(controller, 0f, 0.35f, 0.05f); }

            if (!weapons) return;
            var w = weapons.Current;
            bool canShoot = weapons.HasEnergyFor(w);
            // se acabó la energía: golpe sordo
            if (couldShoot && !canShoot && w != null) Rumble.Play(controller, 0.5f, 0f, 0.15f);
            // sin energía y se recupera un punto: pulso en R2 para saber cuándo vuelve
            if (!canShoot && lastEnergy >= 0 && weapons.Energy > lastEnergy) r2PulseUntil = now + 0.12f;
            couldShoot = canShoot;
            lastEnergy = weapons.Energy;
        }

        // al cambiar de escena o morir el objeto se apaga: gatillos sueltos
        void OnDisable() => Release();

        void Release()
        {
            if (!DualSenseFx.Handles(pad)) return;
            DualSenseFx.SetTrigger(pad, DualSenseFx.Trigger.Off);
            DualSenseFx.SetLeftTrigger(pad, DualSenseFx.Trigger.Off);
        }

        // ---------------- luz: base → estado → destellos ----------------
        Color LightColor()
        {
            float now = Time.unscaledTime;
            var c = StateColor(now);

            if (now < allUntil) c = allRainbow ? Color.HSVToRGB(Step(now * 1.5f % 1f, 12), 1f, 1f) : allColor;
            if (now < flashUntil) c = flashColor;

            if (deadSince >= 0f) c *= 1f - Mathf.Clamp01((now - deadSince) / 1f); // se apaga en 1 s
            if (Time.timeScale == 0f) c *= 0.3f;
            return c;
        }

        Color StateColor(float now)
        {
            if (health && health.Hp * 5 <= health.MaxHp && !health.IsDead)
                return Color.Lerp(HurtColor * 0.2f, HurtColor, Step(0.5f + 0.5f * Mathf.Sin(now * 6f), 5)); // latido

            if (skill && skill.ActiveSkill is SkillKind active)
                return active switch
                {
                    SkillKind.DeployDoble => ReadyColor,
                    SkillKind.JuntaGeneral => GoldColor * Mathf.Lerp(0.5f, 1f, Step(0.5f + 0.5f * Mathf.Sin(now * 8f), 4)),
                    SkillKind.Suplencia => ShieldColor,
                    _ => new Color(1f, 0.45f, 0.1f) * (Mathf.PerlinNoise(Step(now * 10f, 1) * 0.37f, 0f) > 0.4f ? 1f : 0.45f), // Firewall: parpadeo
                };

            var c = controller.Mascot == Mascot.Off ? OffColor : OnColor;
            if (Boss.Current) c = Color.Lerp(c, Boss.Current.Tint, 0.5f);

            // brillo según la energía; sin disparar, respira despacio
            float energy = weapons && weapons.MaxEnergy > 0 ? (float)weapons.Energy / weapons.MaxEnergy : 1f;
            float k = Mathf.Lerp(0.25f, 1f, energy);
            if (weapons && Time.time - weapons.LastShotTime > 1.5f) k *= 0.85f + 0.15f * Mathf.Sin(now * 1.5f);
            return c * Step(k, 16);
        }

        // en escalones: solo se manda un paquete cuando cambia el escalón, no en cada frame
        static float Step(float v, int steps) => Mathf.Round(v * steps) / steps;

        // ---------------- R2: perfil del arma + lo que pasa en el juego ----------------
        DualSenseFx.Trigger RightTrigger()
        {
            var w = weapons ? weapons.Current : null;
            if (Off() || w == null) return DualSenseFx.Trigger.Off;
            var p = w.trigger;
            float now = Time.unscaledTime;
            if (!weapons.HasEnergyFor(w))
                return now < r2PulseUntil ? new DualSenseFx.Trigger(TriggerProfile.Mode.Vibration, 0, 9, 6, 6, 20) : DualSenseFx.Trigger.Locked;
            float sinceShot = Time.time - weapons.LastShotTime;
            // el pulso del disparo va antes que los trabados: si no, las armas que se traban nunca vibran
            if (sinceShot < p.shotPulseSeconds)
                return new DualSenseFx.Trigger(TriggerProfile.Mode.Vibration, 0, 9, 8, 8, p.frequency);
            if (p.lockWhileReloading && weapons.Cooldown > 0f) return DualSenseFx.Trigger.Locked;
            if (p.lockWhileBulletFlies && sinceShot < w.range) return DualSenseFx.Trigger.Locked;

            // Deploy doble: dispara las dos, el gatillo toma el perfil más duro
            if (skill && skill.IsActive(SkillKind.DeployDoble) && weapons.Other is WeaponData other && other.trigger.strength > p.strength)
                p = other.trigger;
            var t = DualSenseFx.Trigger.From(p);
            if (skill && skill.IsActive(SkillKind.JuntaGeneral))
                t = p.mode == TriggerProfile.Mode.Vibration ? t.Harder(2) : new DualSenseFx.Trigger(TriggerProfile.Mode.Vibration, p.start, 9, 3, 3, 40);
            // le quedan 2 disparos o menos: más duro, como aviso
            else if (w.energyCost > 0 && weapons.Energy < w.energyCost * 3) t = t.Harder(2);
            return t;
        }

        // ---------------- L2: la habilidad ----------------
        DualSenseFx.Trigger LeftTrigger()
        {
            if (Off() || !skill || !skill.Role) return DualSenseFx.Trigger.Off;
            if (skill.ActiveSkill is SkillKind active)
            {
                // activa: un pulso al pasar cada segundo del conteo y duro entre pulsos
                float left = skill.ActiveLeft;
                bool pulse = left - Mathf.Floor(left) > 0.8f;
                if (!pulse) return DualSenseFx.Trigger.Locked;
                int hz = active switch
                {
                    SkillKind.DeployDoble => 10,  // dos armas
                    SkillKind.JuntaGeneral => 20, // daño x2
                    SkillKind.Suplencia => 8,     // escudo: sordo
                    _ => 40,                      // Firewall: eléctrico
                };
                return new DualSenseFx.Trigger(TriggerProfile.Mode.Vibration, 0, 9, 8, 8, hz);
            }
            if (skill.Ready) return new DualSenseFx.Trigger(TriggerProfile.Mode.Weapon, 4, 6, 2); // lista: clic ligero
            // recargando: duro recién usada y se afloja mientras carga
            return new DualSenseFx.Trigger(TriggerProfile.Mode.Feedback, 0, 9, 1 + Mathf.RoundToInt(7f * skill.CooldownFraction));
        }

        bool Off() => !Rumble.Enabled || (health && health.IsDead) || Time.timeScale == 0f || !controller.enabled;
    }
}
