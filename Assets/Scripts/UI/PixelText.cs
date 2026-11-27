namespace OnLinces
{
    /// Tamaños de la fuente pixel OnLincesPixel (5x7, em de 11 px).
    /// Solo se ve nítida en múltiplos de 11: 11, 22, 33...
    public static class PixelText
    {
        public const float Base = 11f;

        /// Redondea un tamaño cualquiera al múltiplo de 11 más cercano (mínimo 11).
        public static float Size(float size) => Base * System.Math.Max(1, (int)System.Math.Round(size / Base));
    }
}
