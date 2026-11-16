using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Algo que se usa con E.
    public interface IInteractable
    {
        string Prompt { get; }
        /// null = sin diálogo
        string InfoTitle { get; }
        string InfoBody { get; }
        bool CanInteract { get; }
        Vector3 Position { get; }
        void Interact(GameObject player);
    }

    /// Interactuables activos en la escena.
    public static class Interactables
    {
        public static readonly List<IInteractable> All = new();
    }
}
