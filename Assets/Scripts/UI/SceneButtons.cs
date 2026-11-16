using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Pon este componente en un objeto de la escena y conecta los botones (On Click) a sus funciones.
    /// Los textos son opcionales: llénalos solo en la escena que los usa.
    public sealed class SceneButtons : MonoBehaviour
    {
        [Tooltip("Floor: muestra 'Piso X de 3'")]
        [SerializeField] TMP_Text floorLabel;
        [Tooltip("End: muestra si ganaste o perdiste")]
        [SerializeField] TMP_Text resultLabel;
        [Tooltip("Lobby: muestra con qué mascota juegas")]
        [SerializeField] TMP_Text mascotLabel;

        void Start()
        {
            var run = GameManager.Instance.Run;
            if (floorLabel) floorLabel.text = $"Piso {run.Floor} de {SceneFlow.LastFloor}";
            if (resultLabel) resultLabel.text = run.Won == true ? "¡Ganaste!" : "Game over";
            if (mascotLabel) mascotLabel.text = $"Juegas con {run.Mascot}";
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
