using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Al empezar un nivel muestra su nombre en el centro ("Piso 1: Oficina") y se desvanece.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LevelTitle : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] float holdSeconds = 2f;
        [SerializeField] float fadeSeconds = 0.8f;

        CanvasGroup group;
        float t;

        void Start()
        {
            group = GetComponent<CanvasGroup>();
            var level = FindAnyObjectByType<FloorLevel>();
            int n = level ? level.FloorNumber : GameManager.Instance.Run.Floor;
            string name = level && level.Data ? level.Data.displayName : "";
            label.text = string.IsNullOrEmpty(name) ? $"Piso {n}" : $"Piso {n}: {name}";
            group.alpha = 0f;
        }

        void Update()
        {
            t += Time.deltaTime;
            float a = t < 0.3f ? t / 0.3f                       // aparece
                    : t < 0.3f + holdSeconds ? 1f               // se queda
                    : 1f - (t - 0.3f - holdSeconds) / fadeSeconds; // se desvanece
            group.alpha = Mathf.Clamp01(a);
            if (a <= 0f && t > 1f) gameObject.SetActive(false);
        }
    }
}
