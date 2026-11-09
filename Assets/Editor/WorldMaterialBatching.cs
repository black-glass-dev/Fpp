using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fpp.Editor
{
    // House-local batches retain useful building-scale frustum/occlusion granularity.
    // Never merge the entire map; never change materials, collision, FPV or transparent sorting.
    public static class WorldMaterialBatching
    {
        private static int batchId,before,after;private static readonly Dictionary<Mesh,Mesh> Copies=new Dictionary<Mesh,Mesh>();
        public static void Build()
        {
            batchId=before=after=0;Copies.Clear();var district=GameObject.Find("Bangladesh compact para (120 x 120m)");
            try
            {
                foreach(Transform child in district.transform)
                {
                    if(child.name.IndexOf("house",StringComparison.OrdinalIgnoreCase)<0)continue;
                    var group=child.GetComponent<LODGroup>();
                    if(group!=null)
                    {
                        var old=new HashSet<Renderer>();var levels=group.GetLODs();
                        for(int i=0;i<levels.Length;i++){foreach(var r in levels[i].renderers)old.Add(r);levels[i].renderers=Combine(child,levels[i].renderers,i);}
                        group.SetLODs(levels);var retained=new HashSet<Renderer>();foreach(var level in levels)foreach(var r in level.renderers)retained.Add(r);
                        foreach(var r in old)if(!retained.Contains(r))r.enabled=false;group.RecalculateBounds();group.ForceLOD(-1);
                    }
                    else
                    {
                        var active=new List<Renderer>();foreach(var r in child.GetComponentsInChildren<MeshRenderer>())if(r.enabled)active.Add(r);
                        var merged=Combine(child,active.ToArray(),0);var retained=new HashSet<Renderer>(merged);foreach(var r in active)if(!retained.Contains(r))r.enabled=false;
                    }
                }
            }
            finally{foreach(var copy in Copies.Values)UnityEngine.Object.DestroyImmediate(copy);Copies.Clear();}
            File.WriteAllText("QA/runtime/house-material-batching.txt","House-local opaque submesh submissions across authored LOD levels before="+before+"; after="+after+". Not camera draw calls/FPS. Original collision/material references preserved; source-only renderers disabled, not removed.");
            AssetDatabase.SaveAssets();
        }
        private static Renderer[] Combine(Transform root,Renderer[] renderers,int level)
        {
            var output=new List<Renderer>();var buckets=new Dictionary<Material,List<CombineInstance>>();var exemplars=new Dictionary<Material,MeshRenderer>();
            foreach(var r in renderers)
            {
                var renderer=r as MeshRenderer;var filter=r.GetComponent<MeshFilter>();var materials=r.sharedMaterials;
                bool eligible=renderer!=null&&filter!=null&&filter.sharedMesh!=null&&r.gameObject.layer!=8&&r.lightmapIndex<0;
                foreach(var mat in materials)if(mat==null||mat.renderQueue>=3000)eligible=false;
                if(!eligible){output.Add(r);continue;}
                var mesh=filter.sharedMesh;if(materials.Length!=mesh.subMeshCount){output.Add(r);continue;}
                before+=materials.Length;
                if(!mesh.isReadable){if(!Copies.TryGetValue(mesh,out var copy)){copy=WorldDetailLod.ReadableCopy(mesh);Copies.Add(mesh,copy);}mesh=copy;}
                for(int sub=0;sub<materials.Length;sub++)
                {
                    var mat=materials[sub];if(!buckets.TryGetValue(mat,out var list)){list=new List<CombineInstance>();buckets.Add(mat,list);exemplars.Add(mat,renderer);}
                    list.Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                }
            }
            foreach(var pair in buckets)
            {
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name="House-local geometry batch"};mesh.CombineMeshes(pair.Value.ToArray(),true,true,false);mesh.RecalculateBounds();
                string path="Assets/Generated/Village/Batch_"+(batchId++)+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject("Batched geometry level "+level,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();var source=exemplars[pair.Key];renderer.sharedMaterial=pair.Key;renderer.shadowCastingMode=source.shadowCastingMode;renderer.receiveShadows=source.receiveShadows;
                renderer.lightProbeUsage=source.lightProbeUsage;renderer.reflectionProbeUsage=source.reflectionProbeUsage;renderer.allowOcclusionWhenDynamic=source.allowOcclusionWhenDynamic;
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);output.Add(renderer);after++;
            }
            return output.ToArray();
        }
    }
}
