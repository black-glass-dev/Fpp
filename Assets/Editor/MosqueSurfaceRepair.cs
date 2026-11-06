using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    // Lightweight architectural backing, not a claim to reconstruct missing scan data.
    public static class MosqueSurfaceRepair
    {
        public static void Apply(GameObject scan, Bounds bounds)
        {
            float top=bounds.min.y+.95f;
            foreach(var renderer in scan.GetComponentsInChildren<Renderer>())
                foreach(var material in renderer.sharedMaterials)
                    if(material.HasProperty("_TrimBelow"))material.SetFloat("_TrimBelow",top+.005f);
            var stone=Material("Mosque foundation","Stone",new Color(.8f,.72f,.62f),new Vector2(6,1));
            var brick=Material("Mosque inner brick backing","Brick",Color.white,new Vector2(6,3));
            Vector3 center=bounds.center;
            Cube(scan,"Level stone plinth",new Vector3(center.x,top*.5f,center.z),new Vector3(bounds.size.x+.1f,top,bounds.size.z+.1f),stone);
            float width=bounds.size.x*.74f,depth=bounds.size.z*.74f,height=bounds.size.y*.65f;
            // Keep openings in the backing so it cannot seal the original entrance.
            for(int side=-1;side<=1;side+=2)
            {
                DoorWall(scan,new Vector3(center.x,top,center.z+side*depth*.5f),width,height,false,brick);
                DoorWall(scan,new Vector3(center.x+side*width*.5f,top,center.z),depth,height,true,brick);
                for(int i=0;i<4;i++)
                {
                    float h=top*(i+1)/5f;
                    Cube(scan,"Approach step",new Vector3(center.x,h*.5f,center.z+side*(bounds.size.z*.5f+(4-i)*.32f)),new Vector3(2.5f,h,.65f),stone);
                }
            }
            Debug.Log("FPP mosque repair: trimmed scan skirt, level plinth, backing walls with open doorways");
        }
        private static void DoorWall(GameObject parent,Vector3 foot,float span,float height,bool rotate,Material material)
        {
            const float doorWidth=2.1f,doorHeight=2.7f,thickness=.16f;
            float wing=(span-doorWidth)*.5f;
            for(int side=-1;side<=1;side+=2)
            {
                var offset=new Vector3(side*(doorWidth+wing)*.5f,height*.5f,0);
                var size=new Vector3(wing,height,thickness);
                if(rotate){offset=new Vector3(0,offset.y,offset.x);size=new Vector3(size.z,size.y,size.x);}
                Cube(parent,"Inner facade backing",foot+offset,size,material);
            }
            var lintel=new Vector3(span,height-doorHeight,thickness);
            if(rotate)lintel=new Vector3(lintel.z,lintel.y,lintel.x);
            Cube(parent,"Backing above doorway",foot+Vector3.up*(doorHeight+(height-doorHeight)*.5f),lintel,material);
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
