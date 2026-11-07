using UnityEngine;
namespace Fpp
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        public string displayName;
        public WeaponState State=>gameObject.activeSelf?WeaponState.Dropped:WeaponState.Holstered;
        public WeaponAsset config;
        [SerializeField] private int rounds=-1,reserve=-1;
        public WeaponAsset Asset {get {if(config==null)config=Resources.Load<WeaponAsset>("Gunplay/"+(displayName.Contains("AK")?"ak47":displayName.Contains("Glock")?"glock17":"field"));return config;}}
        public int Rounds=>rounds<0?Asset.magSize:rounds;
        public int Reserve=>reserve<0?Asset.reserveMax:reserve;
        private Vector3 velocity;
        private bool settling;
        public void Store(int mag,int spare){rounds=mag;reserve=spare;}
        public void Drop(Vector3 position,Vector3 forward)
        {
            transform.position=position;gameObject.SetActive(true);velocity=forward*GunplayConfig.Current.shellSpeed;settling=true;
            GunplayEvents.Send(GunplayEventKind.PickupHighlight,position,Asset,"dropped");
        }
        private void Update()
        {
            if(!settling)return;var c=GunplayConfig.Current;float dt=Time.deltaTime;
            velocity+=Vector3.down*c.gravity*dt;Vector3 travel=velocity*dt;
            if(Physics.Raycast(transform.position+Vector3.up*c.nearClip,travel.normalized,out var hit,travel.magnitude+c.nearClip,~(1<<8),QueryTriggerInteraction.Ignore) && hit.collider.transform.root!=transform.root)
            {transform.position=hit.point+Vector3.up*c.nearClip;settling=false;velocity=Vector3.zero;}
            else transform.position+=travel;
        }
    }
}
