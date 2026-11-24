using UnityEngine;

namespace OnLinces
{
    /// Cartel: muestra su texto al acercarse; no se usa con E.
    public sealed class Sign : MonoBehaviour, IInteractable
    {
        [SerializeField] string title;
        [TextArea(3, 8)] [SerializeField] string body;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        public string Prompt => null;
        public string InfoTitle => title;
        public string InfoBody => body;
        public bool CanInteract => true;
        public Vector3 Position => transform.position;
        public void Interact(GameObject player) { }
    }
}
