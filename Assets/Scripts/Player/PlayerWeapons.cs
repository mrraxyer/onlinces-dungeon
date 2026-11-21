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

        [Header("Disparo")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip shootOn;
        [SerializeField] AudioClip shootOff;

        [Header("Energía (se llena en cada piso)")]
        [SerializeField] int maxEnergy = 140;

        const float ShootAnimTime = 0.14f;
        const float DoubleAngle = 0.12f; // radianes

        int current;
        float cooldown, cooldown2, shootAnim, noEnergyWarn;
        PlayerStats stats;
        PlayerSkill skill;

        public WeaponData Current => slots.Count > 0 ? slots[current] : null;
        /// La que no tienes en la mano.
        public WeaponData Other => slots.Count > 1 ? slots[1 - current] : null;
        public bool JustShot => shootAnim > 0f;
        public int Energy { get; private set; }
        public int MaxEnergy => maxEnergy;

        public void AddEnergy(int amount) => Energy = Mathf.Min(maxEnergy, Energy + amount);

        void Start()
        {
            Energy = maxEnergy;
            stats = GetComponent<PlayerStats>();
            skill = GetComponent<PlayerSkill>();
            // se conservan entre pisos
            var run = GameManager.Instance.Run;
            if (run.Weapons.Count > 0) { slots.Clear(); slots.AddRange(run.Weapons); }
            else run.Weapons.AddRange(slots);
            RefreshWeaponSprite();
        }

        /// Devuelve el arma que sueltas, si llevabas 2.
        public WeaponData Take(WeaponData weapon)
        {
            WeaponData dropped = null;
            if (slots.Count < 2) { slots.Add(weapon); current = slots.Count - 1; }
            else { dropped = slots[current]; slots[current] = weapon; }
            var run = GameManager.Instance.Run;
            run.Weapons.Clear();
            run.Weapons.AddRange(slots);
            RefreshWeaponSprite();
            return dropped;
        }

        void Update()
        {
            bool junta = skill && skill.IsActive(SkillKind.JuntaGeneral);
            cooldown -= Time.deltaTime * (junta ? 2f : 1f);
            cooldown2 -= Time.deltaTime;
            shootAnim -= Time.deltaTime;
            noEnergyWarn -= Time.deltaTime;

            if (controller.SwitchWeaponPressed && slots.Count > 1)
            {
                current = (current + 1) % slots.Count;
                RefreshWeaponSprite();
            }

            var aim = controller.Aim;
            hand.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            weaponRenderer.flipY = aim.x < 0;
            weaponRenderer.sortingOrder = aim.y > 0.3f ? -1 : 3; // detrás del cuerpo al apuntar arriba

            if (muzzle != null)
            {
                muzzle.enabled = shootAnim > 0.07f;
                if (muzzle.enabled && muzzleFrames.Length > 0)
                    muzzle.sprite = muzzleFrames[Mathf.Clamp((int)((ShootAnimTime - shootAnim) / 0.025f), 0, muzzleFrames.Length - 1)];
            }

            if (!controller.IsShooting) return;
            float delayMult = stats ? stats.FireDelayMult : 1f;

            if (cooldown <= 0f && Current != null)
            {
                cooldown = Current.fireDelay * delayMult;
                if (Energy >= Current.energyCost)
                {
                    Energy -= Current.energyCost;
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

        static Vector2 Rotate(Vector2 v, float a) =>
            new(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));

        void Fire(WeaponData w, Vector2 aim)
        {
            float damage = w.damage * (stats ? stats.DamageMult : 1f);
            if (skill && skill.IsActive(SkillKind.JuntaGeneral)) damage *= 2f;
            shootAnim = ShootAnimTime;

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
            }

            if (audioSource != null)
            {
                var clip = GameManager.Instance.Run.Mascot == Mascot.Off ? shootOff : shootOn;
                if (clip != null) audioSource.PlayOneShot(clip, 0.6f);
            }
        }

        void RefreshWeaponSprite()
        {
            if (weaponRenderer != null) weaponRenderer.sprite = Current != null ? Current.icon : null;
        }
    }
}
