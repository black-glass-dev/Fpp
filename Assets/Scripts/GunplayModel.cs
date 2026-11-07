using UnityEngine;
namespace Fpp
{
    // Allocation-free, seeded math shared by gameplay and QA. No Unity random global state.
    public sealed class GunplayModel
    {
        public float AdsT {get;private set;}
        public float Bloom {get;private set;}
        public float NextShot {get;private set;}
        public int ShotIndex {get;private set;}
        public Vector2 AimClimb {get;private set;}
        public Vector2 ViewKick {get;private set;}
        public Vector2 GunKick {get;private set;}
        private Vector2 viewVelocity,gunVelocity;
        private float lastShot=-100, stoppedAt=-100, adsAt=-100;
        private bool moving,aimed;
        private uint seed=0x12345678;
        public float Noise(){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;return (seed&0xFFFFFF)/(float)0xFFFFFF;}
        public void Reset(){AdsT=Bloom=NextShot=0;ShotIndex=0;AimClimb=ViewKick=GunKick=viewVelocity=gunVelocity=Vector2.zero;lastShot=stoppedAt=adsAt=-100;moving=aimed=false;seed=0x12345678;}
        public void Tick(float dt,float now,bool ads,bool isMoving,WeaponAsset w,GunplayConfig c)
        {
            AdsT=Mathf.MoveTowards(AdsT,ads?1:0,dt/w.adsTimeSec);
            if(moving&&!isMoving)stoppedAt=now;if(ads&&!aimed)adsAt=now;moving=isMoving;aimed=ads;
            if(now-lastShot>c.bloomDelay)Bloom=Mathf.MoveTowards(Bloom,0,c.bloomDecay*dt);
            if(now-lastShot>w.recoilRecoveryDelaySec)
            {AimClimb=Vector2.MoveTowards(AimClimb,Vector2.zero,w.recoilRecoveryRateDegPerSec*c.recoveryFraction*dt);}
            if(now-lastShot>c.patternResetDelay && AimClimb.sqrMagnitude<.0001f)ShotIndex=0;
            ViewKick=Spring(ViewKick,ref viewVelocity,c.viewOmega,c.viewDamping,dt);
            GunKick=Spring(GunKick,ref gunVelocity,c.gunOmega,c.gunDamping,dt);
        }
        public static Vector2 Spring(Vector2 x,ref Vector2 velocity,float omega,float damping,float dt)
        {
            // Implicit spring integration stays stable under low FPS and lag spikes.
            float f=1+2*dt*damping*omega,oo=omega*omega,hoo=dt*oo,hhoo=dt*hoo,inv=1/(f+hhoo);
            Vector2 next=(f*x+dt*velocity)*inv;velocity=(velocity-hoo*x)*inv;return next;
        }
        public float Spread(float now,WeaponAsset w,GunplayConfig c,MovementState state,float speed,float landing)
        {
            float extra=speed>0?c.movingBloom:0;
            if(state==MovementState.Sprint||state==MovementState.TacticalSprint)extra=c.sprintBloom;
            if(state==MovementState.Slide)extra=c.slideBloom*Mathf.Clamp01(speed/c.sprint);
            if(state==MovementState.Airborne||state==MovementState.Dive)extra+=c.airborneBloom;
            bool accurate=now-lastShot>c.bloomDelay && (now-stoppedAt<c.firstShotWindow || now-adsAt<c.firstShotWindow) && state!=MovementState.Airborne && state!=MovementState.Dive;
            float hip=Mathf.Clamp(w.hipSpreadDeg.x+extra+Bloom+landing*c.airborneBloom,w.hipSpreadDeg.x,w.hipSpreadDeg.y);
            return accurate?w.adsSpreadDeg:Mathf.Lerp(hip,w.adsSpreadDeg,Mathf.SmoothStep(0,1,AdsT));
        }
        public bool CanShoot(float now)=>now>=NextShot;
        public void Schedule(float now,WeaponAsset w)
        {
            float interval=60/w.rpm;NextShot=NextShot<now-interval?now+interval:NextShot+interval;
        }
        public Vector2 Pattern(WeaponAsset w,int index)
        {
            if(w.recoilPattern==null||w.recoilPattern.Length==0)return Vector2.zero;
            var p=w.recoilPattern[index%w.recoilPattern.Length];
            if((index/w.recoilPattern.Length)%2==1)p.y=-p.y;
            return p;
        }
        public Vector2 Shot(float now,WeaponAsset w,GunplayConfig c,bool ads,bool mounted)
        {
            Vector2 impulse=Pattern(w,ShotIndex)*(1+(Noise()*2-1)*c.recoilNoise)*w.adsRecoilMult;
            if(mounted)impulse*=c.mountRecoil;
            // Hip has visual kick but no authored-pattern displacement of aim.
            if(ads)AimClimb+=impulse;
            ViewKick+=impulse*w.viewKickMult;
            GunKick+=impulse*w.gunKickMult*(mounted?c.mountGunKick:1);
            Bloom=Mathf.Min(w.bloomCap,Bloom+w.bloomPerShot);lastShot=now;ShotIndex++;return ads?impulse:Vector2.zero;
        }
        public Vector3 ShotDirection(Transform view,float spread)
        {
            float radius=Mathf.Sqrt(Noise())*Mathf.Tan(spread*Mathf.Deg2Rad),angle=Noise()*Mathf.PI*2;
            return (view.forward+view.right*(Mathf.Cos(angle)*radius)+view.up*(Mathf.Sin(angle)*radius)).normalized;
        }
    }
}
