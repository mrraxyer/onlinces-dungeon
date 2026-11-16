using System;
using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    public enum Mascot { On, Off }

    /// Datos de la partida actual.
    public sealed class RunState
    {
        public Mascot Mascot = Mascot.On;
        public int Floor = 1;
        public bool? Won;       // null = en curso
        public int Hp = -1;     // -1 = llena
        public int BonusMaxHp;  // de ítems
        public RoleData Role;   // null = Miembro
        public Difficulty Difficulty = Difficulty.Media;
        public readonly List<ItemData> Items = new();
        public readonly List<WeaponData> Weapons = new(); // vacío = iniciales

        int coins;
        public int Coins => coins;
        public event Action CoinsChanged;

        public void AddCoins(int n) { coins += n; CoinsChanged?.Invoke(); }

        public bool TrySpend(int n)
        {
            if (coins < n) return false;
            coins -= n;
            CoinsChanged?.Invoke();
            return true;
        }
    }

    /// Persiste entre escenas; se crea solo al arrancar.
    public sealed class GameManager : MonoBehaviour
    {
        static GameManager instance;

        /// Lo busca o lo crea.
        public static GameManager Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<GameManager>();
                if (instance == null) CreateInstance();
                return instance;
            }
        }

        /// No lo crea (para OnDestroy).
        public static bool Exists => instance != null;

        public RunState Run { get; private set; } = new RunState();

        // sin recarga de dominio: limpiar estáticos
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            Interactables.All.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            if (instance != null) return;
            var go = new GameObject("GameManager");
            instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Run.Difficulty = (Difficulty)SaveSystem.Data.lastDifficulty;
        }

        public void ResetRun() => Run = new RunState { Difficulty = (Difficulty)SaveSystem.Data.lastDifficulty };
    }
}
