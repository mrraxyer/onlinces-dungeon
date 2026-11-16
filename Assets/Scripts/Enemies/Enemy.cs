using UnityEngine;

namespace OnLinces
{
    public enum EnemyKind { Bug, Fast, Glitch, Leak, LeakSmall }

    /// Enemigo básico. El comportamiento depende de "kind", igual que en enemy.gd de Godot:
    ///  Bug/Fast/Leak: te persiguen zigzagueando · Glitch: flota y dispara · Leak: al morir se parte en 2.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] EnemyKind kind = EnemyKind.Bug;

        [Header("Arte")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Cuadros de animación (2)")]
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 5f;
        [Tooltip("Color base (Fast usa el sprite de Bug en rojo)")]
        [SerializeField] Color tint = Color.white;
        [SerializeField] SpriteFx deathFx;

        [Header("Vida y velocidad (Godot: base + por piso)")]
        [SerializeField] float baseHp = 3f;
        [SerializeField] float hpPerFloor = 1f;
        [SerializeField] float baseSpeedPx = 48f;
        [SerializeField] float speedPerFloorPx = 4f;
        [Tooltip("Distancia a la que te hace daño al tocarte, en píxeles")]
        [SerializeField] float contactRadiusPx = 17f;

        [Header("Disparo (solo Glitch)")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] Sprite[] bulletSprites;
        [SerializeField] float bulletSpeedPx = 115f;
        [SerializeField] float bulletSpeedPerFloorPx = 10f;

        [Header("Leak: en qué se parte al morir")]
        [SerializeField] Enemy splitInto;

        [Header("Sonidos")]
        [SerializeField] AudioClip hitClip;
        [SerializeField] AudioClip dieClip;
        [SerializeField] AudioClip shootClip;

        public Room Room { get; set; }
        public bool IsAlive => !dying;

        const float SpawnTime = 0.7f;
        Rigidbody2D rb;
        PlayerHealth player;
        float hp, speedPx, t, spawnT = SpawnTime, shootT = 1.6f, flash, seed;
        Vector2 knockPx;
        bool dying;
        int floorNum;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            seed = Random.Range(0f, 10f);
        }

        void Start()
        {
            floorNum = GameManager.Instance.Run.Floor;
            hp = baseHp + hpPerFloor * floorNum;
            speedPx = baseSpeedPx + speedPerFloorPx * floorNum;
            player = FindAnyObjectByType<PlayerHealth>();
        }

        /// Para que las crías de Leak aparezcan casi al instante.
        public void SkipSpawn(float remaining = 0.15f) => spawnT = remaining;
        public void Knock(Vector2 px) => knockPx = px;

        void Update()
        {
            t += Time.deltaTime;
            if (frames.Length > 0) spriteRenderer.sprite = frames[(int)(t * framesPerSecond) % frames.Length];
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
            if (player == null || player.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 to = player.transform.position - transform.position;
            Vector2 dir = to.normalized;
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
                default: // Bug, Fast, Leak, LeakSmall: persiguen zigzagueando
                    float wiggle = kind == EnemyKind.Leak ? 0.25f : 0.6f;
                    var side = new Vector2(-dir.y, dir.x) * Mathf.Sin(t * 6f + seed) * wiggle;
                    velPx = (dir + side).normalized * speedPx;
                    spriteRenderer.flipX = to.x < 0f;
                    break;
            }

            rb.linearVelocity = (velPx + knockPx) / Units.PPU;
            knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 700f * dt);

            if (to.magnitude < Units.Px(contactRadiusPx)) player.Hurt(1, transform.position);
        }

        void Fire(Vector2 dir)
        {
            if (projectilePrefab == null) return;
            var origin = (Vector2)transform.position + Vector2.up * Units.Px(12) + dir * Units.Px(12);
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var s = bulletSprites.Length > 0 ? bulletSprites[Random.Range(0, bulletSprites.Length)] : null;
            p.Launch(s, dir * Units.Px(bulletSpeedPx + bulletSpeedPerFloorPx * floorNum), 1f, 3f, fromPlayer: false);
            Sfx.Play(shootClip, 0.6f);
        }

        public void Hit(float damage, Vector2 dir)
        {
            if (spawnT > 0f || dying) return;
            hp -= damage;
            flash = 0.08f;
            knockPx = dir * 150f * (kind == EnemyKind.Leak ? 0.4f : 1f);
            Sfx.Play(hitClip, 0.5f);
            if (hp <= 0f) Die();
        }

        void Die()
        {
            dying = true;
            Sfx.Play(dieClip, 0.7f);
            if (deathFx != null) Instantiate(deathFx, transform.position + Vector3.up * Units.Px(10), Quaternion.identity);

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
