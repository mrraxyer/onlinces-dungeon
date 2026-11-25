using UnityEngine;

namespace OnLinces
{
    /// Animación de un solo uso.
    public sealed class SpriteFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 12f;
        [Tooltip("Se desvanece hasta el final")]
        [SerializeField] bool fade;

        float t;

        void Update()
        {
            t += Time.deltaTime;
            int i = (int)(t * framesPerSecond);
            if (i >= frames.Length) { Destroy(gameObject); return; }
            spriteRenderer.sprite = frames[i];
            if (fade) { var c = spriteRenderer.color; c.a = 1f - t * framesPerSecond / frames.Length; spriteRenderer.color = c; }
        }
    }
}
