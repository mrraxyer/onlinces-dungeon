using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OnLinces
{
    /// Transición entre escenas: funde a oscuro, carga y vuelve a aparecer (interpolación con easing).
    public sealed class ScreenFade : MonoBehaviour
    {
        const float OutTime = 0.18f, InTime = 0.3f;
        static ScreenFade instance;
        Image image;
        bool busy;

        static ScreenFade Instance
        {
            get
            {
                if (instance) return instance;
                var go = new GameObject("ScreenFade");
                DontDestroyOnLoad(go);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var img = new GameObject("Black").AddComponent<Image>();
                img.transform.SetParent(go.transform, false);
                img.rectTransform.anchorMin = Vector2.zero;
                img.rectTransform.anchorMax = Vector2.one;
                img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
                img.color = new Color(0.047f, 0.039f, 0.07f, 0f);
                img.raycastTarget = false;
                instance = go.AddComponent<ScreenFade>();
                instance.image = img;
                return instance;
            }
        }

        public static void Load(string scene)
        {
            var f = Instance;
            if (f.busy) return;
            f.StartCoroutine(f.Run(scene));
        }

        IEnumerator Run(string scene)
        {
            busy = true;
            image.raycastTarget = true;
            for (float t = 0f; t < OutTime; t += Time.unscaledDeltaTime)
            {
                float p = t / OutTime;
                Set(p * p); // ease-in
                yield return null;
            }
            Set(1f);
            yield return SceneManager.LoadSceneAsync(scene);
            for (float t = 0f; t < InTime; t += Time.unscaledDeltaTime)
            {
                float p = t / InTime;
                Set((1f - p) * (1f - p)); // ease-out
                yield return null;
            }
            Set(0f);
            image.raycastTarget = false;
            busy = false;
        }

        void Set(float a)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
