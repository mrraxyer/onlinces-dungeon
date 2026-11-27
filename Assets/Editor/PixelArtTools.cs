using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace OnLinces.EditorTools
{
    /// Herramientas de una sola vez para dejar todo en pixel art:
    ///  · Fuente pixel OnLincesPixel (la de la versión original) en todos los textos.
    ///  · Paneles oscuros en 9-slice (antes estaban en mosaico).
    ///  · Pasillos y puertas de 3 tiles de ancho.
    /// Uso en terminal: Unity -batchmode -quit -projectPath . -executeMethod OnLinces.EditorTools.PixelArtTools.RunAll
    public static class PixelArtTools
    {
        const string FontTtf = "Assets/Fonts/OnLincesPixel.ttf";
        const string FontAssetPath = "Assets/Fonts/OnLincesPixel.asset";
        const string OldFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        static readonly string[] PanelSprites = { "Assets/UI/Sprites/panel.png", "Assets/UI/Sprites/weapon_panel.png" };
        static readonly string[] CorridorScenes = { "Assets/Scenes/Floor1.unity", "Assets/Scenes/Floor2.unity", "Assets/Scenes/Floor3.unity", "Assets/Scenes/Range.unity" };
        const string Chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                             "¡·¿ÁÉÍÑÓÚáéíñóúü→←×•…—–‘’“”½²º";
        static readonly Color LightText = new(0.93f, 0.92f, 0.96f);
        /// Texto en el mundo: 1 px de fuente = 1 px del juego (32 px por unidad, em de 11 px).
        const float WorldBase = 11f / 32f * 10f;
        const int CorridorWidth = 3;

        [MenuItem("OnLinces/Pixel art/Aplicar todo")]
        public static void RunAll()
        {
            var font = CreatePixelFont();
            RestyleProject(font);
            WidenCorridors();
            AssetDatabase.SaveAssets();
            Debug.Log("PixelArtTools: listo");
        }

        // ---------- fuente ----------

        public static TMP_FontAsset CreatePixelFont()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fa == null)
            {
                var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtf);
                fa = TMP_FontAsset.CreateFontAsset(ttf, 11, 1, GlyphRenderMode.RASTER, 256, 256, AtlasPopulationMode.Dynamic, false);
                fa.name = "OnLincesPixel";
                AssetDatabase.CreateAsset(fa, FontAssetPath);
                fa.atlasTexture.name = "OnLincesPixel Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
                fa.material.name = "OnLincesPixel Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            fa.TryAddCharacters(Chars, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("PixelArtTools: faltan " + missing);
            fa.atlasTexture.filterMode = FilterMode.Point;
            var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldFontPath);
            if (old && (fa.fallbackFontAssetTable == null || !fa.fallbackFontAssetTable.Contains(old)))
                fa.fallbackFontAssetTable = new List<TMP_FontAsset> { old };
            EditorUtility.SetDirty(fa);

            // fuente por defecto de TMP
            var settings = AssetDatabase.LoadAssetAtPath<Object>(TmpSettingsPath);
            if (settings)
            {
                var so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            return fa;
        }

        // ---------- textos y paneles ----------

        public static void RestyleProject(TMP_FontAsset fa)
        {
            var old = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldFontPath);
            var panels = new HashSet<Sprite>(PanelSprites.SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>()));

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("TextMesh Pro")) continue;
                using var scope = new PrefabUtility.EditPrefabContentsScope(path);
                Restyle(scope.prefabContentsRoot, fa, old, panels);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) Restyle(root, fa, old, panels);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static void Restyle(GameObject root, TMP_FontAsset fa, TMP_FontAsset old, HashSet<Sprite> panels)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = fa;
                t.fontSharedMaterial = fa.material;
                float size = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
                t.enableAutoSizing = false;
                if (t is TextMeshPro)
                    t.fontSize = WorldBase * Mathf.Max(1, Mathf.Round(size / WorldBase));
                else
                    t.fontSize = PixelText.Size(size);
                t.fontStyle &= ~(FontStyles.Bold | FontStyles.Italic);
                var c = t.color;
                if (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f < 0.45f) t.color = new Color(LightText.r, LightText.g, LightText.b, c.a);
                EditorUtility.SetDirty(t);
            }

            // referencias sueltas a la fuente vieja (BossBar, StatsHUD...)
            if (old)
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb is TMP_Text) continue;
                    var so = new SerializedObject(mb);
                    var it = so.GetIterator();
                    bool changed = false;
                    while (it.Next(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (it.objectReferenceValue == old) { it.objectReferenceValue = fa; changed = true; }
                        else if (it.objectReferenceValue == old.material) { it.objectReferenceValue = fa.material; changed = true; }
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || !panels.Contains(img.sprite)) continue;
                img.type = Image.Type.Sliced;
                img.fillCenter = true;
                img.pixelsPerUnitMultiplier = 1f;
                img.color = new Color(1f, 1f, 1f, img.color.a);
                EditorUtility.SetDirty(img);
            }
        }

        // ---------- pasillos ----------

        public static void WidenCorridors()
        {
            foreach (var path in CorridorScenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = WidenCorridorsInOpenScene();
                Debug.Log($"PixelArtTools: {path}: {n} pasillos ensanchados");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static Vector2Int Key(Vector3 world) => new(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));

        static Vector3 Center(Vector2Int k) => new(k.x + 0.5f, k.y + 0.5f, 0f);

        /// Ensancha cada pasillo de 1 tile a CorridorWidth (copiando columnas de tiles) y sus puertas.
        static int WidenCorridorsInOpenScene()
        {
            var maps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
            var walls = maps.Where(m => m.name.Contains("Wall")).ToArray();
            var floors = maps.Except(walls).ToArray();

            bool Has(Tilemap[] set, Vector2Int k) => set.Any(m => m.HasTile(m.WorldToCell(Center(k))));
            bool IsWall(Vector2Int k) => Has(walls, k);
            bool IsFloor(Vector2Int k) => !IsWall(k) && Has(floors, k);
            bool IsEmpty(Vector2Int k) => !Has(maps, k);

            // celdas de piso con pared a los dos lados = pasillo o puerta de 1 tile
            var gaps = new HashSet<Vector2Int>();
            foreach (var m in floors)
            {
                foreach (var cell in m.cellBounds.allPositionsWithin)
                {
                    if (!m.HasTile(cell)) continue;
                    var k = Key(m.GetCellCenterWorld(cell));
                    if (IsFloor(k) && IsWall(k + Vector2Int.left) && IsWall(k + Vector2Int.right)) gaps.Add(k);
                }
            }

            // agrupa en columnas verticales seguidas
            var runs = new List<(int x, int y0, int y1)>();
            foreach (var g in gaps.GroupBy(k => k.x))
            {
                var ys = g.Select(k => k.y).OrderBy(y => y).ToList();
                int start = ys[0], prev = ys[0];
                foreach (var y in ys.Skip(1))
                {
                    if (y != prev + 1) { runs.Add((g.Key, start, prev)); start = y; }
                    prev = y;
                }
                runs.Add((g.Key, start, prev));
            }

            int half = CorridorWidth / 2;
            // lee todo antes de escribir
            var writes = new List<(Vector2Int dst, Vector2Int src)>();
            foreach (var (x, y0, y1) in runs)
            {
                for (int y = y0 - 1; y <= y1 + 1; y++)
                {
                    var c = new Vector2Int(x, y);
                    bool corridorRow = y >= y0 && y <= y1 && IsEmpty(new Vector2Int(x - half - 1, y));
                    if (corridorRow)
                    {
                        // las paredes del pasillo se recorren hacia afuera
                        writes.Add((new Vector2Int(x - half - 1, y), new Vector2Int(x - 1, y)));
                        writes.Add((new Vector2Int(x + half + 1, y), new Vector2Int(x + 1, y)));
                    }
                    else if (y < y0 || y > y1)
                    {
                        // fila dentro de la sala: solo si debajo de la puerta hay algo distinto (cara de pared, etc.)
                        if (!IsFloor(c)) continue;
                    }
                    for (int d = 1; d <= half; d++)
                    {
                        writes.Add((new Vector2Int(x - d, y), c));
                        writes.Add((new Vector2Int(x + d, y), c));
                    }
                }
            }

            var snapshot = new Dictionary<(Tilemap, Vector2Int), (TileBase tile, Matrix4x4 m, Color color)>();
            foreach (var (_, src) in writes)
                foreach (var map in maps)
                {
                    var cell = map.WorldToCell(Center(src));
                    snapshot[(map, src)] = (map.GetTile(cell), map.GetTransformMatrix(cell), map.GetColor(cell));
                }
            foreach (var (dst, src) in writes)
                foreach (var map in maps)
                {
                    var (tile, m, color) = snapshot[(map, src)];
                    var cell = map.WorldToCell(Center(dst));
                    map.SetTile(cell, tile);
                    if (tile == null) continue;
                    map.SetTransformMatrix(cell, m);
                    map.SetColor(cell, color);
                }
            foreach (var map in maps)
            {
                map.CompressBounds();
                EditorUtility.SetDirty(map);
                if (map.TryGetComponent<TilemapCollider2D>(out var col)) EditorUtility.SetDirty(col);
            }

            // puertas: el sprite se repite a lo ancho y el bloqueo cubre los 3 tiles
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include))
            {
                var k = Key(door.transform.position);
                if (!runs.Any(r => r.x == k.x && k.y >= r.y0 - 1 && k.y <= r.y1 + 1)) continue;
                var so = new SerializedObject(door);
                Widen(so.FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer);
                Widen(so.FindProperty("gate").objectReferenceValue as SpriteRenderer);
                if (so.FindProperty("blocker").objectReferenceValue is BoxCollider2D box)
                {
                    float sx = Mathf.Abs(box.transform.lossyScale.x);
                    box.size = new Vector2(CorridorWidth / Mathf.Max(sx, 0.0001f), box.size.y);
                    box.offset = new Vector2(0f, box.offset.y);
                    EditorUtility.SetDirty(box);
                }
            }
            return runs.Count;
        }

        static void Widen(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return;
            float sx = Mathf.Abs(sr.transform.lossyScale.x);
            var b = sr.sprite.bounds.size;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(CorridorWidth / Mathf.Max(sx, 0.0001f), b.y);
            EditorUtility.SetDirty(sr);
        }
    }
}
