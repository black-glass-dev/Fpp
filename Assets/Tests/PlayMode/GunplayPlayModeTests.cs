#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Unity.Profiling;
using Fpp;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Fpp.Tests
{
    public sealed class GunplayPlayModeTests
    {
        private static readonly string[] Ids={"mark23","glock17","field","ak47"};
        private static string Output {get {string p="QA/runtime";Directory.CreateDirectory(p);return p;}}
        private static WeaponAsset Weapon(string id){var w=Resources.Load<WeaponAsset>("Gunplay/"+id);Assert.NotNull(w,id);return w;}
        [Test] public void ConfigsAreSerializedAndReferenceValuesMatch()
        {
            var c=GunplayConfig.Current;Assert.AreEqual(4.2f,c.walk);Assert.AreEqual(6.9f,c.sprint);Assert.AreEqual(60,c.targetFPS);
            int[] rpm={480,540,450,600},mag={12,17,17,30};
            for(int i=0;i<Ids.Length;i++){var w=Weapon(Ids[i]);Assert.AreEqual(rpm[i],w.rpm);Assert.AreEqual(mag[i],w.magSize);Assert.Greater(w.recoilPattern.Length,0);Assert.Greater(w.reloadEmptySec,w.reloadTacticalSec);}
        }
        [Test] public void CadenceHasNoBankedShotsAndStaysFrameRateIndependent()
        {
            var csv=new StringBuilder("kind,weapon,fps,shots,seconds\n");
            foreach(var id in Ids)foreach(int fps in new[]{15,30,60,120})
            {
                var w=Weapon(id);var model=new GunplayModel();int shots=0;float duration=10;
                for(int frame=0;frame<fps*duration;frame++){float now=frame/(float)fps;if(model.CanShoot(now)){model.Schedule(now,w);shots++;Assert.False(model.CanShoot(now),"second shot in same frame");}}
                Assert.That(shots,Is.InRange(Mathf.FloorToInt(w.rpm*duration/60)-1,Mathf.CeilToInt(w.rpm*duration/60)+1));
                model.Schedule(100,w);Assert.False(model.CanShoot(100));Assert.False(model.CanShoot(100+.5f*60/w.rpm),"lag catch-up");
                csv.AppendLine("math_scheduler,"+id+","+fps+","+shots+","+duration);
            }
            File.WriteAllText(Output+"/cadence-model.csv",csv.ToString());
        }
        [Test] public void RecoilNoiseBoundHipAsymmetryAndMountReduction()
        {
            var c=GunplayConfig.Current;var w=Weapon("ak47");var normal=new GunplayModel();var mounted=new GunplayModel();var hip=new GunplayModel();
            var csv=new StringBuilder("kind,shot,targetPitch,targetYaw,actualPitch,actualYaw,mountedPitch,mountedYaw\n");float error=0,norm=0,mount=0;
            for(int i=0;i<30;i++)
            {
                var target=normal.Pattern(w,i);var a=normal.Shot(i*60/w.rpm,w,c,true,false);var b=mounted.Shot(i*60/w.rpm,w,c,true,true);hip.Shot(i*60/w.rpm,w,c,false,false);
                error+=(a-target).sqrMagnitude;norm+=a.sqrMagnitude;mount+=b.sqrMagnitude;
                csv.AppendLine("math_impulse,"+i+","+target.x+","+target.y+","+a.x+","+a.y+","+b.x+","+b.y);
            }
            float rms=Mathf.Sqrt(error/30);Assert.LessOrEqual(rms,.5f);Assert.LessOrEqual(Mathf.Sqrt(mount/norm),.2f);Assert.AreEqual(Vector2.zero,hip.AimClimb);
            File.WriteAllText(Output+"/recoil-model-ak47.csv",csv.ToString());Debug.Log("QA model impulse RMS "+rms+" degrees (not wall/hand acceptance)");
        }
        [UnityTest,Timeout(120000)] public IEnumerator SameFrameSemiFireAndActualADSTimingAllWeapons()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var hub=GameplayInput.Instance;var input=new ScriptedInputProvider();hub.OverrideProvider=input;
            yield return new WaitForSeconds(Weapon("mark23").drawSec);
            var pickups=UnityEngine.Object.FindObjectsOfType<WeaponPickup>();var csv=new StringBuilder("weapon,targetSec,measuredSec,frameToleranceSec\n");
            foreach(var id in Ids)
            {
                WeaponPickup choice=null;foreach(var pickup in pickups)if(pickup.Asset.id==id)choice=pickup;
                weapons.QAEquip(choice);input.Value=default(InputFrame);yield return null;
                float begin=Time.time;input.Value.ADS=true;float tolerance=0;
                while(weapons.AdsT<1){tolerance=Mathf.Max(tolerance,Time.deltaTime);yield return null;}
                float measured=Time.time-begin;Assert.LessOrEqual(Mathf.Abs(measured-weapons.ActiveWeapon.adsTimeSec),Mathf.Max(.02f,tolerance));
                csv.AppendLine(id+","+weapons.ActiveWeapon.adsTimeSec+","+measured+","+tolerance);
                yield return null;yield return null;
                Assert.That(weapons.view.GetComponent<Camera>().fieldOfView,Is.EqualTo(weapons.ActiveWeapon.sightFOV).Within(.01f),"ADS must drive the actual camera, not just a progress scalar");
                var sight=weapons.weaponCamera.WorldToViewportPoint(weapons.ActiveSight.position);
                Assert.That(sight.x,Is.EqualTo(.5f).Within(.015f),id+" sight horizontal alignment");Assert.That(sight.y,Is.EqualTo(.5f).Within(.015f),id+" sight vertical alignment");
                input.Value.ADS=false;while(weapons.AdsT>0)yield return null;
            }
            weapons.QAEquip(null);int shots=0;int shotFrame=-1;Action<GunplayEvent> listener=e=>{if(e.Kind==GunplayEventKind.ShotFired){shots++;shotFrame=Time.frameCount;Assert.AreEqual(input.LastPolledFrame,shotFrame);}};
            GunplayEvents.Raised+=listener;
            try
            {
                input.Value.Fire=input.Value.FirePressed=true;yield return null;yield return null;Assert.AreEqual(1,shots);
                yield return new WaitForSeconds(.3f);Assert.AreEqual(1,shots,"semi must not repeat on held trigger");Assert.GreaterOrEqual(shotFrame,0);
            }
            finally{GunplayEvents.Raised-=listener;}
            File.WriteAllText(Output+"/ads-actual-playmode.csv",csv.ToString());
        }
        [UnityTest,Timeout(120000)] public IEnumerator ReloadCancellationConservationAndDroppedAmmo()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            WeaponPickup ak=null;foreach(var p in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())if(p.Asset.id=="ak47")ak=p;
            weapons.QAEquip(ak);weapons.QAAmmo(7,73);int total=weapons.Ammo+weapons.Reserve;
            weapons.Reload();Assert.True(weapons.Reloading);yield return new WaitForSeconds(.3f);weapons.CancelReload();Assert.AreEqual(7,weapons.Ammo);Assert.AreEqual(total,weapons.Ammo+weapons.Reserve);
            weapons.Reload();yield return new WaitForSeconds(weapons.ActiveWeapon.reloadTacticalSec+.1f);Assert.AreEqual(30,weapons.Ammo);Assert.AreEqual(total,weapons.Ammo+weapons.Reserve);
            weapons.QAAmmo(0,73);weapons.Reload();Assert.AreEqual(WeaponState.ReloadEmpty,weapons.State);
            yield return new WaitForSeconds(weapons.ActiveWeapon.reloadEmptySec+.1f);Assert.AreEqual(30,weapons.Ammo);Assert.AreEqual(43,weapons.Reserve);
            weapons.QAAmmo(7,73);weapons.DropActiveWeapon();Assert.True(ak.gameObject.activeSelf);Assert.AreEqual(7,ak.Rounds);Assert.AreEqual(73,ak.Reserve);
            weapons.QAEquip(ak);Assert.AreEqual(7,weapons.Ammo);Assert.AreEqual(73,weapons.Reserve);
            File.WriteAllText(Output+"/ammo-playmode.txt","PASS: tactical/empty conservation, cancel no transfer, secondary drop/pickup restores 7+73. Visual magazine/finger acceptance not asserted.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator MovementSlideDiveAndMantleOnRealController()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var motor=UnityEngine.Object.FindObjectOfType<TestPlayer>();var cc=motor.GetComponent<CharacterController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            yield return new WaitForSeconds(.2f);input.Value.Move=Vector2.up;input.Value.Sprint=true;yield return new WaitForSeconds(.4f);
            Assert.AreEqual(MovementState.Sprint,motor.State);float entry=motor.Speed;
            input.Value.CrouchPressed=input.Value.CrouchHeld=true;float until=Time.time+1;
            while(motor.State!=MovementState.Slide && Time.time<until)yield return null;
            Assert.AreEqual(MovementState.Slide,motor.State);Assert.GreaterOrEqual(motor.Speed,entry*1.35f-.01f);float peak=motor.Speed;
            float slideShotSpread=-1;Action<GunplayEvent> slideListener=e=>{if(e.Kind==GunplayEventKind.ShotFired)slideShotSpread=e.Spread;};GunplayEvents.Raised+=slideListener;
            try{yield return new WaitForSeconds(.2f);input.Value.ADS=input.Value.Fire=input.Value.FirePressed=true;yield return null;yield return null;Assert.That(slideShotSpread,Is.InRange(0,Weapon("mark23").adsSpreadDeg+.001f));}
            finally{GunplayEvents.Raised-=slideListener;}
            input.Value.ADS=input.Value.Fire=false;
            input.Value.Sprint=input.Value.CrouchHeld=false;yield return new WaitForSeconds(GunplayConfig.Current.slideDuration+.1f);Assert.Less(motor.Speed,peak);
            // Stand, then explicitly request tactical sprint and hold crouch into dive.
            input.Value.CrouchPressed=true;yield return null;input.Value.Sprint=input.Value.Tactical=true;yield return new WaitForSeconds(.4f);
            Assert.AreEqual(MovementState.TacticalSprint,motor.State);input.Value.CrouchHeld=input.Value.CrouchPressed=true;
            until=Time.time+1;while(motor.State!=MovementState.Dive&&Time.time<until)yield return null;Assert.AreEqual(MovementState.Dive,motor.State);
            input.Value=default(InputFrame);until=Time.time+2;while(motor.State!=MovementState.Prone&&Time.time<until)yield return null;Assert.AreEqual(MovementState.Prone,motor.State);
            input.Value.Prone=true;yield return null;
            cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="QA mantle 1.5m";wall.transform.position=new Vector3(1000,.75f,1001);wall.transform.localScale=new Vector3(2,1.5f,1);
            Physics.SyncTransforms();yield return new WaitForSeconds(.2f);input.Value.Move=Vector2.up;until=Time.time+2;
            while(motor.State!=MovementState.Mantle&&Time.time<until){input.Value.Jump=true;yield return null;}Assert.AreEqual(MovementState.Mantle,motor.State);
            input.Value.Move=Vector2.zero;yield return new WaitForSeconds(GunplayConfig.Current.mantleDuration+.2f);Assert.Greater(motor.transform.position.y,1.45f);
            File.WriteAllText(Output+"/movement-playmode.txt","PASS: real CharacterController slide entry="+peak+", sprint="+entry+"; dive lands prone; 1.5m mantle height="+motor.transform.position.y);
        }
        [UnityTest,Timeout(420000)] public IEnumerator FiveMinuteCombatSoak500Shots50Reloads()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var pickups=UnityEngine.Object.FindObjectsOfType<WeaponPickup>();float start=Time.realtimeSinceStartup;int shots=0,reloads=0,swaps=0,cycleShots=0;float firstInCycle=0;
            var cadence=new StringBuilder("weapon,cycle,shot,timeSec,rpmCap\n");
            Action<GunplayEvent> listener=e=>{if(e.Kind==GunplayEventKind.ShotFired){if(cycleShots==0)firstInCycle=Time.time;else Assert.GreaterOrEqual(Time.time-firstInCycle,cycleShots*60/e.Weapon.rpm-.005f);cadence.AppendLine(e.Weapon.id+","+swaps+","+cycleShots+","+Time.time+","+e.Weapon.rpm);cycleShots++;shots++;}};GunplayEvents.Raised+=listener;
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            var movement=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"FPP.Movement",1);
            var weaponCPU=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"FPP.Weapons",1);
            var viewmodel=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"FPP.Viewmodel",1);
            long maxGC=0,maxCPU=0;
            try
            {
                for(int cycle=0;cycle<50;cycle++)
                {
                    WeaponPickup next=cycle%4==0?null:pickups[(cycle%4)-1];
                    weapons.QARequestEquip(next);while(weapons.QASwitching)yield return null;swaps++;
                    weapons.GrantAmmo(weapons.ActiveWeapon.reserveMax);cycleShots=0;int goal=shots+10;
                    while(shots<goal)
                    {
                        input.Value.Fire=true;input.Value.FirePressed=true;yield return null;
                        maxGC=Math.Max(maxGC,gc.LastValue);maxCPU=Math.Max(maxCPU,movement.LastValue+weaponCPU.LastValue+viewmodel.LastValue);
                    }
                    input.Value.Fire=false;weapons.Reload();Assert.True(weapons.Reloading);
                    while(weapons.Reloading)yield return null;reloads++;
                    LogAssert.NoUnexpectedReceived();
                }
                while(Time.realtimeSinceStartup-start<300){yield return null;maxGC=Math.Max(maxGC,gc.LastValue);maxCPU=Math.Max(maxCPU,movement.LastValue+weaponCPU.LastValue+viewmodel.LastValue);}
                Assert.AreEqual(500,shots);Assert.AreEqual(50,reloads);Assert.AreEqual(50,swaps);
                File.WriteAllText(Output+"/fast-tap-actual-cadence.csv",cadence.ToString());
                File.WriteAllText(Output+"/soak-editor.txt","Actual Unity Play Mode: seconds="+(Time.realtimeSinceStartup-start)+", shots="+shots+", reloads="+reloads+", switches="+swaps+", editor max total frame GC bytes="+maxGC+", editor movement+weapons marker max nanoseconds="+maxCPU+". Editor total-frame allocation/CPU is NOT target Android combat-loop acceptance; no device profiler capture supplied.");
            }
            finally{GunplayEvents.Raised-=listener;gc.Dispose();movement.Dispose();weaponCPU.Dispose();viewmodel.Dispose();}
        }
        [UnityTest,Timeout(120000)] public IEnumerator SprintOutDelayAndADSCancellationUseActualInputLoop()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var motor=weapons.GetComponent<TestPlayer>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            weapons.QAEquip(null);yield return new WaitForSeconds(.2f);input.Value.Move=Vector2.up;input.Value.Sprint=true;yield return new WaitForSeconds(.4f);Assert.True(motor.IsSprinting);
            float fired=-1;Action<GunplayEvent> listener=e=>{if(e.Kind==GunplayEventKind.ShotFired&&fired<0)fired=Time.time;};GunplayEvents.Raised+=listener;
            try
            {
                input.Value.Fire=input.Value.FirePressed=true;float deadline=Time.time+1;while(fired<0&&Time.time<deadline)yield return null;
                Assert.Greater(fired,0);float delay=fired-motor.SprintExitedAt;Assert.GreaterOrEqual(delay,weapons.ActiveWeapon.sprintOutFireDelaySec-.001f);
                input.Value.Fire=false;yield return new WaitForSeconds(.4f);Assert.True(motor.IsSprinting);
                input.Value.ADS=true;yield return null;yield return null;Assert.False(motor.IsSprinting);
                File.WriteAllText(Output+"/sprint-out-playmode.txt","Actual pistol sprint-out fire delay="+delay+" sec; target minimum="+weapons.ActiveWeapon.sprintOutFireDelaySec+"; ADS cancels sprint.");
            }
            finally{GunplayEvents.Raised-=listener;}
        }
        [UnityTest,Timeout(120000)] public IEnumerator PhysicalMountLocksMovementAndReducesActualShotImpulseRMS()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var motor=weapons.GetComponent<TestPlayer>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            float[] energy=new float[2];int shots=0,phase=0;Action<GunplayEvent> listener=e=>{if(e.Kind==GunplayEventKind.ShotFired){energy[phase]+=e.Pattern.sqrMagnitude;shots++;}};GunplayEvents.Raised+=listener;
            try
            {
                for(phase=0;phase<2;phase++)
                {
                    weapons.QAEquip(null);weapons.QAAmmo(weapons.Capacity,weapons.ActiveWeapon.reserveMax);input.Value=default(InputFrame);input.Value.ADS=true;while(weapons.AdsT<1)yield return null;
                    if(phase==1)
                    {
                        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.name="QA chest-height mount cover";cover.transform.position=new Vector3(motor.transform.position.x,.75f,motor.transform.position.z+.45f);cover.transform.localScale=new Vector3(2,1.5f,.1f);Physics.SyncTransforms();yield return null;yield return null;
                        Assert.True(motor.MountAvailable);input.Value.Mount=true;yield return null;yield return null;Assert.True(motor.Mounted);Assert.AreEqual(0,motor.Speed);
                    }
                    int goal=shots+10;float deadline=Time.time+4;
                    while(shots<goal&&Time.time<deadline){input.Value.Fire=input.Value.FirePressed=true;yield return null;}
                    input.Value.Fire=false;Assert.AreEqual(goal,shots);
                }
                float ratio=Mathf.Sqrt(energy[1]/energy[0]);Assert.LessOrEqual(ratio,.2f);input.Value.Move=Vector2.right;yield return null;yield return null;Assert.False(motor.Mounted);
                File.WriteAllText(Output+"/mount-playmode.txt","Actual ledge probe/mount + shot-event recoil RMS ratio="+ratio+"; maximum .2. Movement locked while mounted and move exits mount.");
            }
            finally{GunplayEvents.Raised-=listener;}
        }
        [UnityTest,Timeout(120000)] public IEnumerator DefaultPistolDropsAsTexturedGunOnlyAndRestoresAuthoredSlot()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();GameplayInput.Instance.OverrideProvider=new ScriptedInputProvider();
            weapons.QAAmmo(5,65);WeaponPickup secondary=null;foreach(var p in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())if(p.Asset.id=="ak47")secondary=p;
            weapons.QAEquip(secondary);weapons.QAEquip(null);weapons.DropActiveWeapon();yield return null;
            WeaponPickup dropped=null;foreach(var p in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())if(p.Asset.id=="mark23")dropped=p;
            Assert.NotNull(dropped);Assert.AreEqual(5,dropped.Rounds);Assert.AreEqual(65,dropped.Reserve);Assert.AreEqual(WeaponState.Dropped,dropped.State);
            Assert.AreEqual(0,dropped.GetComponentsInChildren<SkinnedMeshRenderer>().Length,"world item must not include a live arms rig");
            var renderers=dropped.GetComponentsInChildren<Renderer>();Assert.Greater(renderers.Length,0);
            foreach(var r in renderers)foreach(var m in r.sharedMaterials){Assert.NotNull(m.mainTexture);Assert.False(m.name.Contains("Hand")||m.name.Contains("Glove"),"no arms in floor pickup");}
            var bounds=WeaponController.LocalBounds(dropped.transform);float length=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*dropped.transform.localScale.x;Assert.That(length,Is.EqualTo(dropped.Asset.pickupLength).Within(.001f));
            weapons.QAEquip(dropped);yield return null;Assert.AreEqual("Mark 23",weapons.DisplayName);Assert.AreEqual(5,weapons.Ammo);Assert.AreEqual(65,weapons.Reserve);Assert.False(dropped.gameObject.activeSelf);
            weapons.Reload();yield return new WaitForSeconds(weapons.ActiveWeapon.reloadTacticalSec+.1f);Assert.AreEqual(12,weapons.Ammo);Assert.AreEqual(58,weapons.Reserve);
            File.WriteAllText(Output+"/primary-drop-playmode.txt","Actual Mark23 gun-only textured world pickup preserves 5+65; pickup restores original animated default slot; tactical reload conserves total as 12+58.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator TouchIconsPressedStatesAndLayoutDragPreserveSafeBounds()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var hud=MobileHud.Instance;var controls=UnityEngine.Object.FindObjectsOfType<HudControl>(true);Assert.AreEqual(11,controls.Length);
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=72};
            HudControl move=null;
            foreach(var control in controls)
            {
                Assert.NotNull(control.transform.Find("Icon"),control.placement.id);
                if(control.placement.id=="move")move=control;
                if(!control.gameObject.activeInHierarchy)continue;
                data.position=RectTransformUtility.WorldToScreenPoint(null,control.transform.position);
                control.OnPointerDown(data);Assert.That(control.GetComponent<UnityEngine.UI.Image>().color.a,Is.EqualTo(.70f).Within(.001f));
                control.OnPointerUp(data);Assert.That(control.GetComponent<UnityEngine.UI.Image>().color.a,Is.EqualTo(.38f).Within(.001f));
                Vector3[] corners=new Vector3[4];((RectTransform)control.transform).GetWorldCorners(corners);
                foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);Assert.That(point.x,Is.InRange(Screen.safeArea.xMin-2,Screen.safeArea.xMax+2));Assert.That(point.y,Is.InRange(Screen.safeArea.yMin-2,Screen.safeArea.yMax+2));}
            }
            foreach(var control in controls)if(control.gameObject.activeInHierarchy){data.position=RectTransformUtility.WorldToScreenPoint(null,control.transform.position);control.OnPointerDown(data);}
            GunplayVisualTests.Save(Output+"/touch-pressed.png",UnityEngine.Object.FindObjectOfType<WeaponController>());
            foreach(var control in controls)control.ResetInput();
            var readout=GameObject.Find("Weapon status").GetComponent<RectTransform>();var readoutCorners=new Vector3[4];readout.GetWorldCorners(readoutCorners);
            foreach(var corner in readoutCorners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);Assert.That(point.y,Is.InRange(Screen.safeArea.yMin-2,Screen.safeArea.yMax+2),"range counter must not clip at top");}
            hud.SetEditing(true);float old=move.placement.x;data.position=RectTransformUtility.WorldToScreenPoint(null,move.transform.position);move.OnPointerDown(data);data.position+=Vector2.right*80;move.OnDrag(data);move.OnPointerUp(data);Assert.Greater(move.placement.x,old);
            move.placement.x=old;move.Apply();hud.SetEditing(false);
            File.WriteAllText(Output+"/touch-presentation-playmode.txt","11 atlas icon controls; active pointer-down/up alpha.70/.38; LAYOUT actual drag increases normalized x; default profile bounds within Screen.safeArea. Not a physical notched Android device proof.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator PresentationAudioLayersAreAudibleAndPoolIsBounded()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var audio=PresentationAudio.Active;Assert.NotNull(audio);Assert.AreEqual(32,audio.VoiceCapacity);
            int before=audio.ShotLayersPlayed;
            GunplayEvents.Emit(new GunplayEvent{Kind=GunplayEventKind.ShotFired,Position=audio.transform.position,Weapon=Weapon("mark23")});
            Assert.AreEqual(before+4,audio.ShotLayersPlayed);
            foreach(var source in audio.GetComponentsInChildren<AudioSource>())
            {
                if(source.clip==null || !source.clip.name.StartsWith("SYNTH_PLACEHOLDER"))continue;
                var samples=new float[source.clip.samples];Assert.True(source.clip.GetData(samples,0));
                float energy=0;foreach(float value in samples){energy+=value*value;Assert.LessOrEqual(Mathf.Abs(value),.701f);}
                Assert.Greater(energy/samples.Length,.000001f,"placeholder must not be silent");
            }
            for(int i=0;i<1000;i++)GunplayEvents.Send(GunplayEventKind.UI,audio.transform.position);
            Assert.LessOrEqual(audio.ActiveVoices,32);
            File.WriteAllText(Output+"/audio-pool-playmode.txt","Four synthesized shot layers are non-silent; clip peaks bounded; 1000 UI events retain <=32 allocated voice slots. Not mixed-output LUFS/true-peak or device audibility acceptance.");
        }
        private static void PressMenuButton(string name,Transform parent=null)
        {
            var buttons=parent!=null?parent.GetComponentsInChildren<UnityEngine.UI.Button>(true):UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>(true);
            foreach(var button in buttons)if(button.name==name){button.onClick.Invoke();return;}
            Assert.Fail("Missing menu button: "+name);
        }
        [UnityTest,Timeout(120000)] public IEnumerator GraphicsAudioMenuChangesRealSettingsAndBlocksGameplay()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var hud=MobileHud.Instance;var audio=PresentationAudio.Active;var renderer=PresentationRenderer.Active;float master=audio.BusVolume(0);var tier=renderer.Tier;var day=renderer.Day;bool drs=renderer.DynamicResolution;
            PressMenuButton("SETTINGS");yield return null;PressMenuButton("VISUAL/AUDIO");yield return null;
            var panel=GameObject.Find("Graphics and audio mix");Assert.NotNull(panel);Assert.True(hud.Modal);hud.Press("fire",true);Assert.False(hud.FireHeld);
            try
            {
                var gain=panel.transform.Find("MASTER gain").GetComponent<UnityEngine.UI.Slider>();gain.value=.25f;yield return null;
                Assert.That(audio.BusVolume(0),Is.EqualTo(.25f).Within(.0001f));Assert.True(audio.Mixer.GetFloat("BusMaster",out float db));Assert.That(db,Is.EqualTo(20*Mathf.Log10(.25f)).Within(.05f));
                PressMenuButton("QUALITY",panel.transform);yield return null;Assert.AreNotEqual(tier,renderer.Tier);PressMenuButton("DAYLIGHT",panel.transform);Assert.AreNotEqual(day,renderer.Day);
                PressMenuButton("DRS",panel.transform);Assert.AreNotEqual(drs,renderer.DynamicResolution);if(!renderer.DynamicResolution)Assert.AreEqual(1,renderer.ResolutionScale);
                GunplayVisualTests.Save(Output+"/settings-visual-audio.png",UnityEngine.Object.FindObjectOfType<WeaponController>());
            }
            finally
            {
                audio.SetBusVolume(0,master);renderer.SetTier(tier);renderer.SetDay(day);if(renderer.DynamicResolution!=drs)renderer.ToggleDynamicResolution();PressMenuButton("SAVE / CLOSE",panel.transform);
            }
            Assert.False(hud.Modal);File.WriteAllText(Output+"/graphics-audio-menu.txt","Actual menu callbacks change mixer master dB, tier, daylight and persisted DRS; modal blocks fire; close restores gameplay. Native touch/haptics/device ergonomics separate.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator UrpStackTierFallbacksAndMaterialShadersRenderWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var presentation=PresentationRenderer.Active;Assert.NotNull(presentation);
            var world=weapons.view.GetComponent<Camera>();var overlay=weapons.weaponCamera;
            var worldData=UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(world);
            var overlayData=UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(overlay);
            Assert.AreEqual(UnityEngine.Rendering.Universal.CameraRenderType.Base,worldData.renderType);Assert.AreEqual(UnityEngine.Rendering.Universal.CameraRenderType.Overlay,overlayData.renderType);
            Assert.True(overlayData.clearDepth,"overlay must clear world depth");Assert.Contains(overlay,worldData.cameraStack);Assert.False(worldData.renderPostProcessing,"do not tone-map the world twice");Assert.True(overlayData.renderPostProcessing);Assert.AreEqual(.01f,overlay.nearClipPlane,.0001f);Assert.AreEqual(1<<8,overlay.cullingMask);Assert.AreEqual(0,world.cullingMask&(1<<8));
            foreach(PresentationTier tier in Enum.GetValues(typeof(PresentationTier)))
            {
                presentation.SetTier(tier);yield return null;
                var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;Assert.NotNull(pipeline);
                Assert.AreEqual(tier==PresentationTier.PCUltra?1:4,pipeline.msaaSampleCount);Assert.AreEqual(tier==PresentationTier.PCUltra?200:80,pipeline.shadowDistance);Assert.AreEqual(tier==PresentationTier.PCUltra?4:2,pipeline.shadowCascadeCount);
            }
            presentation.SetTier(PresentationTier.High);yield return null;GunplayVisualTests.AssertUngradedHud(weapons);GunplayVisualTests.Save(Output+"/urp-stack-high.png",weapons);
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())foreach(var material in renderer.sharedMaterials)
            {Assert.NotNull(material);Assert.True(material.shader.isSupported,material.shader.name);Assert.False(UnityEditor.ShaderUtil.ShaderHasError(material.shader),material.shader.name);Assert.AreNotEqual("Standard",material.shader.name,"scene materials must be converted, not rendered pink");}
            File.WriteAllText(Output+"/urp-presentation-playmode.txt","Actual URP camera stack render, independent layer8 FPV near.01, single final post grade, 4 serialized tier assets with 4x mobile MSAA/80m2cascades and PC200m4cascades, supported scene shaders/no compiler errors. Forward+ capability fallback; PC SMAA because URP14 TAA cannot camera-stack. Not device performance or complete PBR/GI proof.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator CollectedWeaponsCycleAwayFromPickupsWithoutLosingAmmo()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            weapons.QAAmmo(5,65);var items=UnityEngine.Object.FindObjectsOfType<WeaponPickup>();
            Assert.AreEqual(1,weapons.InventoryCount);
            foreach(var item in items)
            {
                item.Store(7,73);item.transform.position=weapons.transform.position+weapons.transform.forward*.5f;
                yield return null;yield return null;input.Value.Pickup=true;yield return new WaitForSeconds(1);
                Assert.False(item.gameObject.activeSelf,"collected weapon must be holstered, not dropped");
            }
            Assert.AreEqual(4,weapons.InventoryCount);
            var cc=weapons.GetComponent<CharacterController>();cc.enabled=false;weapons.transform.position+=Vector3.right*100;cc.enabled=true;
            var seen=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<4;i++)
            {
                input.Value.Swap=true;yield return new WaitForSeconds(1);
                Assert.True(seen.Add(weapons.ActiveWeapon.id),"SWAP must visit every owned weapon once");
                Assert.AreEqual(weapons.ActiveWeapon.id=="mark23"?5:7,weapons.Ammo);
                Assert.AreEqual(weapons.ActiveWeapon.id=="mark23"?65:73,weapons.Reserve);
            }
            foreach(var item in items)Assert.False(item.gameObject.activeSelf);
            Assert.True(weapons.OwnsWeapon("mark23"));
            File.WriteAllText(Output+"/inventory-player-loop.txt","Four collected weapons survive movement away from pickup locations; actual SWAP input cycles all4 once, preserving Mark23 5+65 and secondary7+73. No automatic drops.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator CeilingImpactCancelsUpwardVelocityWithoutHeadSticking()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var motor=UnityEngine.Object.FindObjectOfType<TestPlayer>();var cc=motor.GetComponent<CharacterController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(10,1,10);
            var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(1000,2.2f,1000);roof.transform.localScale=new Vector3(5,.4f,5);
            cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();yield return new WaitForSeconds(.25f);Assert.True(cc.isGrounded);
            input.Value.Jump=true;float deadline=Time.time+1;
            while((cc.collisionFlags&CollisionFlags.Above)==0&&Time.time<deadline)yield return null;
            Assert.AreNotEqual(0,(int)(cc.collisionFlags&CollisionFlags.Above),"actual controller must hit low roof");float hitHeight=motor.transform.position.y;
            yield return new WaitForSeconds(.12f);Assert.Less(motor.transform.position.y,hitHeight-.03f,"head impact must stop ascent immediately rather than stick until gravity catches up");
            File.WriteAllText(Output+"/ceiling-impact.txt","Actual jump under2m roof: CharacterController reports Above; upward velocity cancelled and player descends within120ms. No head-sticking plateau.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator MovementDoesNotRunAgainstWallsOrAutoMantleWithoutJump()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;
            var motor=UnityEngine.Object.FindObjectOfType<TestPlayer>();var cc=motor.GetComponent<CharacterController>();var input=new ScriptedInputProvider();GameplayInput.Instance.OverrideProvider=input;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(100,1,100);
            cc.enabled=false;motor.transform.SetPositionAndRotation(new Vector3(1000,.05f,1000),Quaternion.identity);cc.enabled=true;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1000,.75f,1001);wall.transform.localScale=new Vector3(4,1.5f,1);Physics.SyncTransforms();
            input.Value.Move=Vector2.up;input.Value.Sprint=true;yield return new WaitForSeconds(.8f);
            Assert.AreNotEqual(MovementState.Mantle,motor.State,"walking into cover must not steal movement with auto-mantle");Assert.Less(motor.Speed,.2f,"blocked controller must not report running speed");
            input.Value=default(InputFrame);wall.SetActive(false);yield return new WaitForSeconds(.2f);
            input.Value.CrouchPressed=true;yield return null;yield return new WaitForSeconds(.2f);Assert.AreEqual(MovementState.Crouch,motor.State);
            input.Value.Move=Vector2.up;input.Value.Sprint=true;yield return new WaitForSeconds(.3f);Assert.True(motor.IsSprinting,"sprint should exit crouch when standing clearance exists");
            input.Value.CrouchPressed=true;yield return null;yield return null;Assert.AreEqual(MovementState.Slide,motor.State,"single crouch press must enter slide without a hold delay");
            File.WriteAllText(Output+"/movement-regressions.txt","Blocked wall reports actual zero speed, no auto-mantle without jump, sprint exits crouch, one crouch press enters slide.");
        }
        [UnityTest,Timeout(120000)] public IEnumerator DamageMarkersAndHealthAreSynchronous()
        {
            yield return SceneManager.LoadSceneAsync("CottageTest");yield return null;yield return null;
            var target=UnityEngine.Object.FindObjectOfType<TestTarget>();target.ResetTarget();int markers=0;string kind=null;
            Action<GunplayEvent> listener=e=>{if(e.Kind==GunplayEventKind.Hitmarker){markers++;kind=e.Context;}};GunplayEvents.Raised+=listener;
            try
            {
                Assert.True(target.Damage(35,HitPart.Head,target.transform.position,Weapon("mark23")));Assert.AreEqual(1,markers);Assert.AreEqual("head",kind);
                Assert.That(target.Health,Is.EqualTo(100-35*GunplayConfig.Current.headMultiplier).Within(.001));
                target.Damage(100,HitPart.Chest,target.transform.position,Weapon("mark23"));Assert.AreEqual("kill",kind);Assert.AreEqual(0,target.Health);
                Assert.False(target.Damage(1,HitPart.Head,target.transform.position,Weapon("mark23")));target.ResetTarget();Assert.AreEqual(100,target.Health);
            }
            finally{GunplayEvents.Raised-=listener;}
        }
    }
}

#endif
