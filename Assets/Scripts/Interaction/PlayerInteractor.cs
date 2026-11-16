using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Busca lo interactuable más cercano, muestra "E: ..." y su descripción en el HUD, y lo usa al presionar Interact.
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] InputActionReference interact;
        [Tooltip("Distancia máxima en píxeles")]
        [SerializeField] float rangePx = 30f;

        /// true si hay algo cerca con qué interactuar.
        public static bool HasTarget => Current != null;
        /// Lo que tienes cerca (en teléfono el diálogo se pone encima de esto).
        public static IInteractable Current { get; private set; }

        /// En teléfono: tocar el aviso "USAR: ..." equivale a presionar E.
        public static void Tap() => tapRequested = true;
        static bool tapRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; tapRequested = false; }

        IInteractable current;
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
            // en pausa no hay diálogo (si no, queda encima del menú de pausa); al continuar vuelve solo
            if (Time.timeScale == 0f) best = null;
            current = best;
            Current = current;

            // el texto puede cambiar (precio, arma que cambias), así que se refresca cada cuadro
            // (en teléfono no hay tecla E: se usa el botón USAR, y el aviso lo nombra igual)
            var prompt = current != null ? (TouchControls.Active ? "<b>USAR:</b> " : "E: ") + current.Prompt : "";
            if (prompt != shownPrompt)
            {
                shownPrompt = prompt;
                HUD.SetPrompt(prompt);
                HUD.SetInfo(current?.InfoTitle, current?.InfoBody);
            }

            bool tapped = tapRequested && Time.timeScale > 0f; // en pausa no cuenta
            tapRequested = false;
            if (current != null && (interact.action.WasPressedThisFrame() || tapped))
            {
                current.Interact(gameObject);
                shownPrompt = null; // fuerza a refrescar
            }
        }

        void OnDisable()
        {
            Current = null;
            HUD.SetPrompt("");
            HUD.SetInfo(null, null);
            shownPrompt = null;
        }
    }
}
