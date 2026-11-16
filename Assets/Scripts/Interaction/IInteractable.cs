using System.Collections.Generic;
using UnityEngine;

namespace OnLinces
{
    /// Algo con lo que interactúas con E (cofres, cafetera, armas e ítems, artículos de tienda...).
    public interface IInteractable
    {
        /// Texto corto del aviso: "Comprar Unit tests ($12)"
        string Prompt { get; }
        /// Título del cuadro de descripción (null = no se muestra cuadro)
        string InfoTitle { get; }
        /// Qué hace (se muestra en el cuadro de descripción)
        string InfoBody { get; }
        bool CanInteract { get; }
        Vector3 Position { get; }
        void Interact(GameObject player);
    }

    /// Lista de todo lo interactuable que hay en la escena (cada uno se registra en OnEnable).
    public static class Interactables
    {
        public static readonly List<IInteractable> All = new();
    }
}
