using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Tienda: cada espacio ofrece algo aleatorio, sin repetir, entre curas y modificadores.
    public sealed class ShopRoom : MonoBehaviour
    {
        [SerializeField] LootTable loot;
        [SerializeField] Transform heartSlot;
        [SerializeField] Transform drinkSlot;
        [SerializeField] Transform pizzaSlot;
        [SerializeField] Transform itemSlot;
        [SerializeField] int heartPrice = 3;
        [SerializeField] int pizzaPrice = 5;
        [SerializeField] int drinkPrice = 3;
        [SerializeField] int itemPrice = 12;

        void Start()
        {
            var offers = new List<(Pickup prefab, int price, ItemData item)>
            {
                (loot.heart, heartPrice, null),
                (loot.pizza, pizzaPrice, null),
                (loot.drink, drinkPrice, null),
            };
            foreach (var it in loot.items) offers.Add((loot.itemPickup, itemPrice, it));

            foreach (var slot in new[] { heartSlot, drinkSlot, pizzaSlot, itemSlot })
            {
                if (offers.Count == 0) break;
                int i = Random.Range(0, offers.Count);
                var o = offers[i];
                offers.RemoveAt(i);
                var p = Place(o.prefab, slot, o.price);
                if (p && o.item) p.SetItem(o.item);
            }
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
