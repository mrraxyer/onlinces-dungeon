using UnityEngine;

namespace OnLinces
{
    /// Portal al siguiente piso.
    public sealed class FloorExit : MonoBehaviour, IInteractable
    {
        [Tooltip("Se muestra en el diálogo")]
        [SerializeField] string destination = "el siguiente piso";
        [SerializeField] AudioClip enterClip;
        bool used;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        bool IsLast => GameManager.Instance.Run.Floor >= SceneFlow.LastFloor;

        public string Prompt => IsLast ? "Salir del calabozo" : "Entrar al portal";
        public string InfoTitle => "Portal";
        public string InfoBody => IsLast
            ? InfoText.Join("¡Venciste los 3 pisos!", InfoText.Section("Destino", "La salida: sal a celebrar."))
            : InfoText.Join(
                InfoText.Section("Destino", destination),
                InfoText.Section("Conservas", "Vida, monedas, ítems y armas."));
        public bool CanInteract => !used;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            used = true;
            Sfx.Play(enterClip);
            SceneFlow.CompleteFloor();
        }
    }
}
