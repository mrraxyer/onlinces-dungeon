using System;
using System.Collections;
using System.Linq;
using UnityEditor;

/// Quita las resoluciones Portrait del Game view y agrega 4 de prueba (horizontal).
// shortcut: usa reflexión sobre API interna de Unity; si cambia en otra versión, revisar nombres.
[InitializeOnLoad]
static class GameViewSizesSetup
{
    static readonly (int w, int h, string name)[] Sizes =
    {
        (2400, 1080, "Teléfono 20:9"),
        (2048, 1536, "Tableta 4:3"),
        (1366, 768, "Laptop"),
        (2560, 1080, "Ultrawide 21:9"),
    };

    static GameViewSizesSetup() => EditorApplication.delayCall += Apply;

    static void Apply()
    {
        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var sizeType = asm.GetType("UnityEditor.GameViewSize");
        var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var getGroup = sizesType.GetMethod("GetGroup");
        const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        foreach (GameViewSizeGroupType g in Enum.GetValues(typeof(GameViewSizeGroupType)))
        {
            var group = getGroup.Invoke(instance, new object[] { (int)g });
            if (group == null) continue;
            var t = group.GetType();
            foreach (var field in new[] { "m_Builtin", "m_Custom" })
            {
                var list = (IList)t.GetField(field, F).GetValue(group);
                for (int i = list.Count - 1; i >= 0; i--)
                    if (((string)sizeType.GetProperty("baseText").GetValue(list[i])).Contains("Portrait")) list.RemoveAt(i);
            }
            var custom = ((IList)t.GetField("m_Custom", F).GetValue(group)).Cast<object>()
                .Select(s => (string)sizeType.GetProperty("baseText").GetValue(s)).ToList();
            foreach (var (w, h, name) in Sizes)
                if (!custom.Contains(name))
                    t.GetMethod("AddCustomSize").Invoke(group, new[] { Activator.CreateInstance(sizeType, Enum.Parse(sizeKind, "FixedResolution"), w, h, name) });
        }
        sizesType.GetMethod("SaveToHDD").Invoke(instance, null);
    }
}
