using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Sala del jefe: cierra puertas, saca un jefe al azar y al vencerlo abre el portal.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossRoom : MonoBehaviour
    {
        [Tooltip("Uno al azar")]
        [SerializeField] Boss[] bosses;
        [SerializeField] Transform bossSpawn;
        [SerializeField] Door[] doors;
        [SerializeField] GameObject portal;
        [Tooltip("Solo <div>")]
        [SerializeField] TMP_Text crosshair;
        [SerializeField] LootTable loot;
        [SerializeField] AudioClip introClip, doorCloseClip, doorOpenClip;
        [SerializeField] AudioClip bossMusic;
        [SerializeField] AudioClip floorMusic;
        [SerializeField] int maxMinions = 3;

        BoxCollider2D area;
        bool started, done;
        readonly List<Enemy> minions = new();

        public Vector2 Center => area.bounds.center;
        public Rect PlayRect => new(area.bounds.min, area.bounds.size);
        public bool IsFighting => started && !done;

        void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            if (portal) portal.SetActive(false);
            if (crosshair) crosshair.gameObject.SetActive(false);
        }

        public Vector2 ClampInside(Vector2 p)
        {
            var b = area.bounds;
            return new Vector2(Mathf.Clamp(p.x, b.min.x + 0.5f, b.max.x - 0.5f), Mathf.Clamp(p.y, b.min.y + 0.5f, b.max.y - 0.5f));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started) return;
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph == null || ph.gameObject.scene != gameObject.scene) return;
            started = true;
            foreach (var d in doors) if (d) d.SetOpen(false);
            Sfx.Play(doorCloseClip);
            Sfx.Play(introClip);
            Music.Play(bossMusic);
            var prefab = bosses[Random.Range(0, bosses.Length)];
            var boss = Instantiate(prefab, bossSpawn.position, Quaternion.identity);
            boss.Init(this);
            BossBar.Show(boss);
        }

        public void SpawnMinions(Enemy prefab, int n, Vector2 near)
        {
            minions.RemoveAll(m => m == null);
            for (int i = 0; i < n && minions.Count < maxMinions; i++)
            {
                var p = ClampInside(near + Random.insideUnitCircle.normalized * 1.2f);
                minions.Add(Instantiate(prefab, p, Quaternion.identity));
            }
        }

        public void SetCrosshair(bool show, bool centered)
        {
            if (!crosshair) return;
            crosshair.gameObject.SetActive(show);
            if (!show) return;
            crosshair.transform.position = Center;
            crosshair.color = centered ? new Color(0.29f, 0.87f, 0.5f) : new Color(1f, 0.85f, 0.3f, 0.55f + 0.25f * Mathf.Sin(Time.time * 6f));
        }

        public void OnBossDefeated(Boss boss)
        {
            if (done) return;
            done = true;
            foreach (var m in minions) if (m) m.Kill();
            foreach (var d in doors) if (d) d.SetOpen(true);
            if (portal) portal.SetActive(true);
            SetCrosshair(false, false);
            BossBar.Hide();
            Sfx.Play(doorOpenClip);
            Music.Play(floorMusic);
            HUD.Toast("¡Jefe derrotado!");
            if (loot)
            {
                for (int i = 0; i < 8; i++) loot.Spawn(loot.coin, boss.transform.position);
                loot.Spawn(loot.heart, boss.transform.position);
            }
            FindAnyObjectByType<PlayerHealth>()?.Heal(2);
        }
    }
}
