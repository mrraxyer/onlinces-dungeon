using System;
using System.Collections.Generic;
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
        public int BonusMaxHp; // corazones extra de ítems (Unit tests)
        public readonly List<ItemData> Items = new();
        public readonly List<WeaponData> Weapons = new(); // armas que llevas (vacío = las iniciales)

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

    /// Vive durante todo el juego (no se destruye al cambiar de escena).
    /// Se crea solo antes de cargar la primera escena, así funciona aunque le des Play desde cualquier escena.
    public sealed class GameManager : MonoBehaviour
    {
        static GameManager instance;

        /// Siempre devuelve un GameManager vivo (lo busca o lo crea si hace falta).
        public static GameManager Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<GameManager>();
                if (instance == null) CreateInstance();
                return instance;
            }
        }

        /// true si ya hay un GameManager vivo (no crea uno: úsalo en OnDestroy/OnDisable).
        public static bool Exists => instance != null;

        public RunState Run { get; private set; } = new RunState();

        // El proyecto entra a Play sin recargar el dominio (Enter Play Mode Options):
        // las variables estáticas conservan valores de la partida anterior, así que se limpian aquí.
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
        }

        public void ResetRun() => Run = new RunState();
    }
}
