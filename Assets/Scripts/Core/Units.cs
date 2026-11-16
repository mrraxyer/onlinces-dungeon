namespace OnLinces
{
    /// Los números de balance vienen del juego de Godot, que medía todo en píxeles.
    /// Aquí 32 píxeles = 1 unidad de Unity (igual que el Pixels Per Unit de los sprites).
    public static class Units
    {
        public const float PPU = 32f;
        public static float Px(float pixels) => pixels / PPU;
    }
}
