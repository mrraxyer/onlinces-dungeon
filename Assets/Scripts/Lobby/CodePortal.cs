using UnityEngine;

namespace OnLinces
{
    /// Portal del Lobby al Piso 1, o a otra escena (campo de tiro y regreso).
    public sealed class CodePortal : MonoBehaviour, IInteractable
    {
        [SerializeField] string destination = "Piso 1: Oficina";
        [Tooltip("Vacío = calabozo. \"Lobby\" reinicia la partida (armas de prueba fuera)")]
        [SerializeField] string sceneName;
        [SerializeField] string title = "Portal de código";
        [SerializeField] AudioClip enterClip;
        bool used;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        public string Prompt => "Entrar al portal";
        public string InfoTitle => title;
        public string InfoBody
        {
            get
            {
                if (!string.IsNullOrEmpty(sceneName)) return InfoText.Section("Destino", destination);
                var run = GameManager.Instance.Run;
                var role = run.Role ? run.Role.displayName : "Miembro";
                return InfoText.Join(
                    InfoText.Section("Destino", destination),
                    InfoText.Section("Tu partida",
                        $"{InfoText.Stat("Puesto", role)}\n{InfoText.Stat("Dificultad", DifficultyInfo.Name(run.Difficulty))}"));
            }
        }
        public bool CanInteract => !used;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            used = true;
            Sfx.Play(enterClip);
            if (string.IsNullOrEmpty(sceneName)) SceneFlow.EnterDungeon();
            else if (sceneName == SceneFlow.Lobby) SceneFlow.BackToLobby();
            else ScreenFade.Load(sceneName);
        }
    }
}
