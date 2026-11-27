using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Ataque con aviso: parpadea y luego daña.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Telegraph : MonoBehaviour
    {
        enum Shape { Beam, Circle, Ring }

        LineRenderer line;
        Shape shape;
        Vector2 a, b;
        float radius, band, warn, active, t;
        Color color;
        /// Cada jugador recibe un solo golpe.
        readonly List<PlayerHealth> hit = new();

        /// Rayo de a a b.
        public static Telegraph Beam(Telegraph prefab, Vector2 a, Vector2 b, float warn, float active, float halfWidthPx, Color color)
        {
            var tg = Instantiate(prefab);
            tg.Init(Shape.Beam, a, b, Units.Px(halfWidthPx), warn, active, color);
            return tg;
        }

        /// Círculo en p.
        public static Telegraph Circle(Telegraph prefab, Vector2 p, float radiusPx, float warn, Color color)
        {
            var tg = Instantiate(prefab);
            tg.Init(Shape.Circle, p, p, Units.Px(radiusPx), warn, 0.25f, color);
            return tg;
        }

        /// Anillo de grosor bandPx alrededor de p: solo daña sobre la banda.
        public static Telegraph Ring(Telegraph prefab, Vector2 p, float radiusPx, float bandPx, float warn, Color color)
        {
            var tg = Instantiate(prefab);
            tg.band = Units.Px(bandPx);
            tg.Init(Shape.Ring, p, p, Units.Px(radiusPx), warn, 0.3f, color);
            return tg;
        }

        void Init(Shape s, Vector2 pa, Vector2 pb, float r, float w, float act, Color c)
        {
            line = GetComponent<LineRenderer>();
            shape = s; a = pa; b = pb; radius = r; warn = w + 0.3f; active = act; color = c;
            line.useWorldSpace = true;
            line.sortingOrder = 9;
            if (shape == Shape.Beam)
            {
                line.positionCount = 2;
                line.SetPosition(0, a);
                line.SetPosition(1, b);
                line.loop = false;
            }
            else
            {
                int n = shape == Shape.Ring ? 48 : 28;
                line.positionCount = n;
                line.loop = true;
                for (int i = 0; i < n; i++)
                {
                    float ang = Mathf.PI * 2f * i / n;
                    line.SetPosition(i, a + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius);
                }
            }
            Refresh();
        }

        /// Mueve el rayo; puede volver a dañar (el jugador tiene invulnerabilidad breve).
        public void Sweep(Vector2 pa, Vector2 pb)
        {
            a = pa; b = pb;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            hit.Clear();
        }

        void Update()
        {
            t += Time.deltaTime;
            Refresh();
            if (t >= warn)
                foreach (var player in PlayerHealth.All)
                {
                    if (hit.Contains(player) || !Touches(player.transform.position + Vector3.up * Units.Px(6))) continue;
                    hit.Add(player);
                    player.Hurt(1, shape != Shape.Beam ? a : (Vector2)player.transform.position + Vector2.down);
                }
            if (t >= warn + active) Destroy(gameObject);
        }

        void Refresh()
        {
            bool on = t >= warn;
            float blink = 0.35f + 0.25f * Mathf.Sin(t * 20f);
            var c = color; c.a = on ? 0.95f : blink;
            line.startColor = line.endColor = c;
            float w = shape == Shape.Beam ? radius * 2f : shape == Shape.Ring ? band : Units.Px(2);
            line.startWidth = line.endWidth = on ? w : Mathf.Max(Units.Px(1), w * 0.35f);
        }

        bool Touches(Vector2 p)
        {
            if (shape == Shape.Circle) return Vector2.Distance(p, a) <= radius;
            if (shape == Shape.Ring) return Mathf.Abs(Vector2.Distance(p, a) - radius) <= band / 2f + Units.Px(3);
            var ab = b - a;
            float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * k) <= radius + Units.Px(4);
        }
    }
}
