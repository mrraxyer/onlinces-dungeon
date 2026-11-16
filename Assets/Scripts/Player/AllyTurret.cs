using UnityEngine;

namespace OnLinces
{
    /// Bot aliado de la Junta general: se queda donde aparece y dispara solo al enemigo más cercano.
    public sealed class AllyTurret : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] Sprite bulletSprite;
        [SerializeField] float fireDelay = 0.45f;
        [SerializeField] float damage = 2f;
        [SerializeField] float rangePx = 180f;
        [SerializeField] float bulletSpeedPx = 260f;
        [SerializeField] AudioClip shootClip;

        float life = 6f, cooldown, t;

        public void Init(float seconds) => life = seconds;

        void Update()
        {
            t += Time.deltaTime;
            life -= Time.deltaTime;
            cooldown -= Time.deltaTime;
            if (frames.Length > 0) spriteRenderer.sprite = frames[(int)(t * 4f) % frames.Length];
            if (life < 1f) spriteRenderer.color = new Color(1, 1, 1, Mathf.Clamp01(life) * (0.5f + 0.5f * Mathf.Sin(t * 30f)));
            if (life <= 0f) { Destroy(gameObject); return; }
            if (cooldown > 0f) return;

            Transform best = null;
            float bestD = Units.Px(rangePx);
            foreach (var e in FindObjectsByType<Enemy>())
            {
                if (!e.CanBeHit) continue;
                float d = Vector2.Distance(e.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = e.transform; }
            }
            foreach (var b in FindObjectsByType<Boss>())
            {
                if (!b.CanBeHit) continue;
                float d = Vector2.Distance(b.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = b.transform; }
            }
            if (best == null) return;

            cooldown = fireDelay;
            var origin = (Vector2)transform.position + Vector2.up * Units.Px(8);
            var dir = ((Vector2)best.position + Vector2.up * Units.Px(10) - origin).normalized;
            var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
            p.Launch(bulletSprite, dir * Units.Px(bulletSpeedPx), damage, 0.9f, fromPlayer: true);
            spriteRenderer.flipX = dir.x < 0f;
            Sfx.Play(shootClip, 0.3f);
        }
    }
}
