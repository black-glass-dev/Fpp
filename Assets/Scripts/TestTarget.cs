using UnityEngine;

namespace Fpp
{
    public sealed class TestTarget : MonoBehaviour
    {
        private Renderer targetRenderer;
        private float restoreAt;
        private Color original;
        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            original = targetRenderer.material.color;
        }
        public void Hit()
        {
            targetRenderer.material.color = Color.red;
            restoreAt = Time.time + .2f;
        }
        private void Update()
        {
            if (restoreAt > 0 && Time.time > restoreAt)
            { targetRenderer.material.color = original; restoreAt = 0; }
        }
    }
}
