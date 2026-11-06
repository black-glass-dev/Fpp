using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    public static class BuildPreview
    {
        public static void Capture()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
            { Debug.LogWarning("No graphics device for actual scene review captures");return; }
            Directory.CreateDirectory("Docs/Previews");
            var main=GameObject.Find("Main Camera").GetComponent<Camera>();
            var overlay=GameObject.Find("First person camera").GetComponent<Camera>();
            Save(main,overlay,"Docs/Previews/unity-spawn.png");
            var position=main.transform.position;var rotation=main.transform.rotation;
            main.transform.position=new Vector3(8,2,-8);main.transform.LookAt(new Vector3(26,4,5));
            overlay.enabled=false;Save(main,null,"Docs/Previews/unity-environment.png");
            main.transform.position=position;main.transform.rotation=rotation;overlay.enabled=true;
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
