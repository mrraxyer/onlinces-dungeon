using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OnLinces
{
    /// Apagón: solo se ve (nítido) alrededor del jugador. Corre durante peleas.
    public sealed class Blackout : MonoBehaviour
    {
        [SerializeField] Light2D globalLight;
        [Tooltip("Se prende en el apagón")]
        [SerializeField] Light2D playerLight;
        [SerializeField] float calmSeconds = 20f;
        [SerializeField] float warnSeconds = 1.6f;
        [SerializeField] float activeSeconds = 7f;
        [Range(0, 1)] [SerializeField] float darkIntensity = 0.06f;
        [SerializeField] AudioClip warnClip;
        [Tooltip("Full Screen Pass del Renderer2D que desenfoca fuera de la luz")]
        [SerializeField] ScriptableRendererFeature blurFeature;
        [Tooltip("Radio del desenfoque, en alturas de pantalla")]
        [SerializeField] float blurSize = 0.02f;


        enum State { Calm, Warn, Active }
        State state;
        float t, dark;
        float baseIntensity;
        Room[] rooms;
        BossRoom bossRoom;
        readonly List<Light2D> lights = new();

        /// La luz de cada jugador vivo (el invitado trae su copia de la del prefab).
        void FindLights()
        {
            lights.Clear();
            if (!playerLight) return;
            foreach (var p in PlayerHealth.All)
            {
                if (p.IsDead) continue;
                foreach (var l in p.GetComponentsInChildren<Light2D>(true))
                    if (l.name == playerLight.name) { lights.Add(l); break; }
            }
        }

        void Start()
        {
            baseIntensity = globalLight ? globalLight.intensity : 1f;
            rooms = FindObjectsByType<Room>(FindObjectsInactive.Exclude);
            bossRoom = FindAnyObjectByType<BossRoom>();
            t = calmSeconds * 0.5f; // el primero llega antes
            if (playerLight) playerLight.enabled = false;
            SetBlur(0f);
        }

        // el feature vive en un asset: apagarlo al salir para no dejarlo prendido
        void OnDisable() => SetBlur(0f);

        void SetBlur(float amount)
        {
            // el desenfoque lo comparte con el golpe (ScreenBlur)
            var cam = Camera.main;
            if (amount <= 0.01f || !playerLight || !cam || lights.Count == 0) { ScreenBlur.SetBlackout(blurFeature, 0f, Vector4.zero, Vector2.zero, blurSize); return; }
            float h = cam.orthographicSize * 2f;
            Vector2 a = cam.WorldToViewportPoint(lights[0].transform.position);
            Vector2 b = lights.Count > 1 ? cam.WorldToViewportPoint(lights[1].transform.position) : a;
            ScreenBlur.SetBlackout(blurFeature, amount, new Vector4(a.x, a.y, b.x, b.y),
                new Vector2(playerLight.pointLightInnerRadius / h, playerLight.pointLightOuterRadius / h), blurSize);
        }

        bool Fighting()
        {
            foreach (var r in rooms) if (r && r.IsFighting) return true;
            return bossRoom && bossRoom.IsFighting;
        }

        /// Lo llama el jefe al cambiar de fase.
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

            // parpadea en el aviso, oscuro en el apagón
            float want = state == State.Active ? 1f : state == State.Warn && Mathf.Sin(Time.time * 30f) > 0.3f ? 0.5f : 0f;
            dark = Mathf.MoveTowards(dark, want, Time.deltaTime * (state == State.Warn ? 6f : 2f));
            if (globalLight) globalLight.intensity = Mathf.Lerp(baseIntensity, darkIntensity, dark);
            FindLights();
            foreach (var l in lights)
            {
                l.enabled = dark > 0.01f;
                l.intensity = dark * 1.2f;
            }
            SetBlur(dark);
        }
    }
}
