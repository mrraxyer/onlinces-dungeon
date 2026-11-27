using UnityEngine.SceneManagement;

namespace OnLinces
{
    /// Title → Lobby → Floor1..3 → End.
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
            ScreenFade.Load(Lobby);
        }

        public static void EnterDungeon()
        {
            Run.Floor = 1;
            ScreenFade.Load(FloorScene(1));
        }

        public static void CompleteFloor()
        {
            if (Run.Floor < LastFloor)
            {
                Run.Floor++;
                ScreenFade.Load(FloorScene(Run.Floor));
            }
            else
            {
                Run.Won = true;
                SaveSystem.RegisterWin(Run.Difficulty);
                ScreenFade.Load(End);
            }
        }

        public static void Die()
        {
            Run.Won = false;
            ScreenFade.Load(End);
        }

        /// Nueva partida con la misma mascota, puesto y dificultad.
        public static void BackToLobby()
        {
            var old = Run;
            GameManager.Instance.ResetRun();
            Run.Mascot = old.Mascot; Run.Role = old.Role; Run.Difficulty = old.Difficulty;
            ScreenFade.Load(Lobby);
        }

        public static void BackToTitle()
        {
            GameManager.Instance.ResetRun();
            ScreenFade.Load(Title);
        }
    }
}
