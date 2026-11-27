using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Hasta 2 armas: apuntar y disparar.
    public sealed class PlayerWeapons : MonoBehaviour
    {
        [SerializeField] PlayerController controller;

        [Header("Iniciales (máx. 2)")]
        [SerializeField] List<WeaponData> slots = new();

        [Header("Mano y arma")]
        [Tooltip("Gira hacia donde apuntas")]
        [SerializeField] Transform hand;
        [SerializeField] SpriteRenderer weaponRenderer;
        [Tooltip("px")]
        [SerializeField] float muzzleDistancePx = 16f;

        [Header("Fogonazo")]
        [SerializeField] SpriteRenderer muzzle;
        [SerializeField] Sprite[] muzzleFrames;
        [SerializeField] UnityEngine.Rendering.Universal.Light2D muzzleLight;
        [SerializeField] float muzzleLightIntensity = 1.6f;

        [Header("Disparo")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip shootOn;
        [SerializeField] AudioClip shootOff;

        [Header("Energía (se llena en cada piso)")]
        [SerializeField] int maxEnergy = 140;

        [Header("Golpe (Cuadrado / X, clic derecho o F)")]
        [Tooltip("Hoja de 3 cuadros en horizontal")]
        [SerializeField] Sprite meleeSheet;
        [SerializeField] AudioClip meleeClip;
        [SerializeField] float meleeDamage = 4f;
        [SerializeField] float meleeRadiusPx = 24f;
        [SerializeField] float meleeDelay = 0.35f;
        [Tooltip("px/s del impulso hacia donde apuntas")]
        [SerializeField] float meleeLungePx = 260f;

        const float ShootAnimTime = 0.14f;
        const float DoubleAngle = 0.12f; // radianes

        int current;
        const float EmptyRegenSeconds = 3f;
        // armas rápidas: cobran energía como máximo una vez por ventana
        const float EnergyWindow = 0.5f;

        float cooldown, cooldown2, shootAnim, noEnergyWarn, regenTimer, energyWindow, meleeCooldown;
        Sprite[] meleeFrames;
        PlayerStats stats;
        PlayerSkill skill;

        public WeaponData Current => slots.Count > 0 ? slots[current] : null;
        /// La que no tienes en la mano.
        public WeaponData Other => slots.Count > 1 ? slots[1 - current] : null;
        public bool JustShot => shootAnim > 0f;
        public int Energy { get; private set; }
        public int MaxEnergy => maxEnergy;
        /// s que faltan para el siguiente disparo del arma en la mano.
        public float Cooldown => cooldown;
        /// Time.time del último disparo.
        public float LastShotTime { get; private set; } = -99f;
        /// Como lo revisa el disparo: sin esto no dispara.
        public bool HasEnergyFor(WeaponData w) => w != null && Energy >= w.energyCost;

        public void AddEnergy(int amount) => Energy = Mathf.Min(maxEnergy, Energy + amount);

        void Start()
        {
            Energy = maxEnergy;
            stats = GetComponent<PlayerStats>();
            skill = GetComponent<PlayerSkill>();
            // se conservan entre pisos, cada jugador las suyas
            var saved = Saved();
            if (saved.Count > 0) { slots.Clear(); slots.AddRange(saved); }
            else saved.AddRange(slots);
            RefreshWeaponSprite();
        }

        /// Devuelve el arma que sueltas, si llevabas 2.
        public WeaponData Take(WeaponData weapon)
        {
            WeaponData dropped = null;
            if (slots.Count < 2) { slots.Add(weapon); current = slots.Count - 1; }
            else { dropped = slots[current]; slots[current] = weapon; }
            var saved = Saved();
            saved.Clear();
            saved.AddRange(slots);
            RefreshWeaponSprite();
            return dropped;
        }

        List<WeaponData> Saved()
        {
            var run = GameManager.Instance.Run;
            return GetComponent<PlayerController>().IsGuest ? run.GuestWeapons : run.Weapons;
        }

        void Update()
        {
            bool junta = skill && skill.IsActive(SkillKind.JuntaGeneral);
            cooldown -= Time.deltaTime * (junta ? 2f : 1f);
            cooldown2 -= Time.deltaTime;
            shootAnim -= Time.deltaTime;
            noEnergyWarn -= Time.deltaTime;
            energyWindow -= Time.deltaTime;
            meleeCooldown -= Time.deltaTime;

            // recarga lenta mientras no esté al máximo
            if (Energy < maxEnergy)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= EmptyRegenSeconds) { regenTimer -= EmptyRegenSeconds; AddEnergy(1); }
            }
            else regenTimer = 0f;

            if (controller.SwitchWeaponPressed && slots.Count > 1)
            {
                current = (current + 1) % slots.Count;
                RefreshWeaponSprite();
            }

            var aim = controller.Aim;
            hand.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            weaponRenderer.flipY = aim.x < 0;
            weaponRenderer.sortingOrder = aim.y > 0.3f ? -1 : 3; // detrás del cuerpo al apuntar arriba

            if (muzzleLight)
            {
                // destello que se apaga junto con el disparo
                muzzleLight.enabled = shootAnim > 0f;
                muzzleLight.intensity = Mathf.Clamp01(shootAnim / ShootAnimTime) * muzzleLightIntensity;
            }

            if (muzzle != null)
            {
                muzzle.enabled = shootAnim > 0.07f;
                if (muzzle.enabled && muzzleFrames.Length > 0)
                    muzzle.sprite = muzzleFrames[Mathf.Clamp((int)((ShootAnimTime - shootAnim) / 0.025f), 0, muzzleFrames.Length - 1)];
            }

            if (controller.MeleePressed && meleeCooldown <= 0f && controller.enabled && Time.timeScale > 0f)
            {
                meleeCooldown = meleeDelay;
                Melee(aim);
            }

            if (!controller.IsShooting) return;
            float delayMult = stats ? stats.FireDelayMult : 1f;

            if (cooldown <= 0f && Current != null)
            {
                cooldown = Current.fireDelay * delayMult;
                if (energyWindow > 0f) Fire(Current, aim); // ya pagado en esta ventana
                else if (Energy >= Current.energyCost)
                {
                    Energy -= Current.energyCost;
                    if (Current.energyCost > 0) energyWindow = EnergyWindow;
                    Fire(Current, aim);
                }
                else if (noEnergyWarn <= 0f)
                {
                    noEnergyWarn = 1.5f;
                    HUD.Feedback("Sin energía", HUD.Bad);
                }
            }
            // Deploy doble: también la otra arma
            if (skill && skill.IsActive(SkillKind.DeployDoble) && slots.Count > 1 && cooldown2 <= 0f)
            {
                var other = slots[1 - current];
                cooldown2 = other.fireDelay * delayMult;
                Fire(other, Rotate(aim, DoubleAngle));
            }
        }

        /// Golpe hacia donde apuntas: pega a todo lo que quede en el arco, sin buscar enemigos.
        void Melee(Vector2 aim)
        {
            var dir = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            controller.Knock(dir * meleeLungePx);
            shootAnim = ShootAnimTime;
            Sfx.Play(meleeClip, 0.7f);

            float damage = meleeDamage * (stats ? stats.DamageMult : 1f);
            var center = (Vector2)transform.position + dir * Units.Px(14);
            var hit = new HashSet<IHittable>();
            foreach (var col in Physics2D.OverlapCircleAll(center, Units.Px(meleeRadiusPx)))
            {
                var h = col.GetComponentInParent<IHittable>();
                if (h != null && h.CanBeHit && hit.Add(h)) h.Hit(damage, dir);
            }
            if (meleeSheet) StartCoroutine(SlashFx(center, dir));
            // al aire: un toque; si conecta, golpe seco y destello blanco en el DualSense
            if (hit.Count == 0) Rumble.Play(controller, 0.05f, 0.2f, 0.06f);
            else
            {
                Rumble.Play(controller, 0.6f, 0.3f, 0.1f);
                PadFeedback.Flash(controller, Color.white, 0.12f, PadFeedback.Priority.Event);
            }
        }

        System.Collections.IEnumerator SlashFx(Vector2 at, Vector2 dir)
        {
            if (meleeFrames == null)
            {
                // la hoja viene sin cortar: 3 cuadros cuadrados
                var r = meleeSheet.rect;
                float w = r.width / 3f;
                meleeFrames = new Sprite[3];
                for (int i = 0; i < 3; i++)
                    meleeFrames[i] = Sprite.Create(meleeSheet.texture, new Rect(r.x + w * i, r.y, w, r.height), new Vector2(0.5f, 0.5f), meleeSheet.pixelsPerUnit);
            }
            var sr = new GameObject("Slash").AddComponent<SpriteRenderer>();
            sr.transform.SetPositionAndRotation(at + Vector2.up * Units.Px(8), Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            if (weaponRenderer) sr.sharedMaterial = weaponRenderer.sharedMaterial;
            sr.sortingOrder = 10;
            foreach (var f in meleeFrames)
            {
                sr.sprite = f;
                yield return new WaitForSeconds(0.05f);
            }
            Destroy(sr.gameObject);
        }

        static Vector2 Rotate(Vector2 v, float a) =>
            new(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));

        void Fire(WeaponData w, Vector2 aim)
        {
            float damage = w.damage * (stats ? stats.DamageMult : 1f);
            if (skill && skill.IsActive(SkillKind.JuntaGeneral)) damage *= 2f;
            shootAnim = ShootAnimTime;
            LastShotTime = Time.time;
            // retroceso según el peso del tiro (daño x balas): while(true) apenas zumba, la escopeta y el rifle sacuden
            float kick = Mathf.Clamp01(w.damage * w.count / 8f);
            Rumble.Play(controller, kick * 0.6f, 0.15f + kick * 0.3f, 0.04f + kick * 0.08f);
            PadFeedback.Flash(controller, WeaponData.RarityColor(w.rarity), 0.06f, PadFeedback.Priority.Shot); // chispazo

            Vector2 origin = (Vector2)hand.position + aim * Units.Px(muzzleDistancePx);
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < w.count; i++)
            {
                // varias: en abanico; una: desvío al azar
                float offset = w.count > 1
                    ? Mathf.Lerp(-w.spreadDegrees / 2f, w.spreadDegrees / 2f, i / (float)(w.count - 1))
                    : Random.Range(-w.spreadDegrees / 2f, w.spreadDegrees / 2f);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var sprite = w.projectileSprites.Length > 0 ? w.projectileSprites[Random.Range(0, w.projectileSprites.Length)] : null;

                var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
                p.Launch(sprite, dir * Units.Px(w.projectileSpeedPx), damage, w.range, fromPlayer: true);
                if (w.pierce > 0) p.SetPierce(w.pierce, 1f);
                p.SetSpecial(w, w.chain);
            }

            if (audioSource != null)
            {
                var clip = controller.Mascot == Mascot.Off ? shootOff : shootOn;
                if (clip != null) audioSource.PlayOneShot(clip, 0.6f);
            }
        }

        void RefreshWeaponSprite()
        {
            if (weaponRenderer != null) weaponRenderer.sprite = Current != null ? Current.icon : null;
        }
    }
}
