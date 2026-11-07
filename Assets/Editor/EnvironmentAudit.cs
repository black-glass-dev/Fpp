using System;
using UnityEngine;
namespace Fpp.Editor
{
    public static class EnvironmentAudit
    {
        public static void Validate()
        {
            Physics.SyncTransforms();
            var ground=GameObject.Find("Sculpted ground");
            if(ground==null || ground.GetComponent<MeshCollider>()==null || ground.GetComponent<MeshFilter>().sharedMesh.bounds.size.y<.15f)
                throw new Exception("Ground must have real relief and matching collision");
            if(GroundBuilder.Height(0,-20)!=0 || GroundBuilder.Height(20,26)!=0)
                throw new Exception("Road and cottage pads must remain level");
            var trunks=UnityEngine.Object.FindObjectsOfType<CapsuleCollider>();int count=0;
            foreach(var trunk in trunks)if(trunk.gameObject.name.ToLowerInvariant().Contains("tree"))
            {count++;if(trunk.bounds.size.x>1 || trunk.GetComponent<BoxCollider>()!=null)throw new Exception("Tree canopy must not block walking");}
            if(count!=2)throw new Exception("Both trees require narrow trunk collision");
            for(int x=-4;x<=4;x+=2)for(int z=-4;z<=4;z+=2)
                if(Physics.CheckSphere(new Vector3(x,2.1f,32+z),.25f,~(1<<8),QueryTriggerInteraction.Ignore))throw new Exception("Obstruction in cleaned mosque room");
            foreach(Vector3 direction in new[]{Vector3.right,Vector3.left,Vector3.back})
                if(!Physics.Raycast(new Vector3(0,2.1f,32),direction,7,~(1<<8),QueryTriggerInteraction.Ignore))throw new Exception("Mosque walls need collision from inside");
            foreach(float x in new[]{-3.55f,0,3.55f})
                if(Physics.SphereCast(new Vector3(x,2.1f,40),.25f,Vector3.back,out RaycastHit hit,8,~(1<<8),QueryTriggerInteraction.Ignore))throw new Exception("Front passage blocked by "+hit.collider.name);
            Debug.Log("FPP environment audit passed: relief/collider, flat pads, two narrow trunks, clear interior, solid walls and three front passages");
        }
    }
}
