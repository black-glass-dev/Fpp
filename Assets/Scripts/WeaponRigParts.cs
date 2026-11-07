using UnityEngine;
namespace Fpp
{
    // Separated mesh parts; magazine motion can follow the supplied authored skeleton.
    public sealed class WeaponRigParts : MonoBehaviour
    {
        public Transform magazine, action, sight;
        public bool rifle;
        private Vector3 magHome, actionHome;
        private Quaternion magRotation;
        private bool bound;
        public void Bind()
        {
            if(bound)return;bound=true;
            if(magazine!=null){magHome=magazine.localPosition;magRotation=magazine.localRotation;}
            if(action!=null)actionHome=action.localPosition;
        }
        private static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        public void Pose(float t,Transform space)
        {
            Bind();
            float remove=t>0?Ease((t-.12f)/.23f)*(1-Ease((t-.47f)/.23f)):0;
            float rack=t>0?Ease((t-.73f)/.07f)*(1-Ease((t-.82f)/.08f)):0;
            if(magazine!=null)
            {
                magazine.localPosition=magHome;
                magazine.position+=(space.rotation*(new Vector3(-.045f,-(rifle?.14f:.11f),-.025f)*remove));
                magazine.localRotation=magRotation;
                magazine.rotation=Quaternion.AngleAxis(-12*remove,space.forward)*magazine.rotation;
            }
            if(action!=null){action.localPosition=actionHome;action.position-=space.forward*(rifle?.045f:.025f)*rack;}
        }
        public void RetargetMagazine(Vector3 delta,Quaternion spin)
        {
            Bind();magazine.localPosition=magHome;magazine.localRotation=magRotation;
            magazine.position+=delta*(rifle?1.15f:1f);
            magazine.rotation=spin*magazine.rotation;
        }
    }
}
