namespace OnLinces
{
    public enum Difficulty { Facil, Media, Dificil }

    /// Multiplicadores por dificultad.
    public static class DifficultyInfo
    {
        public static string Name(Difficulty d) => d switch { Difficulty.Facil => "Fácil", Difficulty.Media => "Media", _ => "Difícil" };
        public static float EnemyHp(Difficulty d) => d switch { Difficulty.Facil => 0.7f, Difficulty.Media => 1f, _ => 1.35f };
        public static float BossHp(Difficulty d) => EnemyHp(d);
        public static float Bullet(Difficulty d) => d switch { Difficulty.Facil => 0.85f, Difficulty.Media => 1f, _ => 1.2f };
        public static float Count(Difficulty d) => d switch { Difficulty.Facil => 0.75f, Difficulty.Media => 1f, _ => 1.3f };
        /// Medios corazones extra.
        public static int PlayerHp(Difficulty d) => d == Difficulty.Facil ? 2 : 0;

        public static string Summary(Difficulty d) => d switch
        {
            Difficulty.Facil => "Enemigos con 70% de vida, balas más lentas, menos enemigos y +1 corazón.",
            Difficulty.Media => "La experiencia normal.",
            _ => "Enemigos con 135% de vida, balas más rápidas y 30% más enemigos.",
        };

        /// Pasa a la siguiente dificultad de la partida y la guarda.
        public static Difficulty Cycle()
        {
            var run = GameManager.Instance.Run;
            run.Difficulty = (Difficulty)(((int)run.Difficulty + 1) % 3);
            SaveSystem.Data.lastDifficulty = (int)run.Difficulty;
            SaveSystem.Save();
            return run.Difficulty;
        }
    }
}
