using UnityEngine;
namespace Fpp
{
    public enum WeaponClass { Pistol, SMG, AR, LMG, Marksman, Sniper }
    public enum FireMode { Semi, Auto, Burst }
    public enum WeaponState { Holstered, Drawing, Idle, Aiming, Firing, ReloadTactical, ReloadEmpty, SwitchingOut, Dropped }
    public enum PenetrationClass { None, Thin, Wood, ConcreteThin, Metal }
    [CreateAssetMenu(menuName="FPP/Weapon")]
    public sealed class WeaponAsset : ScriptableObject
    {
        public string id="mark23", displayName="Mark 23";
        public WeaponClass weaponClass;
        public FireMode fireMode;
        public int burstCount=3, magSize=12, reserveMax=120, tracerEveryN=4;
        public float rpm=480, adsTimeSec=.16f, sprintOutFireDelaySec=.1f, aimWalkSpeed=2.8f;
        public Vector2 hipSpreadDeg=new Vector2(1,8);
        public float adsSpreadDeg=.03f, bloomPerShot=.55f, bloomCap=4, adsRecoilMult=1;
        public Vector2[] recoilPattern={new Vector2(.7f,-.1f),new Vector2(.8f,.1f),new Vector2(.6f,.15f)};
        [Range(0,1)] public float recoilSmoothness=.85f;
        public float viewKickMult=1, gunKickMult=1, recoilRecoveryDelaySec=.06f, recoilRecoveryRateDegPerSec=8;
        public float reloadTacticalSec=1.6f, reloadEmptySec=2, switchInSec=.45f, switchOutSec=.35f, drawSec=.6f, holsterSec=.4f;
        public float dmg0=35, range0=20, dmg1=26, range1=35, dmg2=20, range2=300;
        public PenetrationClass penetrationClass=PenetrationClass.Thin;
        public float muzzleFlashScale=1, sightFOV=55, pickupLength=.27f;
        public bool mountable=true;
        public string shotEvent="weapon.shot", tailEvent="weapon.tail", reloadEvent="weapon.reload";
        public Vector3 aimPoseOffset, aimPoseEuler, supportHand, triggerFinger, magGrab;
        public Vector3 ejectOffset=new Vector3(.04f,.01f,0);
        public GameObject casingPrefab;
        public float Damage(float distance) => distance<=range0?dmg0:distance<=range1?dmg1:dmg2;
    }
}
