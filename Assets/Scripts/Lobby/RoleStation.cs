using UnityEngine;

namespace OnLinces
{
    /// Toma el puesto de un integrante de la mesa.
    public sealed class RoleStation : MonoBehaviour, IInteractable
    {
        [SerializeField] RoleData role;
        [SerializeField] AudioClip takeClip;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        bool IsMine => GameManager.Instance.Run.Role == role;

        public string Prompt => IsMine ? $"Ya tienes el puesto de {role.displayName}" : $"Tomar el puesto: {role.displayName}";
        public string InfoTitle => role.displayName;
        public string InfoBody => InfoText.Join(
            role.description,
            InfoText.Section("Habilidad",
                $"{InfoText.Strong(role.skillName)}  ·  {InfoText.Stat("recarga", $"{role.cooldown:0.#} s")}\n{role.skillDescription}"),
            InfoText.Section("Datos",
                $"{InfoText.Stat("Vida", Hearts(role.maxHp))}  ·  {InfoText.Stat("Velocidad", role.speedPx.ToString("0"))}"));

        /// 7 → "7 corazones"
        static string Hearts(int hp) => $"{hp} corazones";
        public bool CanInteract => true;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            if (IsMine) return;
            player.GetComponent<PlayerRole>().Set(role);
            Sfx.Play(takeClip);
            HUD.Feedback("Puesto: " + role.displayName, HUD.Info);
        }
    }
}
