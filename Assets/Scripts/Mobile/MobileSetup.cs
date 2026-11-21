using UnityEngine;

namespace OnLinces
{
    /// Teléfono: horizontal y 60 fps. Siempre crea TouchControls (en PC solo aporta el menú de pausa).
    public static class MobileSetup
    {
        const string PrefabName = "TouchControls";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }

            var prefab = Resources.Load<TouchControls>(PrefabName);
            if (prefab == null) return;
            var controls = Object.Instantiate(prefab);
            controls.name = PrefabName;
            Object.DontDestroyOnLoad(controls.gameObject);
        }
    }
}
