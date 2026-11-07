using UnityEngine;
namespace Fpp
{
    public sealed class WeaponSockets : MonoBehaviour
    {
        public Transform muzzle,ejectionPort,opticAlign,triggerFinger,supportHand,magGrab;
        public void Build(Transform barrel,Transform sight,WeaponAsset weapon)
        {
            muzzle=barrel;opticAlign=sight;
            ejectionPort=Socket("Ejection port",weapon.ejectOffset);
            triggerFinger=Socket("Grip triggerFinger APPROXIMATE",weapon.triggerFinger);
            supportHand=Socket("Grip supportHand APPROXIMATE",weapon.supportHand);
            magGrab=Socket("Grip magGrab APPROXIMATE",weapon.magGrab);
        }
        private Transform Socket(string name,Vector3 position){var child=new GameObject(name).transform;child.SetParent(transform,false);child.localPosition=position;return child;}
    }
}
