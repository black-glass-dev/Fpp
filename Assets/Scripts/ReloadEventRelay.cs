using UnityEngine;
namespace Fpp
{
    public sealed class ReloadEventRelay : MonoBehaviour
    {
        public WeaponController controller;
        public void ReloadFoleyMarker(string stage){if(controller!=null)controller.ReloadFoleyMarker(stage);}
    }
}
