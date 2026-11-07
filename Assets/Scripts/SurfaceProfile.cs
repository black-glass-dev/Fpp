using UnityEngine;
namespace Fpp
{
    public sealed class SurfaceProfile : MonoBehaviour
    {
        public string surface="stone";
        public PenetrationClass penetration;
        [Range(0,1)] public float damageRetained=.5f;
        public float maxThickness=.2f;
        public static string Name(Collider collider){var p=collider!=null?collider.GetComponentInParent<SurfaceProfile>():null;return p!=null?p.surface:"default";}
    }
}
