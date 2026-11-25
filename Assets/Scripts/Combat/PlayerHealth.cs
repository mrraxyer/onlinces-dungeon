using System;
using System.Collections;
using UnityEngine;

namespace OnLinces
{
    /// Vida en medios corazones, con invulnerabilidad breve al recibir daño.
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Medios corazones")]
        [SerializeField] int maxHp = 6;
        [Tooltip("Extra de Off")]
        [SerializeField] int offBonus = 2;
        [SerializeField] float invulnerableTime = 0.8f;
        [Tooltip("px/s")]
        [SerializeField] float knockbackPx = 200f;
        [SerializeField] PlayerController controller;
        [Tooltip("Burbuja del escudo")]
        [SerializeField] GameObject shieldVisual;
        [Tooltip("Medios corazones que absorbe")]
        [SerializeField] int shieldPoints = 2;
        [SerializeField] AudioClip hurtClip;
        [SerializeField] AudioClip dieClip;
        [SerializeField] AudioClip healClip;
        [SerializeField] AudioClip shieldBreakClip;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead { get; private set; }
        public int Shield { get; private set; }
        public int MaxShield => shieldPoints;
        public bool HasShield => Shield > 0;
        /// Para "Commit limpio".
        public bool TookDamageThisRoom { get; set; }
        public event Action Changed;

        float iframes;
        SpriteRenderer[] renderers;

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);

        void Start()
        {
            var run = GameManager.Instance.Run;
            MaxHp = ComputeMax();
            Hp = run.Hp > 0 ? Mathf.Min(run.Hp, MaxHp) : MaxHp;
            Changed += () => run.Hp = Hp; // pasa al siguiente piso
            if (shieldVisual) shieldVisual.SetActive(false);
            var role = GetComponent<PlayerRole>();
            if (role) role.Changed += () => { MaxHp = ComputeMax(); Hp = MaxHp; Changed?.Invoke(); };
            Changed?.Invoke();
        }

        int ComputeMax()
        {
            var run = GameManager.Instance.Run;
            var role = GetComponent<PlayerRole>();
            int baseHp = role && role.Current ? role.Current.maxHp : maxHp;
            return baseHp + run.BonusMaxHp + (run.Mascot == Mascot.Off ? offBonus : 0) + DifficultyInfo.PlayerHp(run.Difficulty);
        }

        /// Recalcula al cambiar la dificultad.
        public void RefreshMax()
        {
            MaxHp = ComputeMax();
            Hp = MaxHp;
            Changed?.Invoke();
        }

        /// Invulnerable un momento.
        public void GiveIframes(float seconds) => iframes = Mathf.Max(iframes, seconds);

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
                // absorbe el golpe completo aunque no le alcancen los puntos
                Shield = Mathf.Max(0, Shield - amount);
                if (!HasShield)
                {
                    if (shieldVisual) shieldVisual.SetActive(false);
                    HUD.Feedback("Escudo roto", HUD.Bad);
                    Sfx.Play(shieldBreakClip);
                }
                Changed?.Invoke();
                return;
            }

            TookDamageThisRoom = true;
            int lost = Mathf.Min(Hp, amount);
            Hp = Mathf.Max(0, Hp - amount);
            HUD.Feedback("-" + HalfHearts(lost) + " vida", HUD.Bad);
            Changed?.Invoke();
            if (Hp == 0 && BossRoom.TryRescue(this)) Sfx.Play(hurtClip);
            else if (Hp == 0) StartCoroutine(Die());
            else Sfx.Play(hurtClip);
        }

        /// false si ya estaba lleno.
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

        /// 3 → "1½"
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
            Shield = shieldPoints;
            if (shieldVisual) shieldVisual.SetActive(true);
            Changed?.Invoke();
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
