using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    // Replace only the irregular terrain skirt; preserve the repaired original facade.
    public static class MosqueSurfaceRepair
    {
        public static void Apply(GameObject scan, Bounds bounds)
        {
            float top=bounds.min.y+.95f;
            foreach(var renderer in scan.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                    if(material.HasProperty("_TrimBelow"))material.SetFloat("_TrimBelow",top+.005f);
            var stone=Material("Mosque foundation","Stone",new Color(.8f,.72f,.62f),new Vector2(6,1));
            Vector3 center=bounds.center;
            var footprint=new Vector3(bounds.size.x*.84f,0,bounds.size.z*.84f);
            foreach(var renderer in scan.GetComponentsInChildren<Renderer>())
            foreach(var material in renderer.sharedMaterials)
            {
                material.SetFloat("_FlattenBelow",top+1.2f);
                material.SetVector("_SkirtRect",new Vector4(center.x-footprint.x*.5f,center.z-footprint.z*.5f,center.x+footprint.x*.5f,center.z+footprint.z*.5f));
            }
            Cube(scan,"Level stone plinth",new Vector3(center.x,top*.5f,center.z),new Vector3(footprint.x+.1f,top,footprint.z+.1f),stone);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<4;i++)
                {
                    float h=top*(i+1)/5f;
                    Cube(scan,"Approach step",new Vector3(center.x,h*.5f,center.z+side*(footprint.z*.5f+(4-i)*.32f)),new Vector3(2.5f,h,.65f),stone);
                }
            }
            Debug.Log("FPP mosque repair: preserved original facade, cropped scan skirt and level plinth");
        }
        private static Material Material(string name,string texture,Color tint,Vector2 tiling)
        {
            string path="Assets/Materials/TestMap/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Ground/"+texture+".png");
            mat.color=tint;mat.mainTextureScale=tiling;mat.SetFloat("_Glossiness",0);return mat;
        }
        private static void Cube(GameObject parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.position=position;go.transform.localScale=size;go.transform.SetParent(parent.transform,true);
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.isStatic=true;
        }
    }
}
