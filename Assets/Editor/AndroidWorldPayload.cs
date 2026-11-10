using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;
namespace Fpp.Editor
{
    // Operates on the BUILD scene copy only. Original assets and editor QA scene remain intact.
    public static class AndroidWorldPayload
    {
        [Serializable] private class Receipt {public string status="Build-scene payload audit, NOT phone crash/FPS acceptance. ASTC is lossy high-quality block compression; dimensions/mips retained. Source images unchanged.";public int strippedRenderers,strippedSourceReferences,textures,materials,meshes,colliders;public long textureRgbaEquivalentBytesBefore,textureBytesAfter,collisionTrianglesBefore,collisionTrianglesAfter;}
        private static readonly Dictionary<Mesh,Mesh> Visual=new Dictionary<Mesh,Mesh>(),Collision=new Dictionary<Mesh,Mesh>();
        private static readonly Dictionary<Texture2D,Texture2D> Textures=new Dictionary<Texture2D,Texture2D>();
        private static readonly HashSet<Texture2D> Normals=new HashSet<Texture2D>();
        private static readonly Dictionary<Material,Material> Materials=new Dictionary<Material,Material>();
        private const string Folder="Assets/Generated/AndroidPayload";
        public static void Prepare(Scene scene)
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();Visual.Clear();Collision.Clear();Textures.Clear();Materials.Clear();Normals.Clear();var audit=new Receipt();
            var targets=new List<GameObject>();foreach(var root in scene.GetRootGameObjects())
            {
                // Never remove weapon/pickup renderers dynamically enabled by gameplay.
                if(root.name=="Player"||root.GetComponentInChildren<WeaponPickup>(true)!=null)continue;
                targets.Add(root);
            }
            foreach(var root in targets)foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material!=null)
                foreach(var property in material.GetTexturePropertyNames())if(property.IndexOf("normal",StringComparison.OrdinalIgnoreCase)>=0||property.IndexOf("bump",StringComparison.OrdinalIgnoreCase)>=0){var texture=material.GetTexture(property) as Texture2D;if(texture!=null)Normals.Add(texture);}
            var prefabRoots=new HashSet<GameObject>();foreach(var root in targets)foreach(var t in root.GetComponentsInChildren<Transform>(true)){var prefab=PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);if(prefab!=null)prefabRoots.Add(prefab);}
            foreach(var prefab in prefabRoots)if(PrefabUtility.IsPartOfPrefabInstance(prefab))PrefabUtility.UnpackPrefabInstance(prefab,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var kept=new HashSet<Renderer>();foreach(var root in targets)foreach(var group in root.GetComponentsInChildren<LODGroup>(true))if(group.enabled)foreach(var lod in group.GetLODs())foreach(var renderer in lod.renderers)if(renderer!=null)kept.Add(renderer);
            foreach(var root in targets)
            {
                foreach(var marker in root.GetComponentsInChildren<Fpp.WorldVisualSource>(true)){UnityEngine.Object.DestroyImmediate(marker);audit.strippedSourceReferences++;}
                foreach(var collider in root.GetComponentsInChildren<MeshCollider>(true))
                {
                    if(collider.sharedMesh==null)continue;var input=collider.sharedMesh;
                    audit.collisionTrianglesBefore+=WorldDetailLod.Triangles(input);collider.sharedMesh=ColliderMesh(input);audit.collisionTrianglesAfter+=WorldDetailLod.Triangles(collider.sharedMesh);audit.colliders++;
                }
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(renderer.gameObject.layer==8)continue;
                    if(!renderer.enabled&&!kept.Contains(renderer))
                    {
                        var filter=renderer.GetComponent<MeshFilter>();UnityEngine.Object.DestroyImmediate(renderer);if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);audit.strippedRenderers++;continue;
                    }
                    var sourceFilter=renderer.GetComponent<MeshFilter>();if(sourceFilter!=null&&sourceFilter.sharedMesh!=null)sourceFilter.sharedMesh=VisualMesh(sourceFilter.sharedMesh);
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)if(materials[i]!=null)
                    {
                        var original=materials[i];
                        if(!Materials.TryGetValue(original,out var copy))
                        {
                            copy=new Material(original);
                            foreach(string property in copy.GetTexturePropertyNames())
                            {
                                var image=copy.GetTexture(property) as Texture2D;if(image==null||image.width<128||image.height<128)continue;
                                bool normal=property.IndexOf("normal",StringComparison.OrdinalIgnoreCase)>=0||property.IndexOf("bump",StringComparison.OrdinalIgnoreCase)>=0;
                                copy.SetTexture(property,Texture(image,normal,audit));
                            }
                            Save(copy,"Material_"+Materials.Count);Materials.Add(original,copy);
                        }
                        materials[i]=copy;
                    }
                    renderer.sharedMaterials=materials;
                }
            }
            // No synchronous mesh GPU-buffer mutation during the first gameplay Start callback.
            foreach(var root in scene.GetRootGameObjects())foreach(var residency in root.GetComponentsInChildren<Fpp.WorldLodMeshResidency>(true))UnityEngine.Object.DestroyImmediate(residency);
            audit.materials=Materials.Count;audit.meshes=Visual.Count;audit.textures=Textures.Count;
            if(audit.textures==0||audit.textureBytesAfter>=audit.textureRgbaEquivalentBytesBefore)throw new Exception("Android texture payload did not decrease");
            File.WriteAllText("QA/runtime/android-payload.json",JsonUtility.ToJson(audit,true));AssetDatabase.SaveAssets();
            Visual.Clear();Collision.Clear();Textures.Clear();Materials.Clear();Normals.Clear();
        }
        public static void PruneUnusedResourceMaterials(string payloadScene)
        {
            const string root="Assets/Generated/Resources/Rendering";const string quarantine="Assets/Generated/Editor/UnusedResourceMaterials";
            Directory.CreateDirectory(quarantine);AssetDatabase.Refresh();var used=new HashSet<string>(AssetDatabase.GetDependencies(payloadScene,true));int moved=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{root}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);if(used.Contains(path))continue;
                string error=AssetDatabase.MoveAsset(path,quarantine+"/"+guid+".mat");if(!string.IsNullOrEmpty(error))throw new Exception(error);moved++;
            }
            File.WriteAllText("QA/runtime/android-resource-pruning.txt","Unused generated Resources materials moved to Editor-only quarantine="+moved+"; original image files untouched; GUID references preserved for editor-only scene. APK packed report/size gate remains authoritative.");
        }
        private static void Save(UnityEngine.Object asset,string name)
        {string path=Folder+"/"+name+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(asset,path);}
        private static void CpuReadDisabled(UnityEngine.Object mesh)
        {
            var serialized=new SerializedObject(mesh);var readable=serialized.FindProperty("m_IsReadable");if(readable==null)throw new Exception("Mesh readability serialization contract changed");
            readable.boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(mesh);
            if(mesh is Mesh visual&&visual.isReadable||mesh is Texture2D image&&image.isReadable)throw new Exception("CPU readability flag did not apply: "+mesh.name);
        }
        private static Mesh VisualMesh(Mesh source)
        {
            if(Visual.TryGetValue(source,out var mesh))return mesh;
            mesh=WorldDetailLod.ReadableCopy(source);mesh.name=source.name+" Android visual";Save(mesh,"Visual_"+Visual.Count);CpuReadDisabled(mesh);Visual.Add(source,mesh);return mesh;
        }
        private static Mesh ColliderMesh(Mesh source)
        {
            if(Collision.TryGetValue(source,out var mesh))return mesh;
            var readable=WorldDetailLod.ReadableCopy(source);mesh=new Mesh{indexFormat=source.indexFormat,name=source.name+" position-only collision"};
            mesh.vertices=readable.vertices;var triangles=new List<int>();for(int sub=0;sub<readable.subMeshCount;sub++)
            {if(readable.GetTopology(sub)!=MeshTopology.Triangles)throw new Exception("Unexpected collider topology");triangles.AddRange(readable.GetTriangles(sub));}
            mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();UnityEngine.Object.DestroyImmediate(readable);
            if(triangles.Count/3>12000)
            {
                var simplifier=new MeshSimplifier(mesh);var options=SimplificationOptions.Default;options.PreserveBorderEdges=true;options.ConstrainToEdgeSegment=true;simplifier.SimplificationOptions=options;
                simplifier.SimplifyMesh(4000f/(triangles.Count/3));var reduced=simplifier.ToMesh();
                if(WorldDetailLod.Triangles(reduced)<WorldDetailLod.Triangles(mesh)){UnityEngine.Object.DestroyImmediate(mesh);mesh=reduced;}else UnityEngine.Object.DestroyImmediate(reduced);
                // Preserve actual authored open boundaries, never replace interiors by solid boxes.
            }
            Save(mesh,"Collision_"+Collision.Count);Collision.Add(source,mesh);return mesh;
        }
        private static Texture2D Texture(Texture2D source,bool normal,Receipt audit)
        {
            if(Textures.TryGetValue(source,out var result))return result;
            normal|=Normals.Contains(source);
            bool srgb=GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat),mips=source.mipmapCount>1;
            var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,srgb?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
            var old=RenderTexture.active;bool oldSrgb=GL.sRGBWrite;
            try
            {
                GL.sRGBWrite=srgb;Graphics.Blit(source,rt);RenderTexture.active=rt;
                result=new Texture2D(source.width,source.height,TextureFormat.RGBA32,mips,!srgb){name=source.name+" Android ASTC",wrapModeU=source.wrapModeU,wrapModeV=source.wrapModeV,wrapModeW=source.wrapModeW,filterMode=source.filterMode,anisoLevel=source.anisoLevel,mipMapBias=source.mipMapBias};
                result.ReadPixels(new Rect(0,0,source.width,source.height),0,0);result.Apply(mips,false);
                var format=normal?TextureFormat.ASTC_4x4:TextureFormat.ASTC_6x6;EditorUtility.CompressTexture(result,format,TextureCompressionQuality.Best);
                if(result.format!=format)throw new Exception("ASTC transcode failed: "+source.name);
                if(result.width!=source.width||result.height!=source.height||result.mipmapCount!=source.mipmapCount)throw new Exception("Android compression changed texture dimensions/mips: "+source.name);
                audit.textureRgbaEquivalentBytesBefore+=(long)source.width*source.height*4*(mips?4:3)/3;
                audit.textureBytesAfter+=result.GetRawTextureData<byte>().Length;
                Save(result,"Texture_"+Textures.Count);CpuReadDisabled(result);Textures.Add(source,result);return result;
            }
            finally{GL.sRGBWrite=oldSrgb;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
        }
    }
}
