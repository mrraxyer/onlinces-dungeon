using UnityEngine.SceneManagement;

namespace OnLinces
{
    /// Orden del juego: Title -> Lobby -> Floor (pisos 1, 2 y 3) -> End -> Title.
    /// Los nombres deben coincidir con los archivos de Assets/Scenes y estar en Build Profiles > Scene List.
    public static class SceneFlow
    {
        public const string Title = "Title", Lobby = "Lobby", Floor = "Floor", End = "End";
        public const int LastFloor = 3;

        static RunState Run => GameManager.Instance.Run;

        public static void StartRun(Mascot mascot)
        {
            GameManager.Instance.ResetRun();
            Run.Mascot = mascot;
            SceneManager.LoadScene(Lobby);
        }

        public static void EnterDungeon()
        {
            Run.Floor = 1;
            SceneManager.LoadScene(Floor);
        }

        public static void CompleteFloor()
        {
            if (Run.Floor < LastFloor)
            {
                Run.Floor++;
                SceneManager.LoadScene(Floor);
            }
            else
            {
                Run.Won = true;
                SceneManager.LoadScene(End);
            }
        }

        public static void Die()
        {
            Run.Won = false;
            SceneManager.LoadScene(End);
        }

        public static void BackToTitle()
        {
            GameManager.Instance.ResetRun();
            SceneManager.LoadScene(Title);
        }
    }
}
