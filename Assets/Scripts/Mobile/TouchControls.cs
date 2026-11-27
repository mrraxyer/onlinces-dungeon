using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OnLinces
{
    /// Controles táctiles (mover, apuntar/disparar, habilidad, arma) y menú de pausa (también en PC con Esc). Persiste entre escenas.
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

        [Header("Pausa")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button continueButton;
        [SerializeField] Button quitButton;

        public static bool Active { get; private set; }
        /// (0,0) si no apunta.
        public static Vector2 Aim { get; private set; }
        public static bool FireHeld { get; private set; }
        /// Se está jugando con un mando físico (último dispositivo usado).
        /// Con plataforma elegida en el título queda fija (no cambia al tocar el mando del jugador 2).
        public static bool PadActive => Coop.Configured && !Active ? Coop.P1Pad != null : padActive;
        static bool padActive;
        /// Nombre del botón según el mando: Xbox o PlayStation.
        public static string Pad(string xbox, string ps) =>
            (Coop.Configured && Coop.P1Pad != null ? Coop.P1Pad : Gamepad.current) is UnityEngine.InputSystem.DualShock.DualShockGamepad ? ps : xbox;
        /// Ícono del botón (Resources/Sprite Assets/Buttons) según el dispositivo en uso.
        public static string Key(string keyboard, string xbox, string ps) =>
            $"<sprite=\"Buttons\" name=\"{(PadActive ? Pad(xbox, ps) : keyboard)}\">";

        PlayerController player;
        PlayerSkill skill;
        bool paused;
        GraphicsMenu graphics;
        System.IDisposable anyButton;
        float skillAlpha = 1f;
        float searchT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = false;
            Aim = Vector2.zero;
            FireHeld = false;
            padActive = false;
        }

        void Awake()
        {
            Active = Application.isMobilePlatform || (Application.isEditor && forceInEditor);
            if (skillButton) skillAlpha = skillButton.color.a;
            if (pauseButton) pauseButton.onClick.AddListener(Pause);
            if (continueButton) continueButton.onClick.AddListener(Resume);
            if (quitButton) quitButton.onClick.AddListener(QuitToTitle);
            if (pausePanel && continueButton && quitButton) BuildGraphicsMenu();
            if (gameplay) gameplay.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            // en táctil los sticks en pantalla son un mando virtual: no cuenta
            anyButton = InputSystem.onAnyButtonPress.Call(c => { if (!Active) padActive = c.device is Gamepad; });
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            anyButton?.Dispose();
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
            if (player == null)
            {
                searchT -= Time.unscaledDeltaTime;
                if (searchT <= 0f)
                {
                    searchT = 0.5f;
                    player = PlayerController.Main;
                    skill = player ? player.GetComponent<PlayerSkill>() : null;
                }
            }

            // Esc en PC, botón "atrás" en Android, Start/Options en mando
            var pad = Gamepad.current;
            if (!Active)
            {
                // los sticks y el ratón no son botones: se revisan aparte
                if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.rightStick.ReadValue().sqrMagnitude > 0.25f)) padActive = true;
                else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f) padActive = false;
            }
            if (player != null && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame)))
            {
                if (!paused) Pause();
                else if (graphics && graphics.IsOpen) graphics.Back();
                else Resume();
            }
            // mando en menús: si nada está seleccionado, selecciona el botón más alto
            if (PadActive && (player == null || paused)) SelectForGamepad();

            if (!Active) return; // PC: solo pausa

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

        static void SelectForGamepad()
        {
            var es = EventSystem.current;
            if (es == null || (es.currentSelectedGameObject != null && es.currentSelectedGameObject.activeInHierarchy)) return;
            Selectable best = null;
            foreach (var sel in Selectable.allSelectablesArray)
                if (sel.IsInteractable() && (best == null || sel.transform.position.y > best.transform.position.y)) best = sel;
            if (best) es.SetSelectedGameObject(best.gameObject);
        }

        public void Pause()
        {
            if (paused || player == null) return;
            paused = true;
            Time.timeScale = 0f;
            if (gameplay) gameplay.SetActive(false);
            if (pausePanel) pausePanel.SetActive(true);
            if (PadActive && continueButton) EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            if (graphics) graphics.Close();
            if (pausePanel) pausePanel.SetActive(false);
        }

        // "Opciones" entre Continuar y Salir (la caja crece un botón) y el panel de estadísticas
        void BuildGraphicsMenu()
        {
            const float step = 50f;
            var box = (RectTransform)continueButton.transform.parent;
            var btn = Instantiate(continueButton, box);
            btn.name = "BtnGraphics";
            btn.onClick = new Button.ButtonClickedEvent();
            btn.GetComponentInChildren<TMP_Text>().text = "Opciones";
            btn.transform.SetSiblingIndex(continueButton.transform.GetSiblingIndex() + 1);
            box.sizeDelta += new Vector2(0f, step);
            foreach (RectTransform child in box)
                if (child != btn.transform && child != quitButton.transform) child.anchoredPosition += new Vector2(0f, step / 2f);
            var cont = ((RectTransform)continueButton.transform).anchoredPosition;
            ((RectTransform)btn.transform).anchoredPosition = cont - new Vector2(0f, step);
            ((RectTransform)quitButton.transform).anchoredPosition = cont - new Vector2(0f, 2f * step);
            // el prefab trae navegación explícita Continuar <-> Salir: se rehace en ciclo con Opciones en medio
            Link(continueButton, quitButton, btn);
            Link(btn, continueButton, quitButton);
            Link(quitButton, btn, continueButton);

            graphics = gameObject.AddComponent<GraphicsMenu>();
            graphics.Build(box.gameObject, continueButton);
            btn.onClick.AddListener(graphics.Open);
            gameObject.AddComponent<PerfStats>().Build(pausePanel.transform.parent, continueButton.GetComponentInChildren<TMP_Text>());
        }

        static void Link(Button b, Selectable up, Selectable down) =>
            b.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };

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
