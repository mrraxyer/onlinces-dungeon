using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnLinces
{
    /// Submenú "Gráficos" de la pausa, hecho copiando su caja y sus botones.
    /// Clic / A = siguiente valor; izquierda / derecha = anterior / siguiente; B = volver.
    public sealed class GraphicsMenu : MonoBehaviour
    {
        const float RowStep = 30f, RowHeight = 26f, RowWidth = 220f, Top = 44f, Bottom = 14f;

        sealed class Row
        {
            public Button button;
            public TMP_Text label;
            public Func<string> text;
            public Action<int> step;
            public Func<bool> enabled;
            public bool cycles;
        }

        readonly List<Row> rows = new();
        readonly List<Vector2Int> resolutions = new();
        FullScreenMode[] modes;
        int resIndex, modeIndex;
        GameObject pauseBox, box;

        public bool IsOpen => box && box.activeSelf;

        /// pauseBox: la caja del menú de pausa; template: un botón de esa caja.
        public void Build(GameObject pauseBox, Button template)
        {
            this.pauseBox = pauseBox;
            box = Instantiate(pauseBox, pauseBox.transform.parent);
            box.name = "GraphicsBox";
            foreach (var b in box.GetComponentsInChildren<Button>(true)) Destroy(b.gameObject);
            var title = (RectTransform)box.transform.Find("Title");
            title.GetComponent<TMP_Text>().text = "GRÁFICOS";

            // en teléfono la resolución, el modo y el VSync los decide el sistema
            bool desktop = !Application.isMobilePlatform;
            if (desktop)
            {
                foreach (var r in Screen.resolutions)
                {
                    var v = new Vector2Int(r.width, r.height);
                    if (!resolutions.Contains(v)) resolutions.Add(v);
                }
                if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));
                resIndex = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
                if (resIndex < 0) resIndex = resolutions.Count - 1;
                modes = Application.platform == RuntimePlatform.WindowsPlayer
                    ? new[] { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed }
                    : new[] { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
                modeIndex = Mathf.Max(0, Array.IndexOf(modes, Screen.fullScreenMode));

                Add(template, () => $"Resolución: {resolutions[resIndex].x}x{resolutions[resIndex].y}",
                    d => { resIndex = Wrap(resIndex + d, resolutions.Count); ApplyScreen(); });
                Add(template, () => "Pantalla: " + ModeName(modes[modeIndex]),
                    d => { modeIndex = Wrap(modeIndex + d, modes.Length); ApplyScreen(); });
                Add(template, () => "VSync: " + YesNo(VideoSettings.VSync), _ => VideoSettings.SetVSync(!VideoSettings.VSync));
            }
            var limits = VideoSettings.FpsLimits;
            Add(template, () => "Límite FPS: " + (desktop && VideoSettings.VSync ? "VSync" : VideoSettings.FpsName(VideoSettings.FpsLimit)),
                d => VideoSettings.SetFpsLimit(limits[Wrap(Array.IndexOf(limits, VideoSettings.FpsLimit) + d, limits.Length)]),
                () => !desktop || !VideoSettings.VSync);
            Add(template, () => "Estadísticas: " + YesNo(VideoSettings.ShowStats), _ => VideoSettings.SetShowStats(!VideoSettings.ShowStats));
            Add(template, () => "Volver", _ => Close(), cycles: false);

            var rt = (RectTransform)box.transform;
            float h = Top + RowHeight + (rows.Count - 1) * RowStep + Bottom;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            title.anchoredPosition = new Vector2(0f, h / 2f - Top / 2f);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = (RectTransform)rows[i].button.transform;
                r.sizeDelta = new Vector2(RowWidth, RowHeight);
                r.anchoredPosition = new Vector2(0f, h / 2f - Top - RowHeight / 2f - i * RowStep);
            }
            box.SetActive(false);
        }

        void Add(Button template, Func<string> text, Action<int> step, Func<bool> enabled = null, bool cycles = true)
        {
            var b = Instantiate(template, box.transform);
            b.name = "Row";
            b.onClick = new Button.ButtonClickedEvent(); // sin los listeners del botón copiado
            b.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            var row = new Row { button = b, label = b.GetComponentInChildren<TMP_Text>(), text = text, step = step, enabled = enabled, cycles = cycles };
            b.onClick.AddListener(() => { step(1); Refresh(); });
            rows.Add(row);
        }

        public void Open()
        {
            pauseBox.SetActive(false);
            box.SetActive(true);
            Refresh();
            if (TouchControls.PadActive) EventSystem.current?.SetSelectedGameObject(rows[0].button.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            box.SetActive(false);
            pauseBox.SetActive(true);
        }

        void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) { Close(); return; }

            int d = 0;
            if ((kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame))) d = -1;
            if ((kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame))) d = 1;
            if (d == 0) return;
            var sel = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            foreach (var row in rows)
                if (row.cycles && row.button.gameObject == sel && row.button.interactable) { row.step(d); Refresh(); }
        }

        void Refresh()
        {
            foreach (var row in rows)
            {
                row.label.text = row.text();
                row.button.interactable = row.enabled?.Invoke() ?? true;
            }
        }

        void ApplyScreen() => Screen.SetResolution(resolutions[resIndex].x, resolutions[resIndex].y, modes[modeIndex]);

        static int Wrap(int i, int n) => ((i % n) + n) % n;
        static string YesNo(bool on) => on ? "Sí" : "No";
        static string ModeName(FullScreenMode m) => m switch
        {
            FullScreenMode.ExclusiveFullScreen => "Exclusiva",
            FullScreenMode.Windowed => "Ventana",
            _ => "Completa",
        };
    }
}
