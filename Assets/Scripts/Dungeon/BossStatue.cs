using UnityEngine;

namespace OnLinces
{
    /// Campo de tiro: estatua de un jefe; con E empieza la pelea de práctica.
    public sealed class BossStatue : MonoBehaviour, IInteractable
    {
        [SerializeField] Boss boss;
        [SerializeField] BossRoom room;
        [Tooltip("Piso donde aparece")]
        [SerializeField] string floorName;

        SpriteRenderer[] renderers;

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>();
        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        // se esconde (con sombra) durante la pelea
        void Update()
        {
            foreach (var r in renderers) r.enabled = room.CanStartPractice;
        }

        public string Prompt => "Pelear (práctica)";
        public string InfoTitle => boss.displayName;
        public string InfoBody => InfoText.Join(boss.subtitle, InfoText.Section("Aparece en", floorName));
        public bool CanInteract => room.CanStartPractice;
        public Vector3 Position => transform.position;
        public void Interact(GameObject player) => room.StartFight(boss);
    }
}
