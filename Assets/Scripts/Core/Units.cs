namespace OnLinces
{
    /// 32 px = 1 unidad.
    public static class Units
    {
        public const float PPU = 32f;
        public static float Px(float pixels) => pixels / PPU;
    }
}
