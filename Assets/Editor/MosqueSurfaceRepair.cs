using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class MosqueSurfaceRepair
    {
        public static void Apply(GameObject scan,Bounds bounds)
        {
            float floor=bounds.min.y+.95f;Vector3 c=bounds.center;
            float innerX=5.1f,innerZ=5.1f,height=6.2f;
            foreach(var renderer in scan.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            {
                material.SetFloat("_TrimBelow",floor+.005f);material.SetFloat("_ScanGroundBelow",floor+1.2f);
                // Never discard upward-facing mouldings on the building itself.
                material.SetVector("_SkirtRect",new Vector4(c.x-6.7f,c.z-6.6f,c.x+6.7f,c.z+6.6f));
                material.SetVector("_InteriorRect",new Vector4(c.x-innerX-.08f,c.z-innerZ-.08f,c.x+innerX+.08f,c.z+innerZ+.08f));
                material.SetFloat("_InteriorTop",floor+height+.15f);
                material.SetVector("_Entry",new Vector4(c.x,c.z+4.9f,c.z+6.1f,floor+3.2f));
            }
            var stone=Material("Mosque foundation","Stone",new Color(.8f,.72f,.62f),new Vector2(6,1));
            var brick=Material("Mosque interior brick","Brick",Color.white,new Vector2(6,4));
            var plaster=Material("Mosque interior ceiling","Stone",new Color(.7f,.65f,.56f),new Vector2(3,3));
            var footprint=new Vector3(bounds.size.x*.84f,0,bounds.size.z*.84f);
            Cube(scan,"Level stone plinth",new Vector3(c.x,floor*.5f,c.z),new Vector3(footprint.x+.1f,floor,footprint.z+.1f),stone);
            Cube(scan,"Clean interior floor",new Vector3(c.x,floor+.018f,c.z),new Vector3(10.4f,.035f,10.4f),stone);
            Cube(scan,"Clean interior ceiling",new Vector3(c.x,floor+height+.08f,c.z),new Vector3(10.5f,.16f,10.5f),plaster);
            // An intentionally approximate, clean room, not a historical reconstruction.
            Cube(scan,"Interior west wall",new Vector3(c.x-innerX,floor+height*.5f,c.z),new Vector3(.18f,height,10.4f),brick);
            Cube(scan,"Interior east wall",new Vector3(c.x+innerX,floor+height*.5f,c.z),new Vector3(.18f,height,10.4f),brick);
            Cube(scan,"Interior rear wall",new Vector3(c.x,floor+height*.5f,c.z-innerZ),new Vector3(10.4f,height,.18f),brick);
            Front(scan,c,floor,height,innerZ,10.4f,brick,true);
            // Solid collision at the actual exterior, as well as inside the new room.
            Collision(scan,new Vector3(c.x-6.15f,floor+height*.5f,c.z),new Vector3(.32f,height,12.3f));
            Collision(scan,new Vector3(c.x+6.15f,floor+height*.5f,c.z),new Vector3(.32f,height,12.3f));
            Collision(scan,new Vector3(c.x,floor+height*.5f,c.z-6.05f),new Vector3(12.3f,height,.32f));
            Front(scan,c,floor,height,6.05f,12.3f,null,false);
            foreach(float x in new[]{-3.55f,0,3.55f})
            {
                for(int side=-1;side<=1;side+=2)
                    Cube(scan,"Door passage jamb",new Vector3(c.x+x+side*.95f,floor+1.6f,c.z+5.6f),new Vector3(.13f,3.2f,1.0f),brick);
                Cube(scan,"Door passage ceiling",new Vector3(c.x+x,floor+3.22f,c.z+5.6f),new Vector3(2.03f,.12f,1.0f),brick);
            }
            for(int i=0;i<5;i++)
            {
                float h=floor*(i+1)/5f;
                Cube(scan,"Approach step",new Vector3(c.x,h*.5f,c.z+footprint.z*.5f+(4-i)*.30f+.15f),new Vector3(3.1f,h,.62f),stone);
            }
            Debug.Log("FPP mosque: pillar mouldings preserved; scan interior clipped; clean approximate room, three front passages and wall collision");
        }
        private static void Front(GameObject root,Vector3 c,float floor,float height,float z,float width,Material mat,bool visible)
        {
            float start=-width*.5f;
            foreach(float door in new[]{-3.55f,0,3.55f})
            {
                float end=door-.95f;
                if(end>start)Panel(root,new Vector3(c.x+(start+end)*.5f,floor+height*.5f,c.z+z),new Vector3(end-start,height,.20f),mat,visible);
                Panel(root,new Vector3(c.x+door,floor+3.2f+(height-3.2f)*.5f,c.z+z),new Vector3(1.9f,height-3.2f,.20f),mat,visible);
                start=door+.95f;
            }
            if(start<width*.5f)Panel(root,new Vector3(c.x+(start+width*.5f)*.5f,floor+height*.5f,c.z+z),new Vector3(width*.5f-start,height,.20f),mat,visible);
        }
        private static void Panel(GameObject root,Vector3 position,Vector3 size,Material mat,bool visible)
        {if(visible)Cube(root,"Interior front wall",position,size,mat);else Collision(root,position,size);}
        private static void Collision(GameObject root,Vector3 position,Vector3 size)
        {var go=new GameObject("Mosque wall collision");go.transform.position=position;go.transform.SetParent(root.transform,true);go.AddComponent<BoxCollider>().size=size;go.isStatic=true;}
        private static Material Material(string name,string texture,Color tint,Vector2 tiling)
        {
            string path="Assets/Materials/TestMap/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.shader=Shader.Find(texture=="Brick"?"FPP/World Brick":"Standard");
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Ground/"+texture+".png");mat.color=tint;mat.mainTextureScale=tiling;mat.SetFloat("_Glossiness",0);return mat;
        }
        private static void Cube(GameObject root,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=size;go.transform.SetParent(root.transform,true);
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.isStatic=true;
        }
    }
}
