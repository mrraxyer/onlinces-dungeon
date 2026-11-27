using UnityEngine;

namespace OnLinces
{
    /// Campo de tiro: energía siempre llena.
    public sealed class InfiniteEnergy : MonoBehaviour
    {
        PlayerWeapons weapons;

        void Update()
        {
            foreach (var p in PlayerHealth.All)
                if (p.TryGetComponent(out weapons)) weapons.AddEnergy(weapons.MaxEnergy);
        }
    }
}
