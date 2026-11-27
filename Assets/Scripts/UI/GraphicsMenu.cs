using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OnLinces
{
    /// Submenú "Opciones" de la pausa (gráficos y vibración), hecho copiando su caja y sus botones.
    /// Clic / A = siguiente valor (resolución y límite de FPS abren una lista); izquierda / derecha = anterior / siguiente; B = volver.
    public sealed class GraphicsMenu : MonoBehaviour
    {
        const float RowStep = 30f, RowHeight = 26f, RowWidth = 220f, Top = 44f, Bottom = 14f, ListHeight = 250f;
        static readonly Color Current = new(0.95f, 0.8f, 0.3f);

        sealed class Row
        {
            public Button button;
            public TMP_Text label;
            public Func<string> text;
            public Action<int> step;
            public Action click;
            public Func<bool> enabled;
            public bool cycles;
        }

        /// Lista desplegable con scroll (rueda, arrastre o mando).
        sealed class Dropdown
        {
            public GameObject box;
            public RectTransform viewport, content;
            public readonly List<Button> buttons = new();
            public Func<int> current;
            public Button owner;
        }

        readonly List<Row> rows = new();
        readonly List<Vector2Int> resolutions = new();
        FullScreenMode[] modes;
        int resIndex, modeIndex;
        GameObject pauseBox, box;
        Dropdown open;

        public bool IsOpen => (box && box.activeSelf) || open != null;

        /// pauseBox: la caja del menú de pausa; template: un botón de esa caja.
        public void Build(GameObject pauseBox, Button template)
        {
            this.pauseBox = pauseBox;
            box = Instantiate(pauseBox, pauseBox.transform.parent);
            box.name = "GraphicsBox";
            foreach (var b in box.GetComponentsInChildren<Button>(true)) Destroy(b.gameObject);
            var title = (RectTransform)box.transform.Find("Title");
            title.GetComponent<TMP_Text>().text = "OPCIONES";

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

                var resNames = resolutions.ConvertAll(v => $"{v.x}x{v.y}");
                var resList = BuildDropdown(template, "RESOLUCIÓN", resNames, i => { resIndex = i; ApplyScreen(); }, () => resIndex);
                resList.owner = Add(template, () => $"Resolución: {resNames[resIndex]}",
                    d => { resIndex = Wrap(resIndex + d, resolutions.Count); ApplyScreen(); }, click: () => OpenDropdown(resList));
                Add(template, () => "Pantalla: " + ModeName(modes[modeIndex]),
                    d => { modeIndex = Wrap(modeIndex + d, modes.Length); ApplyScreen(); });
                Add(template, () => "VSync: " + YesNo(VideoSettings.VSync), _ => VideoSettings.SetVSync(!VideoSettings.VSync));
            }
            var limits = VideoSettings.FpsLimits;
            var fpsList = BuildDropdown(template, "LÍMITE FPS", Array.ConvertAll(limits, VideoSettings.FpsName),
                i => VideoSettings.SetFpsLimit(limits[i]), () => Array.IndexOf(limits, VideoSettings.FpsLimit));
            fpsList.owner = Add(template, () => "Límite FPS: " + (desktop && VideoSettings.VSync ? "VSync" : VideoSettings.FpsName(VideoSettings.FpsLimit)),
                d => VideoSettings.SetFpsLimit(limits[Wrap(Array.IndexOf(limits, VideoSettings.FpsLimit) + d, limits.Length)]),
                () => !desktop || !VideoSettings.VSync, click: () => OpenDropdown(fpsList));
            Add(template, () => "Estadísticas: " + YesNo(VideoSettings.ShowStats), _ => VideoSettings.SetShowStats(!VideoSettings.ShowStats));
            Add(template, () => "Vibración: " + YesNo(Rumble.Enabled), _ => Rumble.SetEnabled(!Rumble.Enabled));
            Add(template, () => "Volver", _ => Back(), cycles: false);

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

        Button Add(Button template, Func<string> text, Action<int> step, Func<bool> enabled = null, bool cycles = true, Action click = null)
        {
            var b = Instantiate(template, box.transform);
            b.name = "Row";
            b.onClick = new Button.ButtonClickedEvent(); // sin los listeners del botón copiado
            b.navigation = new Navigation { mode = Navigation.Mode.Vertical };
            var row = new Row { button = b, label = b.GetComponentInChildren<TMP_Text>(), text = text, step = step, click = click, enabled = enabled, cycles = cycles };
            b.onClick.AddListener(() => { if (row.click != null) row.click(); else step(1); Refresh(); });
            rows.Add(row);
            return b;
        }

        public void Open()
        {
            pauseBox.SetActive(false);
            box.SetActive(true);
            Refresh();
            Select(rows[0].button);
        }

        /// Un nivel atrás: lista → Opciones → Pausa.
        public void Back()
        {
            if (open != null)
            {
                var owner = open.owner;
                open.box.SetActive(false);
                open = null;
                box.SetActive(true);
                Refresh();
                Select(owner);
            }
            else Close();
        }

        /// Cierra todo y deja la caja de pausa lista.
        public void Close()
        {
            if (!IsOpen) return;
            if (open != null) open.box.SetActive(false);
            open = null;
            box.SetActive(false);
            pauseBox.SetActive(true);
        }

        Dropdown BuildDropdown(Button template, string title, IList<string> labels, Action<int> pick, Func<int> current)
        {
            var dd = new Dropdown { current = current };
            dd.box = Instantiate(pauseBox, pauseBox.transform.parent);
            dd.box.name = "List " + title;
            foreach (var b in dd.box.GetComponentsInChildren<Button>(true)) Destroy(b.gameObject);
            // tan alta como sus opciones, hasta ListHeight; si no caben, scroll
            float h = Mathf.Min(ListHeight, Top + RowHeight + (labels.Count - 1) * RowStep + Bottom);
            var rt = (RectTransform)dd.box.transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            var titleRt = (RectTransform)dd.box.transform.Find("Title");
            titleRt.GetComponent<TMP_Text>().text = title;
            titleRt.anchoredPosition = new Vector2(0f, h / 2f - Top / 2f);

            dd.viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            dd.viewport.SetParent(rt, false);
            dd.viewport.anchorMin = dd.viewport.anchorMax = dd.viewport.pivot = new Vector2(0.5f, 1f);
            dd.viewport.anchoredPosition = new Vector2(0f, -Top);
            dd.viewport.sizeDelta = new Vector2(RowWidth, h - Top - Bottom);
            dd.viewport.GetComponent<Image>().color = Color.clear; // recibe la rueda y el arrastre entre botones

            dd.content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            dd.content.SetParent(dd.viewport, false);
            dd.content.anchorMin = new Vector2(0f, 1f);
            dd.content.anchorMax = Vector2.one;
            dd.content.pivot = new Vector2(0.5f, 1f);
            dd.content.sizeDelta = Vector2.zero;
            var layout = dd.content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = RowStep - RowHeight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            dd.content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = dd.viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = dd.content;
            scroll.viewport = dd.viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = RowStep;

            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                var b = Instantiate(template, dd.content);
                b.name = labels[i];
                b.onClick = new Button.ButtonClickedEvent();
                b.navigation = new Navigation { mode = Navigation.Mode.Vertical };
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
                b.GetComponentInChildren<TMP_Text>().text = labels[i];
                b.onClick.AddListener(() => { pick(index); Back(); });
                dd.buttons.Add(b);
            }
            dd.box.SetActive(false);
            return dd;
        }

        void OpenDropdown(Dropdown dd)
        {
            box.SetActive(false);
            dd.box.SetActive(true);
            open = dd;
            int cur = dd.current();
            for (int i = 0; i < dd.buttons.Count; i++)
                dd.buttons[i].GetComponentInChildren<TMP_Text>().color = i == cur ? Current : rows[0].label.color;
            var first = dd.buttons[Mathf.Max(0, cur)];
            Canvas.ForceUpdateCanvases();
            Select(first);
            ScrollTo(dd, (RectTransform)first.transform, true);
        }

        static void Select(Button b)
        {
            if (TouchControls.PadActive) EventSystem.current?.SetSelectedGameObject(b.gameObject);
        }

        // deja visible el botón (centrado al abrir; al navegar, solo lo justo)
        static void ScrollTo(Dropdown dd, RectTransform item, bool center)
        {
            float view = dd.viewport.rect.height;
            float max = Mathf.Max(0f, dd.content.rect.height - view);
            float top = -item.anchoredPosition.y - item.rect.height / 2f, bottom = top + item.rect.height;
            float y = dd.content.anchoredPosition.y;
            if (center) y = (top + bottom - view) / 2f;
            else if (top < y) y = top;
            else if (bottom > y + view) y = bottom - view;
            dd.content.anchoredPosition = new Vector2(0f, Mathf.Clamp(y, 0f, max));
        }

        void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) { Back(); return; }
            var sel = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (open != null)
            {
                if (sel && sel.transform.parent == open.content) ScrollTo(open, (RectTransform)sel.transform, false);
                return;
            }

            int d = 0;
            if ((kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame))) d = -1;
            if ((kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame))) d = 1;
            if (d == 0) return;
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
