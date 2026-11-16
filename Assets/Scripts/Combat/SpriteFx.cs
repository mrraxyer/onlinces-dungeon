using UnityEngine;

namespace OnLinces
{
    /// Efecto de un solo uso: reproduce sus cuadros y se destruye (polvo, explosión...).
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
