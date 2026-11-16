using UnityEngine;

namespace OnLinces
{
    /// Caja explosiva.
    public sealed class Crate : MonoBehaviour, IHittable
    {
        [SerializeField] float hp = 2f;
        [SerializeField] Explosion explosionPrefab;
        [SerializeField] SpriteRenderer spriteRenderer;

        bool exploded;
        float flash;

        public bool CanBeHit => !exploded;

        void Update()
        {
            flash -= Time.deltaTime;
            spriteRenderer.color = flash > 0f ? new Color(1f, 0.6f, 0.4f) : Color.white;
        }

        public void Hit(float damage, Vector2 direction)
        {
            if (exploded) return;
            hp -= Mathf.Min(damage, 1f);
            flash = 0.08f;
            if (hp > 0f) return;
            exploded = true;
            Instantiate(explosionPrefab, transform.position + Vector3.up * Units.Px(10), Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
