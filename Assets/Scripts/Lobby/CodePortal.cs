using UnityEngine;

namespace OnLinces
{
    /// Portal de código del Lobby: con E empieza la partida en el Piso 1.
    public sealed class CodePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] string destination = "Piso 1: Oficina";
        [SerializeField] AudioClip enterClip;
        bool used;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        public string Prompt => "Entrar al portal";
        public string InfoTitle => "Portal de código";
        public string InfoBody
        {
            get
            {
                var run = GameManager.Instance.Run;
                var role = run.Role ? run.Role.displayName : "Miembro";
                return $"Te lleva a {destination}.\nPuesto: {role} · Dificultad: {DifficultyInfo.Name(run.Difficulty)}";
            }
        }
        public bool CanInteract => !used;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            used = true;
            Sfx.Play(enterClip);
            SceneFlow.EnterDungeon();
        }
    }
}
