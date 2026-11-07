using UnityEngine;
namespace Fpp
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        public float maxHealth=100;
        public float Health {get;private set;}
        private void Awake(){Health=maxHealth;}
        public void Damage(float amount,Vector3 source)
        {
            if(Health<=0)return;Health=Mathf.Max(0,Health-Mathf.Max(0,amount));
            float angle=Vector3.SignedAngle(transform.forward,source-transform.position,Vector3.up);
            GunplayEvents.Send(GunplayEventKind.Damage,source,null,"incoming",angle);
            if(Health<=0)GetComponent<TestPlayer>().SetState(MovementState.Dead);
        }
    }
}
