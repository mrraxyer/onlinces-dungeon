using UnityEngine;

namespace OnLinces
{
    /// Puerta de sala.
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite openSprite;
        [SerializeField] Sprite closedSprite;
        [Tooltip("Bloquea si está cerrada")]
        [SerializeField] Collider2D blocker;
        [SerializeField] bool startOpen = true;
        [Tooltip("Opcional: reja que baja al cerrar")]
        [SerializeField] SpriteRenderer gate;

        void Start() => SetOpen(startOpen);

        public void SetOpen(bool open)
        {
            blocker.enabled = !open;
            spriteRenderer.sprite = open ? openSprite : closedSprite;
            if (gate) gate.enabled = !open;
        }
    }
}
