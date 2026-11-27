using UnityEngine;

namespace OnLinces
{
    /// Sigue al jugador (o al punto medio de los vivos en cooperativo), suavizado y alineado al píxel.
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Más alto = sigue más rápido")]
        [SerializeField] float sharpness = 10f;

        Vector3 smooth;
        static float shakePx;
        static Vector2 kickPx, kickVel;
        static float tiltDeg, tiltVel;
        const float TiltK = 200f, TiltC = 13f;
        // resorte amortiguado: k = rigidez, c = amortiguamiento (poco menos que crítico: rebota una vez)
        const float SpringK = 260f, SpringC = 18f;

        /// Sacudida de cámara (traslación aleatoria que se apaga sola). En píxeles del juego.
        public static void Shake(float px) => shakePx = Mathf.Max(shakePx, px);

        /// Empujón: todo el mundo brinca de golpe hacia un lado y regresa con un resorte. En píxeles.
        public static void Kick(Vector2 px, float tilt = 0f)
        {
            kickPx += px;
            kickVel = Vector2.zero;
            tiltDeg += tilt; // inclinación en grados, también regresa con resorte
            tiltVel = 0f;
        }

        void Start() => smooth = transform.position;

        void LateUpdate()
        {
            if (target == null) return;
            // shortcut: sin zoom; si se separan mucho uno queda fuera de cuadro (las salas se cierran con ambos dentro)
            Vector2 sum = Vector2.zero; int n = 0;
            foreach (var p in PlayerHealth.All) if (!p.IsDead) { sum += (Vector2)p.transform.position; n++; }
            Vector2 c = n > 0 ? sum / n : (Vector2)target.position;
            var goal = new Vector3(c.x, c.y, transform.position.z);
            smooth = Vector3.Lerp(smooth, goal, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
            // tiempo real: sigue moviéndose durante el hitstop
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            var shake = shakePx > 0f ? Random.insideUnitCircle * shakePx : Vector2.zero;
            shakePx = Mathf.MoveTowards(shakePx, 0f, 30f * dt);
            kickVel += (-SpringK * kickPx - SpringC * kickVel) * dt; // x'' = -k·x - c·x'
            kickPx += kickVel * dt;
            if (kickPx.sqrMagnitude < 0.01f && kickVel.sqrMagnitude < 0.01f) kickPx = kickVel = Vector2.zero;
            tiltVel += (-TiltK * tiltDeg - TiltC * tiltVel) * dt;
            tiltDeg += tiltVel * dt;
            if (Mathf.Abs(tiltDeg) < 0.01f && Mathf.Abs(tiltVel) < 0.01f) tiltDeg = tiltVel = 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, tiltDeg);
            var off = shake + kickPx;
            transform.position = new Vector3(
                Mathf.Round(smooth.x * Units.PPU + off.x) / Units.PPU,
                Mathf.Round(smooth.y * Units.PPU + off.y) / Units.PPU,
                smooth.z);
        }
    }
}
