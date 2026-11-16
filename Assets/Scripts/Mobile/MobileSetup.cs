using UnityEngine;

namespace OnLinces
{
    /// Arranque para teléfono: se ejecuta solo antes de cargar la primera escena.
    /// Fija la pantalla en horizontal a 60 fps y crea los controles táctiles (Assets/Resources/TouchControls).
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
            else if (!Application.isEditor) return; // en PC no se crea nada

            var prefab = Resources.Load<TouchControls>(PrefabName);
            if (prefab == null) return;
            var controls = Object.Instantiate(prefab);
            controls.name = PrefabName;
            Object.DontDestroyOnLoad(controls.gameObject);
        }
    }
}
