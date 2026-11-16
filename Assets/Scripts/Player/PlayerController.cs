using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Movimiento y puntería del jugador.
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
        }

        void OnEnable()
        {
            foreach (var a in new[] { move, look, attack, nextWeapon })
                if (a != null) a.action.Enable();
        }

        void Update()
        {
            MoveInput = move.action.ReadValue<Vector2>();
            IsShooting = attack.action.IsPressed() || TouchControls.FireHeld;
            SwitchWeaponPressed = nextWeapon != null && nextWeapon.action.WasPressedThisFrame();
            UpdateAim();
        }

        void UpdateAim()
        {
            if (TouchControls.Active)
            {
                // suelta: mira hacia donde camina
                if (TouchControls.FireHeld) Aim = TouchControls.Aim.normalized;
                else if (MoveInput.sqrMagnitude > 0.01f) Aim = MoveInput.normalized;
                return;
            }

            var stick = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
            if (stick.sqrMagnitude > 0.09f)
            {
                usingMouse = false;
                Aim = stick.normalized;
                return;
            }

            if (Mouse.current != null)
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

        /// Empujón en px/s que se frena solo.
        public void Knock(Vector2 px) => knockPx = px;

        void OnDisable()
        {
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
