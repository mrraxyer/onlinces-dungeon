using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OnLinces
{
    public enum EnemyKind { Bug, Fast, Glitch, Leak, LeakSmall, CopyPaste }

    /// Enemigo común.
    ///  · Bug/Fast/Leak: persiguen en zigzag (Leak se parte al morir).
    ///  · Glitch: flota y dispara.
    ///  · CopyPaste: se copia cada ~5 s.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Enemy : MonoBehaviour, IHittable
    {
        [Serializable]
        public struct FrameSet { public Sprite[] frames; }

        [SerializeField] EnemyKind kind = EnemyKind.Bug;
        [Tooltip("Campo de tiro: no se mueve, no daña y reaparece al morir")]
        [SerializeField] bool dummy;

        [Header("Arte")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [Tooltip("Opcional, uno por piso")]
        [SerializeField] FrameSet[] floorFrames;
        [SerializeField] float framesPerSecond = 5f;
        [SerializeField] Color tint = Color.white;
        [SerializeField] SpriteFx deathFx;

        [Header("Vida y velocidad (base + por piso)")]
        [SerializeField] float baseHp = 3f;
        [SerializeField] float hpPerFloor = 1f;
        [SerializeField] float baseSpeedPx = 48f;
        [SerializeField] float speedPerFloorPx = 4f;
        [Tooltip("px")]
        [SerializeField] float contactRadiusPx = 17f;

        [Header("Disparo (solo Glitch)")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] Sprite[] bulletSprites;
        [SerializeField] float bulletSpeedPx = 115f;
        [SerializeField] float bulletSpeedPerFloorPx = 10f;

        [Header("Leak: se parte en")]
        [SerializeField] Enemy splitInto;

        [Header("Sonidos")]
        [SerializeField] AudioClip hitClip;
        [SerializeField] AudioClip dieClip;
        [SerializeField] AudioClip shootClip;
        [SerializeField] AudioClip cloneClip;
        [SerializeField] string cloneToast = "Ctrl+V";

        public Room Room { get; set; }
        public bool CanBeHit => spawnT <= 0f && !dying;

        const float SpawnTime = 0.7f;
        const int MaxCopies = 7;
        Rigidbody2D rb;
        PlayerHealth player;
        float hp, fullHp, speedPx, t, spawnT = SpawnTime, shootT = 1.6f, flash, seed, cloneT;
        Vector2 knockPx;
        bool dying;
        int floorNum, generation;
        float? presetHp;
        Sprite[] activeFrames;
        Vector2 home;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            seed = Random.Range(0f, 10f);
            cloneT = Random.Range(4.5f, 5.5f);
        }

        void Start()
        {
            floorNum = GameManager.Instance.Run.Floor;
            hp = presetHp ?? (baseHp + hpPerFloor * floorNum) * DifficultyInfo.EnemyHp(GameManager.Instance.Run.Difficulty);
            fullHp = hp;
            home = transform.position;
            speedPx = baseSpeedPx + speedPerFloorPx * floorNum;
            player = FindAnyObjectByType<PlayerHealth>();
            int i = floorNum - 1;
            activeFrames = floorFrames != null && i >= 0 && i < floorFrames.Length && floorFrames[i].frames is { Length: > 0 }
                ? floorFrames[i].frames : frames;
        }

        /// Aparece casi al instante.
        public void SkipSpawn(float remaining = 0.15f) => spawnT = remaining;
        public void Knock(Vector2 px) => knockPx = px;

        void Update()
        {
            t += Time.deltaTime;
            if (activeFrames != null && activeFrames.Length > 0)
                spriteRenderer.sprite = activeFrames[(int)(t * framesPerSecond) % activeFrames.Length];
            flash -= Time.deltaTime;
            var c = flash > 0f ? new Color(1f, 0.55f, 0.55f) : tint;
            c.a = spawnT > 0f ? 1f - Mathf.Clamp01(spawnT / SpawnTime) : 1f;
            spriteRenderer.color = c;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (spawnT > 0f)
            {
                spawnT -= dt;
                rb.linearVelocity = knockPx / Units.PPU;
                knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 700f * dt);
                return;
            }
            if (dummy)
            {
                // solo retrocede por el golpe y vuelve a su lugar
                rb.linearVelocity = knockPx / Units.PPU + (home - rb.position) * 6f;
                knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 700f * dt);
                return;
            }
            if (player == null || player.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 to = player.transform.position - transform.position;
            Vector2 dir = to.normalized;
            Vector2 side = new(-dir.y, dir.x);
            Vector2 velPx;
            switch (kind)
            {
                case EnemyKind.Glitch:
                    velPx = new Vector2(Mathf.Cos(t * 0.7f + seed), Mathf.Sin(t * 0.9f + seed * 2f)) * speedPx;
                    shootT -= dt;
                    if (shootT <= 0f)
                    {
                        Fire(dir);
                        shootT = Random.Range(1.6f, 2.4f);
                    }
                    break;
                case EnemyKind.CopyPaste:
                    velPx = (dir + side * Mathf.Sin(t * 3f + seed) * 0.4f).normalized * speedPx;
                    spriteRenderer.flipX = to.x < 0f;
                    cloneT -= dt;
                    if (cloneT <= 0f)
                    {
                        cloneT = Random.Range(4.5f, 5.5f);
                        TryClone();
                    }
                    break;
                default: // persiguen en zigzag
                    float wiggle = kind == EnemyKind.Leak ? 0.25f : 0.6f;
                    velPx = (dir + side * Mathf.Sin(t * 6f + seed) * wiggle).normalized * speedPx;
                    spriteRenderer.flipX = to.x < 0f;
                    break;
            }

            rb.linearVelocity = (velPx + knockPx) / Units.PPU;
            knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 700f * dt);

            if (to.magnitude < Units.Px(contactRadiusPx)) player.Hurt(1, transform.position);
        }

        void TryClone()
        {
            if (generation >= 2 || Room == null) return;
            int copies = 0;
            foreach (var e in Room.Alive) if (e.kind == EnemyKind.CopyPaste) copies++;
            if (copies >= MaxCopies) return;
            var pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * Units.Px(18);
            var copy = Instantiate(this, pos, Quaternion.identity);
            copy.generation = generation + 1;
            copy.presetHp = Mathf.Max(2f, hp * 0.8f);
            copy.SkipSpawn(0.3f);
            Room.Register(copy);
            Sfx.Play(cloneClip, 0.5f);
            if (!string.IsNullOrEmpty(cloneToast)) HUD.Toast(cloneToast);
        }

        void Fire(Vector2 dir)
        {
            if (projectilePrefab == null) return;
            var origin = (Vector2)transform.position + Vector2.up * Units.Px(12) + dir * Units.Px(12);
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var s = bulletSprites.Length > 0 ? bulletSprites[Random.Range(0, bulletSprites.Length)] : null;
            float bullet = DifficultyInfo.Bullet(GameManager.Instance.Run.Difficulty);
            p.Launch(s, dir * Units.Px((bulletSpeedPx + bulletSpeedPerFloorPx * floorNum) * bullet), 1f, 3f, fromPlayer: false);
            Sfx.Play(shootClip, 0.6f);
        }

        public void Hit(float damage, Vector2 dir)
        {
            if (!CanBeHit) return;
            hp -= damage;
            flash = 0.08f;
            knockPx = dir * 150f * (kind == EnemyKind.Leak ? 0.4f : 1f);
            Sfx.Play(hitClip, 0.5f);
            if (hp <= 0f) Die();
        }

        /// Muere aunque esté apareciendo.
        public void Kill()
        {
            if (!dying) Die();
        }

        void Die()
        {
            dying = true;
            Sfx.Play(dieClip, 0.7f);
            if (deathFx != null) Instantiate(deathFx, transform.position + Vector3.up * Units.Px(10), Quaternion.identity);

            if (dummy)
            {
                // reaparece en su lugar
                dying = false;
                hp = fullHp;
                knockPx = Vector2.zero;
                rb.position = home;
                spawnT = SpawnTime;
                return;
            }

            if (splitInto != null)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    var child = Instantiate(splitInto, transform.position + Vector3.right * Units.Px(12 * s), Quaternion.identity);
                    child.SkipSpawn();
                    child.Knock(new Vector2(120f * s, 0f));
                    if (Room != null) Room.Register(child);
                }
            }

            if (Room != null) Room.OnEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
