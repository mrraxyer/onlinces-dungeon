using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Sala de combate. Cuando entras se cierran las puertas y salen oleadas de enemigos;
    /// al acabar con todas se abren y puede aparecer un cofre.
    /// Necesita un BoxCollider2D (Is Trigger) que cubra el interior.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Room : MonoBehaviour
    {
        [SerializeField] Door[] doors;
        [Tooltip("Si está vacío, el FloorBuilder pone los enemigos del piso")]
        [SerializeField] EnemyChance[] enemies;
        [SerializeField] int waves = 2;
        [Tooltip("No aparecen enemigos más cerca que esto del jugador (píxeles)")]
        [SerializeField] float minDistanceFromPlayerPx = 110f;
        [Tooltip("Capas donde no puede aparecer un enemigo (Walls)")]
        [SerializeField] LayerMask blockedBy;
        [SerializeField] LootTable loot;
        [SerializeField] AudioClip doorCloseClip;
        [SerializeField] AudioClip doorOpenClip;

        enum State { Waiting, Fighting, Cleared }
        State state;
        int wavesLeft;
        readonly List<Enemy> alive = new();
        BoxCollider2D area;
        PlayerHealth player;

        public event Action Cleared;
        public bool IsCleared => state == State.Cleared;
        public bool IsFighting => state == State.Fighting;
        public IReadOnlyList<Enemy> Alive => alive;

        void Awake() => area = GetComponent<BoxCollider2D>();

        public void SetEnemies(EnemyChance[] pool)
        {
            if (enemies == null || enemies.Length == 0) enemies = pool;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (state != State.Waiting) return;
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph == null || ph.gameObject.scene != gameObject.scene) return;
            player = ph;
            state = State.Fighting;
            player.TookDamageThisRoom = false;
            player.GetComponent<PlayerStats>()?.OnRoomEntered();
            foreach (var d in doors) d.SetOpen(false);
            Sfx.Play(doorCloseClip);
            wavesLeft = waves;
            StartCoroutine(SpawnWave(0.4f));
        }

        IEnumerator SpawnWave(float delay)
        {
            yield return new WaitForSeconds(delay);
            wavesLeft--;
            var b = area.bounds;
            int tiles = Mathf.RoundToInt(b.size.x * b.size.y); // 1 unidad = 1 tile de 32 px
            int floorNum = GameManager.Instance.Run.Floor;
            int n = Mathf.Clamp(Mathf.RoundToInt(tiles / 45f + floorNum), 2, 11);

            int placed = 0;
            for (int tries = 0; placed < n && tries < 200; tries++)
            {
                var p = new Vector2(
                    Mathf.Floor(UnityEngine.Random.Range(b.min.x + 1f, b.max.x - 1f)) + 0.5f,
                    Mathf.Floor(UnityEngine.Random.Range(b.min.y + 1f, b.max.y - 1f)) + 0.5f);
                if (Vector2.Distance(p, player.transform.position) < Units.Px(minDistanceFromPlayerPx)) continue;
                if (Physics2D.OverlapCircle(p, 0.45f, blockedBy)) continue;
                var prefab = Pick();
                if (prefab == null) break;
                Register(Instantiate(prefab, p, Quaternion.identity));
                placed++;
            }
            if (placed == 0) Clear();
        }

        Enemy Pick()
        {
            int total = 0;
            foreach (var e in enemies) total += Mathf.Max(0, e.weight);
            if (total == 0) return null;
            int r = UnityEngine.Random.Range(0, total);
            foreach (var e in enemies)
            {
                r -= Mathf.Max(0, e.weight);
                if (r < 0) return e.prefab;
            }
            return null;
        }

        public void Register(Enemy e)
        {
            e.Room = this;
            alive.Add(e);
        }

        public void OnEnemyDied(Enemy e)
        {
            alive.Remove(e);
            if (loot) loot.DropFromEnemy(e.transform.position);
            if (state != State.Fighting || alive.Count > 0) return;
            if (wavesLeft > 0) StartCoroutine(SpawnWave(0.6f));
            else Clear();
        }

        void Clear()
        {
            state = State.Cleared;
            foreach (var d in doors) d.SetOpen(true);
            Sfx.Play(doorOpenClip);
            if (player) player.GetComponent<PlayerStats>()?.OnRoomCleared(player.TookDamageThisRoom);
            if (loot && UnityEngine.Random.value < loot.chestChance)
            {
                var chest = UnityEngine.Random.value < loot.weaponChestShare ? loot.weaponChest : loot.supplyChest;
                Instantiate(chest, area.bounds.center, Quaternion.identity);
            }
            Cleared?.Invoke();
        }
    }
}
