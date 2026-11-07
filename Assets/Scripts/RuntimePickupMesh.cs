using System.Collections.Generic;
using UnityEngine;
namespace Fpp
{
    // One startup snapshot of the pistol-only skin. Never bakes the arms or unreadable AK meshes.
    public sealed class RuntimePickupMesh : MonoBehaviour
    {
        private readonly List<Mesh> owned=new List<Mesh>();
        public WeaponPickup Build(WeaponController owner,WeaponAsset asset)
        {
            transform.SetPositionAndRotation(owner.weaponMount.position,owner.weaponMount.rotation);
            foreach(var renderer in owner.pistolRenderers)
            {
                var mesh=new Mesh();var skin=renderer as SkinnedMeshRenderer;
                if(skin!=null)skin.BakeMesh(mesh,false);
                else
                {
                    var filter=renderer.GetComponent<MeshFilter>();
                    if(filter==null||filter.sharedMesh==null){Destroy(mesh);continue;}
                    Destroy(mesh);mesh=Instantiate(filter.sharedMesh);
                }
                owned.Add(mesh);
                var part=new GameObject("Dropped Mark23 gun mesh (no hands)",typeof(MeshFilter),typeof(MeshRenderer));
                part.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);part.transform.localScale=renderer.transform.lossyScale;part.transform.SetParent(transform,true);
                part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            }
            var bounds=WeaponController.LocalBounds(transform);
            foreach(Transform child in transform)child.localPosition-=bounds.center;
            float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            if(longest<=0)throw new System.InvalidOperationException("Default pistol world pickup has no gun-only geometry");
            transform.localScale=Vector3.one*(asset.pickupLength/longest);
            gameObject.AddComponent<BoxCollider>().size=bounds.size;
            var pickup=gameObject.AddComponent<WeaponPickup>();pickup.displayName=asset.displayName;pickup.config=asset;
            WeaponController.SetLayer(gameObject,0);gameObject.SetActive(false);return pickup;
        }
        private void OnDestroy(){foreach(var mesh in owned)if(mesh!=null)Destroy(mesh);}
    }
}
