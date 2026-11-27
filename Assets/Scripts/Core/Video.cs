using UnityEngine;

namespace OnLinces
{
    /// VSync, límite de FPS y estadísticas en PlayerPrefs. La resolución y el modo de pantalla los guarda Unity solo.
    public static class VideoSettings
    {
        /// -1 = sin límite.
        public static readonly int[] FpsLimits = { 30, 60, 120, 144, 165, 240, 360, -1 };

        public static bool VSync { get; private set; }
        public static int FpsLimit { get; private set; }
        public static bool ShowStats { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            VSync = PlayerPrefs.GetInt("video.vsync", 1) == 1;
            FpsLimit = PlayerPrefs.GetInt("video.fps", Application.isMobilePlatform ? 60 : -1);
            ShowStats = PlayerPrefs.GetInt("video.stats", 0) == 1;
            Apply();
        }

        public static void SetVSync(bool on) { VSync = on; PlayerPrefs.SetInt("video.vsync", on ? 1 : 0); Apply(); }
        public static void SetFpsLimit(int fps) { FpsLimit = fps; PlayerPrefs.SetInt("video.fps", fps); Apply(); }
        public static void SetShowStats(bool on) { ShowStats = on; PlayerPrefs.SetInt("video.stats", on ? 1 : 0); PlayerPrefs.Save(); }

        static void Apply()
        {
            // con VSync activo (en PC) Unity ignora targetFrameRate; en teléfono VSync no aplica
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FpsLimit;
            PlayerPrefs.Save();
        }

        public static string FpsName(int fps) => fps < 0 ? "Sin límite" : fps.ToString();
    }
}
