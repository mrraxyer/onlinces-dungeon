using System;
using System.Linq;
using UnityEngine;

namespace OnLinces
{
    /// La apariencia de On u Off con un puesto: las 3 hojas de sprites ya cortadas.
    /// Se crea en Project: clic derecho > Create > OnLinces > Character Skin.
    /// Solo arrastra las 3 imágenes (body, head, extra); los cuadros se llenan solos y en orden.
    [CreateAssetMenu(menuName = "OnLinces/Character Skin", fileName = "Skin")]
    public sealed class CharacterSkin : ScriptableObject
    {
        [Tooltip("5 x 4: filas abajo, arriba, derecha, izquierda; columnas quieto + 4 de caminar")]
        [SerializeField] Texture2D bodySheet;
        [Tooltip("3 x 4: mismas filas; columnas normal, disparando, parpadeo")]
        [SerializeField] Texture2D headSheet;
        [Tooltip("4 x 1: poses especiales")]
        [SerializeField] Texture2D extraSheet;

        [Header("Se llenan solos")]
        [SerializeField] Sprite[] body = Array.Empty<Sprite>();
        [SerializeField] Sprite[] head = Array.Empty<Sprite>();
        [SerializeField] Sprite[] extra = Array.Empty<Sprite>();

        public Sprite[] Body => body;
        public Sprite[] Head => head;
        public Sprite[] Extra => extra;

#if UNITY_EDITOR
        void OnValidate()
        {
            body = Load(bodySheet);
            head = Load(headSheet);
            extra = Load(extraSheet);
        }

        // Ordena por el número final del nombre (on_body_2 antes que on_body_10).
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
