#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Fpp;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Fpp.Tests
{
    public sealed class GunplayVisualTests
    {
        [SetUp] public void ReportNativeStart(){Debug.Log("FPP QA START "+TestContext.CurrentContext.Test.FullName);}
        [TearDown] public void ReportNativeFinish(){Debug.Log("FPP QA END "+TestContext.CurrentContext.Test.FullName+" "+TestContext.CurrentContext.Result.Outcome.Status);}

        private static IEnumerator WaitForPose(WeaponController weapons,Func<bool> complete,string phase,float seconds)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            while(!complete() && clock.Elapsed.TotalSeconds<seconds)yield return null;
            Assert.True(complete(),"Pose wait expired: "+phase+" weapon="+weapons.ActiveWeapon.id+" ads="+weapons.AdsT+" reload="+weapons.Reloading+" modal="+WeaponController.MenuOpen+" enabled="+weapons.enabled+" frameADS="+GameplayInput.Instance.Frame.ADS+" dt="+Time.deltaTime);
        }
        // No live recoil-model calls in the oracle: integrate the authored noiseless impulses independently.
        private sealed class PatternOracle
        {
            public Vector2 climb,kick,velocity,residual;public float last=-100;
            public void Tick(float dt,float now,WeaponAsset w,GunplayConfig c)
            {
                if(now-last>w.recoilRecoveryDelaySec)climb=Vector2.MoveTowards(climb,residual,w.recoilRecoveryRateDegPerSec*dt);
                float f=1+2*dt*c.viewDamping*c.viewOmega,oo=c.viewOmega*c.viewOmega,den=f+dt*dt*oo;
                var previous=kick;kick=(f*previous+dt*velocity)/den;velocity=(velocity-dt*oo*previous)/den;
            }
            public void Shot(int index,float now,WeaponAsset w,GunplayConfig c)
            {var p=w.recoilPattern[index%w.recoilPattern.Length];if((index/w.recoilPattern.Length)%2==1)p.y=-p.y;climb+=p*w.adsRecoilMult;residual+=p*w.adsRecoilMult*(1-c.recoveryFraction);kick+=p*w.adsRecoilMult*w.viewKickMult;last=now;}
        }
        [UnityTest,Timeout(120000)] public IEnumerator ActualADS30RoundWallPatternAt15m()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var motor=weapons.GetComponent<TestPlayer>();var cc=motor.GetComponent<CharacterController>();
            var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="QA flat floor";floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            WeaponPickup ak=null;foreach(var p in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())if(p.Asset.id=="ak47")ak=p;
            weapons.QAEquip(ak);input.Value.ADS=true;yield return WaitForPose(weapons,()=>weapons.AdsT>=1,"ADS in",3);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA pattern wall exactly 15m";wall.transform.position=weapons.view.position+Vector3.forward*15.05f;wall.transform.localScale=new Vector3(20,20,.1f);Physics.SyncTransforms();
            var reference=new PatternOracle();var w=weapons.ActiveWeapon;var c=GunplayConfig.Current;bool active=true;int shots=0,impacts=0;float squaredError=0;
            var csv=new StringBuilder("shot,worldX,worldY,worldZ,expectedPitch,expectedYaw,actualPitch,actualYaw,errorDeg\n");
            var dotMaterial=new Material(Shader.Find("Sprites/Default")){color=Color.red};
            input.BeforePoll=()=>{if(active)reference.Tick(Time.deltaTime,Time.time,w,c);};
            Action<GunplayEvent> listener=e=>
            {
                if(!active)return;
                if(e.Kind==GunplayEventKind.Impact && Mathf.Abs(e.Position.z-(wall.transform.position.z-.05f))<.1f)
                {
                    var d=e.Position-weapons.view.position;float pitch=Mathf.Atan2(d.y,Mathf.Sqrt(d.x*d.x+d.z*d.z))*Mathf.Rad2Deg,yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
                    var expected=reference.climb+reference.kick;float error=Vector2.Distance(expected,new Vector2(pitch,yaw));squaredError+=error*error;
                    csv.AppendLine(impacts+","+e.Position.x+","+e.Position.y+","+e.Position.z+","+expected.x+","+expected.y+","+pitch+","+yaw+","+error);impacts++;
                    var dot=GameObject.CreatePrimitive(PrimitiveType.Quad);dot.name="QA recorded impact dot (not projectile)";UnityEngine.Object.Destroy(dot.GetComponent<Collider>());dot.transform.position=e.Position+e.Normal*.005f;dot.transform.rotation=Quaternion.LookRotation(-e.Normal);dot.transform.localScale=Vector3.one*.04f;dot.GetComponent<Renderer>().sharedMaterial=dotMaterial;
                }
                if(e.Kind==GunplayEventKind.ShotFired){reference.Shot(shots,Time.time,w,c);shots++;if(shots==30)input.Value.Fire=false;}
            };
            GunplayEvents.Raised+=listener;
            try
            {
                input.Value.Fire=true;float deadline=Time.time+8;while(shots<30&&Time.time<deadline)yield return null;active=false;input.Value.Fire=false;
                Assert.AreEqual(30,shots);Assert.AreEqual(30,impacts);
                float rms=Mathf.Sqrt(squaredError/impacts);Directory.CreateDirectory("QA/runtime");File.WriteAllText("QA/runtime/recoil_ak47.csv",csv.ToString());File.WriteAllText("QA/runtime/recoil-wall-result.txt","Actual Play Mode hitscan on a fixed 15m wall; noiseless authored-pattern plus recovery/spring oracle; RMS="+rms+" degrees; maximum acceptance 0.5 degrees. Fixed transform, no touch input.");
                Save("QA/runtime/recoil-wall-ak47.png",weapons);Assert.LessOrEqual(rms,.5f);
            }
            finally{active=false;GunplayEvents.Raised-=listener;input.BeforePoll=null;}
        }
        [UnityTest,Timeout(120000)] public IEnumerator CapturePoseAndReloadEvidence_NotFingerContactAcceptance()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var choices=new WeaponPickup[4];int index=1;foreach(var p in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())choices[index++]=p;
            foreach(var choice in choices)
            {
                weapons.QAEquip(choice);input.Value=default(InputFrame);yield return null;yield return null;string id=weapons.ActiveWeapon.id;
                Save("QA/runtime/"+id+"-idle.png",weapons);
                input.Value.ADS=true;yield return WaitForPose(weapons,()=>weapons.AdsT>=1,"ADS in",3);yield return null;yield return null;Save("QA/runtime/"+id+"-ads.png",weapons);
                input.Value.Fire=input.Value.FirePressed=true;yield return null;yield return null;input.Value.Fire=false;Save("QA/runtime/"+id+"-fire.png",weapons);
                input.Value.ADS=false;yield return WaitForPose(weapons,()=>weapons.AdsT<=0,"ADS out",3);
                foreach(bool empty in new[]{false,true})
                {
                    weapons.QAAmmo(empty?0:1,weapons.ActiveWeapon.reserveMax);weapons.Reload();float start=Time.time;
                    foreach(float t in new[]{.15f,.4f,.7f,.95f})
                    {
                        float duration=empty?weapons.ActiveWeapon.reloadEmptySec:weapons.ActiveWeapon.reloadTacticalSec;
                        while(Time.time-start<duration*t)yield return null;Save("QA/runtime/"+id+"-"+(empty?"empty":"tactical")+"-"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",weapons);
                    }
                    yield return WaitForPose(weapons,()=>!weapons.Reloading,"reload completion",5);
                }
                input.Value.Move=Vector2.up;input.Value.Sprint=true;yield return new WaitForSeconds(.4f);Save("QA/runtime/"+id+"-sprint.png",weapons);input.Value=default(InputFrame);
            }
            File.WriteAllText("QA/runtime/visual-review-status.txt","Captured actual Play Mode idle/ADS/fire/reload/sprint and four-frame tactical/empty sequences for all four weapons. Images require human review. Approximate secondary fingers/sockets are NOT declared zero-floaters by this capture test.");
        }
        public static void AssertUngradedHud(WeaponController weapons)
        {
            var canvas=GameObject.Find("Mobile HUD").GetComponent<Canvas>();
            var patch=new GameObject("QA-only ungraded magenta patch",typeof(RectTransform),typeof(UnityEngine.UI.Image));patch.transform.SetParent(canvas.transform,false);
            var rect=patch.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.one*64;patch.GetComponent<UnityEngine.UI.Image>().color=Color.magenta;
            var rt=RenderTexture.GetTemporary(320,180,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;Texture2D image=null;
            try
            {
                RenderEvidence.Into(weapons.view.GetComponent<Camera>(),weapons.weaponCamera,rt,canvas);RenderTexture.active=rt;
                image=new Texture2D(320,180,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,320,180),0,0);image.Apply();var pixel=image.GetPixel(160,90);
                File.WriteAllBytes("QA/runtime/urp-hud-sentinel.png",image.EncodeToPNG());
                Assert.Greater(pixel.r,.9f,"HUD must be present and not ACES-graded");Assert.Less(pixel.g,.1f,"HUD patch must not be replaced by scene pixels");Assert.Greater(pixel.b,.9f,"HUD must be present and not ACES-graded");
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(patch);}
        }
        public static void Save(string path,WeaponController weapons)
        {
            Directory.CreateDirectory("QA/runtime");
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)Assert.Inconclusive("Graphics device unavailable: visual acceptance cannot be claimed");
            var world=weapons.view.GetComponent<Camera>();var overlay=weapons.weaponCamera;var canvas=GameObject.Find("Mobile HUD").GetComponent<Canvas>();
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var transforms=canvas.GetComponentsInChildren<Transform>(true);int[] layers=new int[transforms.Length];
            for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=8;}
            var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=overlay;canvas.planeDistance=.08f;Canvas.ForceUpdateCanvases();
                RenderEvidence.Into(world,overlay,rt,canvas);RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            finally
            {
                world.targetTexture=overlay.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;
                for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
            }
        }
    }
}

#endif
