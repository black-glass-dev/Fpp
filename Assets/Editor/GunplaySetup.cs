using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class GunplaySetup
    {
        public static void Configure(GameObject player,WeaponController weapons)
        {
            var config=GunplayConfig.Current;
            if(player.GetComponent<GameplayInput>()==null)player.AddComponent<GameplayInput>();
            player.AddComponent<PlayerHealth>();player.AddComponent<GunplayFeedback>();
            var camera=weapons.view.GetComponent<Camera>();camera.fieldOfView=config.worldFOV;camera.nearClipPlane=config.nearClip;
            camera.allowDynamicResolution=true;
            var animation=weapons.defaultViewmodel.GetComponentInChildren<Animation>();
            AnimationClip source=null;string key=null;
            foreach(AnimationState state in animation)if(state.name.ToLowerInvariant().Contains("reload")){source=state.clip;key=state.name;break;}
            if(source==null)throw new Exception("Preserved Mark23 reload missing");
            Directory.CreateDirectory("Assets/Generated/Gunplay");
            var clone=UnityEngine.Object.Instantiate(source);clone.name="Authored Mark23 reload with foley";clone.legacy=true;
            var stages=new[]{"magOut","magIn","boltRelease"};var times=new[]{config.magOutMarker,config.magInMarker,config.boltMarker};var events=new AnimationEvent[stages.Length];
            for(int i=0;i<stages.Length;i++)events[i]=new AnimationEvent{functionName="ReloadFoleyMarker",stringParameter=stages[i],time=clone.length*times[i]};
            AnimationUtility.SetAnimationEvents(clone,events);string path="Assets/Generated/Gunplay/Mark23Reload.anim";
            AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(clone,path);animation.RemoveClip(key);animation.AddClip(clone,key);
            var relay=animation.gameObject.AddComponent<ReloadEventRelay>();relay.controller=weapons;
            var sockets=weapons.defaultViewmodel.AddComponent<WeaponSockets>();sockets.Build(weapons.muzzle,weapons.defaultSight,Resources.Load<WeaponAsset>("Gunplay/mark23"));
            foreach(var pickup in UnityEngine.Object.FindObjectsOfType<WeaponPickup>())pickup.config=pickup.Asset;
            foreach(var target in UnityEngine.Object.FindObjectsOfType<TestTarget>())
            {
                var rootCollider=target.GetComponent<Collider>();if(rootCollider!=null)rootCollider.enabled=false;
                Part(target,"Head",HitPart.Head,new Vector3(0,.35f,0),new Vector3(.6f,.3f,1));
                Part(target,"Chest",HitPart.Chest,new Vector3(0,.08f,0),new Vector3(.64f,.24f,1));
                Part(target,"Stomach",HitPart.Stomach,new Vector3(0,-.16f,0),new Vector3(.64f,.24f,1));
                Part(target,"Legs",HitPart.Legs,new Vector3(0,-.39f,0),new Vector3(.55f,.22f,1));
                Part(target,"Left arm",HitPart.Arms,new Vector3(-.41f,.05f,0),new Vector3(.18f,.5f,1));
                Part(target,"Right arm",HitPart.Arms,new Vector3(.41f,.05f,0),new Vector3(.18f,.5f,1));
            }
            foreach(var collider in UnityEngine.Object.FindObjectsOfType<Collider>())
            {
                if(collider.GetComponentInParent<WeaponController>()!=null||collider.GetComponentInParent<WeaponPickup>()!=null||collider.GetComponentInParent<TestTarget>()!=null)continue;
                if(collider.GetComponent<SurfaceProfile>()==null)
                {
                    var surface=collider.gameObject.AddComponent<SurfaceProfile>();
                    string name=collider.name.ToLowerInvariant();surface.surface=name.Contains("ground")?"earth":name.Contains("road")?"dirt":name.Contains("tree")?"wood":"stone";
                    surface.penetration=name.Contains("wood")?PenetrationClass.Wood:PenetrationClass.None;
                }
            }
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name="PLACEHOLDER ammo box";box.transform.position=player.transform.position+new Vector3(-2,.3f,2);box.transform.localScale=Vector3.one*.5f;
            box.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/WeaponRig/MagazineMetal.mat");
            box.GetComponent<BoxCollider>().isTrigger=true;var body=box.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;box.AddComponent<AmmoBox>();
        }
        private static void Part(TestTarget target,string name,HitPart kind,Vector3 position,Vector3 size)
        {
            var child=new GameObject("Target "+name);child.transform.SetParent(target.transform,false);child.transform.localPosition=position;
            child.AddComponent<BoxCollider>().size=size;var part=child.AddComponent<TargetPart>();part.target=target;part.part=kind;
        }
    }
}
