using UnityEngine;

namespace OnLinces
{
    /// Animación de un solo uso.
    public sealed class SpriteFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 12f;

        float t;

        void Update()
        {
            t += Time.deltaTime;
            int i = (int)(t * framesPerSecond);
            if (i >= frames.Length) { Destroy(gameObject); return; }
            spriteRenderer.sprite = frames[i];
        }
    }
}
