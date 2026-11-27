using UnityEngine;

namespace OnLinces
{
    /// Teléfono: horizontal (los 60 fps los pone VideoSettings). Siempre crea TouchControls (en PC solo aporta el menú de pausa).
    public static class MobileSetup
    {
        const string PrefabName = "TouchControls";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Application.isMobilePlatform)
            {
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
