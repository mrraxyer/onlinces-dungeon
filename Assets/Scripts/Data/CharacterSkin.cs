using System;
using System.Linq;
using UnityEngine;

namespace OnLinces
{
    /// Hojas de sprites de On/Off con un puesto. Los cuadros se llenan solos.
    [CreateAssetMenu(menuName = "OnLinces/Character Skin", fileName = "Skin")]
    public sealed class CharacterSkin : ScriptableObject
    {
        [Tooltip("5x4: abajo, arriba, der, izq · quieto + 4 pasos")]
        [SerializeField] Texture2D bodySheet;
        [Tooltip("3x4: normal, disparo, parpadeo")]
        [SerializeField] Texture2D headSheet;

        [Header("Se llenan solos")]
        [SerializeField] Sprite[] body = Array.Empty<Sprite>();
        [SerializeField] Sprite[] head = Array.Empty<Sprite>();

        public Sprite[] Body => body;
        public Sprite[] Head => head;

#if UNITY_EDITOR
        void OnValidate()
        {
            body = Load(bodySheet);
            head = Load(headSheet);
        }

        // orden numérico: _2 antes que _10
        static Sprite[] Load(Texture2D sheet)
        {
            if (sheet == null) return Array.Empty<Sprite>();
            var path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
            return UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => Index(s.name))
                .ToArray();
        }

        static int Index(string name)
        {
            int i = name.LastIndexOf('_');
            return i >= 0 && int.TryParse(name.Substring(i + 1), out var n) ? n : 0;
        }
#endif
    }
}
