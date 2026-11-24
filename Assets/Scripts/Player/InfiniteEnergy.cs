using UnityEngine;

namespace OnLinces
{
    /// Campo de tiro: energía siempre llena.
    public sealed class InfiniteEnergy : MonoBehaviour
    {
        PlayerWeapons weapons;

        void Update()
        {
            if (!weapons) weapons = FindAnyObjectByType<PlayerWeapons>();
            if (weapons) weapons.AddEnergy(weapons.MaxEnergy);
        }
    }
}
