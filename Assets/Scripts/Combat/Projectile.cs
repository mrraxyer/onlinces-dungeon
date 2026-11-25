using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Bala en línea recta.
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
            life = lifeSeconds;
            FromPlayer = fromPlayer;
            if (glow) glow.color = fromPlayer ? playerGlow : enemyGlow;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        void Spark()
        {
            if (hitFx) Instantiate(hitFx, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if ((destroyOn.value & (1 << other.gameObject.layer)) != 0)
            {
                Sfx.Play(wallClip, 0.3f);
                Spark();
                Destroy(gameObject);
                return;
            }

            if (FromPlayer)
            {
                var target = other.GetComponentInParent<IHittable>();
                if (target != null && target.CanBeHit && alreadyHit.Add(target))
                {
                    target.Hit(Damage, rb.linearVelocity.normalized * knock);
                    Spark();
                    if (pierce-- <= 0) Destroy(gameObject);
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
