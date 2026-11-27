using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Bala en línea recta (o boomerang / en cadena / explosiva / teledirigida según el arma).
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Capas que la destruyen")]
        [SerializeField] LayerMask destroyOn;
        [Tooltip("Sin luz: brilla en el apagón")]
        [SerializeField] Material enemyMaterial;
        [Tooltip("Luz pequeña que viaja con la bala")]
        [SerializeField] Light2D glow;
        [SerializeField] Color playerGlow = new(1f, 0.85f, 0.45f);
        [SerializeField] Color enemyGlow = new(1f, 0.3f, 0.45f);
        [Tooltip("Chispa al chocar")]
        [SerializeField] SpriteFx hitFx;
        [Tooltip("Al chocar con la pared")]
        [SerializeField] AudioClip wallClip;

        public float Damage { get; private set; }
        public bool FromPlayer { get; private set; }

        Rigidbody2D rb;
        float life;
        int pierce;
        float knock = 1f;
        readonly System.Collections.Generic.HashSet<IHittable> alreadyHit = new();

        const float ChainRadiusPx = 90f, HomingRadiusPx = 140f, ChainDamage = 0.75f;
        WeaponData special;
        int chainLeft;
        float lifeTotal, speed, scanT;
        bool returning;
        Transform homingTarget, owner;

        // Al llegar a su alcance: se frena, crece y se desvanece en vez de borrarse de golpe.
        const float FadeTime = 0.16f;
        float fadeT = -1f, glowBase;
        Vector3 scaleBase;
        Color colorBase;

        void Awake() => rb = GetComponent<Rigidbody2D>();

        /// Atraviesa N enemigos.
        public void SetPierce(int count, float knockMultiplier)
        {
            pierce = count;
            knock = knockMultiplier;
        }

        public void Launch(Sprite sprite, Vector2 velocity, float damage, float lifeSeconds, bool fromPlayer)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            if (!fromPlayer && enemyMaterial != null) spriteRenderer.sharedMaterial = enemyMaterial;
            rb.linearVelocity = velocity;
            Damage = damage;
            life = lifeTotal = lifeSeconds;
            speed = velocity.magnitude;
            FromPlayer = fromPlayer;
            if (glow) glow.color = fromPlayer ? playerGlow : enemyGlow;
        }

        /// Mecánicas especiales del arma del jugador.
        public void SetSpecial(WeaponData w, int chains)
        {
            special = w;
            chainLeft = chains;
            if (w.returns)
            {
                pierce = int.MaxValue;
                owner = PlayerHealth.Nearest(transform.position)?.transform; // quien la lanzó
            }
        }

        void Update()
        {
            if (fadeT >= 0f) { Fade(); return; }
            life -= Time.deltaTime;
            if (special && special.returns && !returning && life <= lifeTotal / 2f && owner)
            {
                returning = true;
                life = lifeTotal; // tiempo para volver
                alreadyHit.Clear(); // golpea otra vez de regreso
            }
            if (life <= 0f) End(fadeOut: true);
        }

        void StartFade()
        {
            fadeT = FadeTime;
            scaleBase = transform.localScale;
            colorBase = spriteRenderer.color;
            glowBase = glow ? glow.intensity : 0f;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        }

        void Fade()
        {
            fadeT -= Time.deltaTime;
            float k = Mathf.Clamp01(fadeT / FadeTime);      // 1 → 0
            float e = 1f - k * k;                           // ease-out
            transform.localScale = scaleBase * (1f + 0.7f * e);
            var c = colorBase; c.a *= k;
            spriteRenderer.color = c;
            if (glow) glow.intensity = glowBase * k;
            rb.linearVelocity *= Mathf.Exp(-14f * Time.deltaTime); // frena suave
            if (fadeT <= 0f) Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (!special || fadeT >= 0f) return;
            if (returning)
            {
                Vector2 to = owner.position - transform.position;
                if (to.magnitude < Units.Px(10)) Destroy(gameObject);
                else rb.linearVelocity = to.normalized * speed;
            }
            else if (special.homingDegPerSec > 0f)
            {
                scanT -= Time.fixedDeltaTime;
                if (scanT <= 0f) { scanT = 0.1f; homingTarget = Nearest(HomingRadiusPx); }
                if (homingTarget)
                {
                    Vector2 v = rb.linearVelocity, to = homingTarget.position - transform.position;
                    float a = Mathf.MoveTowardsAngle(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg,
                        special.homingDegPerSec * Time.fixedDeltaTime) * Mathf.Deg2Rad;
                    rb.linearVelocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed;
                }
            }
        }

        /// Enemigo vivo más cercano que aún no golpeó esta bala.
        Transform Nearest(float radiusPx)
        {
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, Units.Px(radiusPx)))
            {
                var h = col.GetComponentInParent<IHittable>();
                if (h == null || h is Crate || !h.CanBeHit || alreadyHit.Contains(h)) continue;
                float d = Vector2.Distance(col.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = ((Component)h).transform; }
            }
            return best;
        }

        void Chain()
        {
            var next = Nearest(ChainRadiusPx);
            if (!next) return;
            var p = Instantiate(this, transform.position, Quaternion.identity);
            p.alreadyHit.UnionWith(alreadyHit);
            p.Launch(spriteRenderer.sprite, ((Vector2)(next.position - transform.position)).normalized * speed, Damage * ChainDamage, lifeTotal, true);
            p.SetSpecial(special, chainLeft - 1);
        }

        /// Se acaba: explota si el arma lo pide.
        void End(bool fadeOut = false)
        {
            if (special && special.explosion)
            {
                Instantiate(special.explosion, transform.position, Quaternion.identity).SetDamage(Damage, 0);
                Destroy(gameObject);
            }
            else if (fadeOut) StartFade();
            else Destroy(gameObject);
        }

        void Spark()
        {
            if (hitFx) Instantiate(hitFx, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (fadeT >= 0f) return;
            if ((destroyOn.value & (1 << other.gameObject.layer)) != 0)
            {
                Sfx.Play(wallClip, 0.3f);
                Spark();
                End();
                return;
            }

            if (FromPlayer)
            {
                var target = other.GetComponentInParent<IHittable>();
                if (target != null && target.CanBeHit && alreadyHit.Add(target))
                {
                    target.Hit(Damage, rb.linearVelocity.normalized * knock);
                    Spark();
                    if (chainLeft > 0) Chain();
                    if (pierce-- <= 0) End();
                }
            }
            else
            {
                var player = other.GetComponentInParent<PlayerHealth>();
                if (player != null)
                {
                    player.Hurt(Mathf.Max(1, Mathf.RoundToInt(Damage)), transform.position);
                    Destroy(gameObject);
                }
            }
        }
    }
}
