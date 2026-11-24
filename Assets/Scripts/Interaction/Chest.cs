using UnityEngine;

namespace OnLinces
{
    public enum ChestKind { Supply, Weapon }

    /// Cofre de suministros (monedas, a veces corazón o arma) o de arma.
    public sealed class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] ChestKind kind;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;
        [SerializeField] LootTable loot;
        [SerializeField] AudioClip appearClip;
        [SerializeField] AudioClip openClip;

        bool opened;
        float t;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);
        void Start()
        {
            spriteRenderer.sprite = closedSprite;
            Sfx.Play(appearClip, 0.5f);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (!opened && kind == ChestKind.Weapon)
                spriteRenderer.color = Color.Lerp(Color.white, new Color(0.85f, 0.8f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 4f));
        }

        public string Prompt => kind == ChestKind.Weapon ? "Abrir cofre de arma" : "Abrir cofre de suministros";
        public string InfoTitle => null;
        public string InfoBody => null;
        public bool CanInteract => !opened;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            opened = true;
            spriteRenderer.sprite = openSprite;
            spriteRenderer.color = Color.white;
            Sfx.Play(openClip);
            int energy = kind == ChestKind.Weapon ? 20 : 40;
            player.GetComponent<PlayerWeapons>().AddEnergy(energy);
            HUD.Feedback($"+{energy} energía", HUD.Good);
            var below = transform.position + Vector3.down * Units.Px(26);
            if (kind == ChestKind.Weapon)
            {
                SpawnWeapon(below);
            }
            else
            {
                int coins = Random.Range(3, 6);
                for (int i = 0; i < coins; i++) loot.Spawn(loot.coin, below);
                if (Random.value < 0.35f) loot.Spawn(loot.heart, below);
                if (Random.value < 0.35f) SpawnWeapon(below + Vector3.down * Units.Px(6));
            }
            Destroy(gameObject, 4f);
        }

        void SpawnWeapon(Vector3 pos)
        {
            var w = Instantiate(loot.weaponPickup, pos, Quaternion.identity);
            w.SetWeapon(loot.RandomWeapon());
            w.NoPedestal();
        }
    }
}
