using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    public static class ViewmodelSetup
    {
        public static void Attach(Transform worldCamera, WeaponController controller)
        {
            const string path="Assets/Viewmodels/Mark23/Mark23.fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(model==null)throw new Exception("Original skinned Mark23 FBX missing");
            var overlay=new GameObject("First person camera");overlay.transform.SetParent(worldCamera,false);
            var camera=overlay.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.Depth;camera.cullingMask=1<<8;
            camera.depth=10;camera.nearClipPlane=.01f;camera.farClipPlane=5;camera.fieldOfView=60;camera.allowHDR=false;
            controller.weaponCamera=camera;
            var holder=new GameObject("Mark23 viewmodel");holder.transform.SetParent(overlay.transform,false);
            var calibration=new GameObject("Viewmodel calibration");calibration.transform.SetParent(holder.transform,false);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);instance.transform.SetParent(calibration.transform,false);
            var animation=instance.GetComponent<Animation>();if(animation==null)animation=instance.AddComponent<Animation>();
            AnimationClip draw=null;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip=asset as AnimationClip;if(clip==null || clip.name.StartsWith("__preview"))continue;
                animation.AddClip(clip,clip.name);if(clip.name.ToLowerInvariant().Contains("draw"))draw=clip;
            }
            if(draw==null)throw new Exception("Mark23 authored Draw animation was not imported");
            draw.SampleAnimation(instance,Mathf.Max(0,draw.length-.001f));
            var gun=new List<Renderer>();
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var slots=renderer.sharedMaterials;
                if(slots.Length==0)throw new Exception("Viewmodel renderer has no source material slots");
                for(int i=0;i<slots.Length;i++)
                {
                    string key=slots[i]!=null?slots[i].name:"";
                    string materialName=key.Contains("Glove")?"Glove_D":key.Contains("Hand")?"Hand_D":"Mark23_D";
                    string texPath="Assets/Viewmodels/Mark23/"+materialName+".png";
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);if(texture==null)throw new Exception("Missing viewmodel texture: "+texPath);
                    string matPath="Assets/Viewmodels/Mark23/"+materialName+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,matPath);}
                    material.mainTexture=texture;material.color=Color.white;material.SetFloat("_Glossiness",.15f);material.SetFloat("_Metallic",0);
                    slots[i]=material;
                    if(materialName=="Mark23_D" && !gun.Contains(renderer))gun.Add(renderer);
                }
                renderer.sharedMaterials=slots;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                var skinned=renderer as SkinnedMeshRenderer;if(skinned!=null)skinned.updateWhenOffscreen=true;
            }
            if(gun.Count==0)throw new Exception("Could not identify pistol geometry");
            Transform hand=null,main=null,side=null;
            foreach(var t in instance.GetComponentsInChildren<Transform>())
            {
                if(t.name=="RightHand1_029")hand=t;if(t.name=="main_j_050")main=t;if(t.name=="side_j_051")side=t;
            }
            if(hand!=null && main!=null && side!=null)
            {
                Vector3 forward=holder.transform.InverseTransformDirection(main.position-hand.position).normalized;
                Vector3 up=holder.transform.InverseTransformDirection(side.position-main.position).normalized;
                calibration.transform.localRotation=Quaternion.Inverse(Quaternion.LookRotation(forward,up));
            }
            Bounds bound=PosedBounds(gun,holder.transform);
            float size=Mathf.Max(bound.size.x,bound.size.y,bound.size.z);
            if(size<.0001f)throw new Exception("Invalid posed pistol bounds");
            calibration.transform.localScale*=.30f/size;
            bound=PosedBounds(gun,holder.transform);calibration.transform.localPosition-=bound.center;
            holder.transform.localPosition=new Vector3(.115f,-.16f,.5f);
            controller.defaultViewmodel=holder;controller.pistolRenderers=gun.ToArray();
            var mount=new GameObject("Replacement weapon grip");mount.transform.SetParent(holder.transform,false);controller.weaponMount=mount.transform;
            var muzzle=new GameObject("Muzzle");muzzle.transform.SetParent(holder.transform,false);muzzle.transform.localPosition=new Vector3(0,.05f,.16f);controller.muzzle=muzzle.transform;
            WeaponController.SetLayer(holder,8);
            string flashPath="Assets/Materials/TestMap/Muzzle.mat";
            var flash=AssetDatabase.LoadAssetAtPath<Material>(flashPath);
            var shader=Shader.Find("FPP/Muzzle");if(shader==null)throw new Exception("Muzzle shader missing");
            if(flash==null){flash=new Material(shader);AssetDatabase.CreateAsset(flash,flashPath);}controller.flashMaterial=flash;
        }
        private static Bounds PosedBounds(List<Renderer> renderers, Transform space)
        {
            var points=new List<Vector3>();
            foreach(var r in renderers)
            {
                var skin=r as SkinnedMeshRenderer;
                if(skin!=null)
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    foreach(var v in mesh.vertices)points.Add(space.InverseTransformPoint(skin.transform.TransformPoint(v)));UnityEngine.Object.DestroyImmediate(mesh);
                }
                else
                {
                    var f=r.GetComponent<MeshFilter>();if(f!=null)foreach(var v in f.sharedMesh.vertices)points.Add(space.InverseTransformPoint(f.transform.TransformPoint(v)));
                }
            }
            if(points.Count==0)throw new Exception("No viewmodel vertices");
            var bound=new Bounds(points[0],Vector3.zero);foreach(var point in points)bound.Encapsulate(point);return bound;
        }
    }
    public sealed class ViewmodelImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/Viewmodels/"))return;
            var importer=(ModelImporter)assetImporter;importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Legacy;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.isReadable=true;importer.optimizeGameObjects=false;
        }
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Viewmodels/") && !assetPath.StartsWith("Assets/Art/"))return;
            var importer=(TextureImporter)assetImporter;importer.maxTextureSize=1024;importer.mipmapEnabled=!assetPath.Contains("/Icon/");
            importer.wrapMode=assetPath.Contains("/Ground/")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Compressed;importer.isReadable=false;
        }
    }
}
