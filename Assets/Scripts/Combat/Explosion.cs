using UnityEngine;

namespace OnLinces
{
    /// Explosión: al aparecer daña a enemigos y al jugador dentro del radio. Necesita SpriteFx para la animación.
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
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, r))
            {
                var dir = ((Vector2)(col.transform.position - transform.position)).normalized;
                var hittable = col.GetComponentInParent<IHittable>();
                if (hittable != null && hittable.CanBeHit && !ReferenceEquals(hittable, this)) hittable.Hit(enemyDamage, dir);
                var player = col.GetComponentInParent<PlayerHealth>();
                if (player != null && playerDamage > 0) player.Hurt(playerDamage, transform.position);
            }
        }
    }
}
