using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fpp.Editor
{
    // Tile the actual cleaned scan, NOT a replacement texture. Clip intersections once,
    // preserve metre-scale brick UVs, merge into local chunks, keep texture references.
    public static class CleanRoadNetwork
    {
        [Serializable] private class Layout {public Lane[] lanes;}
        [Serializable] private class Lane {public string name;public float x,z,width,length;}
        private struct V {public Vector3 p,n;public Vector2 uv;public static V Lerp(V a,V b,float t){return new V{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}}
        private sealed class Chunk {public List<Vector3> p=new List<Vector3>(),n=new List<Vector3>();public List<Vector2> uv=new List<Vector2>();public List<int> ix=new List<int>();}
        public static void Build()
        {
            var model=GameObject.Find("Road(ready to use)");if(model==null)throw new Exception("Missing cleaned scan source");
            var filters=model.GetComponentsInChildren<MeshFilter>();if(filters.Length==0)throw new Exception("Missing cleaned core mesh");
            var filter=filters[0];var reduced=WorldDetailLod.Derivative(filter.sharedMesh,0,800);
            if(WorldDetailLod.Triangles(reduced)>2000)throw new Exception("Network scan exceeds per-core geometry budget: "+WorldDetailLod.Triangles(reduced)+" from "+WorldDetailLod.Triangles(filter.sharedMesh)+"; do not replicate heavy scan");
            var mesh=WorldDetailLod.ReadableCopy(reduced);var points=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var indices=mesh.triangles;
            if(uv.Length!=points.Length||normals.Length!=points.Length)throw new Exception("Road scan lost normals/UV");
            var b=new Bounds(filter.transform.TransformPoint(points[0]),Vector3.zero);
            for(int i=0;i<points.Length;i++){points[i]=filter.transform.TransformPoint(points[i]);b.Encapsulate(points[i]);normals[i]=filter.transform.TransformDirection(normals[i]).normalized;}
            float w=b.size.x,d=b.size.z;if(w<1||d<1)throw new Exception("Invalid scan dimensions");
            for(int i=0;i<points.Length;i++)points[i]-=new Vector3(b.min.x,b.min.y-.035f,b.min.z);
            var lanes=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/VillageLayout.json")).lanes;
            var chunks=new Dictionary<Vector2Int,Chunk>();int rectangles=0;
            for(int tz=Mathf.FloorToInt(-60/d);tz*d<60;tz++)for(int tx=Mathf.FloorToInt(-60/w);tx*w<60;tx++)
            {
                float x=tx*w,z=tz*d;var xs=new SortedSet<float>{x,x+w};var zs=new SortedSet<float>{z,z+d};bool hit=false;
                foreach(var lane in lanes)
                {
                    float xmin=lane.x-lane.width/2,xmax=lane.x+lane.width/2,zmin=lane.z-lane.length/2,zmax=lane.z+lane.length/2;
                    if(xmax<=x||xmin>=x+w||zmax<=z||zmin>=z+d)continue;hit=true;
                    if(xmin>x&&xmin<x+w)xs.Add(xmin);if(xmax>x&&xmax<x+w)xs.Add(xmax);
                    if(zmin>z&&zmin<z+d)zs.Add(zmin);if(zmax>z&&zmax<z+d)zs.Add(zmax);
                }
                if(!hit)continue;var xx=new List<float>(xs);var zz=new List<float>(zs);
                for(int zi=0;zi<zz.Count-1;zi++)for(int xi=0;xi<xx.Count-1;xi++)
                {
                    float cx=(xx[xi]+xx[xi+1])/2,cz=(zz[zi]+zz[zi+1])/2;bool inside=false;
                    foreach(var lane in lanes)if(Mathf.Abs(cx-lane.x)<lane.width/2&&Mathf.Abs(cz-lane.z)<lane.length/2){inside=true;break;}
                    if(!inside)continue;rectangles++;var key=new Vector2Int(Mathf.FloorToInt(cx/8),Mathf.FloorToInt(cz/8));
                    if(!chunks.TryGetValue(key,out var chunk)){chunk=new Chunk();chunks.Add(key,chunk);}
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var polygon=new List<V>(3);for(int j=0;j<3;j++){int id=indices[i+j];polygon.Add(new V{p=points[id]+new Vector3(x,0,z),n=normals[id],uv=uv[id]});}
                        polygon=Clip(polygon,0,xx[xi],true);polygon=Clip(polygon,0,xx[xi+1],false);
                        polygon=Clip(polygon,2,zz[zi],true);polygon=Clip(polygon,2,zz[zi+1],false);
                        for(int j=1;j<polygon.Count-1;j++)
                        {
                            var a=polygon[0];var bb=polygon[j];var c=polygon[j+1];
                            if(Vector3.Cross(bb.p-a.p,c.p-a.p).sqrMagnitude<1e-14f)continue;
                            foreach(var v in new[]{a,bb,c}){chunk.ix.Add(chunk.p.Count);chunk.p.Add(v.p);chunk.n.Add(v.n);chunk.uv.Add(v.uv);}
                        }
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(mesh);var district=GameObject.Find("Bangladesh compact para (120 x 120m)");long triangles=0;
            foreach(var pair in chunks)
            {
                var c=pair.Value;if(c.ix.Count==0)continue;var combined=new Mesh{indexFormat=IndexFormat.UInt32,name="Cleaned network "+pair.Key};
                combined.SetVertices(c.p);combined.SetNormals(c.n);combined.SetUVs(0,c.uv);combined.SetTriangles(c.ix,0);combined.RecalculateBounds();combined.RecalculateTangents();
                string path="Assets/Generated/Village/Network_"+pair.Key.x+"_"+pair.Key.y+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(combined,path);
                var go=new GameObject(combined.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(district.transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=combined;go.GetComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<MeshRenderer>().sharedMaterials;
                // Ground/legacy lane COLLIDERS remain. The visual scan never adds million-triangle collision copies.
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);triangles+=c.ix.Count/3;
            }
            foreach(var lane in lanes){var old=GameObject.Find(lane.name);if(old!=null){var r=old.GetComponent<Renderer>();if(r!=null)r.enabled=false;}}
            File.WriteAllText("QA/runtime/road-network.txt","Actual cleaned scan clipped over UNION of all "+lanes.Length+" lane/courtyard rectangles; no duplicated intersection surfaces. Chunks="+chunks.Count+"; partition rectangles="+rectangles+"; near triangles="+triangles+"; source textures/UV metre scale retained; original model intact. Render/route acceptance pending.");
            if(triangles>600000)throw new Exception("Network geometry budget exceeded");
        }
        private static List<V> Clip(List<V> input,int axis,float bound,bool greater)
        {
            var output=new List<V>();if(input.Count==0)return output;var a=input[input.Count-1];bool ai=greater?a.p[axis]>=bound:a.p[axis]<=bound;
            foreach(var b in input){bool bi=greater?b.p[axis]>=bound:b.p[axis]<=bound;if(ai!=bi)output.Add(V.Lerp(a,b,(bound-a.p[axis])/(b.p[axis]-a.p[axis])));if(bi)output.Add(b);a=b;ai=bi;}return output;
        }
    }
}
