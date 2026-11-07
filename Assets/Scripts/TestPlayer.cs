using UnityEngine;
using Unity.Profiling;
namespace Fpp
{
    public enum MovementState { Idle, Walk, Sprint, TacticalSprint, Crouch, Prone, Slide, Dive, Airborne, Mantle, Mount, Dead }
    [DefaultExecutionOrder(75),RequireComponent(typeof(CharacterController),typeof(GameplayInput))]
    public sealed class TestPlayer : MonoBehaviour
    {
        public Transform view;
        public MovementState State {get;private set;}
        public float Speed {get;private set;}
        public float LandingPenalty {get;private set;}
        public bool MountAvailable {get;private set;}
        public float SprintExitedAt {get;private set;}=-100;
        public bool IsSprinting=>State==MovementState.Sprint||State==MovementState.TacticalSprint;
        public bool BlocksFire=>State==MovementState.Dive||State==MovementState.Mantle||State==MovementState.Dead;
        public bool Mounted=>State==MovementState.Mount;
        public Vector3 LookEuler=>new Vector3(pitch,0,0);
        private CharacterController controller;private WeaponController weapons;
        private float pitch,falling,sprintTime,crouchSince,jumpAt=-100,stateAt,slideEntry,fallPeak,stepPhase;
        private Vector3 momentum,mantleStart,mantleEnd,eyeHome;
        private bool crouched,prone,tacticalRequested,wasGrounded;
        private void Awake(){controller=GetComponent<CharacterController>();weapons=GetComponent<WeaponController>();eyeHome=view!=null?view.localPosition:Vector3.zero;}
        public void CancelSprint(){if(IsSprinting){SprintExitedAt=Time.time;SetState(MovementState.Walk);}sprintTime=0;tacticalRequested=false;}
        public void SetState(MovementState next)
        {
            if(State==next)return;
            if(IsSprinting&&next!=MovementState.Sprint&&next!=MovementState.TacticalSprint)SprintExitedAt=Time.time;
            State=next;stateAt=Time.time;
            GunplayEvents.Send(GunplayEventKind.StateChanged,transform.position,null,next.ToString());
            if(next==MovementState.Mount)GunplayEvents.Send(GunplayEventKind.Mount,transform.position);
            if(next==MovementState.Slide)GunplayEvents.Send(GunplayEventKind.Slide,transform.position);
            if(next==MovementState.Dive)GunplayEvents.Send(GunplayEventKind.Dive,transform.position);
        }
        private static readonly ProfilerMarker UpdateMarker=new ProfilerMarker("FPP.Movement");
        private void Update(){using(UpdateMarker.Auto())TickGameplay();}
        private void TickGameplay()
        {
            var c=GunplayConfig.Current;var hub=GameplayInput.Instance;if(hub==null||view==null)return;
            if(WeaponController.MenuOpen){momentum=Vector3.zero;return;}
            var input=hub.Frame;float dt=Time.deltaTime,now=Time.time;
            var look=hub.Look(weapons!=null?weapons.AdsT:0,weapons!=null?weapons.ActiveWeapon.sightFOV:c.worldFOV);
            if(State!=MovementState.Dead){transform.Rotate(0,look.x,0);pitch=Mathf.Clamp(pitch-look.y,-c.pitchLimit,c.pitchLimit);}
            bool grounded=controller.isGrounded;
            if(!grounded)fallPeak=Mathf.Max(fallPeak,transform.position.y);
            if(grounded&&!wasGrounded)
            {
                float height=Mathf.Max(0,fallPeak-transform.position.y);
                if(height>c.landingThreshold)LandingPenalty=1;
                GunplayEvents.Send(GunplayEventKind.LandImpact,transform.position,weapons!=null?weapons.ActiveWeapon:null,null,height);
                if(State==MovementState.Dive){prone=true;crouched=false;SetState(MovementState.Prone);}
            }
            if(grounded)fallPeak=transform.position.y;
            wasGrounded=grounded;LandingPenalty=Mathf.MoveTowards(LandingPenalty,0,dt/c.landingPenalty);
            if(State==MovementState.Dead)return;
            if(State==MovementState.Mantle){TickMantle(c,dt);return;}
            if(State==MovementState.Mount)
            {if(input.Move.sqrMagnitude>0||input.Jump||!input.ADS)SetState(MovementState.Walk);else{Speed=0;UpdateEye(c,dt);return;}}
            MountAvailable=input.ADS&&Physics.SphereCast(view.position-Vector3.up*controller.radius,controller.radius*.5f,view.forward,out _,c.mountDistance,~(1<<8),QueryTriggerInteraction.Ignore);
            if(input.Mount&&MountAvailable&&weapons!=null&&weapons.ActiveWeapon.mountable){SetState(MovementState.Mount);return;}
            bool sprintRequest=(input.Sprint||input.Tactical||tacticalRequested)&&input.Move.y>0 && !input.ADS && !input.Fire && !prone;
            if(input.Tactical)tacticalRequested=true;
            if(input.ADS&&IsSprinting)CancelSprint();
            if(input.CrouchPressed){crouchSince=now;if(!IsSprinting){crouched=!crouched;prone=false;}}
            if(input.Prone){prone=!prone;crouched=false;}
            if(input.CrouchHeld && IsSprinting && now-crouchSince>=c.crouchHold)
            {
                slideEntry=Speed*c.slideMultiplier;
                bool dive=State==MovementState.TacticalSprint;
                momentum=transform.forward*(dive?c.diveImpulse:slideEntry);
                if(dive)falling=c.jumpVelocity*.5f;
                SetState(dive?MovementState.Dive:MovementState.Slide);
            }
            bool special=State==MovementState.Slide||State==MovementState.Dive;
            if(State==MovementState.Slide)
            {
                float t=Mathf.Clamp01((now-stateAt)/c.slideDuration);
                float speed=Mathf.Lerp(slideEntry,c.crouch, (1-Mathf.Exp(-c.slideDecay*t))/(1-Mathf.Exp(-c.slideDecay)));
                momentum=momentum.normalized*speed;
                if(t>=1||input.Jump){crouched=true;SetState(MovementState.Crouch);special=false;}
            }
            if(!special)
            {
                float targetSpeed=c.walk;
                if(!grounded){SetState(MovementState.Airborne);}
                else if(prone){SetState(MovementState.Prone);targetSpeed=c.prone;}
                else if(crouched && !CanStand(c)){SetState(MovementState.Crouch);targetSpeed=c.crouch;}
                else if(crouched){SetState(MovementState.Crouch);targetSpeed=c.crouch;}
                else if(sprintRequest){sprintTime+=dt;SetState(sprintTime>=c.tacticalAfter||tacticalRequested?MovementState.TacticalSprint:MovementState.Sprint);targetSpeed=State==MovementState.TacticalSprint?c.tacticalSprint:c.sprint;}
                else {if(IsSprinting)CancelSprint();sprintTime=0;tacticalRequested=false;SetState(input.Move.sqrMagnitude>0?MovementState.Walk:MovementState.Idle);}
                if(input.ADS&&weapons!=null)targetSpeed=Mathf.Min(targetSpeed,weapons.ActiveWeapon.aimWalkSpeed);
                var target=(transform.right*input.Move.x+transform.forward*input.Move.y)*targetSpeed;
                momentum=Vector3.MoveTowards(momentum,target,c.tacticalSprint/c.movementBlend*dt);
            }
            if(grounded && input.Jump && now-jumpAt>=c.jumpCooldown && CanStand(c))
            {jumpAt=now;falling=c.jumpVelocity;prone=crouched=false;SetState(MovementState.Airborne);}
            if(input.Move.y>0 && grounded && !prone && TryMantle(c)){return;}
            falling=grounded&&falling<=0?c.groundedVelocity:falling-c.gravity*dt;
            controller.Move((momentum+Vector3.up*falling)*dt);Speed=new Vector2(momentum.x,momentum.z).magnitude;
            if(grounded&&Speed>c.prone)
            {
                stepPhase+=dt*(IsSprinting?c.sprintCadence:c.walkCadence);
                if(stepPhase>=1){stepPhase-=1;Physics.Raycast(transform.position+Vector3.up*controller.radius,Vector3.down,out var ground,c.standingHeight,~(1<<8));GunplayEvents.Send(GunplayEventKind.Footstep,transform.position,weapons!=null?weapons.ActiveWeapon:null,SurfaceProfile.Name(ground.collider),Speed);}
            }
            UpdateEye(c,dt);
        }
        private bool CanStand(GunplayConfig c)
        {
            return !Physics.SphereCast(transform.position+Vector3.up*(controller.height-controller.radius),controller.radius*.95f,Vector3.up,out _,Mathf.Max(0,c.standingHeight-controller.height),~(1<<8),QueryTriggerInteraction.Ignore);
        }
        private void UpdateEye(GunplayConfig c,float dt)
        {
            float height=State==MovementState.Prone||State==MovementState.Dive?c.proneHeight:State==MovementState.Crouch||State==MovementState.Slide?c.crouchHeight:c.standingHeight;
            // Do not expand through a ceiling when leaving slide/prone.
            if(height>controller.height && !CanStand(c)){height=c.crouchHeight;crouched=true;}
            controller.height=Mathf.MoveTowards(controller.height,height,c.standingHeight/c.movementBlend*dt);controller.center=Vector3.up*controller.height*.5f;
            eyeHome.y=controller.height-c.eyeInset;
            float phase=stepPhase*Mathf.PI*2,ads=weapons!=null?weapons.AdsT:0;
            var bob=new Vector3(Mathf.Sin(phase),Mathf.Sin(phase*2),0)*c.bobAmplitude*Mathf.Clamp01(Speed/c.walk)*Mathf.Lerp(1,c.adsBobMultiplier,ads);
            view.localPosition=eyeHome+bob-Vector3.up*(LandingPenalty*c.bobAmplitude);
        }
        private bool TryMantle(GunplayConfig c)
        {
            if(!Physics.Raycast(transform.position+Vector3.up*c.mantleMin,transform.forward,out var wall,c.mantleProbe,~(1<<8),QueryTriggerInteraction.Ignore))return false;
            var above=wall.point+transform.forward*(controller.radius+c.mantleClearance)+Vector3.up*(c.mantleMax-c.mantleMin+c.mantleClearance);
            if(!Physics.Raycast(above,Vector3.down,out var top,c.mantleMax-c.mantleMin+c.mantleClearance,~(1<<8),QueryTriggerInteraction.Ignore))return false;
            float h=top.point.y-transform.position.y;if(h<c.mantleMin||h>c.mantleMax)return false;
            Vector3 destination=top.point+Vector3.up*(controller.skinWidth+c.mantleClearance);
            if(Physics.CheckCapsule(destination+Vector3.up*controller.radius,destination+Vector3.up*(c.standingHeight-controller.radius),controller.radius*.95f,~(1<<8),QueryTriggerInteraction.Ignore))return false;
            mantleStart=transform.position;mantleEnd=destination;SetState(MovementState.Mantle);GunplayEvents.Send(GunplayEventKind.Mantle,top.point);return true;
        }
        private void TickMantle(GunplayConfig c,float dt)
        {
            float t=Mathf.Clamp01((Time.time-stateAt)/c.mantleDuration);Vector3 target;
            if(t<.5f)target=Vector3.Lerp(mantleStart,new Vector3(mantleStart.x,mantleEnd.y,mantleStart.z),Mathf.SmoothStep(0,1,t*2));
            else target=Vector3.Lerp(new Vector3(mantleStart.x,mantleEnd.y,mantleStart.z),mantleEnd,Mathf.SmoothStep(0,1,(t-.5f)*2));
            controller.Move(target-transform.position);UpdateEye(c,dt);if(t>=1){falling=0;fallPeak=transform.position.y;SetState(MovementState.Idle);}
        }
    }
}
