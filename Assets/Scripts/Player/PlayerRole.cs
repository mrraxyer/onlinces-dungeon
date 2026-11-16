using System;
using UnityEngine;

namespace OnLinces
{
    /// El puesto que tiene el jugador (Miembro si no ha tomado ninguno en el Lobby).
    public sealed class PlayerRole : MonoBehaviour
    {
        [SerializeField] RoleData defaultRole;

        public RoleData Current
        {
            get
            {
                var r = GameManager.Instance.Run.Role;
                return r != null ? r : defaultRole;
            }
        }

        /// Se dispara al tomar un puesto nuevo (para cambiar vestimenta, vida, habilidad...).
        public event Action Changed;

        public void Set(RoleData role)
        {
            GameManager.Instance.Run.Role = role;
            Changed?.Invoke();
        }
    }
}
