using UnityEngine;
namespace Fpp
{
    public sealed class AmmoBox : MonoBehaviour
    {

        private void OnTriggerEnter(Collider other){var w=other.GetComponent<WeaponController>();if(w!=null&&w.GrantAmmo(GunplayConfig.Current.ammoBoxGrant)>0)gameObject.SetActive(false);}
    }
}
