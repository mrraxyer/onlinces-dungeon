using System.Collections.Generic;
using OnLinces;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Acomoda la UI de las escenas (una sola para PC y teléfono). Menú OnLinces.
public static class UiLayout
{
    const float ReferenceHeight = 360f; // más alto = UI más chica
    const string SafeName = "SafeArea";
    const string TouchPrefab = "Assets/Resources/TouchControls.prefab";
    const float InfoWidth = 250f;

    // desde arriba a la derecha, a la izquierda del botón de pausa
    static readonly Vector2 MinimapCorner = new(-66f, -8f);
    static readonly Vector2 FeedbackCorner = new(-66f, -8f);
    static readonly Vector2 FeedbackCornerWithMinimap = new(-104f, -8f);
    static readonly Vector2 ToastBottom = new(-55f, 12f); // hueco entre palancas
    static readonly Vector2 WeaponTopLeft = new(8f, -116f); // bajo la habilidad
    static readonly Color TapColor = new(1f, 0.86f, 0.5f);

    [MenuItem("OnLinces/Acomodar UI de todas las escenas")]
    static void AllScenes()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Debug.Log(ApplyAll());
    }

    public static string ApplyAll()
    {
        var log = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = Apply(scene);
            EditorSceneManager.SaveScene(scene);
            log.Add($"{scene.name}: {n}");
        }
        FixTouchControls();
        return "UI acomodada · " + string.Join(", ", log);
    }

    [MenuItem("OnLinces/Acomodar UI de esta escena")]
    static void ThisScene()
    {
        var scene = SceneManager.GetActiveScene();
        Apply(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    /// Devuelve cuántos Canvas acomodó.
    public static int Apply(Scene scene)
    {
        int touchOrder = TouchOrder();
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
        {
            var canvas = scaler.GetComponent<Canvas>();
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (canvas.transform.Find(SafeName) != null) continue; // ya hecho
            count++;

            Undo.RecordObject(scaler, "UI");
            Proportional(scaler, ReferenceHeight);
            var safe = WrapInSafeArea(canvas.transform);

            var minimap = canvas.GetComponentInChildren<Minimap>(true);
            if (minimap != null) ToCorner((RectTransform)minimap.transform, Vector2.one, MinimapCorner);
            if (safe.Find("Feedback") is RectTransform feedback)
                ToCorner(feedback, Vector2.one, minimap != null ? FeedbackCornerWithMinimap : FeedbackCorner);
            if (safe.Find("WeaponHUD") is RectTransform weapon)
                ToCorner(weapon, new Vector2(0f, 1f), WeaponTopLeft);

            if (safe.Find("ToastLabelPanel") is RectTransform toast)
            {
                toast.anchorMin = toast.anchorMax = toast.pivot = new Vector2(0.5f, 0f);
                toast.anchoredPosition = ToastBottom;
            }

            var prompt = safe.Find("PromptLabelPanel") as RectTransform;
            var info = safe.Find("InfoPanel") as RectTransform;
            if (prompt != null) Dialog(canvas, safe, prompt, info, touchOrder);
        }
        return count;
    }

    /// Escala proporcional; siempre caben 2h x h.
    static void Proportional(CanvasScaler scaler, float h)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referenceResolution = new Vector2(h * 2f, h);
    }

    /// El diálogo sigue al interactuable; su aviso es un botón. El resto de la UI deja pasar toques a las palancas.
    static void Dialog(Canvas canvas, RectTransform safe, RectTransform prompt, RectTransform info, int touchOrder)
    {
        canvas.sortingOrder = touchOrder + 1;
        foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        foreach (var rt in new[] { prompt, info })
        {
            if (rt == null) continue;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
        }
        if (info != null) info.sizeDelta = new Vector2(InfoWidth, info.sizeDelta.y);

        var dialog = safe.gameObject.AddComponent<InteractionDialog>();
        var so = new SerializedObject(dialog);
        so.FindProperty("prompt").objectReferenceValue = prompt;
        so.FindProperty("info").objectReferenceValue = info;
        so.ApplyModifiedPropertiesWithoutUndo();

        var img = prompt.GetComponent<Image>();
        img.raycastTarget = true;
        img.color = TapColor;
        var button = prompt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.pressedColor = new Color(0.75f, 0.6f, 0.3f);
        button.colors = colors;
        UnityEventTools.AddPersistentListener(button.onClick, dialog.Use);

        // más grande para el dedo
        if (prompt.GetComponent<HorizontalLayoutGroup>() is { } layout)
            layout.padding = new RectOffset(layout.padding.left + 6, layout.padding.right + 6, layout.padding.top + 4, layout.padding.bottom + 4);
    }

    /// Mete los hijos del Canvas en un área segura.
    static RectTransform WrapInSafeArea(Transform canvas)
    {
        var children = new List<Transform>();
        foreach (Transform c in canvas) children.Add(c);

        var go = new GameObject(SafeName, typeof(RectTransform), typeof(OnLinces.SafeArea)) { layer = canvas.gameObject.layer };
        var safe = (RectTransform)go.transform;
        safe.SetParent(canvas, false);
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;

        foreach (var c in children) c.SetParent(safe, false);
        return safe;
    }

    static void ToCorner(RectTransform rt, Vector2 corner, Vector2 position)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = corner;
        rt.anchoredPosition = position;
    }

    static int TouchOrder()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TouchPrefab);
        return prefab && prefab.TryGetComponent<Canvas>(out var c) ? c.sortingOrder : 100;
    }

    /// Los controles táctiles: proporcionales, 2h x h con h = 320.
    static void FixTouchControls()
    {
        var root = PrefabUtility.LoadPrefabContents(TouchPrefab);
        if (root.TryGetComponent<CanvasScaler>(out var scaler)) Proportional(scaler, 320f);
        PrefabUtility.SaveAsPrefabAsset(root, TouchPrefab);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
