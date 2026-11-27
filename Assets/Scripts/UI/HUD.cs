using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OnLinces
{
    /// Textos del HUD: monedas, aviso, diálogo, toast y avisos apilados.
    public sealed class HUD : MonoBehaviour
    {
        // claros: los paneles pixel art son oscuros
        public static readonly Color Good = new(0.32f, 0.85f, 0.45f);
        public static readonly Color Bad = new(0.95f, 0.32f, 0.35f);
        public static readonly Color Info = Color.white;
        public static readonly Color Gold = new(1f, 0.82f, 0.29f);

        [SerializeField] TMP_Text coinsLabel;
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] TMP_Text toastLabel;
        [SerializeField] float toastSeconds = 2.5f;

        [Header("Diálogo")]
        [SerializeField] GameObject infoPanel;
        [SerializeField] TMP_Text infoTitle;
        [SerializeField] TMP_Text infoBody;

        [Header("Avisos")]
        [Tooltip("Plantilla desactivada")]
        [SerializeField] TMP_Text feedbackTemplate;
        [SerializeField] float feedbackSeconds = 2f;
        [SerializeField] int maxFeedback = 5;

        class Entry { public GameObject go; public TMP_Text text; public CanvasGroup group; public float t; public int coins; }

        static HUD instance;
        TMP_Text guestPrompt;
        Transform guestTarget;
        Canvas canvas;
        float toastT;
        CanvasGroup toastGroup;
        readonly List<Entry> entries = new();

        void Awake()
        {
            instance = this;
            if (toastLabel) toastGroup = PanelOf(toastLabel).GetComponent<CanvasGroup>();
        }

        void Start()
        {
            GameManager.Instance.Run.CoinsChanged += RefreshCoins;
            RefreshCoins();
            Show(promptLabel, "");
            Show(toastLabel, "");
            SetInfo(null, null);
            if (feedbackTemplate) PanelOf(feedbackTemplate).SetActive(false);
        }

        void OnDestroy()
        {
            if (GameManager.Exists) GameManager.Instance.Run.CoinsChanged -= RefreshCoins;
            if (instance == this) instance = null;
        }

        void Update()
        {
            // en cooperativo la cámara no está sobre nadie: cada aviso va sobre su jugador
            if (guestPrompt)
            {
                Place(PanelOf(promptLabel), PlayerController.Main ? PlayerController.Main.transform : null);
                Place(PanelOf(guestPrompt), guestTarget);
            }

            if (toastLabel != null && toastT > 0f)
            {
                toastT -= Time.unscaledDeltaTime;
                if (toastGroup) toastGroup.alpha = Mathf.Clamp01(toastT / 0.5f);
                if (toastT <= 0f) Show(toastLabel, "");
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.t -= Time.unscaledDeltaTime;
                if (e.group) e.group.alpha = Mathf.Clamp01(e.t / 0.4f);
                if (e.t <= 0f) { Destroy(e.go); entries.RemoveAt(i); }
            }
        }

        void RefreshCoins()
        {
            if (coinsLabel) coinsLabel.text = "$ " + GameManager.Instance.Run.Coins;
        }

        static GameObject PanelOf(TMP_Text label) =>
            label.transform.parent != null && label.transform.parent.GetComponent<UnityEngine.UI.Image>() != null
                ? label.transform.parent.gameObject : label.gameObject;

        static void Show(TMP_Text label, string text)
        {
            if (label == null) return;
            label.text = text;
            PanelOf(label).SetActive(!string.IsNullOrEmpty(text));
        }

        public static void SetPrompt(string text)
        {
            if (instance) Show(instance.promptLabel, text);
        }

        /// Aviso "usar" del jugador 2, sobre su cabeza.
        public static void SetGuestPrompt(string text, Transform target)
        {
            if (!instance || !instance.promptLabel) return;
            if (!instance.guestPrompt)
            {
                var p = PanelOf(instance.promptLabel);
                instance.guestPrompt = Instantiate(p, p.transform.parent).GetComponentInChildren<TMP_Text>(true);
                instance.canvas = instance.GetComponentInParent<Canvas>();
            }
            instance.guestTarget = target;
            if (instance.guestPrompt.text != text || string.IsNullOrEmpty(text)) Show(instance.guestPrompt, text);
        }

        void Place(GameObject panel, Transform target)
        {
            var cam = Camera.main;
            if (!panel || !panel.activeSelf || !target || !cam) return;
            var rt = (RectTransform)panel.transform;
            Vector2 screen = cam.WorldToScreenPoint(target.position + Vector3.up * Units.Px(30));
            var uiCam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, screen, uiCam, out var local))
                rt.anchoredPosition = local;
        }

        /// title null = esconder.
        public static void SetInfo(string title, string body)
        {
            if (!instance || !instance.infoPanel) return;
            bool show = !string.IsNullOrEmpty(title);
            instance.infoPanel.SetActive(show);
            if (!show) return;
            instance.infoTitle.text = title;
            instance.infoBody.text = body ?? "";
        }

        public static void Toast(string text)
        {
            if (!instance || !instance.toastLabel) return;
            Show(instance.toastLabel, text);
            if (instance.toastGroup) instance.toastGroup.alpha = 1f;
            instance.toastT = instance.toastSeconds;
        }

        /// Aviso que se desvanece.
        public static void Feedback(string text, Color color)
        {
            if (instance) instance.Add(text, color, 0);
        }

        /// Junta monedas seguidas en un aviso.
        public static void FeedbackCoins(int n)
        {
            if (!instance) return;
            var last = instance.entries.Count > 0 ? instance.entries[^1] : null;
            if (last != null && last.coins > 0 && last.t > instance.feedbackSeconds - 0.8f)
            {
                last.coins += n;
                last.text.text = "+$" + last.coins;
                last.t = instance.feedbackSeconds;
                return;
            }
            instance.Add("+$" + n, Gold, n);
        }

        void Add(string text, Color color, int coins)
        {
            if (feedbackTemplate == null) return;
            var template = PanelOf(feedbackTemplate);
            var go = Instantiate(template, template.transform.parent);
            go.SetActive(true);
            go.transform.SetAsFirstSibling(); // nuevo arriba
            var label = go.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            label.color = color;
            var e = new Entry { go = go, text = label, group = go.GetComponent<CanvasGroup>(), t = feedbackSeconds, coins = coins };
            entries.Add(e);
            while (entries.Count > maxFeedback) { Destroy(entries[0].go); entries.RemoveAt(0); }
        }
    }
}
