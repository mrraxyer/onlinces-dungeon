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
        [SerializeField] AudioClip hurtClip;
        [SerializeField] AudioClip dieClip;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDead { get; private set; }
        /// Se dispara cuando cambia la vida (para el HUD).
        public event Action Changed;

        float iframes;
        SpriteRenderer[] renderers;

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);

        void Start()
        {
            var run = GameManager.Instance.Run;
            MaxHp = maxHp + (run.Mascot == Mascot.Off ? offBonus : 0);
            Hp = run.Hp > 0 ? Mathf.Min(run.Hp, MaxHp) : MaxHp;
            Changed += () => run.Hp = Hp; // la vida se conserva entre pisos
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

            Hp = Mathf.Max(0, Hp - amount);
            Changed?.Invoke();
            if (Hp == 0) StartCoroutine(Die());
            else Sfx.Play(hurtClip);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
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
