using System;
using System.Collections.Generic;
using UnityEngine;
namespace Fpp.Editor
{
    public static class ViewmodelAnchors
    {
        public static void Build(Transform holder,List<Renderer> guns,Transform gunBone,WeaponController controller,Transform muzzle)
        {
            var points=new List<Vector3>();
            foreach(var renderer in guns)
            {
                var skin=renderer as SkinnedMeshRenderer;
                if(skin==null)continue;
                var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;var bones=skin.bones;
                var matrices=new Matrix4x4[bones.Length];for(int i=0;i<bones.Length;i++)matrices[i]=bones[i].localToWorldMatrix*binds[i];
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 world;
                    if(weights.Length==vertices.Length)
                    {
                        var w=weights[i];world=Vector3.zero;
                        if(w.weight0>0)world+=matrices[w.boneIndex0].MultiplyPoint3x4(vertices[i])*w.weight0;
                        if(w.weight1>0)world+=matrices[w.boneIndex1].MultiplyPoint3x4(vertices[i])*w.weight1;
                        if(w.weight2>0)world+=matrices[w.boneIndex2].MultiplyPoint3x4(vertices[i])*w.weight2;
                        if(w.weight3>0)world+=matrices[w.boneIndex3].MultiplyPoint3x4(vertices[i])*w.weight3;
                    }
                    else world=skin.transform.TransformPoint(vertices[i]);
                    points.Add(holder.InverseTransformPoint(world));
                }
            }
            if(points.Count==0)throw new Exception("No skinned gun vertices for muzzle/sight calibration");
            float front=float.MinValue;foreach(var point in points)front=Mathf.Max(front,point.z);
            if(front<.05f || front>.7f)throw new Exception("Invalid skin-matrix gun anchor range: "+front);
            Vector3 bore=Vector3.zero;int count=0;float top=float.MinValue;
            foreach(var point in points){if(point.z>front-.008f){bore+=point;count++;}}
            bore/=Mathf.Max(1,count);bore.z=front+.006f;
            foreach(var point in points)if(point.z>front*.45f && Mathf.Abs(point.x-bore.x)<.006f)top=Mathf.Max(top,point.y);
            Vector3 sight=Vector3.zero;count=0;foreach(var point in points)if(point.z>front*.45f && Mathf.Abs(point.x-bore.x)<.006f && point.y>top-.001f){sight+=point;count++;}
            if(count==0)throw new Exception("Missing authored pistol front sight vertices");
            sight/=count;
            var anchor=new GameObject("Authored pistol front sight").transform;anchor.position=holder.TransformPoint(sight);anchor.SetParent(gunBone,true);controller.defaultSight=anchor;
            muzzle.position=holder.TransformPoint(bore);muzzle.rotation=holder.rotation;muzzle.SetParent(gunBone,true);
            Debug.Log("FPP skin-matrix anchors: muzzle="+bore+" sight="+sight);
        }
    }
}
