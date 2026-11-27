using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OnLinces
{
    /// Borde del mapa que se funde con el vacío (sombreado interpolado / Gouraud):
    /// a cada esquina de tile se le calcula su distancia al vacío y de ahí una opacidad;
    /// el GPU interpola el color de las esquinas dentro de cada cuadro, así no hay escalones.
    public sealed class EdgeFade : MonoBehaviour
    {
        const int Search = 4;
        const float Opaque = 0.45f, Clear = 2.4f; // tiles
        const int SortingOrder = -4;              // encima del piso y las paredes, debajo de los personajes

        void Start()
        {
            var shader = Resources.Load<Shader>("VertexColorUnlit");
            if (shader == null) return;
            var solid = new HashSet<Vector2Int>();
            foreach (var map in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            {
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    if (!map.HasTile(cell)) continue;
                    var w = map.GetCellCenterWorld(cell);
                    solid.Add(new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y)));
                }
            }
            if (solid.Count == 0) return;

            var color = Camera.main ? Camera.main.backgroundColor : new Color(0.047f, 0.039f, 0.07f);
            var alphaAt = new Dictionary<Vector2Int, float>();
            float Alpha(Vector2Int corner)
            {
                if (alphaAt.TryGetValue(corner, out var a)) return a;
                float best = float.MaxValue;
                for (int y = corner.y - Search; y < corner.y + Search; y++)
                    for (int x = corner.x - Search; x < corner.x + Search; x++)
                    {
                        if (solid.Contains(new Vector2Int(x, y))) continue;
                        // distancia del punto al cuadro vacío [x, x+1] x [y, y+1]
                        float dx = Mathf.Max(x - corner.x, 0f, corner.x - (x + 1));
                        float dy = Mathf.Max(y - corner.y, 0f, corner.y - (y + 1));
                        best = Mathf.Min(best, dx * dx + dy * dy);
                    }
                float d = Mathf.Sqrt(best);
                float k = Mathf.Clamp01((d - Opaque) / (Clear - Opaque));
                a = 1f - k * k * (3f - 2f * k); // smoothstep
                alphaAt[corner] = a;
                return a;
            }

            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            foreach (var c in solid)
            {
                var corners = new[] { c, c + Vector2Int.right, c + Vector2Int.one, c + Vector2Int.up };
                float a0 = Alpha(corners[0]), a1 = Alpha(corners[1]), a2 = Alpha(corners[2]), a3 = Alpha(corners[3]);
                if (a0 + a1 + a2 + a3 < 0.01f) continue;
                int i = verts.Count;
                foreach (var p in corners) verts.Add(new Vector3(p.x, p.y, 0f));
                cols.Add(new Color(color.r, color.g, color.b, a0));
                cols.Add(new Color(color.r, color.g, color.b, a1));
                cols.Add(new Color(color.r, color.g, color.b, a2));
                cols.Add(new Color(color.r, color.g, color.b, a3));
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            var mesh = new Mesh { name = "EdgeFade", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            var go = new GameObject("Edge Fade");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(shader);
            mr.sortingOrder = SortingOrder;
        }
    }
}
