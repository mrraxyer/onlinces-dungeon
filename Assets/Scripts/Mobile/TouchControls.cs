using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OnLinces
{
    /// Controles táctiles (prefab Assets/Resources/TouchControls). MobileSetup lo crea solo al arrancar
    /// y vive durante todo el juego; no hay que ponerlo en ninguna escena.
    ///  · Solo se usa en teléfono (o en el editor con "Force In Editor").
    ///  · Solo se ve en las escenas donde hay jugador (Lobby y pisos).
    ///  · Palanca izquierda = mover. Palanca derecha = apuntar y disparar sola mientras la empujas.
    ///  · Los botones son botones de gamepad (OnScreenButton), los mismos que ya usa InputSystem_Actions.
    public sealed class TouchControls : MonoBehaviour
    {
        [Tooltip("Mostrar los controles en el editor para probarlos con el ratón")]
        [SerializeField] bool forceInEditor;

        [Header("Mientras juegas")]
        [Tooltip("Padre de palancas y botones")]
        [SerializeField] GameObject gameplay;
        [SerializeField] FloatingStick aimStick;
        [Tooltip("Cuánto hay que empujar la palanca derecha para apuntar y disparar (0 a 1)")]
        [SerializeField, Range(0.05f, 0.9f)] float fireThreshold = 0.3f;
        [Tooltip("Solo se ve cuando hay algo con qué interactuar")]
        [SerializeField] GameObject interactButton;
        [Tooltip("Solo se ve si tu puesto tiene habilidad; se atenúa mientras recarga")]
        [SerializeField] Image skillButton;
        [SerializeField] Button pauseButton;

        [Header("Pausa")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        /// true cuando se juega con la pantalla táctil (aunque esta escena no tenga jugador).
        public static bool Active { get; private set; }
        /// Dirección de la palanca derecha; (0,0) si no se está apuntando.
        public static Vector2 Aim { get; private set; }
        /// true mientras la palanca derecha está empujada: el jugador dispara.
        public static bool FireHeld { get; private set; }

        PlayerController player;
        PlayerSkill skill;
        bool paused;
        float skillAlpha = 1f;
        float searchT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = false;
            Aim = Vector2.zero;
            FireHeld = false;
        }

        void Awake()
        {
            Active = Application.isMobilePlatform || (Application.isEditor && forceInEditor);
            if (skillButton) skillAlpha = skillButton.color.a;
            if (pauseButton) pauseButton.onClick.AddListener(Pause);
            if (continueButton) continueButton.onClick.AddListener(Resume);
            if (quitButton) quitButton.onClick.AddListener(QuitToTitle);
            if (gameplay) gameplay.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (paused) Time.timeScale = 1f;
            ResetStatics();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Resume();
            player = null;
            searchT = 0f;
        }

        void Update()
        {
            if (!Active) return;

            if (player == null)
            {
                // el jugador ya está en la escena desde el inicio; se busca de vez en cuando por si acaso
                searchT -= Time.unscaledDeltaTime;
                if (searchT <= 0f)
                {
                    searchT = 0.5f;
                    player = FindAnyObjectByType<PlayerController>();
                    skill = player ? player.GetComponent<PlayerSkill>() : null;
                }
            }

            // botón "atrás" de Android
            if (player != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (paused) Resume(); else Pause();
            }

            bool show = player != null && !paused;
            if (gameplay && gameplay.activeSelf != show) gameplay.SetActive(show);

            var aim = show && aimStick ? aimStick.Value : Vector2.zero;
            bool aiming = aim.magnitude >= fireThreshold;
            Aim = aiming ? aim : Vector2.zero;
            FireHeld = aiming;

            if (!show) return;

            if (interactButton && interactButton.activeSelf != PlayerInteractor.HasTarget)
                interactButton.SetActive(PlayerInteractor.HasTarget);

            if (skillButton)
            {
                bool hasSkill = skill != null && skill.Role != null;
                if (skillButton.gameObject.activeSelf != hasSkill) skillButton.gameObject.SetActive(hasSkill);
                if (hasSkill)
                {
                    var c = skillButton.color;
                    c.a = skill.Ready ? skillAlpha : skillAlpha * 0.35f;
                    skillButton.color = c;
                }
            }
        }

        public void Pause()
        {
            if (!Active || paused || player == null) return;
            paused = true;
            Time.timeScale = 0f;
            if (gameplay) gameplay.SetActive(false);
            if (pausePanel) pausePanel.SetActive(true);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            if (pausePanel) pausePanel.SetActive(false);
        }

        void QuitToTitle()
        {
            Resume();
            SceneFlow.BackToTitle();
        }

        // al salir de la app (llamada, botón de inicio) el juego se queda en pausa
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Pause();
        }
    }
}
