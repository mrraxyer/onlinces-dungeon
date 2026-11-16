using UnityEngine;

namespace OnLinces
{
    /// Sala del tesoro: un ítem gratis.
    public sealed class TreasureRoom : MonoBehaviour
    {
        [SerializeField] LootTable loot;
        [SerializeField] Transform itemSlot;

        void Start()
        {
            var p = Instantiate(loot.itemPickup, itemSlot.position, Quaternion.identity, itemSlot);
            p.SetItem(loot.RandomItem());
        }
    }
}
