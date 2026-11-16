using System;
using UnityEngine;

namespace OnLinces
{
    /// Puesto actual (Miembro por defecto).
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

        public event Action Changed;

        public void Set(RoleData role)
        {
            GameManager.Instance.Run.Role = role;
            Changed?.Invoke();
        }
    }
}
