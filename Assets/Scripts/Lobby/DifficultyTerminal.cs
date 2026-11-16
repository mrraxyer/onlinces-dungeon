using UnityEngine;

namespace OnLinces
{
    /// Terminal junto al portal: con E cambias la dificultad (Fácil → Media → Difícil).
    public sealed class DifficultyTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] AudioClip clickClip;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        Difficulty Current => GameManager.Instance.Run.Difficulty;

        public string Prompt => $"Dificultad: {DifficultyInfo.Name(Current)}  (cambiar)";
        public string InfoTitle => "Dificultad: " + DifficultyInfo.Name(Current) + (SaveSystem.HasWon(Current) ? "  (ya la ganaste)" : "");
        public string InfoBody => DifficultyInfo.Summary(Current);
        public bool CanInteract => true;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            var run = GameManager.Instance.Run;
            run.Difficulty = (Difficulty)(((int)run.Difficulty + 1) % 3);
            SaveSystem.Data.lastDifficulty = (int)run.Difficulty;
            SaveSystem.Save();
            player.GetComponent<PlayerHealth>()?.RefreshMax();
            Sfx.Play(clickClip);
            HUD.Feedback("Dificultad: " + DifficultyInfo.Name(run.Difficulty), HUD.Info);
        }
    }
}
