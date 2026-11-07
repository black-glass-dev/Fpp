using UnityEngine;
namespace Fpp
{
    public static class Ballistics
    {
        public static Vector3 Fire(Vector3 origin,Vector3 direction,WeaponAsset weapon,ref int hits)
        {
            var c=GunplayConfig.Current;
            if(!Physics.Raycast(origin,direction,out var hit,c.rayDistance,~(1<<8),QueryTriggerInteraction.Ignore))return origin+direction*c.rayDistance;
            Resolve(hit,weapon,weapon.Damage(hit.distance),ref hits);
            var surface=hit.collider.GetComponentInParent<SurfaceProfile>();
            if(surface!=null && surface.penetration!=PenetrationClass.None && weapon.penetrationClass!=PenetrationClass.None)
            {
                // Reverse-ray the same collider to find a real exit, not an arbitrary second ray through thick walls.
                var beyond=hit.point+direction*surface.maxThickness;
                if(hit.collider.Raycast(new Ray(beyond,-direction),out var exit,surface.maxThickness) && Vector3.Dot(exit.normal,direction)>0)
                {
                    Impact(exit,weapon);
                    if(Physics.Raycast(exit.point+direction*c.nearClip,direction,out var second,Mathf.Max(0,c.rayDistance-hit.distance),~(1<<8),QueryTriggerInteraction.Ignore))
                        Resolve(second,weapon,weapon.Damage(hit.distance+second.distance)*surface.damageRetained,ref hits);
                }
            }
            return hit.point;
        }
        private static void Resolve(RaycastHit hit,WeaponAsset weapon,float damage,ref int hits)
        {
            Impact(hit,weapon);var part=hit.collider.GetComponent<TargetPart>();
            var target=part!=null?part.target:hit.collider.GetComponentInParent<TestTarget>();
            if(target==null||target.Friendly||target.Health<=0)return;
            if(target.Damage(damage,part!=null?part.part:HitPart.Chest,hit.point,weapon))hits++;
        }
        private static void Impact(RaycastHit hit,WeaponAsset weapon)
        {
            GunplayEvents.Emit(new GunplayEvent{Kind=GunplayEventKind.Impact,Address="Impact",Position=hit.point,Normal=hit.normal,Weapon=weapon,Context=SurfaceProfile.Name(hit.collider)});
        }
    }
}
