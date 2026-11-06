using UnityEngine;

namespace Fpp
{
    public sealed class WeaponController : MonoBehaviour
    {
        public Transform view;
        public static bool MenuOpen { get; private set; }
        private string credits;
        private Vector2 creditsScroll;
        private WeaponPickup[] pickups;
        private WeaponPickup nearest;
        private WeaponPickup equipped;
        private Vector3 groundPosition;
        private Quaternion groundRotation;
        private Vector3 groundScale;
        private int rounds;
        private int hits;
        private float nextShot;
        private float reloadEnd;
        private float flashEnd;
        private bool fireHeld;
        private GameObject flash;
        private const int Magazine = 30;

        public static bool IsActionArea(Vector2 screenPosition)
        {
            return screenPosition.x > Screen.width * .77f &&
                (screenPosition.y < Screen.height * .52f || screenPosition.y > Screen.height * .82f);
        }
        private void Start()
        {
            MenuOpen = false;
            var notices = Resources.Load<TextAsset>("THIRD_PARTY_NOTICES");
            credits = notices != null ? notices.text : "Credits unavailable";
            var mosqueNotices = Resources.Load<TextAsset>("MOSQUE_NOTICE");
            if (mosqueNotices != null) credits += "\n\n" + mosqueNotices.text;
            pickups = FindObjectsOfType<WeaponPickup>();
            flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Shot flash";
            Destroy(flash.GetComponent<Collider>());
            flash.transform.SetParent(view, false);
            flash.transform.localPosition = new Vector3(.19f, -.14f, .7f);
            flash.transform.localScale = Vector3.one * .07f;
            flash.GetComponent<Renderer>().material.color = Color.yellow;
            flash.SetActive(false);
        }
        private void Update()
        {
            if (MenuOpen) { fireHeld = false; return; }
            nearest = null;
            float distance = 2.5f;
            foreach (var pickup in pickups)
            {
                if (pickup == null || pickup == equipped) continue;
                float d = Vector3.Distance(transform.position, pickup.transform.position);
                if (d < distance) { nearest = pickup; distance = d; }
            }
            if (reloadEnd > 0 && Time.time >= reloadEnd) { rounds = Magazine; reloadEnd = 0; }
            if (Input.GetKeyDown(KeyCode.E)) EquipNearest();
            if (Input.GetKeyDown(KeyCode.R)) Reload();
            bool touchFire = false;
            foreach (Touch touch in Input.touches)
                if (FireRect().Contains(new Vector2(touch.position.x, Screen.height - touch.position.y)) &&
                    touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled) touchFire = true;
            bool desktopFire = Input.touchCount == 0 && Input.GetMouseButton(0) && !IsActionArea(Input.mousePosition);
            if (touchFire || fireHeld || desktopFire) Fire();
            if (flash != null) flash.SetActive(Time.time < flashEnd);
        }
        private void EquipNearest()
        {
            if (nearest == null) return;
            if (equipped != null)
            {
                equipped.transform.SetParent(null, true);
                equipped.transform.position = groundPosition;
                equipped.transform.rotation = groundRotation;
                equipped.transform.localScale = groundScale;
            }
            equipped = nearest;
            groundPosition = equipped.transform.position;
            groundRotation = equipped.transform.rotation;
            groundScale = equipped.transform.localScale;
            equipped.transform.SetParent(view, false);
            equipped.transform.localPosition = new Vector3(.22f, -.25f, .5f);
            equipped.transform.localRotation = Quaternion.identity;
            equipped.transform.localScale = Vector3.one * .55f;
            rounds = Magazine;
            reloadEnd = 0;
        }
        private void Reload() { if (equipped != null && rounds < Magazine && reloadEnd == 0) reloadEnd = Time.time + 1.5f; }
        private void Fire()
        {
            if (equipped == null || rounds <= 0 || reloadEnd > 0 || Time.time < nextShot) return;
            nextShot = Time.time + .14f;
            rounds--;
            flashEnd = Time.time + .045f;
            if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, 100))
            {
                var target = hit.collider.GetComponent<TestTarget>();
                if (target != null) { target.Hit(); hits++; }
            }
        }
        private static Rect FireRect() => new Rect(Screen.width * .8f, Screen.height * .72f, Screen.width * .18f, Screen.height * .24f);
        private void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width - 140, 20, 120, 45), MenuOpen ? "CLOSE" : "CREDITS"))
                MenuOpen = !MenuOpen;
            if (MenuOpen)
            {
                fireHeld = false;
                var outer = new Rect(20, 80, Screen.width - 40, Screen.height - 100);
                GUI.Box(outer, "Asset credits / license notices");
                var content = new Rect(0, 0, outer.width - 45, 1600);
                creditsScroll = GUI.BeginScrollView(new Rect(outer.x + 10, outer.y + 30, outer.width - 20, outer.height - 40), creditsScroll, content);
                GUI.TextArea(content, credits);
                GUI.EndScrollView();
                return;
            }
            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, Screen.height / 35) };
            GUI.Label(new Rect(24, 95, Screen.width * .75f, 90),
                (equipped == null ? "Find a weapon on the ground" : equipped.displayName + " | Ammo " + rounds + "/30") +
                " | Target hits: " + hits + (reloadEnd > 0 ? " | Reloading..." : ""), style);
            GUI.Label(new Rect(Screen.width / 2f - 6, Screen.height / 2f - 12, 24, 30), "+", style);
            fireHeld = GUI.RepeatButton(FireRect(), "FIRE");
            if (GUI.Button(new Rect(Screen.width * .8f, Screen.height * .51f, Screen.width * .18f, Screen.height * .09f), "RELOAD")) Reload();
            if (nearest != null && GUI.Button(new Rect(Screen.width * .8f, Screen.height * .61f, Screen.width * .18f, Screen.height * .09f), "PICK UP (E)")) EquipNearest();
        }
        private void OnApplicationFocus(bool focus) { fireHeld = false; }
    }
}
