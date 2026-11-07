using UnityEngine;
namespace Fpp
{
    [CreateAssetMenu(menuName="FPP/Gunplay Config")]
    public sealed class GunplayConfig : ScriptableObject
    {
        public float joystickDeadZone=.18f, joystickExponent=1.4f, lookSensitivity=120, mouseSensitivity=2;
        public float horizontalSensitivity=1, verticalSensitivity=1, adsSensitivity=1, gyroSensitivity=.5f;
        public float doubleTapWindow=.25f, crouchHold=.25f;
        public float walk=4.2f, sprint=6.9f, tacticalSprint=8.5f, tacticalAfter=1.2f, crouch=2.1f, prone=1.2f;
        public float movementBlend=.14f, slideMultiplier=1.35f, slideDuration=.9f, diveImpulse=6.5f;
        public float jumpVelocity=4.6f, gravity=22, jumpCooldown=.2f, groundedVelocity=-2;
        public float standingHeight=1.8f, crouchHeight=1.15f, proneHeight=.55f, eyeInset=.16f;
        public float mantleMin=.9f, mantleMax=1.9f, mantleProbe=.65f, mantleDuration=.7f, mantleClearance=.1f;
        public float mountDistance=.55f, mountRecoil=.12f, mountGunKick=.2f;
        public float walkCadence=1.9f, sprintCadence=2.6f, landingThreshold=2, landingPenalty=.25f;
        public float worldFOV=85, viewmodelFOV=60, minFOV=60, maxFOV=120, nearClip=.03f;
        public float viewOmega=28, viewDamping=.9f, gunOmega=34, gunDamping=.9f, swayOmega=12;
        public float bobAmplitude=.012f, adsBobMultiplier=.2f, breathHz=.13f, breathDegrees=.04f;
        public float movementLag=.02f, sprintCant=35, sprintRaise=.06f, collisionDistance=.55f, collisionTilt=35;
        public float recoveryFraction=.85f, recoilNoise=.08f;
        public float movingBloom=2.2f, sprintBloom=6, slideBloom=3, airborneBloom=5;
        public float bloomDecay=6, bloomDelay=.12f, firstShotWindow=.1f, rayDistance=300;
        public float hitmarkerSeconds=.09f, hitmarkerPunch=1.3f, damageNumberSeconds=.65f;
        public float headMultiplier=1.35f, chestMultiplier=1, stomachMultiplier=.92f, armMultiplier=.8f, legMultiplier=.75f;
        public float pickupRadius=1.6f, reloadDebounce=.25f, magOutMarker=.25f, magInMarker=.65f, boltMarker=.9f;
        public float shellLife=2, impactLife=.22f, shellSpeed=2, fallbackClickVolume=.12f;
        public int targetFPS=60, baseWidth=1920, baseHeight=1080, resolutionSampleFrames=60;
        public float minResolutionScale=.6f, maxResolutionScale=1, resolutionStep=.05f, resolutionHysteresis=1.12f;
        public bool dynamicResolution=true;
        public static GunplayConfig Current { get { if(current==null)current=Resources.Load<GunplayConfig>("Gunplay/Config"); if(current==null)throw new System.InvalidOperationException("Missing Resources/Gunplay/Config");return current; } }
        private static GunplayConfig current;
    }
}
