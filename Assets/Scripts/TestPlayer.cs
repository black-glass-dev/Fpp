using UnityEngine;

namespace Fpp
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TestPlayer : MonoBehaviour
    {
        public Transform view;
        private CharacterController controller;
        private float pitch;
        private float falling;
        private int moveFinger = -1;
        private int lookFinger = -1;
        private Vector2 moveOrigin;

        private void Awake() { controller = GetComponent<CharacterController>(); }

        private void Update()
        {
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        if (touch.position.x < Screen.width * .45f && moveFinger < 0)
                        { moveFinger = touch.fingerId; moveOrigin = touch.position; }
                        else if (touch.position.x >= Screen.width * .45f && lookFinger < 0)
                            lookFinger = touch.fingerId;
                    }
                    bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                    if (touch.fingerId == moveFinger)
                    {
                        if (ended) moveFinger = -1;
                        else move = Vector2.ClampMagnitude((touch.position - moveOrigin) / (Screen.height * .12f), 1);
                    }
                    if (touch.fingerId == lookFinger)
                    {
                        if (ended) lookFinger = -1;
                        else look = touch.deltaPosition * (120f / Screen.height);
                    }
                }
            }
            else
            {
                moveFinger = lookFinger = -1;
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

        private void OnApplicationFocus(bool focus) { moveFinger = lookFinger = -1; }
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, Screen.height / 35) };
            GUI.Label(new Rect(24, 24, Screen.width - 48, 90),
                "Cottage test map | Left drag: walk | Right drag: look\nDesktop: WASD + hold right mouse | Weapon pack pending", style);
        }
    }
}
