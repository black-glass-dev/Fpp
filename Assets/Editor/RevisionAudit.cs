using System;
using UnityEngine;

namespace Fpp.Editor
{
    public static class RevisionAudit
    {
        public static void Validate()
        {
            var weapons=UnityEngine.Object.FindObjectOfType<WeaponController>();
            if(weapons==null || weapons.defaultViewmodel==null)throw new Exception("No default pistol viewmodel");
            if(weapons.defaultViewmodel.GetComponentsInChildren<SkinnedMeshRenderer>().Length<2)throw new Exception("Default arms lost their original skin");
            var animation=weapons.defaultViewmodel.GetComponentInChildren<Animation>();
            bool draw=false,reload=false,shoot=false;
            foreach(AnimationState state in animation)
            { string n=state.name.ToLowerInvariant();draw|=n.Contains("draw");reload|=n.Contains("reload");shoot|=n.Contains("shoot"); }
            if(!draw || !reload || !shoot)throw new Exception("Authored pistol clips missing");
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                if(renderer is LineRenderer)continue;
                if(renderer.sharedMaterials.Length==0)throw new Exception("Unbound renderer: "+renderer.name);
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material==null)throw new Exception("Missing material on "+renderer.name);
                    if(material.shader.name=="Standard" && material.mainTexture==null)
                        throw new Exception("Diffuse texture not bound: "+renderer.name);
                }
                if(renderer.name.ToLowerInvariant().Contains("kriss"))throw new Exception("Excluded Kriss model in scene");
            }
            if(UnityEngine.Object.FindObjectsOfType<WeaponPickup>().Length!=3)throw new Exception("Expected three scattered pickups, no arm models on ground");
            Debug.Log("FPP revision audit passed: skinned default pistol, authored clips, textured renderers, three pickups");
        }
    }
}
