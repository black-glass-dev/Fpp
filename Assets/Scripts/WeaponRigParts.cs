using UnityEngine;
namespace Fpp
{
    // Real mesh parts separated in the editor; no downloaded animation is implied.
    public sealed class WeaponRigParts : MonoBehaviour
    {
        public Transform magazine, action;
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
                magazine.position+=space.TransformVector(new Vector3(-.045f,-(rifle?.14f:.11f),-.025f)*remove);
                magazine.localRotation=magRotation;
                magazine.rotation=Quaternion.AngleAxis(-12*remove,space.forward)*magazine.rotation;
            }
            if(action!=null){action.localPosition=actionHome;action.position-=space.forward*(rifle?.045f:.025f)*rack;}
        }
        public Vector3 HandTarget(float t,Transform space,Vector3 rest)
        {
            Vector3 home=space.TransformPoint(rest);
            if(magazine==null)return home;
            Vector3 grip=magazine.position+space.TransformVector(new Vector3(-.018f,rifle?.025f:.015f,0));
            if(t<.16f)return Vector3.Lerp(home,grip,Ease(t/.16f));
            if(t<.70f)return grip;
            Vector3 handle=action!=null?action.position+space.right*.018f:space.TransformPoint(new Vector3(0,.06f,-.06f));
            if(t<.77f)return Vector3.Lerp(grip,handle,Ease((t-.7f)/.07f));
            if(t<.9f)return handle;
            return Vector3.Lerp(handle,home,Ease((t-.9f)/.1f));
        }
    }
}
