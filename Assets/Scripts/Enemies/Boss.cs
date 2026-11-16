using TMPro;
using UnityEngine;

namespace OnLinces
{
    public enum BossKind { Spaghetti, Div }

    /// Jefe con 3 fases (66% y 33% de vida).
    ///  · Spaghetti: espiral, embestida, látigos e invoca bugs.
    ///  · <div>: solo recibe daño centrado; los disparos lo empujan.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Boss : MonoBehaviour, IHittable
    {
        [SerializeField] BossKind kind;
        public string displayName = "SPAGHETTI CODE";
        public string subtitle = "Sin documentar desde 2009";
        [SerializeField] float maxHp = 42f;
        [SerializeField] float contactRadiusPx = 30f;

        [Header("Arte")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] SpriteFx deathFx;
        [Tooltip("Solo <div>")]
        [SerializeField] TMP_Text cssLabel;

        [Header("Ataques")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] Sprite[] bulletSprites;
        [SerializeField] Telegraph telegraphPrefab;
        [Tooltip("Esbirro que invoca")]
        [SerializeField] Enemy minionPrefab;

        [Header("Sonidos")]
        [SerializeField] AudioClip hitClip, phaseClip, dashClip, shootClip, dieClip, centeredClip, nopeClip;

        const float BulletSpeed = 0.8f, Rest = 1.5f;
        static readonly (string css, Vector2 dir)[] CssMoves =
        {
            ("justify-content: flex-start", new(-1, 0.6f)), ("justify-content: flex-end", new(1, 0.6f)),
            ("align-items: flex-end", new(0.3f, -1)), ("float: left", new(-1, -0.4f)), ("float: right", new(1, -0.5f)),
            ("position: absolute; top: 0", new(-0.4f, 1)), ("margin-left: 9999px", new(1, 0)),
            ("text-align: center /* ? */", new(-0.8f, -0.9f)), ("vertical-align: middle", new(0.8f, 1)),
        };

        public float Hp { get; private set; }
        public float MaxHp => maxHp;
        public bool CanBeHit => intro <= 0f && !dying;

        BossRoom room;
        Rigidbody2D rb;
        PlayerHealth player;
        float t, intro = 1.6f, flash, stateT = 1.2f, subT, spiralA, cssT, nopeT, lockT;
        int phase = 1, attackI, subN;
        string state = "move";
        Vector2 knockPx, dashDir, anchor;
        bool dying, centered;

        public void Init(BossRoom r)
        {
            room = r;
            var run = GameManager.Instance.Run;
            maxHp *= 1f + 0.35f * (run.Floor - 1); // +35% por piso
            maxHp *= DifficultyInfo.BossHp(run.Difficulty);
            Hp = maxHp;
            anchor = transform.position;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            player = FindAnyObjectByType<PlayerHealth>();
            if (Hp <= 0f) Hp = maxHp;
        }

        Vector2 Center => room ? room.Center : (Vector2)transform.position;
        Vector2 DivGoal => Center - new Vector2(0, Units.Px(28)); // cuerpo sobre la mira

        void Update()
        {
            t += Time.deltaTime;
            if (frames.Length > 0) spriteRenderer.sprite = frames[(int)(t * (phase < 3 ? 6 : 9)) % frames.Length];
            flash -= Time.deltaTime;
            cssT -= Time.deltaTime;
            Color c = flash > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;
            if (phase == 3 && flash <= 0f) c = new Color(1f, 0.75f + 0.25f * Mathf.Sin(t * 8f), 0.75f + 0.25f * Mathf.Sin(t * 8f));
            if (kind == BossKind.Div && intro <= 0f && flash <= 0f) c = centered ? new Color(0.75f, 1f, 0.8f) : new Color(1, 1, 1, 0.85f);
            if (intro > 0f) c.a = 1f - intro / 1.6f;
            spriteRenderer.color = c;
            if (cssLabel) cssLabel.alpha = Mathf.Clamp01(cssT / 0.4f);
            if (room) room.SetCrosshair(kind == BossKind.Div && !dying, centered);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dying) { rb.linearVelocity = Vector2.zero; return; }
            if (intro > 0f) { intro -= dt; rb.linearVelocity = Vector2.zero; return; }
            if (player == null || player.IsDead) { rb.linearVelocity = Vector2.zero; return; }

            int newPhase = Hp > maxHp * 0.66f ? 1 : Hp > maxHp * 0.33f ? 2 : 3;
            if (newPhase != phase)
            {
                if (kind == BossKind.Div) Uncenter();
                phase = newPhase;
                Sfx.Play(phaseClip);
                HUD.Toast("FASE " + phase);
                FindAnyObjectByType<Blackout>()?.TriggerNow();
                state = "move";
                stateT = 0.8f * Rest;
            }

            Vector2 velPx = kind == BossKind.Spaghetti ? Spaghetti(dt) : Div(dt);
            rb.linearVelocity = (velPx + knockPx) / Units.PPU;
            knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 300f * dt);

            if (Vector2.Distance(player.transform.position, transform.position) < Units.Px(contactRadiusPx))
                player.Hurt(1, transform.position);
        }

        // ---------------- Spaghetti Code ----------------
        Vector2 Spaghetti(float dt)
        {
            float spd = 1f + (phase - 1) * 0.15f;
            stateT -= dt;
            Vector2 to = (Vector2)(player.transform.position - transform.position);
            switch (state)
            {
                case "move":
                    if (stateT <= 0f)
                    {
                        string[] attacks = phase >= 2 ? new[] { "spiral", "dash", "whips", "summon" } : new[] { "spiral", "dash", "whips" };
                        state = attacks[attackI++ % attacks.Length];
                        stateT = 2.2f; subT = 0f; subN = 0;
                        if (state == "dash") { state = "dash_aim"; stateT = 0.9f; }
                    }
                    return to.normalized * 28f * spd;
                case "spiral":
                    subT -= dt;
                    if (subT <= 0f)
                    {
                        subT = phase < 3 ? 0.14f : 0.11f;
                        spiralA += 0.45f;
                        Fire(Dir(spiralA), 95f);
                        if (phase >= 3) Fire(Dir(spiralA + Mathf.PI), 95f);
                    }
                    if (stateT <= 0f) { state = "move"; stateT = 1.2f * Rest; }
                    return Vector2.zero;
                case "dash_aim":
                    dashDir = to.normalized;
                    spriteRenderer.transform.localPosition = new Vector3(Mathf.Sin(t * 50f) * Units.Px(1.5f), spriteRenderer.transform.localPosition.y, 0);
                    if (stateT <= 0f)
                    {
                        spriteRenderer.transform.localPosition = new Vector3(0, spriteRenderer.transform.localPosition.y, 0);
                        state = "dash"; stateT = 1.5f;
                        Sfx.Play(dashClip);
                    }
                    return Vector2.zero;
                case "dash":
                    if (stateT <= 0f) { state = "move"; stateT = 1f * Rest; }
                    return dashDir * 185f * spd;
                case "whips":
                    if (subN == 0)
                    {
                        int n = phase < 3 ? 3 : 4;
                        var p0 = (Vector2)transform.position + Vector2.up * Units.Px(20);
                        for (int i = 0; i < n; i++)
                        {
                            float a = Mathf.PI * 2f * i / n + Random.value * 0.4f;
                            Telegraph.Beam(telegraphPrefab, p0, p0 + Dir(a) * Units.Px(300), 0.85f, 0.3f, 5f, new Color(1f, 0.85f, 0.3f));
                        }
                        subN = 1;
                    }
                    if (stateT <= 0.9f) { state = "move"; stateT = 1.1f * Rest; }
                    return Vector2.zero;
                case "summon":
                    if (subN == 0)
                    {
                        if (room) room.SpawnMinions(minionPrefab, phase < 3 ? 1 : 2, transform.position);
                        subN = 1;
                    }
                    if (stateT <= 1.2f) { state = "move"; stateT = 1f * Rest; }
                    return Vector2.zero;
            }
            return Vector2.zero;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            // embestida contra la pared: anillo de balas
            if (state != "dash" || c.gameObject.layer != LayerMask.NameToLayer("Walls")) return;
            state = "move";
            stateT = 0.9f * Rest;
            Ring(phase < 3 ? 7 : 9, 100f);
        }

        // ---------------- <div> sin centrar ----------------
        Vector2 Div(float dt)
        {
            stateT -= dt; nopeT -= dt; lockT -= dt;
            var goal = DivGoal;
            float distPx = Vector2.Distance(transform.position, goal) * Units.PPU;
            if (centered)
            {
                knockPx = Vector2.zero;
                subT -= dt;
                if (subT <= 0f) Uncenter();
                return (goal - (Vector2)transform.position) * 6f * Units.PPU;
            }
            if (distPx < 18f && lockT <= 0f)
            {
                centered = true;
                subT = 3.2f - phase * 0.5f;
                Css("display: flex; place-items: center", subT);
                Sfx.Play(centeredClip);
                HUD.Toast("¡DIV CENTRADO!");
                return Vector2.zero;
            }

            Vector2 toAnchorPx = (anchor - (Vector2)transform.position) * Units.PPU;
            Vector2 velPx = Vector2.ClampMagnitude(toAnchorPx, 1f) * Mathf.Min(16f + phase * 5f, toAnchorPx.magnitude * 2f);
            Vector2 toPlayer = ((Vector2)(player.transform.position - transform.position)).normalized;
            switch (state)
            {
                case "move":
                    if (stateT <= 0f)
                    {
                        string[] attacks = phase >= 2 ? new[] { "float", "flex", "margin", "zindex", "span" } : new[] { "float", "flex", "margin", "zindex" };
                        state = attacks[attackI++ % attacks.Length];
                        stateT = 1.8f; subT = 0f; subN = 0;
                        var mv = CssMoves[Random.Range(0, CssMoves.Length)];
                        anchor = AnchorFor(mv.dir);
                        Css(mv.css, 3f);
                        // a veces intenta centrarse solo
                        if (attackI % 4 == 0) { anchor = goal; Css("display: grid; place-items: center", 3f); }
                        else if (attackI % 4 == 2) { anchor = goal + new Vector2(Units.Px(Random.Range(-40f, 40f)), Units.Px(Random.Range(-24f, 24f))); Css("margin: 0 auto; /* casi */", 3f); }
                    }
                    break;
                case "float":
                    subT -= dt;
                    if (subT <= 0f && subN < 3)
                    {
                        subT = 0.35f; subN++;
                        int n = phase < 3 ? 3 : 5, mid = phase < 3 ? 1 : 2;
                        for (int i = 0; i < n; i++) Fire(Rotate(toPlayer, (i - mid) * 0.22f), 105f);
                    }
                    if (stateT <= 0f) { state = "move"; stateT = 1.1f * Rest; }
                    break;
                case "flex":
                    if (subN == 0)
                    {
                        subN = 1;
                        var r = room.PlayRect;
                        var pp = (Vector2)player.transform.position;
                        Telegraph.Beam(telegraphPrefab, new Vector2(r.xMin, pp.y), new Vector2(r.xMax, pp.y), 0.9f, 0.25f, 6f, new Color(1f, 0.43f, 0.78f));
                        if (phase >= 2) Telegraph.Beam(telegraphPrefab, new Vector2(pp.x, r.yMin), new Vector2(pp.x, r.yMax), 0.9f, 0.25f, 6f, new Color(0.37f, 0.95f, 1f));
                        Css("display: flex; flex-direction: row", 1.5f);
                    }
                    if (stateT <= 0.6f) { state = "move"; stateT = 1f * Rest; }
                    break;
                case "margin":
                    if (subN == 0)
                    {
                        subN = 1;
                        int n = phase < 3 ? 4 : 6;
                        for (int i = 0; i < n; i++)
                        {
                            var p = (Vector2)player.transform.position + Dir(Mathf.PI * 2f * i / n) * Units.Px(46);
                            Telegraph.Circle(telegraphPrefab, room.ClampInside(p), 20f, 1f, new Color(0.96f, 0.7f, 0.42f));
                        }
                        Css("margin: auto  /* en todos lados */", 1.5f);
                    }
                    if (stateT <= 0.6f) { state = "move"; stateT = 1f * Rest; }
                    break;
                case "zindex":
                    subT -= dt;
                    if (subT <= 0f && subN < 2)
                    {
                        subT = 0.6f; subN++;
                        Ring(8 + phase * 2, 85f, subN * 0.25f);
                        Css("z-index: 9999 !important", 1.5f);
                    }
                    if (stateT <= 0f) { state = "move"; stateT = 1f * Rest; }
                    break;
                case "span":
                    if (subN == 0)
                    {
                        subN = 1;
                        room.SpawnMinions(minionPrefab, 1, transform.position);
                        Css("<span> <span> <span>", 1.5f);
                    }
                    if (stateT <= 1f) { state = "move"; stateT = 1f * Rest; }
                    break;
            }
            return velPx;
        }

        Vector2 AnchorFor(Vector2 dir)
        {
            var r = room.PlayRect;
            float sx = r.width / 2f - Units.Px(56), sy = r.height / 2f - Units.Px(56);
            return r.center + new Vector2(dir.x * sx, dir.y * sy);
        }

        void Uncenter()
        {
            if (!centered) return;
            centered = false;
            lockT = 1.6f;
            Css("position: absolute; /* ups */", 2f);
            var mv = CssMoves[Random.Range(0, CssMoves.Length)];
            anchor = AnchorFor(mv.dir);
            knockPx = (anchor - (Vector2)transform.position).normalized * 160f;
            Sfx.Play(dashClip);
            Ring(8 + phase * 2, 90f);
            state = "move";
            stateT = 1.4f * Rest;
        }

        void Css(string text, float seconds)
        {
            if (!cssLabel) return;
            cssLabel.text = text;
            cssT = seconds;
        }

        // ---------------- disparos ----------------
        static Vector2 Dir(float a) => new(Mathf.Cos(a), Mathf.Sin(a));
        static Vector2 Rotate(Vector2 v, float a) => new(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));

        void Fire(Vector2 dir, float speedPx)
        {
            var origin = (Vector2)transform.position + Vector2.up * Units.Px(26) + dir * Units.Px(28);
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var s = bulletSprites.Length > 0 ? bulletSprites[Random.Range(0, bulletSprites.Length)] : null;
            p.Launch(s, dir * Units.Px(speedPx * BulletSpeed * DifficultyInfo.Bullet(GameManager.Instance.Run.Difficulty)), 1f, 4f, fromPlayer: false);
            Sfx.Play(shootClip, 0.35f);
        }

        void Ring(int n, float speedPx, float offset = 0f)
        {
            for (int i = 0; i < n; i++) Fire(Dir(Mathf.PI * 2f * i / n + offset), speedPx);
        }

        // ---------------- daño ----------------
        public void Hit(float damage, Vector2 dir)
        {
            if (!CanBeHit) return;
            if (kind == BossKind.Div && !centered)
            {
                // sin daño, solo empuje
                knockPx = Vector2.ClampMagnitude(knockPx + dir * 80f, 220f);
                if (nopeT <= 0f) { nopeT = 0.7f; HUD.Toast("¡No está centrado!"); Sfx.Play(nopeClip, 0.4f); }
                return;
            }
            if (kind == BossKind.Div) damage *= 1.25f;
            Hp -= damage;
            flash = 0.06f;
            Sfx.Play(hitClip, 0.5f);
            if (Hp <= 0f) Die();
        }

        void Die()
        {
            dying = true;
            Sfx.Play(dieClip);
            if (deathFx) Instantiate(deathFx, transform.position + Vector3.up * Units.Px(24), Quaternion.identity);
            if (room) room.OnBossDefeated(this);
            Destroy(gameObject, 0.05f);
        }
    }
}
