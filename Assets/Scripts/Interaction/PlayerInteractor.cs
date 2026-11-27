using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Elige el interactuable más cercano, muestra su diálogo y lo usa.
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] InputActionReference interact;
        [Tooltip("px")]
        [SerializeField] float rangePx = 30f;

        public static IInteractable Current { get; private set; }

        /// Toque en el aviso (teléfono) = E.
        public static void Tap() => tapRequested = true;
        static bool tapRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; tapRequested = false; }

        IInteractable current, shownTarget;
        PlayerController controller;
        string shownPrompt;

        void OnEnable() { if (interact) interact.action.Enable(); }

        void Update()
        {
            IInteractable best = null;
            float bestD = Units.Px(rangePx);
            foreach (var it in Interactables.All)
            {
                if (!it.CanInteract) continue;
                float d = Vector2.Distance(it.Position, transform.position);
                if (d < bestD) { bestD = d; best = it; }
            }
            if (Time.timeScale == 0f) best = null; // en pausa no hay diálogo

            // jugador 2: su propio aviso sobre la cabeza (el diálogo de info es del jugador 1)
            if (!controller) controller = GetComponent<PlayerController>();
            if (controller.IsGuest)
            {
                bool usable = best != null && !string.IsNullOrEmpty(best.Prompt);
                HUD.SetGuestPrompt(usable ? controller.Key("key_e", "xb_a", "ps_cross") + " " + best.Prompt : "", transform);
                if (usable && controller.Act(interact).WasPressedThisFrame()) best.Interact(gameObject);
                return;
            }

            current = best;
            Current = current;

            // sin Prompt (carteles): solo el diálogo, sin botón
            var prompt = current != null && !string.IsNullOrEmpty(current.Prompt) ? (TouchControls.Active ? "<b>USAR:</b> " : TouchControls.Key("key_e", "xb_a", "ps_cross") + " ") + current.Prompt : "";
            if (prompt != shownPrompt || current != shownTarget)
            {
                shownPrompt = prompt;
                shownTarget = current;
                HUD.SetPrompt(prompt);
                HUD.SetInfo(current?.InfoTitle, current?.InfoBody);
            }

            bool tapped = tapRequested && Time.timeScale > 0f; // en pausa no cuenta
            tapRequested = false;
            if (current != null && !string.IsNullOrEmpty(current.Prompt) && (interact.action.WasPressedThisFrame() || tapped))
            {
                current.Interact(gameObject);
                // puede haberse destruido; Destroy ocurre antes de LateUpdate (InteractionDialog)
                current = null;
                Current = null;
                shownPrompt = null; // refrescar
            }
        }

        void OnDisable()
        {
            Current = null;
            HUD.SetPrompt("");
            HUD.SetInfo(null, null);
            shownPrompt = null;
            shownTarget = null;
        }
    }
}
