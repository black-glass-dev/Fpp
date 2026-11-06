using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    public static class BuildPreview
    {
        public static void Capture()
        {
            var hud=HudAudit.Prepare();
            try
            {
                if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new Exception("A graphics device is required for actual visual review captures");
                Directory.CreateDirectory("Docs/Previews");
                var main=GameObject.Find("Main Camera").GetComponent<Camera>();
                var overlay=GameObject.Find("First person camera").GetComponent<Camera>();
                hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=overlay;hud.planeDistance=.08f;
                foreach(var t in hud.GetComponentsInChildren<Transform>(true))t.gameObject.layer=8;
                Canvas.ForceUpdateCanvases();
                foreach(var skin in GameObject.Find("Mark23 viewmodel").GetComponentsInChildren<SkinnedMeshRenderer>())
                    Debug.Log("FPP skin review: "+skin.name+" enabled="+skin.enabled+" bounds="+skin.bounds+" viewport="+overlay.WorldToViewportPoint(skin.bounds.center)+" quality="+QualitySettings.skinWeights);
                Save(main,overlay,"Docs/Previews/unity-spawn.png");
                // Independent CPU-skinned diagnostic distinguishes editor skin update/culling from a bad pose.
                var snapshots=new List<GameObject>();var skins=GameObject.Find("Mark23 viewmodel").GetComponentsInChildren<SkinnedMeshRenderer>();
                try
                {
                    foreach(var skin in skins)
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh);mesh.RecalculateBounds();
                        var go=new GameObject("CPU skin review");go.layer=8;go.transform.SetParent(skin.transform,false);
                        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                        snapshots.Add(go);skin.enabled=false;
                    }
                    Save(main,overlay,"Docs/Previews/unity-baked-pose.png");
                }
                finally
                {
                    foreach(var go in snapshots){var mesh=go.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);}
                    foreach(var skin in skins)skin.enabled=true;
                }
                hud.enabled=false;
                var position=main.transform.position;var rotation=main.transform.rotation;
                try
                {
                    main.transform.position=new Vector3(8,2,-8);main.transform.LookAt(new Vector3(26,4,5));
                    overlay.enabled=false;Save(main,null,"Docs/Previews/unity-environment.png");
                    main.transform.position=new Vector3(10,3,15);main.transform.LookAt(new Vector3(0,5,32));
                    Save(main,null,"Docs/Previews/unity-mosque.png");
                }
                finally {main.transform.position=position;main.transform.rotation=rotation;overlay.enabled=true;}
            }
            finally {HudAudit.Cleanup();}
        }
        private static void Save(Camera world,Camera overlay,string path)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            world.targetTexture=rt;world.Render();
            if(overlay!=null){overlay.targetTexture=rt;overlay.Render();overlay.targetTexture=null;}
            var old=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            world.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
        }
    }
}
