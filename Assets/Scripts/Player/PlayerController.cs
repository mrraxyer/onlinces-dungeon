using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Movimiento y puntería del jugador. El de la escena es el jugador 1; el 2 (invitado) se crea si se eligió en el título.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Controles")]
        [SerializeField] InputActionReference move;
        [SerializeField] InputActionReference look;
        [SerializeField] InputActionReference attack;
        [SerializeField] InputActionReference nextWeapon;

        [Header("Movimiento")]
        [Tooltip("px/s sin puesto")]
        [SerializeField] float speedPx = 184f;

        [Header("Apuntar")]
        [Tooltip("La mano")]
        [SerializeField] Transform aimOrigin;
        [SerializeField] Camera cam;

        public Vector2 MoveInput { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        public bool IsShooting { get; private set; }
        public bool SwitchWeaponPressed { get; private set; }
        /// Jugador 2: no guarda nada en la partida y solo usa su mando.
        public bool IsGuest { get; private set; }
        /// Personaje de este jugador (el 2 puede elegir otro).
        public Mascot Mascot => IsGuest ? Coop.P2Mascot : GameManager.Instance.Run.Mascot;
        public static PlayerController Main { get; private set; }
        static bool spawningGuest;
        static readonly Color GuestTint = new(0.65f, 0.85f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Main = null; spawningGuest = false; }

        InputActionAsset guestActions;

        Rigidbody2D rb;
        PlayerStats stats;
        PlayerRole role;
        bool usingMouse = true;
        Vector2 lastMouse;
        Vector2 knockPx;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<PlayerStats>();
            role = GetComponent<PlayerRole>();
            if (cam == null) cam = Camera.main;
            if (aimOrigin == null) aimOrigin = transform;
            IsGuest = spawningGuest;
            if (IsGuest)
            {
                guestActions = Instantiate(move.asset);
                guestActions.devices = new InputDevice[] { Coop.P2Pad };
                guestActions.Enable();
            }
            else Main = this;
        }

        void Start()
        {
            if (IsGuest) return;
            FilterMainDevices();
            if (Coop.HasGuest) SpawnGuest();
        }

        /// Jugador 1: solo la plataforma elegida en el título (sin elegir, o en el teléfono: todo).
        void FilterMainDevices() =>
            move.asset.devices = !Coop.Configured || TouchControls.Active ? null
                : Coop.P1Pad != null ? new InputDevice[] { Coop.P1Pad }
                : InputSystem.devices.Where(d => d is Keyboard || d is Mouse).ToArray();

        void OnDeviceChange(InputDevice d, InputDeviceChange c) => FilterMainDevices();

        /// La acción de este jugador (el invitado usa su propia copia del asset).
        public InputAction Act(InputActionReference r) =>
            r == null ? null : IsGuest ? guestActions.FindAction(r.action.id) : r.action;

        void SpawnGuest()
        {
            spawningGuest = true;
            var go = Instantiate(gameObject, transform.position + Vector3.right * 0.75f, transform.rotation);
            spawningGuest = false;
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>(true)) r.color = GuestTint;
            StatsHUD.AddFor(go.GetComponent<PlayerHealth>());
            SkillHUD.AddFor(go.GetComponent<PlayerSkill>());
            WeaponHUD.AddFor(go.GetComponent<PlayerWeapons>());
        }

        /// Ícono del botón para este jugador (el invitado siempre con su mando).
        public string Key(string keyboard, string xbox, string ps) =>
            IsGuest ? Coop.PadKey(Coop.P2Pad, xbox, ps) : TouchControls.Key(keyboard, xbox, ps);

        void OnEnable()
        {
            if (!IsGuest) InputSystem.onDeviceChange += OnDeviceChange;
            foreach (var a in new[] { move, look, attack, nextWeapon })
                if (a != null) a.action.Enable();
        }

        void Update()
        {
            MoveInput = Act(move).ReadValue<Vector2>();
            IsShooting = Act(attack).IsPressed() || (!IsGuest && TouchControls.FireHeld);
            SwitchWeaponPressed = nextWeapon != null && Act(nextWeapon).WasPressedThisFrame();
            UpdateAim();
        }

        void UpdateAim()
        {
            if (TouchControls.Active && !IsGuest)
            {
                // suelta: mira hacia donde camina
                if (TouchControls.FireHeld) Aim = TouchControls.Aim.normalized;
                else if (MoveInput.sqrMagnitude > 0.01f) Aim = MoveInput.normalized;
                return;
            }

            // con plataforma elegida, cada uno apunta solo con lo suyo
            var pad = IsGuest ? Coop.P2Pad : Coop.Configured ? Coop.P1Pad : Gamepad.current;
            bool mouseOk = !IsGuest && (!Coop.Configured || Coop.P1Pad == null);
            var stick = pad != null && pad.added ? pad.rightStick.ReadValue() : Vector2.zero;
            if (stick.sqrMagnitude > 0.09f)
            {
                usingMouse = false;
                Aim = stick.normalized;
                return;
            }

            if (mouseOk && Mouse.current != null)
            {
                var m = Mouse.current.position.ReadValue();
                if (m != lastMouse || Mouse.current.leftButton.isPressed) usingMouse = true;
                lastMouse = m;
                if (usingMouse)
                {
                    Vector2 world = cam.ScreenToWorldPoint(m);
                    var dir = world - (Vector2)aimOrigin.position;
                    if (dir.sqrMagnitude > 0.0001f) Aim = dir.normalized;
                    return;
                }
            }

            if (MoveInput.sqrMagnitude > 0.01f) Aim = MoveInput.normalized;
        }

        void OnDestroy() { if (guestActions) Destroy(guestActions); }

        /// Empujón en px/s que se frena solo.
        public void Knock(Vector2 px) => knockPx = px;

        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            MoveInput = Vector2.zero;
            IsShooting = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            float mult = stats ? stats.CurrentSpeedMult : 1f;
            float baseSpeed = role && role.Current ? role.Current.speedPx : speedPx;
            rb.linearVelocity = MoveInput * Units.Px(baseSpeed) * mult + knockPx / Units.PPU;
            knockPx = Vector2.MoveTowards(knockPx, Vector2.zero, 700f * Time.fixedDeltaTime);
        }
    }
}
