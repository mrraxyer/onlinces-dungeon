using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OnLinces
{
    /// Minimapa de salas.
    public sealed class Minimap : MonoBehaviour
    {
        [SerializeField] FloorLevel level;
        [SerializeField] Transform player;
        [SerializeField] RectTransform content;
        [SerializeField] Image roomTemplate;
        [SerializeField] Image playerDot;
        [Tooltip("UI por tile")]
        [SerializeField] float scale = 0.55f;
        [SerializeField] Color unknown = new(0.17f, 0.1f, 0.06f, 0.18f);
        [SerializeField] Color visited = new(0.17f, 0.1f, 0.06f, 0.55f);
        [SerializeField] Color current = new(0.85f, 0.45f, 0.1f, 0.95f);
        [SerializeField] Color boss = new(0.7f, 0.12f, 0.15f, 0.8f);

        readonly List<Image> boxes = new();
        readonly HashSet<int> seen = new();
        bool[] isBoss;
        Rect worldBounds;

        void Start()
        {
            if (level == null) level = FindAnyObjectByType<FloorLevel>();
            if (level == null) { gameObject.SetActive(false); return; }
            var rooms = level.Rooms;
            worldBounds = Bounds(rooms[0]);
            foreach (var r in rooms) { var b = Bounds(r); worldBounds = Rect.MinMaxRect(Mathf.Min(worldBounds.xMin, b.xMin), Mathf.Min(worldBounds.yMin, b.yMin), Mathf.Max(worldBounds.xMax, b.xMax), Mathf.Max(worldBounds.yMax, b.yMax)); }
            content.sizeDelta = worldBounds.size * scale;
            isBoss = new bool[rooms.Length];
            for (int i = 0; i < rooms.Length; i++) isBoss[i] = rooms[i].GetComponentInChildren<BossRoom>() != null;
            foreach (var r in rooms)
            {
                var img = Instantiate(roomTemplate, content);
                img.gameObject.SetActive(true);
                var b = Bounds(r);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                rt.anchoredPosition = (b.min - worldBounds.min) * scale;
                rt.sizeDelta = b.size * scale;
                boxes.Add(img);
            }
            playerDot.transform.SetAsLastSibling();
        }

        static Rect Bounds(RoomShell r)
        {
            var p = r.transform.position;
            float x0 = p.x - (r.width - 1) / 2f;
            return new Rect(x0, p.y, r.width, r.height);
        }

        void Update()
        {
            if (level == null || player == null) return;
            var rooms = level.Rooms;
            int cur = -1;
            for (int i = 0; i < rooms.Length; i++)
                if (Bounds(rooms[i]).Contains(player.position)) { cur = i; seen.Add(i); }
            for (int i = 0; i < boxes.Count; i++)
                boxes[i].color = i == cur ? current : seen.Contains(i) ? (isBoss[i] ? boss : visited) : (isBoss[i] ? new Color(boss.r, boss.g, boss.b, 0.3f) : unknown);
            var rt = playerDot.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = ((Vector2)player.position - worldBounds.min) * scale;
        }
    }
}
