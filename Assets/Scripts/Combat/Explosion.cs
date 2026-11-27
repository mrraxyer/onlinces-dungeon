using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Daña a todo lo que esté en el radio, jugador incluido.
    public sealed class Explosion : MonoBehaviour
    {
        [SerializeField] float radiusPx = 40f;
        [SerializeField] float enemyDamage = 6f;
        [SerializeField] int playerDamage = 1;
        [SerializeField] AudioClip clip;
        [Tooltip("Destello que se desvanece")]
        [SerializeField] Light2D flash;
        [SerializeField] float flashSeconds = 0.35f;

        float flashStart, t;

        /// Llamar justo tras Instantiate (antes de Start).
        public void SetDamage(float enemy, int player)
        {
            enemyDamage = enemy;
            playerDamage = player;
        }

        void Start()
        {
            Sfx.Play(clip);
            CameraFollow.Shake(6f);
            Rumble.PlayAll(0.45f, 0.25f, 0.18f);
            if (flash) flashStart = flash.intensity;
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

        void Update()
        {
            if (!flash) return;
            t += Time.deltaTime;
            flash.intensity = flashStart * Mathf.Clamp01(1f - t / flashSeconds);
            flash.enabled = flash.intensity > 0f;
        }
    }
}
