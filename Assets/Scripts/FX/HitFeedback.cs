using UnityEngine;

namespace OnLinces
{
    /// Reacción al recibir un golpe: solo movimiento de cámara (empujón, inclinación y temblor)
    /// y un desenfoque breve alrededor del jugador, como el del apagón.
    public static class HitFeedback
    {
        /// strong = el golpe quita vida (si solo pega en el escudo, la reacción es más suave).
        public static void PlayerHit(Transform player, Vector2 away, bool strong)
        {
            float k = strong ? 1f : 0.45f;
            // el mundo brinca hacia el lado del golpe y la cámara se ladea en contra
            float side = Mathf.Abs(away.x) > 0.2f ? Mathf.Sign(away.x) : (Random.value < 0.5f ? -1f : 1f);
            CameraFollow.Kick(away * 16f * k, -side * 5f * k);
            CameraFollow.Shake(8f * k);
            if (strong) ScreenBlur.Hit(player);
        }
    }
}
