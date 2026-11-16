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
        int pierce;
        float knock = 1f;
        readonly System.Collections.Generic.HashSet<IHittable> alreadyHit = new();

        void Awake() => rb = GetComponent<Rigidbody2D>();

        /// Atraviesa N enemigos y los empuja más fuerte (Post viral).
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
                if (target != null && target.CanBeHit && alreadyHit.Add(target))
                {
                    target.Hit(Damage, rb.linearVelocity.normalized * knock);
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
