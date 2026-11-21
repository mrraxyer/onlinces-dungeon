using UnityEngine;

namespace OnLinces
{
    /// Cambia la dificultad en ciclo.
    public sealed class DifficultyTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] AudioClip clickClip;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        Difficulty Current => GameManager.Instance.Run.Difficulty;

        public string Prompt => $"Dificultad: {DifficultyInfo.Name(Current)}  (cambiar)";
        public string InfoTitle => "Dificultad: " + DifficultyInfo.Name(Current) + (SaveSystem.HasWon(Current) ? "  (ya la ganaste)" : "");
        public string InfoBody => InfoText.Join(
            InfoText.Section("Efectos", DifficultyInfo.Summary(Current)),
            InfoText.Section("Cambiar", "Con E pasas a la siguiente: Fácil → Media → Difícil."));
        public bool CanInteract => true;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            var d = DifficultyInfo.Cycle();
            player.GetComponent<PlayerHealth>()?.RefreshMax();
            Sfx.Play(clickClip);
            HUD.Feedback("Dificultad: " + DifficultyInfo.Name(d), HUD.Info);
        }
    }
}
