using UnityEngine;

namespace OnLinces
{
    /// Puerta de sala: abierta deja pasar, cerrada bloquea (door_0 = abierta, door_1 = cerrada).
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite openSprite;
        [SerializeField] Sprite closedSprite;
        [Tooltip("Colisionador que bloquea el paso cuando está cerrada")]
        [SerializeField] Collider2D blocker;
        [SerializeField] bool startOpen = true;

        void Start() => SetOpen(startOpen);

        public void SetOpen(bool open)
        {
            blocker.enabled = !open;
            spriteRenderer.sprite = open ? openSprite : closedSprite;
        }
    }
}
