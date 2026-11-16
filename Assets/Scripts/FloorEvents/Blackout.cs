using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Dinámica del piso 1 — Apagón: "se va la luz" y solo ves un círculo alrededor tuyo.
    /// Las balas enemigas siguen brillando (usan material sin iluminación).
    /// Solo corre mientras peleas en una sala. Avisa 1.6 s antes, dura 7 s, y hay 20 s de calma (Godot).
    public sealed class Blackout : MonoBehaviour
    {
        [SerializeField] Light2D globalLight;
        [Tooltip("Luz que lleva el jugador (se prende en el apagón)")]
        [SerializeField] Light2D playerLight;
        [SerializeField] float calmSeconds = 20f;
        [SerializeField] float warnSeconds = 1.6f;
        [SerializeField] float activeSeconds = 7f;
        [Range(0, 1)] [SerializeField] float darkIntensity = 0.06f;
        [SerializeField] AudioClip warnClip;

        enum State { Calm, Warn, Active }
        State state;
        float t, dark;
        float baseIntensity;
        Room[] rooms;
        BossRoom bossRoom;

        void Start()
        {
            baseIntensity = globalLight ? globalLight.intensity : 1f;
            rooms = FindObjectsByType<Room>(FindObjectsInactive.Exclude);
            bossRoom = FindAnyObjectByType<BossRoom>();
            t = calmSeconds * 0.5f; // el primero llega un poco antes
            if (playerLight) playerLight.enabled = false;
        }

        bool Fighting()
        {
            foreach (var r in rooms) if (r && r.IsFighting) return true;
            return FindAnyObjectByType<Boss>() != null;
        }

        /// El jefe lo dispara al cambiar de fase.
        public void TriggerNow()
        {
            if (state == State.Calm) { state = State.Warn; t = warnSeconds; Warn(); }
        }

        void Warn()
        {
            HUD.Toast("¡Se va la luz!");
            Sfx.Play(warnClip);
        }

        void Update()
        {
            bool fighting = Fighting();
            switch (state)
            {
                case State.Calm:
                    if (fighting) t -= Time.deltaTime;
                    if (t <= 0f) { state = State.Warn; t = warnSeconds; Warn(); }
                    break;
                case State.Warn:
                    t -= Time.deltaTime;
                    if (t <= 0f) { state = State.Active; t = activeSeconds; }
                    break;
                case State.Active:
                    t -= Time.deltaTime;
                    if (t <= 0f || !fighting) { state = State.Calm; t = calmSeconds; }
                    break;
            }

            // parpadeo durante el aviso, oscuridad durante el apagón
            float want = state == State.Active ? 1f : state == State.Warn && Mathf.Sin(Time.time * 30f) > 0.3f ? 0.5f : 0f;
            dark = Mathf.MoveTowards(dark, want, Time.deltaTime * (state == State.Warn ? 6f : 2f));
            if (globalLight) globalLight.intensity = Mathf.Lerp(baseIntensity, darkIntensity, dark);
            if (playerLight)
            {
                playerLight.enabled = dark > 0.01f;
                playerLight.intensity = dark * 1.2f;
            }
        }
    }
}
