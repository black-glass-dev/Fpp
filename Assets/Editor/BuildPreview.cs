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
                var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();
                var pickups=UnityEngine.Object.FindObjectsOfType<WeaponPickup>();
                SecondaryWeaponAudit.Validate(weapons,pickups);
                foreach(var pickup in pickups)
                {
                    string id=pickup.displayName.Contains("Glock")?"glock":pickup.displayName.Contains("AK")?"ak":"field";
                    weapons.ReviewPose(pickup,false,0);UnityEngine.Object.FindObjectOfType<MobileHud>().ReviewRefresh();
                    Save(main,overlay,"Docs/Previews/unity-"+id+"-held.png");
                    if(id=="ak"){weapons.ReviewPose(pickup,true,0);Save(main,overlay,"Docs/Previews/unity-ak-ads.png");weapons.ReviewPose(pickup,false,.55f);UnityEngine.Object.FindObjectOfType<MobileHud>().ReviewRefresh();Save(main,overlay,"Docs/Previews/unity-ak-reload.png");}
                    if(id=="field"){weapons.ReviewPose(pickup,false,.55f);Save(main,overlay,"Docs/Previews/unity-field-reload.png");}
                    if(id=="glock")
                    {
                        weapons.ReviewPose(pickup,true,0);Save(main,overlay,"Docs/Previews/unity-glock-ads.png");
                        weapons.ReviewPose(pickup,false,.55f);UnityEngine.Object.FindObjectOfType<MobileHud>().ReviewRefresh();
                        Save(main,overlay,"Docs/Previews/unity-glock-reload-out.png");
                        weapons.ReviewPose(pickup,false,.88f);Save(main,overlay,"Docs/Previews/unity-glock-reload-in.png");
                    }
                }
                weapons.ReviewPose(null,true,0);UnityEngine.Object.FindObjectOfType<MobileHud>().ReviewRefresh();
                Save(main,overlay,"Docs/Previews/unity-mark23-ads.png");
                weapons.ReviewPose(null,false,0);UnityEngine.Object.FindObjectOfType<MobileHud>().ReviewRefresh();
                hud.enabled=false;
                var position=main.transform.position;var rotation=main.transform.rotation;
                try
                {
                    main.transform.position=new Vector3(8,2,-8);main.transform.LookAt(new Vector3(26,4,5));
                    overlay.enabled=false;Save(main,null,"Docs/Previews/unity-environment.png");
                    main.transform.position=new Vector3(7,1.05f,-10);main.transform.LookAt(new Vector3(11,.25f,-6));
                    Save(main,null,"Docs/Previews/unity-ground-detail.png");
                    main.transform.position=new Vector3(10,3,15);main.transform.LookAt(new Vector3(0,5,32));
                    Save(main,null,"Docs/Previews/unity-mosque.png");
                    main.transform.position=new Vector3(0,2.5f,43);main.transform.LookAt(new Vector3(0,4,32));
                    Save(main,null,"Docs/Previews/unity-mosque-entry.png");
                    main.transform.position=new Vector3(0,2.65f,33);main.transform.LookAt(new Vector3(1.4f,3.2f,28));
                    Save(main,null,"Docs/Previews/unity-mosque-interior.png");
                    // Review every supplied model close-up, not just the old spawn block.
                    var district=GameObject.Find("Bangladesh compact para (120 x 120m)");
                    if(!File.Exists("QA/runtime/village-placement.json"))throw new Exception("Missing placement receipt");
                    var receipt=JsonUtility.FromJson<ReviewReceipt>(File.ReadAllText("QA/runtime/village-placement.json"));
                    int modelIndex=0;var names=new HashSet<string>();
                    foreach(Transform child in district.transform)
                    {
                        bool supplied=false;
                        // The receipt records exact names, including localized author comments.
                        foreach(var entry in receipt.files)if(entry.placement==child.name)supplied=true;
                        if(!supplied||!names.Add(child.name))continue;
                        var renderers=child.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Empty review model");
                        var bounds=renderers[0].bounds;foreach(var renderer in renderers)if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
                        float span=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                        main.transform.position=bounds.center+new Vector3(.8f,.48f,-1).normalized*span*1.35f;main.transform.LookAt(bounds.center);
                        Save(main,null,"Docs/Previews/unity-village-"+(modelIndex++).ToString("00")+".png");
                    }
                    if(modelIndex!=18)throw new Exception("Expected close-up review of all18 supplied models");
                    CaptureLodComparison(main);
                    main.transform.position=new Vector3(32,5,28);main.transform.LookAt(new Vector3(46,-.3f,40));Save(main,null,"Docs/Previews/unity-village-pond.png");
                    foreach(var pickup in pickups)if(pickup.displayName.Contains("Glock"))
                    {
                        main.transform.position=pickup.transform.position+new Vector3(.6f,.8f,-.6f);
                        main.transform.LookAt(pickup.transform.position+Vector3.up*.03f);
                        Save(main,null,"Docs/Previews/unity-glock-floor.png");
                    }
                }
                finally {main.transform.position=position;main.transform.rotation=rotation;overlay.enabled=true;}
                weapons.ReviewPose(null,false,0);weapons.ReviewShotEffects();
                Save(main,overlay,"Docs/Previews/unity-shot-effects.png");
                weapons.ReviewSmokeBurst();Save(main,overlay,"Docs/Previews/unity-smoke-burst.png");
            }
            finally {HudAudit.Cleanup();}
        }
        private static void CaptureLodComparison(Camera main)
        {
            var model=GameObject.Find("Bd house 1");var group=model!=null?model.GetComponent<LODGroup>():null;
            if(group==null)throw new Exception("Missing real heavy-house LOD comparison target");
            var levels=group.GetLODs();var bounds=levels[0].renderers[0].bounds;
            foreach(var renderer in levels[0].renderers)bounds.Encapsulate(renderer.bounds);
            main.transform.position=bounds.center+new Vector3(1.1f,.65f,-1.4f).normalized*bounds.size.magnitude*1.6f;
            main.transform.LookAt(bounds.center);var fade=group.fadeMode;
            try
            {
                // Exact same camera/material/light settings; force pure levels, not a blended composite.
                group.fadeMode=LODFadeMode.None;
                string[] names={"near","medium","far"};
                for(int i=0;i<3;i++){group.ForceLOD(i);Save(main,null,"Docs/Previews/unity-lod-"+names[i]+".png");}
            }
            finally {group.fadeMode=fade;group.ForceLOD(-1);}
        }
        [Serializable] private class ReviewEntry { public string placement; }
        [Serializable] private class ReviewReceipt { public ReviewEntry[] files; }
        private static void Save(Camera world,Camera overlay,string path)
        {
            Directory.CreateDirectory("QA/runtime");File.AppendAllText("QA/runtime/build-progress.txt","CAPTURE START "+path+"\n");
            var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            RenderEvidence.Into(world,overlay,rt,GameObject.Find("Mobile HUD").GetComponent<Canvas>());
            var old=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            world.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
            File.AppendAllText("QA/runtime/build-progress.txt","CAPTURE END "+path+"\n");
        }
    }
}
