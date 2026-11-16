using UnityEngine;

namespace OnLinces
{
    /// Algo que las balas del jugador pueden golpear (enemigos, cajas).
    public interface IHittable
    {
        bool CanBeHit { get; }
        void Hit(float damage, Vector2 direction);
    }
}
