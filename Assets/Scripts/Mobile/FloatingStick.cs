using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace OnLinces
{
    /// Palanca táctil flotante: va en una zona invisible de la pantalla (una Image transparente con Raycast Target).
    /// Al tocar la zona, la base salta a donde pusiste el dedo; al soltar vuelve a su lugar.
    /// Para el juego es el stick de un gamepad (Control Path), así que no hay que cambiar el código del jugador.
    public sealed class FloatingStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] string m_ControlPath = "<Gamepad>/leftStick";

        [Tooltip("Círculo grande (hijo de esta zona)")]
        [SerializeField] RectTransform stickBase;
        [Tooltip("Círculo chico (hijo de la base)")]
        [SerializeField] RectTransform knob;
        [Tooltip("Qué tanto se aleja el círculo chico del centro, en unidades del Canvas")]
        [SerializeField] float radius = 30f;

        /// Dirección actual, de -1 a 1 en cada eje. (0,0) = suelta.
        public Vector2 Value { get; private set; }

        protected override string controlPathInternal { get => m_ControlPath; set => m_ControlPath = value; }

        RectTransform zone;
        Vector2 restPosition;
        bool restSaved;
        int pointerId = int.MinValue;

        void Awake()
        {
            zone = (RectTransform)transform;
            if (stickBase != null) { restPosition = stickBase.anchoredPosition; restSaved = true; }
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue || stickBase == null) return;
            pointerId = e.pointerId;
            if (TryLocal(e, out var p)) stickBase.localPosition = p;
            Set(Vector2.zero);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId || !TryLocal(e, out var p)) return;
            var delta = Vector2.ClampMagnitude(p - (Vector2)stickBase.localPosition, radius);
            Set(delta / radius);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) Release();
        }

        protected override void OnDisable()
        {
            Release();
            base.OnDisable();
        }

        bool TryLocal(PointerEventData e, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out local);

        void Set(Vector2 value)
        {
            Value = value;
            if (knob != null) knob.anchoredPosition = value * radius;
            SendValueToControl(value);
        }

        void Release()
        {
            bool wasHeld = pointerId != int.MinValue;
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (restSaved) stickBase.anchoredPosition = restPosition;
            if (wasHeld) SendValueToControl(Vector2.zero);
        }
    }
}
