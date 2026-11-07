using UnityEngine;
namespace Fpp
{
    [ExecuteAlways]
    public sealed class ReloadEventRelay : MonoBehaviour
    {
        public WeaponController controller;
        public void ReloadFoleyMarker(string stage){if(Application.isPlaying && controller!=null)controller.ReloadFoleyMarker(stage);}
    }
}
