using UnityEngine;
using UnityEngine.EventSystems;

namespace Fpp
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TestPlayer : MonoBehaviour
    {
        public Transform view;
        private CharacterController controller;
        private float pitch;
        private float falling;
        private int lookFinger = -1;

        private void Awake() { controller = GetComponent<CharacterController>(); }

        private void Update()
        {
            if (WeaponController.MenuOpen) { lookFinger = -1; return; }
            Vector2 move = MobileHud.Instance != null ? MobileHud.Instance.Move : Vector2.zero;
            Vector2 look = Vector2.zero;
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began && (MobileHud.Instance == null || !MobileHud.Instance.BlocksLook(touch.position)) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                    {
                        if (touch.position.x >= Screen.width * .35f && lookFinger < 0)
                            lookFinger = touch.fingerId;
                    }
                    bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                    if (touch.fingerId == lookFinger)
                    {
                        if (ended) lookFinger = -1;
                        else look = touch.deltaPosition * (120f / Screen.height);
                    }
                }
            }
            else
            {
                lookFinger = -1;
                move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
                if (Input.GetMouseButton(1))
                    look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2;
            }
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -75, 75);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
            falling = controller.isGrounded ? -2 : falling + Physics.gravity.y * Time.deltaTime;
            var velocity = (transform.right * move.x + transform.forward * move.y) * 4;
            velocity.y = falling;
            controller.Move(velocity * Time.deltaTime);
        }

        private void OnApplicationFocus(bool focus) { lookFinger = -1; }
    }
}
