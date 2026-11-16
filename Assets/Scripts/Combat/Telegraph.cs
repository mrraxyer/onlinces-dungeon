using UnityEngine;

namespace OnLinces
{
    /// Aviso de ataque: primero se dibuja una línea o círculo tenue (aviso) y luego "se activa" y daña.
    /// Así el jugador siempre ve venir el golpe (regla de diseño del juego).
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Telegraph : MonoBehaviour
    {
        enum Shape { Beam, Circle }

        LineRenderer line;
        Shape shape;
        Vector2 a, b;
        float radius, warn, active, t;
        Color color;
        bool hitDone;
        PlayerHealth player;

        /// Rayo de a hasta b. halfWidthPx = medio ancho en píxeles.
        public static Telegraph Beam(Telegraph prefab, Vector2 a, Vector2 b, float warn, float active, float halfWidthPx, Color color)
        {
            var tg = Instantiate(prefab);
            tg.Init(Shape.Beam, a, b, Units.Px(halfWidthPx), warn, active, color);
            return tg;
        }

        /// Explosión circular en p.
        public static Telegraph Circle(Telegraph prefab, Vector2 p, float radiusPx, float warn, Color color)
        {
            var tg = Instantiate(prefab);
            tg.Init(Shape.Circle, p, p, Units.Px(radiusPx), warn, 0.25f, color);
            return tg;
        }

        void Init(Shape s, Vector2 pa, Vector2 pb, float r, float w, float act, Color c)
        {
            line = GetComponent<LineRenderer>();
            shape = s; a = pa; b = pb; radius = r; warn = w + 0.3f; active = act; color = c;
            player = FindAnyObjectByType<PlayerHealth>();
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
                const int n = 28;
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

        void Update()
        {
            t += Time.deltaTime;
            Refresh();
            if (t >= warn && !hitDone && player != null && Touches(player.transform.position + Vector3.up * Units.Px(6)))
            {
                hitDone = true;
                player.Hurt(1, shape == Shape.Circle ? a : (Vector2)player.transform.position + Vector2.down);
            }
            if (t >= warn + active) Destroy(gameObject);
        }

        void Refresh()
        {
            bool on = t >= warn;
            float blink = 0.35f + 0.25f * Mathf.Sin(t * 20f);
            var c = color; c.a = on ? 0.95f : blink;
            line.startColor = line.endColor = c;
            float w = shape == Shape.Beam ? radius * 2f : Units.Px(2);
            line.startWidth = line.endWidth = on ? w : Mathf.Max(Units.Px(1), w * 0.35f);
        }

        bool Touches(Vector2 p)
        {
            if (shape == Shape.Circle) return Vector2.Distance(p, a) <= radius;
            var ab = b - a;
            float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * k) <= radius + Units.Px(4);
        }
    }
}
