using UnityEngine;

namespace OnLinces
{
    /// Integrante de la mesa directiva en el Lobby. Con E tomas su puesto:
    /// tu On/Off se pone su vestimenta y cambia tu habilidad.
    public sealed class RoleStation : MonoBehaviour, IInteractable
    {
        [SerializeField] RoleData role;
        [SerializeField] AudioClip takeClip;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        bool IsMine => GameManager.Instance.Run.Role == role;

        public string Prompt => IsMine ? $"Ya tienes el puesto de {role.displayName}" : $"Tomar el puesto: {role.displayName}";
        public string InfoTitle => string.IsNullOrEmpty(role.person) ? role.displayName : $"{role.person} — {role.displayName}";
        public string InfoBody => $"{role.description}\nHabilidad: {role.skillName} — {role.skillDescription}";
        public bool CanInteract => true;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            if (IsMine) return;
            player.GetComponent<PlayerRole>().Set(role);
            Sfx.Play(takeClip);
            HUD.Feedback("Puesto: " + role.displayName, HUD.Info);
        }
    }
}
