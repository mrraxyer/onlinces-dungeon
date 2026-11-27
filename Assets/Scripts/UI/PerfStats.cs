using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace OnLinces
{
    /// Estadísticas abajo a la izquierda: FPS, ms, mín/máx, CPU/GPU, memoria, resolución y gráfica de FPS.
    /// Se prende en Pausa > Opciones > Estadísticas.
    public sealed class PerfStats : MonoBehaviour
    {
        const int N = 172, H = 32, Pad = 4, TextHeight = 44;
        const float Bucket = 0.05f; // cada barra = 50 ms → la gráfica cubre ~8.6 s sin importar los FPS
        static readonly Color32 Empty = new(0, 0, 0, 0), Good = new(110, 220, 120, 255), Mid = new(240, 200, 80, 255),
            Bad = new(235, 90, 80, 255), Guide = new(255, 255, 255, 70);

        readonly float[] frames = new float[N];
        readonly float[] cols = new float[N]; // FPS promedio por barra
        readonly FrameTiming[] timing = new FrameTiming[1];
        Color32[] px;
        Texture2D tex;
        TMP_Text text;
        GameObject root;
        int head, count, colHead, colCount, bucketFrames;
        float textT, bucketT;

        /// parent: dónde va el panel (encima de lo demás); style: un texto de la UI para copiar fuente y color.
        public void Build(Transform parent, TMP_Text style)
        {
            root = new GameObject("PerfStats", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.SetParent(parent, false);
            rt.SetAsLastSibling();
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(Pad, Pad);
            rt.sizeDelta = new Vector2(N + 2 * Pad, TextHeight + H + 3 * Pad);
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var trt = text.rectTransform;
            trt.SetParent(rt, false);
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(Pad, -Pad);
            trt.sizeDelta = new Vector2(N, TextHeight);
            text.font = style.font;
            text.fontSharedMaterial = style.fontSharedMaterial;
            text.fontSize = PixelText.Base;
            text.color = style.color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            tex = new Texture2D(N, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            px = new Color32[N * H];
            var graph = new GameObject("Graph", typeof(RectTransform)).AddComponent<RawImage>();
            var grt = graph.rectTransform;
            grt.SetParent(rt, false);
            grt.anchorMin = grt.anchorMax = grt.pivot = Vector2.zero;
            grt.anchoredPosition = new Vector2(Pad, Pad);
            grt.sizeDelta = new Vector2(N, H);
            graph.texture = tex;
            graph.raycastTarget = false;

            root.SetActive(false);
        }

        void OnDestroy()
        {
            if (tex) Destroy(tex);
        }

        void Update()
        {
            if (root == null) return;
            bool show = VideoSettings.ShowStats;
            if (root.activeSelf != show) { root.SetActive(show); head = count = colHead = colCount = bucketFrames = 0; textT = bucketT = 0f; }
            if (!show) return;

            float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
            frames[head] = dt;
            head = (head + 1) % N;
            if (count < N) count++;
            FrameTimingManager.CaptureFrameTimings();

            textT -= dt;
            if (textT <= 0f) { textT = 0.25f; WriteText(); }

            bucketT += dt;
            bucketFrames++;
            if (bucketT >= Bucket)
            {
                cols[colHead] = bucketFrames / bucketT;
                colHead = (colHead + 1) % N;
                if (colCount < N) colCount++;
                bucketT = 0f;
                bucketFrames = 0;
                Draw();
            }
        }

        void WriteText()
        {
            float sum = 0f, worst = 0f, best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float f = frames[i];
                sum += f;
                if (f > worst) worst = f;
                if (f < best) best = f;
            }
            float avg = sum / count;
            string s = $"FPS {1f / avg:0} · {avg * 1000f:0.0} ms\nmín {1f / worst:0} · máx {1f / best:0}\n";
            // CPU/GPU necesita "Frame Timing Stats" en Player Settings; sin eso no hay datos y se omite
            s += FrameTimingManager.GetLatestTimings(1, timing) > 0
                ? $"CPU {timing[0].cpuFrameTime:0.0} · GPU {timing[0].gpuFrameTime:0.0} ms\n"
                : "\n";
            s += $"{Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB · {Screen.width}x{Screen.height}";
            text.text = s;
        }

        // barras de FPS promedio cada 50 ms (la más nueva a la derecha); escala hasta el máximo visto, mínimo 60, con guía en 60
        void Draw()
        {
            float top = 60f;
            for (int i = 0; i < colCount; i++) top = Mathf.Max(top, cols[i]);
            int guide = Mathf.Clamp(Mathf.RoundToInt(60f / top * (H - 1)), 0, H - 1);
            for (int x = 0; x < N; x++)
            {
                int age = N - 1 - x; // barras hacia atrás
                int h = 0;
                Color32 c = Good;
                if (age < colCount)
                {
                    float fps = cols[(colHead - 1 - age + N) % N];
                    h = Mathf.Clamp(Mathf.RoundToInt(fps / top * H), 1, H);
                    c = fps >= 55f ? Good : fps >= 28f ? Mid : Bad;
                }
                for (int y = 0; y < H; y++)
                    px[y * N + x] = y < h ? c : y == guide ? Guide : Empty;
            }
            tex.SetPixels32(px);
            tex.Apply(false);
        }
    }
}
