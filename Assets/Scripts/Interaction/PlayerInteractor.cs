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

        /// true si hay algo cerca con qué interactuar (los controles táctiles muestran su botón).
        public static bool HasTarget { get; private set; }

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
            current = best;
            HasTarget = current != null;

            // el texto puede cambiar (precio, arma que cambias), así que se refresca cada cuadro
            // (en teléfono no hay tecla E: se usa el botón USAR)
            var prompt = current != null ? (TouchControls.Active ? "" : "E: ") + current.Prompt : "";
            if (prompt != shownPrompt)
            {
                shownPrompt = prompt;
                HUD.SetPrompt(prompt);
                HUD.SetInfo(current?.InfoTitle, current?.InfoBody);
            }

            if (current != null && interact.action.WasPressedThisFrame())
            {
                current.Interact(gameObject);
                shownPrompt = null; // fuerza a refrescar
            }
        }

        void OnDisable()
        {
            HasTarget = false;
            HUD.SetPrompt("");
            HUD.SetInfo(null, null);
            shownPrompt = null;
        }
    }
}
