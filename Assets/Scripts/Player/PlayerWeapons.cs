using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Lleva hasta 2 armas, las apunta y dispara.
    public sealed class PlayerWeapons : MonoBehaviour
    {
        [SerializeField] PlayerController controller;

        [Header("Armas con las que empiezas (máx. 2)")]
        [SerializeField] List<WeaponData> slots = new();

        [Header("Mano y arma")]
        [Tooltip("Objeto que gira hacia donde apuntas")]
        [SerializeField] Transform hand;
        [SerializeField] SpriteRenderer weaponRenderer;
        [Tooltip("Distancia de la mano a la punta del arma, en píxeles")]
        [SerializeField] float muzzleDistancePx = 16f;

        [Header("Fogonazo (muzzle_0..2)")]
        [SerializeField] SpriteRenderer muzzle;
        [SerializeField] Sprite[] muzzleFrames;

        [Header("Disparo")]
        [SerializeField] Projectile projectilePrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip shootOn;
        [SerializeField] AudioClip shootOff;

        const float ShootAnimTime = 0.14f;
        int current;
        float cooldown;
        float shootAnim;

        public WeaponData Current => slots.Count > 0 ? slots[current] : null;
        public bool JustShot => shootAnim > 0f;

        void Start() => RefreshWeaponSprite();

        void Update()
        {
            cooldown -= Time.deltaTime;
            shootAnim -= Time.deltaTime;

            if (controller.SwitchWeaponPressed && slots.Count > 1)
            {
                current = (current + 1) % slots.Count;
                RefreshWeaponSprite();
            }

            // Girar la mano hacia donde apuntas
            var aim = controller.Aim;
            hand.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            weaponRenderer.flipY = aim.x < 0;
            // Si apuntas hacia arriba el arma queda detrás del cuerpo
            weaponRenderer.sortingOrder = aim.y > 0.3f ? -1 : 3;

            if (muzzle != null)
            {
                muzzle.enabled = shootAnim > 0.07f;
                if (muzzle.enabled && muzzleFrames.Length > 0)
                    muzzle.sprite = muzzleFrames[Mathf.Clamp((int)((ShootAnimTime - shootAnim) / 0.025f), 0, muzzleFrames.Length - 1)];
            }

            if (controller.IsShooting && cooldown <= 0f && Current != null)
                Fire(Current, aim);
        }

        void Fire(WeaponData w, Vector2 aim)
        {
            cooldown = w.fireDelay;
            shootAnim = ShootAnimTime;

            Vector2 origin = (Vector2)hand.position + aim * Units.Px(muzzleDistancePx);
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < w.count; i++)
            {
                // Varias balas: repartidas en el abanico. Una sola: pequeña desviación al azar.
                float offset = w.count > 1
                    ? Mathf.Lerp(-w.spreadDegrees / 2f, w.spreadDegrees / 2f, i / (float)(w.count - 1))
                    : Random.Range(-w.spreadDegrees / 2f, w.spreadDegrees / 2f);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var sprite = w.projectileSprites.Length > 0 ? w.projectileSprites[Random.Range(0, w.projectileSprites.Length)] : null;

                var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
                p.Launch(sprite, dir * Units.Px(w.projectileSpeedPx), w.damage, w.range, fromPlayer: true);
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
