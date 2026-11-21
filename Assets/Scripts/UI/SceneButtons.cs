using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Acciones para los botones de las escenas. Textos opcionales.
    public sealed class SceneButtons : MonoBehaviour
    {
        [SerializeField] TMP_Text floorLabel;
        [SerializeField] TMP_Text mascotLabel;
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] AudioClip clickClip;

        void Start()
        {
            var run = GameManager.Instance.Run;
            if (floorLabel) floorLabel.text = $"Piso {run.Floor} de {SceneFlow.LastFloor}";
            if (mascotLabel) mascotLabel.text = $"Juegas con {run.Mascot}";
            ShowDifficulty();
        }

        public void CycleDifficulty()
        {
            DifficultyInfo.Cycle();
            Sfx.Play(clickClip);
            ShowDifficulty();
        }

        void ShowDifficulty()
        {
            if (difficultyLabel) difficultyLabel.text = $"Dificultad: <b>{DifficultyInfo.Name(GameManager.Instance.Run.Difficulty)}</b>  >";
        }

        public void PlayAsOn() => SceneFlow.StartRun(Mascot.On);
        public void PlayAsOff() => SceneFlow.StartRun(Mascot.Off);
        public void EnterDungeon() => SceneFlow.EnterDungeon();
        public void CompleteFloor() => SceneFlow.CompleteFloor();
        public void Die() => SceneFlow.Die();
        public void BackToTitle() => SceneFlow.BackToTitle();
        public void PlayAgain() => SceneFlow.BackToLobby();
        public void Quit() => Application.Quit();
    }
}
