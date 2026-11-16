using UnityEngine;

namespace OnLinces
{
    /// Lo que las balas del jugador pueden golpear.
    public interface IHittable
    {
        bool CanBeHit { get; }
        void Hit(float damage, Vector2 direction);
    }
}
