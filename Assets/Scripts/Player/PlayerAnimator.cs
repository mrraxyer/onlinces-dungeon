using UnityEngine;

namespace OnLinces
{
    /// Anima cuerpo y cabeza del jugador.
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] PlayerWeapons weapons;
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer head;

        [Header("Sin puesto")]
        [SerializeField] CharacterSkin onSkin;
        [SerializeField] CharacterSkin offSkin;

        // filas de la hoja
        const int Down = 0, Up = 1, Right = 2, Left = 3;

        CharacterSkin skin;
        int bodyRow = Down;
        float walkT;
        float blinkT = 3f;
        Vector3 headBase;

        void Start()
        {
            headBase = head.transform.localPosition;
            RefreshSkin();
            var role = GetComponent<PlayerRole>();
            if (role) role.Changed += RefreshSkin;
        }

        /// Según mascota y puesto.
        void RefreshSkin()
        {
            var mascot = GameManager.Instance.Run.Mascot;
            var role = GetComponent<PlayerRole>();
            var roleSkin = role && role.Current ? role.Current.SkinFor(mascot) : null;
            skin = roleSkin != null ? roleSkin : (mascot == Mascot.Off ? offSkin : onSkin);
        }

        void Update()
        {
            var mv = controller.MoveInput;
            bool moving = mv.sqrMagnitude > 0.01f;
            if (moving)
            {
                bodyRow = Row(mv);
                walkT += Time.deltaTime * 9f;
            }
            else walkT = 0f;

            int walkFrame = moving ? 1 + (int)walkT % 4 : 0;
            body.sprite = skin.Body[bodyRow * 5 + walkFrame];

            // al disparar mira hacia donde apuntas
            int headRow = controller.IsShooting ? Row(controller.Aim) : bodyRow;
            blinkT -= Time.deltaTime;
            if (blinkT < -0.12f) blinkT = Random.Range(2f, 4.5f);
            int expr = weapons != null && weapons.JustShot ? 1 : blinkT < 0f ? 2 : 0;
            if (headRow == Up) expr = 0;
            head.sprite = skin.Head[headRow * 3 + expr];

            // rebote al caminar
            bool side = bodyRow == Right || bodyRow == Left;
            int bob = side ? (walkFrame == 2 || walkFrame == 4 ? 1 : 0) : (walkFrame == 1 || walkFrame == 3 ? 1 : 0);
            head.transform.localPosition = headBase + Vector3.down * Units.Px(bob);
        }

        static int Row(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x > 0 ? Right : Left;
            return v.y < 0 ? Down : Up;
        }
    }
}
