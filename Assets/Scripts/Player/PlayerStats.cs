using UnityEngine;

namespace OnLinces
{
    /// Modificadores del jugador por ítems y efectos temporales (café). Los ítems vienen de RunState
    /// y se vuelven a aplicar en cada piso.
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;

        public float SpeedMult { get; private set; } = 1f;
        public float FireDelayMult { get; private set; } = 1f;
        public float DamageMult { get; private set; } = 1f;
        public bool HasShieldItem { get; private set; }
        public bool HasDebugger { get; private set; }
        public bool Boosted => boostT > 0f;

        float boostT;

        void Start()
        {
            var run = GameManager.Instance.Run;
            // ventaja de la mascota (se muestra en la pantalla de inicio). El +1 corazón de Off está en PlayerHealth.
            if (run.Mascot == Mascot.On) { SpeedMult *= 1.1f; FireDelayMult /= 1.1f; } // +10% velocidad y cadencia
            else DamageMult *= 1.15f;                                                     // +15% daño
            foreach (var it in run.Items) ApplyPassive(it);
        }

        /// Textos de la pantalla de inicio (apodo y ventaja de cada mascota).
        public static string MascotTitle(Mascot m) => m == Mascot.On ? "El optimista" : "El escéptico";
        public static string MascotPerk(Mascot m) => m == Mascot.On ? "+10% velocidad y cadencia" : "+15% daño y +1 corazón";

        void Update() => boostT -= Time.deltaTime;

        /// Velocidad extra por un rato (café, bebida ON). Godot: x1.3
        public void Boost(float seconds) => boostT = Mathf.Max(boostT, seconds);
        public float CurrentSpeedMult => SpeedMult * (Boosted ? 1.3f : 1f);

        /// Recoger un ítem nuevo: se guarda en la partida y aplica sus efectos de una vez.
        public void Collect(ItemData item)
        {
            GameManager.Instance.Run.Items.Add(item);
            ApplyPassive(item);
            switch (item.effect)
            {
                case ItemEffect.UnitTests:
                    GameManager.Instance.Run.BonusMaxHp += 2;
                    health.RaiseMax(2);
                    break;
                case ItemEffect.Debugger:
                    health.Heal(1);
                    break;
                case ItemEffect.Firewall:
                    health.GiveShield();
                    break;
            }
            HUD.Feedback("Nuevo ítem: " + item.displayName, HUD.Info);
        }

        // Efectos que se recalculan en cada piso (valores de Godot: apply_item)
        void ApplyPassive(ItemData item)
        {
            switch (item.effect)
            {
                case ItemEffect.Cafe: SpeedMult *= 1.13f; FireDelayMult *= 0.9f; break;
                case ItemEffect.Teclado: FireDelayMult *= 0.8f; break;
                case ItemEffect.GitPush: DamageMult *= 1.25f; break;
                case ItemEffect.Debugger: HasDebugger = true; break;
                case ItemEffect.Firewall: HasShieldItem = true; break;
            }
        }

        /// Llamado por la sala al entrar: Firewall da un escudo por sala.
        public void OnRoomEntered()
        {
            if (HasShieldItem) health.GiveShield();
        }

        /// Llamado por la sala al limpiarla.
        public void OnRoomCleared(bool tookDamage)
        {
            if (HasDebugger) health.Heal(1);
            if (!tookDamage && health.Hp < health.MaxHp)
            {
                HUD.Feedback("Commit limpio", HUD.Good);
                health.Heal(1);
            }
        }
    }
}
