using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OnLinces
{
    /// Controles táctiles: mover, apuntar/disparar, habilidad, arma y pausa. Persiste entre escenas.
    public sealed class TouchControls : MonoBehaviour
    {
        [Tooltip("Probar con el ratón en el editor")]
        [SerializeField] bool forceInEditor;

        [Header("Mientras juegas")]
        [SerializeField] GameObject gameplay;
        [SerializeField] FloatingStick aimStick;
        [SerializeField, Range(0.05f, 0.9f)] float fireThreshold = 0.3f;
        [SerializeField] Image skillButton;
        [SerializeField] Button pauseButton;

        [Header("HUD")]
        [Tooltip("Más alto = HUD más chico")]
        [SerializeField] float hudReferenceHeight = 360f;

        [Header("Pausa")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        public static bool Active { get; private set; }
        /// (0,0) si no apunta.
        public static Vector2 Aim { get; private set; }
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
            if (Active && TryGetComponent<CanvasScaler>(out var scaler)) MobileHud.Proportional(scaler, 320f);
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
            if (Active) MobileHud.Apply(hudReferenceHeight, GetComponent<Canvas>());
        }

        void Update()
        {
            if (!Active) return;

            if (player == null)
            {
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

        // pausa al salir de la app
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Pause();
        }
    }
}
