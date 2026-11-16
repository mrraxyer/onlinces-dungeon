using UnityEngine;

namespace OnLinces
{
    /// Tienda: llena sus lugares con artículos (Godot: corazón $3, pizza $5, ítem $12).
    public sealed class ShopRoom : MonoBehaviour
    {
        [SerializeField] LootTable loot;
        [SerializeField] Transform heartSlot;
        [SerializeField] Transform pizzaSlot;
        [SerializeField] Transform itemSlot;
        [SerializeField] int heartPrice = 3;
        [SerializeField] int pizzaPrice = 5;
        [SerializeField] int itemPrice = 12;

        void Start()
        {
            Place(loot.heart, heartSlot, heartPrice);
            Place(loot.pizza, pizzaSlot, pizzaPrice);
            var item = Place(loot.itemPickup, itemSlot, itemPrice);
            if (item) item.SetItem(loot.RandomItem());
        }

        Pickup Place(Pickup prefab, Transform slot, int price)
        {
            if (prefab == null || slot == null) return null;
            var p = Instantiate(prefab, slot.position, Quaternion.identity, slot);
            p.SetPrice(price);
            return p;
        }
    }
}
