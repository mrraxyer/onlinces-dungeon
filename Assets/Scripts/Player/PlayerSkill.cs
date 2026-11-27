using UnityEngine;
using UnityEngine.InputSystem;

namespace OnLinces
{
    /// Habilidad del puesto.
    ///  · Deploy doble: dispara las dos armas.
    ///  · Junta general: daño y cadencia x2 + torreta.
    ///  · Suplencia: escudo temporal e invulnerable.
    ///  · Firewall total: burbuja que borra balas.
    public sealed class PlayerSkill : MonoBehaviour
    {
        [SerializeField] InputActionReference skillAction;
        [SerializeField] PlayerRole role;

        [Header("Junta general")]
        [SerializeField] AllyTurret allyPrefab;

        [Header("Firewall total")]
        [SerializeField] GameObject firewallBubble;
        [SerializeField] float firewallRadiusPx = 42f;

        [SerializeField] AudioClip useClip;
        [SerializeField] AudioClip notReadyClip;

        const float SuplenciaShieldSeconds = 6f;
        float cooldownLeft, activeLeft;
        SkillKind activeKind;

        public RoleData Role => role ? role.Current : null;
        /// 0 lista, 1 recién usada
        public float CooldownFraction => Role && Role.cooldown > 0f ? Mathf.Clamp01(cooldownLeft / Role.cooldown) : 0f;
        public bool Ready => cooldownLeft <= 0f;
        public bool IsActive(SkillKind k) => activeLeft > 0f && activeKind == k;

        void OnEnable() { if (skillAction) skillAction.action.Enable(); }

        void Start()
        {
            if (firewallBubble) firewallBubble.SetActive(false);
            if (role) role.Changed += () => { cooldownLeft = 0f; activeLeft = 0f; };
        }

        void Update()
        {
            cooldownLeft -= Time.deltaTime;
            activeLeft -= Time.deltaTime;

            if (IsActive(SkillKind.FirewallTotal))
            {
                float r = Units.Px(firewallRadiusPx);
                foreach (var p in FindObjectsByType<Projectile>())
                    if (!p.FromPlayer && Vector2.Distance(p.transform.position, transform.position + Vector3.up * Units.Px(12)) < r)
                        Destroy(p.gameObject);
            }
            if (firewallBubble) firewallBubble.SetActive(IsActive(SkillKind.FirewallTotal));

            if (skillAction == null || !skillAction.action.WasPressedThisFrame()) return;
            var health = GetComponent<PlayerHealth>();
            if (health && health.IsDead) return;
            if (!Ready) { Sfx.Play(notReadyClip, 0.4f); return; }
            Use();
        }

        void Use()
        {
            var r = Role;
            if (r == null) return;
            cooldownLeft = r.cooldown;
            activeKind = r.skill;
            activeLeft = r.duration;
            Sfx.Play(useClip, 0.7f);
            HUD.Feedback(r.skillName, HUD.Info);
            var health = GetComponent<PlayerHealth>();

            switch (r.skill)
            {
                case SkillKind.JuntaGeneral:
                    if (allyPrefab)
                    {
                        var bot = Instantiate(allyPrefab, transform.position + Vector3.down * Units.Px(14), Quaternion.identity);
                        bot.Init(r.duration);
                    }
                    break;
                case SkillKind.Suplencia:
                    health?.GiveShield(SuplenciaShieldSeconds);
                    health?.GiveIframes(2.5f);
                    break;
                case SkillKind.FirewallTotal:
                    health?.GiveIframes(0.5f);
                    break;
            }
        }
    }
}
