using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class SecondaryWeaponRigBuilder
    {
        public static void Build(GameObject root,string name)
        {
            Directory.CreateDirectory("Assets/Generated/WeaponRig");AssetDatabase.Refresh();
            var rig=root.AddComponent<WeaponRigParts>();rig.rifle=name.Contains("AK");
            var mag=new GameObject("Animated magazine").transform;mag.SetParent(root.transform,false);rig.magazine=mag;
            var action=new GameObject("Animated action").transform;action.SetParent(root.transform,false);rig.action=action;
            var filters=root.GetComponentsInChildren<MeshFilter>();
            if(rig.rifle)
            {
                foreach(var filter in filters)
                {
                    Transform part=filter.transform;
                    while(part.parent!=null && part.parent!=root.transform && !part.name.StartsWith("Part_"))part=part.parent;
                    if(part.name.StartsWith("Part_001") || part.name.StartsWith("Part_003") || part.name.StartsWith("Part_004"))part.SetParent(mag,true);
                    else if(part.name.StartsWith("Part_021"))part.SetParent(action,true);
                }
            }
            else
            {
                Bounds bounds=WeaponController.LocalBounds(root.transform);
                float cut=bounds.min.y+bounds.size.y*.20f,slide=bounds.min.y+bounds.size.y*.69f;
                int index=0;
                foreach(var filter in filters)
                {
                    var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
                    var buckets=new[]{new List<int>(),new List<int>(),new List<int>()};
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        Vector3 center=root.transform.InverseTransformPoint(filter.transform.TransformPoint((vertices[triangles[i]]+vertices[triangles[i+1]]+vertices[triangles[i+2]])/3));
                        int group=center.y<cut?1:center.y>slide?2:0;
                        for(int k=0;k<3;k++)buckets[group].Add(triangles[i+k]);
                    }
                    var material=filter.GetComponent<Renderer>().sharedMaterial;
                    for(int group=0;group<3;group++)
                    {
                        if(buckets[group].Count==0)continue;
                        var piece=new GameObject(new[]{"Frame","Magazine base","Slide"}[group],typeof(MeshFilter),typeof(MeshRenderer));
                        piece.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);piece.transform.localScale=filter.transform.lossyScale;
                        piece.transform.SetParent(group==1?mag:group==2?action:root.transform,true);
                        var split=Subset(mesh,buckets[group]);
                        string folder="Assets/Generated/WeaponRig";
                        string path=folder+"/"+name.Replace(' ','_')+"_"+index+"_"+group+".asset";
                        AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(split,path);
                        piece.GetComponent<MeshFilter>().sharedMesh=split;piece.GetComponent<MeshRenderer>().sharedMaterial=material;
                        piece.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    UnityEngine.Object.DestroyImmediate(filter.gameObject);index++;
                }
                var baseBounds=WorldBounds(mag.gameObject);
                // The source is a fused static gun: extend the extracted base upward inside
                // its grip so a full magazine is visible only while it is withdrawn.
                var extension=GameObject.CreatePrimitive(PrimitiveType.Cube);extension.name="Magazine internal body";
                extension.transform.position=new Vector3(baseBounds.center.x,baseBounds.max.y+bounds.size.y*.16f,baseBounds.center.z);
                extension.transform.localScale=new Vector3(Mathf.Max(.025f,baseBounds.size.x*.57f),bounds.size.y*.32f,Mathf.Max(.025f,baseBounds.size.z*.65f));
                extension.transform.SetParent(mag,true);UnityEngine.Object.DestroyImmediate(extension.GetComponent<Collider>());
                var metal=new Material(Shader.Find("Standard"));metal.color=new Color(.09f,.095f,.1f);metal.SetFloat("_Metallic",.3f);
                string matPath="Assets/Generated/WeaponRig/MagazineMetal.mat";
                var existing=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(existing==null){AssetDatabase.CreateAsset(metal,matPath);existing=metal;}else UnityEngine.Object.DestroyImmediate(metal);
                extension.GetComponent<Renderer>().sharedMaterial=existing;
            }
            CenterPivot(mag);CenterPivot(action);
            if(mag.childCount==0 || action.childCount==0)throw new Exception("Weapon rig parts missing: "+name);
        }
        private static Mesh Subset(Mesh source,List<int> indices)
        {
            var old=source.vertices;var normals=source.normals;var uv=source.uv;
            var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<Vector2>();var faces=new List<int>();
            foreach(int id in indices)
            {
                if(!map.TryGetValue(id,out int next)){next=v.Count;map[id]=next;v.Add(old[id]);n.Add(normals.Length==old.Length?normals[id]:Vector3.up);t.Add(uv.Length==old.Length?uv[id]:Vector2.zero);}
                faces.Add(next);
            }
            var mesh=new Mesh();if(v.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,t);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();return mesh;
        }
        private static Bounds WorldBounds(GameObject root)
        {var rs=root.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        private static void CenterPivot(Transform root)
        {
            if(root.childCount==0)return;var children=new List<Transform>();foreach(Transform child in root)children.Add(child);
            Vector3 center=WorldBounds(root.gameObject).center;foreach(var child in children)child.SetParent(null,true);root.position=center;foreach(var child in children)child.SetParent(root,true);
        }
    }
}
