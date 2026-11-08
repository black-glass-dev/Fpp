using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    // Only add detail to opaque materials with no supplied base-colour image.
    // Intentionally plain glass/metal/leaves and existing source PBR images are retained.
    public static class VillageSurfaceRepair
    {
        public static void Apply(Renderer renderer,string pack,string model)
        {
            if(pack!="House"&&pack!="Road")return;
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null)continue;
                if(source.HasProperty("baseColorTexture")&&source.GetTexture("baseColorTexture")!=null)continue;
                string name=source.name.ToLowerInvariant();string kind=null;
                if(pack=="Road"&&name.Contains("shoulder_earth"))kind="Earth";
                else if(model.IndexOf("shop",StringComparison.OrdinalIgnoreCase)>=0)kind="Wood";
                else if(name.Contains("wood")||name.Contains("timber")||name.Contains("walnut"))kind="Wood";
                else if(name.Contains("plaster")||name.Contains("lime"))kind="Plaster";
                else if(name.Contains("concrete")||name.Contains("tile")||name.Contains("cement"))kind="Concrete";
                if(kind==null)continue;
                // Transparent surfaces must not accidentally become opaque detail materials.
                if(source.renderQueue>=2450)continue;
                const string folder="Assets/Generated/Village/SurfaceMaterials";
                if(!Directory.Exists(folder)){Directory.CreateDirectory(folder);AssetDatabase.Refresh();}
                var key=Hash128.Compute(AssetDatabase.GetAssetPath(source)+"/"+source.name+"/"+model);
                string path=folder+"/"+key+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null)
                {
                    material=new Material(source){name=source.name+" (original metric detail)"};
                    var tint=source.HasProperty("baseColorFactor")?(Color)source.GetVector("baseColorFactor"):source.HasProperty("_Color")?source.color:Color.white;
                    if(model.IndexOf("shop",StringComparison.OrdinalIgnoreCase)>=0)tint=new Color(.72f,.62f,.45f);
                    if(kind=="Earth")tint=Color.white;
                    var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(kind=="Earth"?"Assets/Art/Ground/Dirt.png":"Assets/Generated/Village/SurfaceTextures/"+kind+"_BaseColor.png");
                    var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(kind=="Earth"?"Assets/Art/Ground/DirtNormal.png":"Assets/Generated/Village/SurfaceTextures/"+kind+"_Normal.png");
                    if(albedo==null||normal==null)throw new Exception("Missing original surface maps: "+kind);
                    bool suppliedPbrDetail=source.HasProperty("normalTexture")&&source.GetTexture("normalTexture")!=null||source.HasProperty("metallicRoughnessTexture")&&source.GetTexture("metallicRoughnessTexture")!=null;
                    if(suppliedPbrDetail&&source.HasProperty("baseColorTexture"))
                    {
                        // Retain valid authored normal/roughness data and its original shader/UVs.
                        material.SetTexture("baseColorTexture",albedo);
                    }
                    else
                    {
                        material.shader=Shader.Find("FPP/Metric Surface Lit");if(material.shader==null)throw new Exception("Missing metric surface shader");
                        material.SetTexture("_MainTex",albedo);material.SetTexture("_BumpMap",normal);material.SetColor("_Color",tint);
                        material.SetFloat("_DetailScale",1);material.SetFloat("_Smoothness",kind=="Wood"?.2f:.16f);
                    }
                    AssetDatabase.CreateAsset(material,path);
                    Directory.CreateDirectory("QA/runtime");File.AppendAllText("QA/runtime/material-detail-repairs.txt",model+" / "+source.name+" -> "+kind+"; original source unchanged; existing images preserved\n");
                }
                materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
    }
}
