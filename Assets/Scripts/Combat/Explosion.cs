using UnityEngine;

namespace OnLinces
{
    /// Daña a todo lo que esté en el radio, jugador incluido.
    public sealed class Explosion : MonoBehaviour
    {
        [SerializeField] float radiusPx = 40f;
        [SerializeField] float enemyDamage = 6f;
        [SerializeField] int playerDamage = 1;
        [SerializeField] AudioClip clip;

        void Start()
        {
            Sfx.Play(clip);
            float r = Units.Px(radiusPx);
            var hit = new System.Collections.Generic.HashSet<IHittable>(); // un golpe por objetivo
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, r))
            {
                var dir = ((Vector2)(col.transform.position - transform.position)).normalized;
                var hittable = col.GetComponentInParent<IHittable>();
                if (hittable != null && hittable.CanBeHit && hit.Add(hittable)) hittable.Hit(enemyDamage, dir);
                var player = col.GetComponentInParent<PlayerHealth>();
                if (player != null && playerDamage > 0) player.Hurt(playerDamage, transform.position);
            }
        }
    }
}
