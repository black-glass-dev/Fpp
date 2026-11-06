using System;
using UnityEngine;
namespace Fpp.Editor
{
    public static class SecondaryWeaponAudit
    {
        public static void Validate(WeaponController weapons,WeaponPickup[] pickups)
        {
            var rest=new Vector3(.03f,-.01f,-.10f);
            if(Vector3.Distance(WeaponPresentation.SupportTarget(0,rest,false),rest)>.0001f ||
               Vector3.Distance(WeaponPresentation.SupportTarget(1,rest,false),rest)>.0001f)
                throw new Exception("Reload hand must start and finish on the grip");
            if(Vector3.Distance(WeaponPresentation.SupportTarget(.42f,rest,false),rest)<.1f)
                throw new Exception("Reload must move the support hand through magazine handling");
            for(int i=0;i<=100;i++)
            {
                float t=i/100f;var pose=WeaponPresentation.Sample(false,true,t);
                if(pose.offset.y>.0001f || pose.euler.z>.0001f || pose.euler.z < -12.01f)
                    throw new Exception("Secondary reload escaped its single controlled motion");
                var original=WeaponPresentation.Sample(false,false,t);
                if(original.offset!=Vector3.zero || original.euler!=Vector3.zero)
                    throw new Exception("Procedural reload must not override authored Mark23 animation");
            }
            foreach(var pickup in pickups)
            {
                foreach(var renderer in pickup.GetComponentsInChildren<Renderer>())
                foreach(var mat in renderer.sharedMaterials)
                    if(mat.shader.name!="FPP/Mobile Scan" || mat.mainTexture==null || mat.GetFloat("_TrimBelow")>-1000)
                        throw new Exception("Floor weapon must use opaque two-sided untrimmed textured material");
                weapons.ReviewAmmoAudit(pickup);
            }
            weapons.ReviewAmmoAudit(null);weapons.ReviewPose(null,false,0);
            Debug.Log("FPP secondary reload audit passed: three guns, ammo timing/fire blocking, magazine hand path; original sidearm motion untouched");
        }
    }
}
