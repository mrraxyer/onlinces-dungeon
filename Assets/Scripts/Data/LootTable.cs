using UnityEngine;

namespace OnLinces
{
    /// Qué puede salir de enemigos, cofres y tienda. Create > OnLinces > Loot Table.
    [CreateAssetMenu(menuName = "OnLinces/Loot Table", fileName = "Loot")]
    public sealed class LootTable : ScriptableObject
    {
        [Header("Prefabs de cosas que se recogen")]
        public Pickup coin;
        public Pickup heart;
        public Pickup pizza;
        public Pickup drink;
        public Pickup itemPickup;
        public Pickup weaponPickup;

        [Header("Cofres")]
        public Chest supplyChest;
        public Chest weaponChest;

        [Header("Qué puede salir")]
        public WeaponData[] weapons;
        public ItemData[] items;

        [Header("Al morir un enemigo (Godot: on_enemy_killed)")]
        [Range(0, 1)] public float coinChance = 0.55f;
        [Range(0, 1)] public float heartChance = 0.035f;
        [Range(0, 1)] public float drinkChance = 0.015f;
        [Range(0, 1)] public float pizzaChance = 0.01f;

        [Header("Al limpiar una sala")]
        [Range(0, 1)] public float chestChance = 0.65f;
        [Range(0, 1)] public float weaponChestShare = 0.4f;

        public WeaponData RandomWeapon() => weapons.Length > 0 ? weapons[Random.Range(0, weapons.Length)] : null;
        public ItemData RandomItem() => items.Length > 0 ? items[Random.Range(0, items.Length)] : null;

        public void Spawn(Pickup prefab, Vector3 pos)
        {
            if (prefab == null) return;
            var p = Object.Instantiate(prefab, pos, Quaternion.identity);
            p.Pop();
        }

        public void DropFromEnemy(Vector3 pos)
        {
            float r = Random.value;
            if (r < heartChance) Spawn(heart, pos);
            else if (r < heartChance + drinkChance) Spawn(drink, pos);
            else if (r < heartChance + drinkChance + pizzaChance) Spawn(pizza, pos);
            if (Random.value < coinChance) Spawn(coin, pos);
        }
    }
}
