using UnityEngine;

namespace OnLinces
{
    public enum ItemEffect { Cafe, Teclado, GitPush, UnitTests, Debugger, Firewall }

    /// Ítem pasivo.
    [CreateAssetMenu(menuName = "OnLinces/Item", fileName = "Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string displayName = "Café de las 3 a.m.";
        [TextArea] public string description = "+velocidad  +cadencia";
        public Sprite icon;
        public ItemEffect effect;
    }
}
