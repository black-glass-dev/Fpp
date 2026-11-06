using UnityEngine;

namespace Fpp
{
    // Deterministic single reload cycle: lower, handle magazine, rack, return. No sine sway.
    public static class WeaponPresentation
    {
        public struct Pose { public Vector3 offset, euler; }
        public static Pose Sample(bool aim,bool secondary,float progress,float secondaryAimLift=.08f)
        {
            var pose=new Pose {offset=aim?new Vector3(-.115f,secondary?secondaryAimLift:.036f,.03f):Vector3.zero};
            if(!secondary || progress<=0 || progress>=1)return pose;
            float lower=Ease(progress/.18f)*(1-Ease((progress-.72f)/.28f));
            pose.offset+=new Vector3(0,-.12f,-.025f)*lower;
            pose.euler=new Vector3(18,-8,-12)*lower;return pose;
        }
        public static Vector3 SupportTarget(float t,Vector3 rest,bool rifle)
        {
            Vector3 well=new Vector3(0,rifle?-.15f:-.09f,-.07f);
            Vector3 removed=well+new Vector3(-.04f,-.17f,-.025f);
            Vector3 rack=new Vector3(.015f,.065f,-.055f);
            if(t<.18f)return Vector3.Lerp(rest,well,Ease(t/.18f));
            if(t<.42f)return Vector3.Lerp(well,removed,Ease((t-.18f)/.24f));
            if(t<.68f)return Vector3.Lerp(removed,well,Ease((t-.42f)/.26f));
            if(t<.84f)return Vector3.Lerp(well,rack,Ease((t-.68f)/.16f));
            return Vector3.Lerp(rack,rest,Ease((t-.84f)/.16f));
        }
        private static float Ease(float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(x));
    }
}
