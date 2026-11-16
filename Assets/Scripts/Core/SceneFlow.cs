using UnityEngine.SceneManagement;

namespace OnLinces
{
    /// Orden del juego: Title -> Lobby -> Floor1 -> Floor2 -> Floor3 -> End -> Title.
    /// Cada piso es su propia escena, diseñada a mano (Assets/Scenes/Floor1..3).
    /// Los nombres deben coincidir con los archivos y estar en Build Profiles > Scene List.
    public static class SceneFlow
    {
        public const string Title = "Title", Lobby = "Lobby", End = "End";
        public const int LastFloor = 3;

        public static string FloorScene(int floor) => "Floor" + floor;

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
            SceneManager.LoadScene(FloorScene(1));
        }

        public static void CompleteFloor()
        {
            if (Run.Floor < LastFloor)
            {
                Run.Floor++;
                SceneManager.LoadScene(FloorScene(Run.Floor));
            }
            else
            {
                Run.Won = true;
                SaveSystem.RegisterWin(Run.Difficulty);
                SceneManager.LoadScene(End);
            }
        }

        public static void Die()
        {
            Run.Won = false;
            SceneManager.LoadScene(End);
        }

        /// Empieza otra partida con la misma mascota, puesto y dificultad (desde End o al morir).
        public static void BackToLobby()
        {
            var old = Run;
            GameManager.Instance.ResetRun();
            Run.Mascot = old.Mascot; Run.Role = old.Role; Run.Difficulty = old.Difficulty;
            SceneManager.LoadScene(Lobby);
        }

        public static void BackToTitle()
        {
            GameManager.Instance.ResetRun();
            SceneManager.LoadScene(Title);
        }
    }
}
