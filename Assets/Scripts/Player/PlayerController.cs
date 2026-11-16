using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Lee los controles y mueve al jugador. Apunta con el ratón o con el stick derecho.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Controles (de InputSystem_Actions > Player)")]
        [SerializeField] InputActionReference move;
        [SerializeField] InputActionReference look;
        [SerializeField] InputActionReference attack;
        [SerializeField] InputActionReference nextWeapon;

        [Header("Movimiento")]
        [Tooltip("Píxeles por segundo. Godot: 115 (Miembro) x 1.6")]
        [SerializeField] float speedPx = 184f;

        [Header("Apuntar")]
        [Tooltip("Punto desde donde se apunta (la mano)")]
        [SerializeField] Transform aimOrigin;
        [SerializeField] Camera cam;

        public Vector2 MoveInput { get; private set; }
        public Vector2 Aim { get; private set; } = Vector2.right;
        public bool IsShooting { get; private set; }
        public bool SwitchWeaponPressed { get; private set; }

        Rigidbody2D rb;
        PlayerStats stats;
        PlayerRole role;
        PlayerSkill skill;
        bool usingMouse = true;
        Vector2 lastMouse;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<PlayerStats>();
            role = GetComponent<PlayerRole>();
            skill = GetComponent<PlayerSkill>();
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
            // en teléfono se dispara solo mientras empujas la palanca derecha
            IsShooting = attack.action.IsPressed() || TouchControls.FireHeld;
            SwitchWeaponPressed = nextWeapon != null && nextWeapon.action.WasPressedThisFrame();
            UpdateAim();
        }

        void UpdateAim()
        {
            if (TouchControls.Active)
            {
                // palanca derecha táctil; si está suelta, miras hacia donde caminas
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

        Vector2 knockPx;

        /// Empujón (en píxeles por segundo) que se va frenando solo.
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
