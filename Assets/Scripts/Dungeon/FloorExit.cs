using UnityEngine;

namespace OnLinces
{
    /// Portal al final de cada nivel (aparece al vencer al jefe). Con E pasas al siguiente piso.
    public sealed class FloorExit : MonoBehaviour, IInteractable
    {
        [Tooltip("A dónde lleva (se muestra en el cuadro)")]
        [SerializeField] string destination = "el siguiente piso";
        [SerializeField] AudioClip enterClip;
        bool used;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        bool IsLast => GameManager.Instance.Run.Floor >= SceneFlow.LastFloor;

        public string Prompt => IsLast ? "Salir del calabozo" : "Entrar al portal";
        public string InfoTitle => "Portal";
        public string InfoBody => IsLast ? "¡Venciste los 3 pisos! Sal a celebrar." : $"Te lleva a {destination}.\nTu vida, monedas, ítems y armas se quedan contigo.";
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
