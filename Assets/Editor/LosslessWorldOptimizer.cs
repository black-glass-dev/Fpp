using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class LosslessWorldOptimizer
    {
        [Serializable] private class Report
        {
            public string status="Structural editor audit, NOT measured Android FPS. No mesh simplification, texture resizing, lighting/MSAA/shadow/resolution reduction or distance LOD applied.";
            public int renderers,uniqueMeshes,sharedMeshInstances,instancingEligibleRenderers,deduplicatedTextureReferences;
            public long visibleTriangleInstances,uniqueMeshTriangles,editorTextureObjectBytesBefore,editorTextureObjectBytesAfter;
        }
        public static void BakeVisibility()
        {
            var area=new GameObject("Compact para conservative visibility bake").AddComponent<OcclusionArea>();
            area.center=new Vector3(0,10,0);area.size=new Vector3(120,40,120);
            StaticOcclusionCulling.smallestOccluder=2;StaticOcclusionCulling.smallestHole=.25f;StaticOcclusionCulling.backfaceThreshold=100;
            Directory.CreateDirectory("QA/runtime");File.AppendAllText("QA/runtime/build-progress.txt","VISIBILITY BAKE START\n");
            bool baked=StaticOcclusionCulling.Compute();
            File.AppendAllText("QA/runtime/build-progress.txt","VISIBILITY BAKE END "+baked+"\n");
            if(!baked)throw new Exception("Conservative static visibility bake failed; do not claim baked occlusion");
            Directory.CreateDirectory("QA/runtime");File.WriteAllText("QA/runtime/visibility-bake.txt","Unity static occlusion bake completed. Full-resolution source geometry retained; smallest hole.25m; trees are not static occluders. FPV remains excluded from world occlusion. Actual device frame-time improvement remains unmeasured.");
        }
        public static void Apply()
        {
            var report=new Report();var meshes=new HashSet<Mesh>();var before=new HashSet<Texture>();var after=new HashSet<Texture>();
            var textures=new Dictionary<string,Texture>();var copies=new Dictionary<Material,Material>();
            var counts=new Dictionary<Mesh,int>();var renderers=UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled||renderer.gameObject.layer==8)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var mesh=filter.sharedMesh;if(!counts.ContainsKey(mesh))counts[mesh]=0;counts[mesh]++;
            }
            const string folder="Assets/Generated/Village/SharedMaterials";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled||renderer.gameObject.layer==8)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var mesh=filter.sharedMesh;report.renderers++;long triangles=0;
                for(int sub=0;sub<mesh.subMeshCount;sub++)if(mesh.GetTopology(sub)==MeshTopology.Triangles)triangles+=(long)mesh.GetIndexCount(sub)/3;
                report.visibleTriangleInstances+=triangles;
                if(meshes.Add(mesh))report.uniqueMeshTriangles+=triangles;else report.sharedMeshInstances++;
                var materials=renderer.sharedMaterials;bool eligible=counts[mesh]>1;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];if(source==null)continue;
                    foreach(var property in source.GetTexturePropertyNames()){var texture=source.GetTexture(property);if(texture!=null)before.Add(texture);}
                    Material copy;
                    if(!copies.TryGetValue(source,out copy))
                    {
                        copy=new Material(source);copies.Add(source,copy);
                        foreach(var property in source.GetTexturePropertyNames())
                        {
                            var texture=source.GetTexture(property);if(texture==null)continue;
                            var image=texture as Texture2D;if(image==null)continue;
                            var hash=image.imageContentsHash;
                            // An unset hash is not identity evidence. Never merge by filename/size alone.
                            if(!hash.isValid)continue;
                            string key=hash+"/"+texture.width+"/"+texture.height+"/"+texture.graphicsFormat+"/"+image.mipmapCount+"/"+texture.filterMode+"/"+texture.wrapModeU+"/"+texture.wrapModeV+"/"+texture.wrapModeW+"/"+texture.anisoLevel+"/"+texture.mipMapBias;
                            Texture canonical;if(textures.TryGetValue(key,out canonical))
                            {if(canonical!=texture){copy.SetTexture(property,canonical);report.deduplicatedTextureReferences++;}}
                            else textures.Add(key,texture);
                        }
                        copy.enableInstancing=source.shader.name.StartsWith("Shader Graphs/glTF",StringComparison.Ordinal)||source.shader.name=="Universal Render Pipeline/Lit";
                        string path=folder+"/Material_"+copies.Count.ToString("000")+".mat";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(copy,path);
                    }
                    materials[i]=copy;eligible&=copy.enableInstancing;
                    foreach(var property in copy.GetTexturePropertyNames()){var texture=copy.GetTexture(property);if(texture!=null)after.Add(texture);}
                }
                renderer.sharedMaterials=materials;
                if(eligible)
                {
                    // Preserve shared vertex buffers for repeats instead of baking an expanded copy per tree.
                    // SRP Batcher may still take priority; do not claim draw-call savings without profiling.
                    var flags=GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,flags&~StaticEditorFlags.BatchingStatic);
                    report.instancingEligibleRenderers++;
                }
            }
            report.uniqueMeshes=meshes.Count;
            foreach(var texture in before)report.editorTextureObjectBytesBefore+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            foreach(var texture in after)report.editorTextureObjectBytesAfter+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            Directory.CreateDirectory("QA/runtime");File.WriteAllText("QA/runtime/lossless-world-audit.json",JsonUtility.ToJson(report,true));
        }
    }
}
