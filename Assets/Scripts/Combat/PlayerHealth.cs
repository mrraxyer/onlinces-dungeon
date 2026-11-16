using System;
using System.Collections;
using UnityEngine;

namespace OnLinces
{
    /// Vida del jugador en medios corazones (6 = 3 corazones). Parpadea y es invulnerable un momento tras recibir daño.
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Medios corazones. Godot: Miembro = 6")]
        [SerializeField] int maxHp = 6;
        [Tooltip("Off tiene +1 corazón (Godot: hp_bonus 2)")]
        [SerializeField] int offBonus = 2;
        [SerializeField] float invulnerableTime = 0.8f;
        [Tooltip("Empujón al recibir daño, en píxeles por segundo")]
        [SerializeField] float knockbackPx = 200f;
        [SerializeField] PlayerController controller;
        [Tooltip("Burbuja que se ve mientras tienes escudo (ítem Firewall)")]
        [SerializeField] GameObject shieldVisual;
        [SerializeField] AudioClip hurtClip;
        [SerializeField] AudioClip dieClip;
        [SerializeField] AudioClip healClip;
        [SerializeField] AudioClip shieldBreakClip;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasShield { get; private set; }
        /// Para "Commit limpio": ¿te dañaron en la sala actual?
        public bool TookDamageThisRoom { get; set; }
        /// Se dispara cuando cambia la vida (para el HUD).
        public event Action Changed;

        float iframes;
        SpriteRenderer[] renderers;

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);

        void Start()
        {
            var run = GameManager.Instance.Run;
            MaxHp = maxHp + run.BonusMaxHp + (run.Mascot == Mascot.Off ? offBonus : 0);
            Hp = run.Hp > 0 ? Mathf.Min(run.Hp, MaxHp) : MaxHp;
            Changed += () => run.Hp = Hp; // la vida se conserva entre pisos
            if (shieldVisual) shieldVisual.SetActive(false);
            Changed?.Invoke();
        }

        void Update()
        {
            iframes -= Time.deltaTime;
            float a = iframes > 0f && (int)(iframes * 14f) % 2 == 0 ? 0.35f : 1f;
            foreach (var r in renderers)
            {
                var c = r.color; c.a = a; r.color = c;
            }
        }

        public void Hurt(int amount, Vector2 from)
        {
            if (iframes > 0f || IsDead) return;
            iframes = invulnerableTime;
            var away = (Vector2)transform.position - from;
            if (away.sqrMagnitude > 0.0001f) controller.Knock(away.normalized * knockbackPx);

            if (HasShield)
            {
                HasShield = false;
                if (shieldVisual) shieldVisual.SetActive(false);
                HUD.Feedback("Escudo roto", HUD.Bad);
                Sfx.Play(shieldBreakClip);
                return;
            }

            TookDamageThisRoom = true;
            int lost = Mathf.Min(Hp, amount);
            Hp = Mathf.Max(0, Hp - amount);
            HUD.Feedback("-" + HalfHearts(lost) + " vida", HUD.Bad);
            Changed?.Invoke();
            if (Hp == 0) StartCoroutine(Die());
            else Sfx.Play(hurtClip);
        }

        /// Cura medios corazones. Devuelve false si ya estabas lleno.
        public bool Heal(int amount)
        {
            if (IsDead || Hp >= MaxHp) return false;
            int before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            Sfx.Play(healClip, 0.7f);
            HUD.Feedback("+" + HalfHearts(Hp - before) + " vida", HUD.Good);
            Changed?.Invoke();
            return true;
        }

        /// 1 → "½", 2 → "1", 3 → "1½"
        static string HalfHearts(int halves) => (halves / 2 > 0 ? (halves / 2).ToString() : "") + (halves % 2 == 1 ? "½" : "");

        public void RaiseMax(int amount)
        {
            MaxHp += amount;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            HUD.Feedback("+" + HalfHearts(amount) + " corazón máximo", HUD.Good);
            Changed?.Invoke();
        }

        public void GiveShield()
        {
            if (!HasShield) HUD.Feedback("Escudo activado", HUD.Info);
            HasShield = true;
            if (shieldVisual) shieldVisual.SetActive(true);
        }

        IEnumerator Die()
        {
            IsDead = true;
            controller.enabled = false;
            Sfx.Play(dieClip);
            yield return new WaitForSeconds(1.2f);
            SceneFlow.Die();
        }
    }
}
