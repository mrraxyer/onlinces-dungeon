using System;
using System.Collections;
using System.Collections.Generic;
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

        float iframes, shieldTimer;
        SpriteRenderer[] renderers;

        /// Jugadores en la escena (1 o 2).
        public static readonly List<PlayerHealth> All = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        /// El vivo más cercano (o cualquiera si todos murieron).
        public static PlayerHealth Nearest(Vector2 from)
        {
            PlayerHealth best = null;
            float bestD = float.MaxValue;
            foreach (var p in All)
            {
                float d = ((Vector2)p.transform.position - from).sqrMagnitude + (p.IsDead ? 1e6f : 0f);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        /// Al cerrarse una sala, los demás entran con quien la abrió.
        public static void GatherAt(PlayerHealth who)
        {
            foreach (var p in All)
                if (p != who && !p.IsDead) p.GetComponent<Rigidbody2D>().position = who.transform.position;
        }

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var run = GameManager.Instance.Run;
            MaxHp = ComputeMax();
            bool guest = controller && controller.IsGuest;
            Hp = run.Hp > 0 && !guest ? Mathf.Min(run.Hp, MaxHp) : MaxHp;
            if (!guest) Changed += () => run.Hp = Hp; // pasa al siguiente piso
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
            return baseHp + run.BonusMaxHp + (controller.Mascot == Mascot.Off ? offBonus : 0) + DifficultyInfo.PlayerHp(run.Difficulty);
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
            if (shieldTimer > 0f && (shieldTimer -= Time.deltaTime) <= 0f && HasShield)
            {
                Shield = 0;
                if (shieldVisual) shieldVisual.SetActive(false);
                HUD.Feedback("Escudo agotado", HUD.Info);
                Changed?.Invoke();
            }
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
            HitFeedback.PlayerHit(transform, away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero, !HasShield);

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
            HUD.Feedback("-" + lost + " vida", HUD.Bad);
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
            HUD.Feedback("+" + (Hp - before) + " vida", HUD.Good);
            Changed?.Invoke();
            return true;
        }

        public void RaiseMax(int amount)
        {
            MaxHp += amount;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            HUD.Feedback("+" + amount + " vida máxima", HUD.Good);
            Changed?.Invoke();
        }

        /// seconds > 0: se acaba solo (si no había uno permanente).
        public void GiveShield(float seconds = 0f)
        {
            if (!HasShield) HUD.Feedback("Escudo activado", HUD.Info);
            shieldTimer = seconds > 0f && (!HasShield || shieldTimer > 0f) ? seconds : 0f;
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
            // en cooperativo sigue el otro y el caído sale del mapa; vuelve en el siguiente piso
            foreach (var p in All) if (!p.IsDead) { gameObject.SetActive(false); yield break; }
            SceneFlow.Die();
        }
    }
}
