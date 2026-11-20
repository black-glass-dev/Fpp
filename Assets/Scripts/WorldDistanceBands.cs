using System.Collections.Generic;
using UnityEngine;

namespace Fpp
{
    /// <summary>
    /// Distance design requested for the phone: geometry beyond the fog's opaque point is not
    /// drawn at all, and it comes back the moment the player approaches. Fog only makes that cut
    /// invisible; this component is what actually removes the work.
    /// It never touches first-person (layer 8) geometry, LOD-group members (the LODGroup owns its
    /// own bands) or anything with a collider that gameplay depends on staying visible.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class WorldDistanceBands : MonoBehaviour
    {
        private readonly List<Renderer> candidates = new List<Renderer>();
        private readonly List<bool> authored = new List<bool>();
        private Camera world;
        private Transform target;
        private PresentationVisualSettings visual;
        private float refresh, connectRetry;
        private int hidden;

        public static WorldDistanceBands Active { get; private set; }
        public int HiddenCount => hidden;
        public int CandidateCount => candidates.Count;

        public void Initialize(Camera camera, Transform viewer)
        {
            world = camera;
            target = viewer;
            Active = this;
            visual = PresentationVisualSettings.Current;
            if (world != null && visual != null) world.farClipPlane = visual.FarClip;
            // Small clutter is invisible long before the far clip; a per-layer cull distance is
            // the cheapest correct way to drop it without touching material or texture settings.
            int clutter = LayerMask.NameToLayer("WorldClutter");
            if (world != null && clutter >= 0 && visual != null)
            {
                var distances = world.layerCullDistances;
                distances[clutter] = Mathf.Min(visual.mediumBand, visual.fogEnd * .55f);
                world.layerCullDistances = distances;
            }
            Rebuild();
        }

        /// <summary>Snapshot the world renderers that this component is allowed to hide.</summary>
        public void Rebuild()
        {
            Restore();
            candidates.Clear();
            authored.Clear();
            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
            {
                if (renderer == null || renderer.gameObject.layer == 8) continue;
                if (renderer.GetComponentInParent<LODGroup>() != null) continue;
                if (renderer.GetComponent<LODGroup>() != null) continue;
                // A renderer that carries gameplay collision must keep its authored visibility;
                // hiding it would make an invisible obstacle. Colliders live on parents too.
                if (renderer.GetComponentInParent<Collider>() != null) continue;
                candidates.Add(renderer);
                authored.Add(renderer.enabled);
            }
            hidden = 0;
        }

        private void Start()
        {
            // Component order is not a contract: if PresentationRenderer.Start has not connected
            // us yet, find the real camera ourselves so distance culling is never silently absent.
            if (world == null) TryConnect();
        }

        private void TryConnect()
        {
            var weapons = UnityEngine.Object.FindObjectOfType<WeaponController>();
            if (weapons == null || weapons.view == null) return;
            var camera = weapons.view.GetComponent<Camera>();
            if (camera == null) return;
            Initialize(camera, weapons.view);
        }

        private void LateUpdate()
        {
            if (world == null) { connectRetry -= Time.unscaledDeltaTime; if (connectRetry <= 0f) { connectRetry = 1f; TryConnect(); } }
            if (world == null || target == null || visual == null || candidates.Count == 0) return;
            refresh -= Time.unscaledDeltaTime;
            // Bounds are static; a handful of checks per second is enough and avoids per-frame
            // renderer toggling on a slow phone.
            if (refresh > 0f) return;
            refresh = .25f;
            float cull = Mathf.Min(visual.fogEnd, world.farClipPlane);
            float limit = cull * cull;
            var position = target.position;
            int now = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                var renderer = candidates[i];
                if (renderer == null) continue;
                bool far = (renderer.bounds.center - position).sqrMagnitude > limit;
                bool visible = !far && authored[i];
                if (renderer.enabled != visible) renderer.enabled = visible;
                if (far && authored[i]) now++;
            }
            hidden = now;
        }

        /// <summary>Restore authored visibility (scene change, QA capture, component teardown).</summary>
        public void Restore()
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] != null) candidates[i].enabled = authored[i];
            hidden = 0;
        }

        private void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += Reloaded; }
        private void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= Reloaded; }
        private void Reloaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { Rebuild(); }

        private void OnDestroy()
        {
            Restore();
            if (Active == this) Active = null;
        }
    }
}
