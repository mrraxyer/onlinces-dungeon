using UnityEngine;

namespace OnLinces
{
    /// Cartel bajo la estatua de un jefe: qué hace en cada fase (texto en el prefab del jefe).
    public sealed class BossPhasesSign : MonoBehaviour, IInteractable
    {
        [SerializeField] Boss boss;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        public string Prompt => null;
        public string InfoTitle => "Fases: " + boss.displayName;
        public string InfoBody
        {
            get
            {
                var parts = new string[boss.phaseNotes.Length];
                for (int i = 0; i < parts.Length; i++)
                    if (!string.IsNullOrEmpty(boss.phaseNotes[i])) parts[i] = InfoText.Section($"Fase {i + 1}", boss.phaseNotes[i]);
                return InfoText.Join(parts);
            }
        }
        public bool CanInteract => true;
        public Vector3 Position => transform.position;
        public void Interact(GameObject player) { }
    }
}
