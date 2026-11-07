using UnityEngine;

namespace Fpp
{
    // ADS root offsets. Reload motion comes from the supplied authored skeleton.
    public static class WeaponPresentation
    {
        public struct Pose { public Vector3 offset, euler; }
        public static Pose Sample(bool aim,bool secondary,float progress,float secondaryAimLift=.08f)
        {
            var pose=new Pose {offset=aim?new Vector3(-.115f,secondary?secondaryAimLift:.036f,.03f):Vector3.zero};
            // Reload now follows the supplied authored arm/gun-bone clip, not a root sway.
            return pose;
        }
    }
}
