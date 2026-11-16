using UnityEngine;

namespace OnLinces
{
    public enum PickupKind { Coin, Heart, Pizza, Drink, Item, Weapon }

    /// Recogible: lo gratis se toma al pasar; ítems, armas y tienda con E.
    [RequireComponent(typeof(Collider2D))]
    public sealed class Pickup : MonoBehaviour, IInteractable
    {
        [SerializeField] PickupKind kind;
        [Tooltip("Ítems y armas usan el suyo")]
        [SerializeField] string displayName;
        [TextArea] [SerializeField] string description;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Sprite[] frames;
        [SerializeField] float framesPerSecond = 8f;
        [Tooltip("0 = gratis")]
        [SerializeField] int price;
        [SerializeField] TMPro.TMP_Text priceLabel;
        [SerializeField] ItemData item;
        [SerializeField] WeaponData weapon;
        [SerializeField] AudioClip takeClip;
        [SerializeField] AudioClip deniedClip;

        float t, baseY;
        Vector2 popVelPx;
        bool used;
        Transform player;

        public int Price => price;
        bool NeedsE => price > 0 || kind is PickupKind.Item or PickupKind.Weapon;

        void Awake()
        {
            t = Random.value * 3f;
            if (visual) baseY = visual.transform.localPosition.y;
        }

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        void Start()
        {
            var ph = FindAnyObjectByType<PlayerHealth>();
            if (ph) player = ph.transform;
            RefreshVisual();
        }

        public void SetItem(ItemData data) { item = data; RefreshVisual(); }
        public void SetWeapon(WeaponData data) { weapon = data; RefreshVisual(); }
        public void SetPrice(int p) { price = p; RefreshVisual(); }
        /// Salta un poco al aparecer.
        public void Pop() => popVelPx = Random.insideUnitCircle.normalized * Random.Range(40f, 90f);

        void RefreshVisual()
        {
            if (visual == null) return;
            if (kind == PickupKind.Item && item) visual.sprite = item.icon;
            if (kind == PickupKind.Weapon && weapon) visual.sprite = weapon.icon;
            if (priceLabel)
            {
                priceLabel.text = price > 0 ? "$" + price : "";
                // fondo del precio
                var bg = priceLabel.transform.parent != transform ? priceLabel.transform.parent : null;
                if (bg) bg.gameObject.SetActive(price > 0);
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            if (frames != null && frames.Length > 1) visual.sprite = frames[(int)(t * framesPerSecond) % frames.Length];

            if (popVelPx.sqrMagnitude > 1f)
            {
                transform.position += (Vector3)(popVelPx / Units.PPU * Time.deltaTime);
                popVelPx = Vector2.MoveTowards(popVelPx, Vector2.zero, 300f * Time.deltaTime);
            }
            else if (kind == PickupKind.Coin && player && t > 0.5f)
            {
                // imán de monedas
                Vector2 d = player.position - transform.position;
                float dpx = d.magnitude * Units.PPU;
                if (dpx < 80f) transform.position += (Vector3)(d.normalized * Units.Px(Mathf.Min(dpx, (260f - dpx * 2f) * Time.deltaTime)));
            }

            if (visual) visual.transform.localPosition = new Vector3(0, baseY + Units.Px(Mathf.Sin(t * 3f) * 1.5f), 0);
            if (priceLabel && price > 0)
                priceLabel.color = GameManager.Instance.Run.Coins >= price ? new Color(0.17f, 0.1f, 0.06f) : new Color(0.75f, 0.12f, 0.15f);
        }

        // ---------- al pasar encima ----------
        void OnTriggerEnter2D(Collider2D other) => TryWalkOver(other);
        void OnTriggerStay2D(Collider2D other) => TryWalkOver(other);

        void TryWalkOver(Collider2D other)
        {
            if (used || NeedsE) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead) return;
            if (kind is PickupKind.Heart or PickupKind.Pizza && health.Hp >= health.MaxHp) return; // vida llena
            Apply(health.gameObject);
        }

        /// Aplica el efecto y desaparece (o deja el arma vieja).
        void Apply(GameObject p)
        {
            var health = p.GetComponent<PlayerHealth>();
            var stats = p.GetComponent<PlayerStats>();
            switch (kind)
            {
                case PickupKind.Coin:
                    GameManager.Instance.Run.AddCoins(1);
                    HUD.FeedbackCoins(1);
                    break;
                case PickupKind.Heart: health.Heal(2); break;
                case PickupKind.Pizza: health.Heal(4); break;
                case PickupKind.Drink:
                    health.Heal(1);
                    stats.Boost(6f);
                    HUD.Feedback("+velocidad", HUD.Good);
                    break;
                case PickupKind.Item:
                    if (item) stats.Collect(item);
                    break;
                case PickupKind.Weapon:
                    var dropped = p.GetComponent<PlayerWeapons>().Take(weapon);
                    HUD.Feedback("Arma: " + weapon.displayName, HUD.Info);
                    if (dropped != null)
                    {
                        // la tuya queda en el piso, gratis
                        Sfx.Play(takeClip, 0.7f);
                        weapon = dropped;
                        price = 0;
                        RefreshVisual();
                        return;
                    }
                    break;
            }
            used = true;
            Sfx.Play(takeClip, 0.7f);
            Destroy(gameObject);
        }

        // ---------- con E ----------
        string Name => kind switch
        {
            PickupKind.Item when item => item.displayName,
            PickupKind.Weapon when weapon => weapon.displayName,
            _ => displayName,
        };

        public string Prompt => price > 0 ? $"Comprar {Name} (${price})" : (kind == PickupKind.Weapon ? "Tomar " : "Recoger ") + Name;
        public string InfoTitle => Name;
        public string InfoBody
        {
            get
            {
                string desc = kind switch
                {
                    PickupKind.Item when item => item.description,
                    PickupKind.Weapon when weapon => weapon.description,
                    _ => description,
                };
                string stats = kind == PickupKind.Weapon && weapon
                    ? InfoText.Section("Datos",
                        $"{InfoText.Stat("Rareza", RarityName(weapon.rarity))}\n" +
                        $"{InfoText.Stat("Daño", weapon.damage.ToString("0.#") + (weapon.count > 1 ? $" x{weapon.count}" : ""))}  ·  " +
                        InfoText.Stat("Cadencia", $"{1f / weapon.fireDelay:0.#} tiros/s"))
                    : null;
                string cost = price > 0 ? InfoText.Section("Precio", InfoText.Price(price)) : null;
                return InfoText.Join(desc, stats, cost);
            }
        }
        public bool CanInteract => !used && NeedsE && Name != null;
        public Vector3 Position => transform.position;

        public void Interact(GameObject p)
        {
            var health = p.GetComponent<PlayerHealth>();
            if (kind is PickupKind.Heart or PickupKind.Pizza && health.Hp >= health.MaxHp)
            {
                Deny("Ya tienes la vida llena");
                return;
            }
            if (price > 0 && !GameManager.Instance.Run.TrySpend(price))
            {
                Deny("No te alcanza");
                return;
            }
            if (price > 0) HUD.Feedback($"-${price}", HUD.Bad);
            Apply(p);
        }

        void Deny(string why)
        {
            Sfx.Play(deniedClip);
            HUD.Toast(why);
        }

        static string RarityName(Rarity r) => r switch
        {
            Rarity.Comun => "Común",
            Rarity.PocoComun => "Poco común",
            Rarity.Rara => "Rara",
            Rarity.Epica => "Épica",
            _ => "Legendaria",
        };
    }
}
