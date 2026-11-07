using System;
using UnityEngine;
namespace Fpp.Editor
{
    public static class SecondaryWeaponAudit
    {
        public static void Validate(WeaponController weapons,WeaponPickup[] pickups)
        {
            for(int i=0;i<=100;i++)
            {
                float t=i/100f;var pose=WeaponPresentation.Sample(false,true,t);
                if(pose.offset.y>.061f || pose.euler.z>.0001f || pose.euler.z < -20.01f)
                    throw new Exception("Secondary reload escaped its single controlled motion");
                var original=WeaponPresentation.Sample(false,false,t);
                if(original.offset!=Vector3.zero || original.euler!=Vector3.zero)
                    throw new Exception("Procedural reload must not override authored Mark23 animation");
            }
            foreach(var pickup in pickups)
            {
                foreach(var renderer in pickup.GetComponentsInChildren<Renderer>())
                foreach(var mat in renderer.sharedMaterials)
                    if(renderer.name!="Magazine internal body" && (mat.shader.name!="FPP/Mobile Scan" || mat.mainTexture==null || mat.GetFloat("_TrimBelow")>-1000))
                        throw new Exception("Floor weapon must use opaque two-sided untrimmed textured material");
                var rig=pickup.GetComponent<WeaponRigParts>();
                if(rig==null || rig.magazine==null || rig.action==null)throw new Exception("Missing real animated weapon parts");
                rig.Bind();var home=rig.magazine.position;var actionHome=rig.action.position;
                rig.Pose(.36f,pickup.transform);if(Vector3.Distance(home,rig.magazine.position)<.1f)throw new Exception("Magazine must actually leave its well");
                rig.Pose(.8f,pickup.transform);if(Vector3.Distance(actionHome,rig.action.position)<.02f)throw new Exception("Action must actually rack");
                rig.Pose(1,pickup.transform);if(Vector3.Distance(home,rig.magazine.position)>.0001f || Vector3.Distance(actionHome,rig.action.position)>.0001f)throw new Exception("Weapon parts must return home");
                weapons.ReviewRetargetAudit(pickup);
                weapons.ReviewAmmoAudit(pickup);
            }
            weapons.ReviewAmmoAudit(null);weapons.ReviewPose(null,false,0);
            Debug.Log("FPP secondary reload audit passed: three guns, ammo timing/fire blocking, magazine hand path; original sidearm motion untouched");
        }
    }
}
