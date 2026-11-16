using UnityEngine;

namespace OnLinces
{
    /// Bala de código. Avanza en línea recta, desaparece al acabarse su alcance o al chocar con una pared.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Capas que destruyen la bala (Walls)")]
        [SerializeField] LayerMask destroyOn;
        [Tooltip("Material sin iluminación para balas enemigas: brillan en el apagón")]
        [SerializeField] Material enemyMaterial;

        public float Damage { get; private set; }
        public bool FromPlayer { get; private set; }

        Rigidbody2D rb;
        float life;

        void Awake() => rb = GetComponent<Rigidbody2D>();

        public void Launch(Sprite sprite, Vector2 velocity, float damage, float lifeSeconds, bool fromPlayer)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
            if (!fromPlayer && enemyMaterial != null) spriteRenderer.sharedMaterial = enemyMaterial;
            rb.linearVelocity = velocity;
            Damage = damage;
            life = lifeSeconds;
            FromPlayer = fromPlayer;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if ((destroyOn.value & (1 << other.gameObject.layer)) != 0)
            {
                Destroy(gameObject);
                return;
            }

            if (FromPlayer)
            {
                var target = other.GetComponentInParent<IHittable>();
                if (target != null && target.CanBeHit)
                {
                    target.Hit(Damage, rb.linearVelocity.normalized);
                    Destroy(gameObject);
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
