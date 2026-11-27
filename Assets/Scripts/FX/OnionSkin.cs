using UnityEngine;

namespace OnLinces
{
    /// Onion skinning: deja copias fantasma del sprite que se desvanecen (estela de movimiento).
    public sealed class OnionSkin : MonoBehaviour
    {
        const int Count = 5;
        const float Interval = 0.05f, Life = 0.25f, StartAlpha = 0.45f, MinSpeed = 1.2f;

        SpriteRenderer source;
        Color tint = new(0.65f, 0.85f, 1f);
        readonly SpriteRenderer[] ghosts = new SpriteRenderer[Count];
        readonly float[] age = new float[Count];
        int next;
        float timer;
        Vector3 last;

        public void Init(SpriteRenderer sr, Color color)
        {
            source = sr;
            tint = color;
            last = transform.position;
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Onion");
                var g = go.AddComponent<SpriteRenderer>();
                g.sharedMaterial = sr.sharedMaterial;
                g.sortingLayerID = sr.sortingLayerID;
                g.sortingOrder = sr.sortingOrder - 1;
                g.enabled = false;
                ghosts[i] = g;
                age[i] = Life;
            }
        }

        void LateUpdate()
        {
            if (source == null) return;
            float speed = (transform.position - last).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            last = transform.position;

            timer -= Time.deltaTime;
            if (timer <= 0f && speed > MinSpeed && source.enabled && source.color.a > 0.5f)
            {
                timer = Interval;
                var g = ghosts[next];
                g.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                g.transform.localScale = source.transform.lossyScale;
                g.sprite = source.sprite;
                g.flipX = source.flipX;
                g.enabled = true;
                age[next] = 0f;
                next = (next + 1) % Count;
            }

            for (int i = 0; i < Count; i++)
            {
                if (!ghosts[i].enabled) continue;
                age[i] += Time.deltaTime;
                float k = 1f - age[i] / Life; // se apaga lineal
                if (k <= 0f) { ghosts[i].enabled = false; continue; }
                ghosts[i].color = new Color(tint.r, tint.g, tint.b, StartAlpha * k);
            }
        }

        void OnDestroy()
        {
            foreach (var g in ghosts) if (g) Destroy(g.gameObject);
        }
    }
}
