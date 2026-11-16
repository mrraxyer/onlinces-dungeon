using UnityEngine;

namespace OnLinces
{
    public enum Mascot { On, Off }

    /// Datos de la partida actual. Se reinician al empezar una partida nueva.
    public sealed class RunState
    {
        public Mascot Mascot = Mascot.On;
        public int Floor = 1;
        public bool? Won; // null = la partida sigue
        public int Hp = -1; // vida que se lleva al siguiente piso (-1 = llena)
    }

    /// Vive durante todo el juego (no se destruye al cambiar de escena).
    /// Se crea solo antes de cargar la primera escena, así funciona aunque le des Play desde cualquier escena.
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public RunState Run { get; private set; } = new RunState();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            if (Instance != null) return;
            var go = new GameObject("GameManager");
            Instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ResetRun() => Run = new RunState();
    }
}
