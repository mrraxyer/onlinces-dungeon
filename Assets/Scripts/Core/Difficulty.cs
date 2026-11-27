namespace OnLinces
{
    public enum Difficulty { Facil, Media, Dificil }

    /// Multiplicadores por dificultad. Los valores están en Assets/Resources/Difficulty.asset.
    public static class DifficultyInfo
    {
        static DifficultySettings.Level L(Difficulty d) => DifficultySettings.Current.Get(d);

        public static string Name(Difficulty d) => L(d).displayName;
        public static float EnemyHp(Difficulty d) => L(d).enemyHp;
        public static float BossHp(Difficulty d, BossKind k) => DifficultySettings.Current.BossHp(d, k);
        public static float Bullet(Difficulty d) => L(d).bulletSpeed;
        public static float Count(Difficulty d) => L(d).enemyCount;
        /// Medios corazones extra.
        public static int PlayerHp(Difficulty d) => L(d).playerHalfHearts;
        public static string Summary(Difficulty d) => DifficultySettings.Current.Summary(d);

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
