using UnityEngine;

namespace OnLinces
{
    /// Botín de enemigos, cofres y tienda.
    [CreateAssetMenu(menuName = "OnLinces/Loot Table", fileName = "Loot")]
    public sealed class LootTable : ScriptableObject
    {
        [Header("Recogibles")]
        public Pickup coin;
        public Pickup energy;
        public Pickup heart;
        public Pickup pizza;
        public Pickup drink;
        public Pickup itemPickup;
        public Pickup weaponPickup;

        [Header("Cofres")]
        public Chest supplyChest;
        public Chest weaponChest;

        [Header("Posibles")]
        public WeaponData[] weapons;
        public ItemData[] items;

        [Header("Al morir un enemigo")]
        [Range(0, 1)] public float coinChance = 0.55f;
        [Range(0, 1)] public float heartChance = 0.035f;
        [Range(0, 1)] public float drinkChance = 0.06f;
        [Range(0, 1)] public float pizzaChance = 0.01f;
        [Tooltip("Orbes de energía (mín, máx inclusive)")]
        public Vector2Int energyDrops = new(1, 3);
        public int bossEnergyDrops = 12;

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
            SpawnMany(energy, pos, Random.Range(energyDrops.x, energyDrops.y + 1));
        }

        public void SpawnMany(Pickup prefab, Vector3 pos, int n)
        {
            for (int i = 0; i < n; i++) Spawn(prefab, pos);
        }
    }
}
