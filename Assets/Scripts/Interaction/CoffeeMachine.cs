using UnityEngine;

namespace OnLinces
{
    /// Cafetera: velocidad temporal, ½ vida y energía.
    public sealed class CoffeeMachine : MonoBehaviour, IInteractable
    {
        [SerializeField] int price = 2;
        [SerializeField] float boostSeconds = 6f;
        [SerializeField] int energy = 25;
        [SerializeField] AudioClip buyClip;
        [SerializeField] AudioClip deniedClip;

        void OnEnable() => Interactables.All.Add(this);
        void OnDisable() => Interactables.All.Remove(this);

        public string Prompt => $"Café (${price})";
        public string InfoTitle => "Cafetera";
        public string InfoBody => InfoText.Join(
            "Un café recién hecho.",
            InfoText.Section("Efecto", $"+velocidad por {boostSeconds:0} s\n+½ vida\n+{energy} energía"));
        public bool CanInteract => true;
        public Vector3 Position => transform.position;

        public void Interact(GameObject player)
        {
            if (!GameManager.Instance.Run.TrySpend(price))
            {
                Sfx.Play(deniedClip);
                HUD.Toast("No te alcanza");
                return;
            }
            HUD.Feedback($"-${price}", HUD.Bad);
            player.GetComponent<PlayerStats>().Boost(boostSeconds);
            player.GetComponent<PlayerHealth>().Heal(1);
            player.GetComponent<PlayerWeapons>().AddEnergy(energy);
            HUD.Feedback($"+velocidad +{energy} energía", HUD.Good);
            Sfx.Play(buyClip);
        }
    }
}
