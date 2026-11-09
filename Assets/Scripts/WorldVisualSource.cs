using UnityEngine;
namespace Fpp
{
    // Records untouched source identity for native collision/visual-derivative checks.
    public sealed class WorldVisualSource : MonoBehaviour { public Mesh originalMesh; }
}
