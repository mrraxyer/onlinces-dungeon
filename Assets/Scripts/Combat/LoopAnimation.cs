using UnityEngine;

namespace OnLinces
{
    /// Repite unos cuadros para siempre (vendedor, portal...).
    public sealed class LoopAnimation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 2f;
        [Tooltip("Grados por segundo (portal)")]
        [SerializeField] float spinDegrees;

        float t;

        void Update()
        {
            t += Time.deltaTime;
            if (frames.Length > 0) spriteRenderer.sprite = frames[(int)(t * framesPerSecond) % frames.Length];
            if (spinDegrees != 0f) spriteRenderer.transform.Rotate(0, 0, spinDegrees * Time.deltaTime);
        }
    }
}
