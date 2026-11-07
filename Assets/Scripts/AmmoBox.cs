using UnityEngine;
namespace Fpp
{
    public sealed class AmmoBox : MonoBehaviour
    {
        public int amount=60;
        private void OnTriggerEnter(Collider other){var w=other.GetComponent<WeaponController>();if(w!=null&&w.GrantAmmo(amount)>0)gameObject.SetActive(false);}
    }
}
