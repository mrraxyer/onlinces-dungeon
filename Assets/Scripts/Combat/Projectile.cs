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

        public float Damage { get; private set; }
        public bool FromPlayer { get; private set; }

        Rigidbody2D rb;
        float life;

        void Awake() => rb = GetComponent<Rigidbody2D>();

        public void Launch(Sprite sprite, Vector2 velocity, float damage, float lifeSeconds, bool fromPlayer)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
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
            if ((destroyOn.value & (1 << other.gameObject.layer)) != 0) Destroy(gameObject);
        }
    }
}
