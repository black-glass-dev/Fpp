using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class MosqueSurfaceRepair
    {
        public static void Apply(GameObject scan,Bounds bounds)
        {
            float floor=bounds.min.y+.95f;Vector3 c=bounds.center;
            float innerX=4.2f,innerZ=3.8f,height=6.2f;
            Vector4 walls=MeasureOuterWalls(scan,c,floor);float tunnelEnd=walls.w-.3f;
            foreach(var renderer in scan.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
            {
                material.SetFloat("_TrimBelow",floor+.005f);material.SetFloat("_ScanGroundBelow",floor+1.2f);
                // Never discard upward-facing mouldings on the building itself.
                material.SetVector("_SkirtRect",new Vector4(c.x-6.7f,c.z-6.6f,c.x+6.7f,c.z+6.6f));
                material.SetVector("_InteriorRect",new Vector4(c.x-innerX-.08f,c.z-innerZ-.08f,c.x+innerX+.08f,c.z+innerZ+.08f));
                material.SetFloat("_InteriorTop",floor+height+.15f);
                material.SetVector("_Entry",new Vector4(c.x,c.z+innerZ-.1f,c.z+tunnelEnd+.1f,floor+3.2f));
            }
            var stone=Material("Mosque foundation","Stone",new Color(.8f,.72f,.62f),new Vector2(6,1));
            var brick=Material("Mosque interior brick","Brick",Color.white,new Vector2(6,4));
            var plaster=Material("Mosque interior ceiling","Stone",new Color(.7f,.65f,.56f),new Vector2(3,3));
            var footprint=new Vector3(bounds.size.x*.84f,0,bounds.size.z*.84f);
            Cube(scan,"Level stone plinth",new Vector3(c.x,floor*.5f,c.z),new Vector3(footprint.x+.1f,floor,footprint.z+.1f),stone);
            Cube(scan,"Clean interior floor",new Vector3(c.x,floor+.018f,c.z),new Vector3(8.6f,.035f,7.8f),stone);
            Cube(scan,"Clean interior ceiling",new Vector3(c.x,floor+height+.08f,c.z),new Vector3(8.6f,.16f,7.8f),plaster);
            // An intentionally approximate, clean room, not a historical reconstruction.
            Cube(scan,"Interior west wall",new Vector3(c.x-innerX,floor+height*.5f,c.z),new Vector3(.18f,height,7.8f),brick);
            Cube(scan,"Interior east wall",new Vector3(c.x+innerX,floor+height*.5f,c.z),new Vector3(.18f,height,7.8f),brick);
            Cube(scan,"Interior rear wall",new Vector3(c.x,floor+height*.5f,c.z-innerZ),new Vector3(8.6f,height,.18f),brick);
            Front(scan,c,floor,height,innerZ,8.4f,brick,true);
            // Solid collision at the actual exterior, as well as inside the new room.
            float depth=walls.w-walls.z,wallCentreZ=c.z+(walls.z+walls.w)*.5f;
            Collision(scan,new Vector3(c.x+walls.x+.16f,floor+height*.5f,wallCentreZ),new Vector3(.32f,height,depth));
            Collision(scan,new Vector3(c.x+walls.y-.16f,floor+height*.5f,wallCentreZ),new Vector3(.32f,height,depth));
            Collision(scan,new Vector3(c.x+(walls.x+walls.y)*.5f,floor+height*.5f,c.z+walls.z+.16f),new Vector3(walls.y-walls.x,height,.32f));
            Front(scan,c,floor,height,walls.w-.10f,walls.y-walls.x,null,false,walls.x,walls.y);
            foreach(float x in new[]{walls.x,walls.y})foreach(float z in new[]{walls.z,walls.w})
            {
                var pillar=new GameObject("Mosque corner pillar collision");pillar.transform.SetParent(scan.transform,true);
                pillar.transform.position=new Vector3(c.x+x,floor+height*.5f,c.z+z);
                var capsule=pillar.AddComponent<CapsuleCollider>();capsule.radius=.65f;capsule.height=height;
            }
            foreach(float x in new[]{-3.55f,0,3.55f})
            {
                for(int side=-1;side<=1;side+=2)
                    Cube(scan,"Door passage jamb",new Vector3(c.x+x+side*.95f,floor+1.6f,c.z+(innerZ+tunnelEnd)*.5f),new Vector3(.13f,3.2f,tunnelEnd-innerZ),brick);
                Cube(scan,"Door passage ceiling",new Vector3(c.x+x,floor+3.22f,c.z+(innerZ+tunnelEnd)*.5f),new Vector3(2.03f,.12f,tunnelEnd-innerZ),brick);
            }
            for(int i=0;i<5;i++)
            {
                float h=floor*(i+1)/5f;
                Cube(scan,"Approach step",new Vector3(c.x,h*.5f,c.z+footprint.z*.5f+(4-i)*.30f+.15f),new Vector3(3.1f,h,.62f),stone);
            }
            Debug.Log("FPP mosque: pillar mouldings preserved; scan interior clipped; clean approximate room, three front passages and wall collision");
        }
        private static Vector4 MeasureOuterWalls(GameObject scan,Vector3 centre,float floor)
        {
            // Area-weighted vertical-plane histograms locate the actual asymmetric scan
            // walls; its full bounds include scan-ground garbage and are not a building box.
            var hx=new float[180];var hz=new float[180];
            foreach(var filter in scan.GetComponentsInChildren<MeshFilter>())
            {
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;var matrix=filter.transform.localToWorldMatrix;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 a=matrix.MultiplyPoint3x4(vertices[triangles[i]]),b=matrix.MultiplyPoint3x4(vertices[triangles[i+1]]),c=matrix.MultiplyPoint3x4(vertices[triangles[i+2]]);
                    Vector3 mid=(a+b+c)/3,normal=Vector3.Cross(b-a,c-a);float area=normal.magnitude;
                    if(mid.y<floor+1 || mid.y>floor+4.5f || area<.000001f)continue;
                    normal/=area;int x=Mathf.FloorToInt((mid.x-centre.x+9)*10),z=Mathf.FloorToInt((mid.z-centre.z+9)*10);
                    if(Mathf.Abs(normal.x)>.85f && x>=0 && x<hx.Length)hx[x]+=area;
                    if(Mathf.Abs(normal.z)>.85f && z>=0 && z<hz.Length)hz[z]+=area;
                }
            }
            var walls=new Vector4(OuterPlane(hx,false),OuterPlane(hx,true),OuterPlane(hz,false),OuterPlane(hz,true));
            Debug.Log("FPP measured outer mosque planes (left/right/rear/front): "+walls);return walls;
        }
        private static float OuterPlane(float[] histogram,bool positive)
        {
            float peak=0,result=0;
            for(int i=0;i<histogram.Length;i++){float p=i*.1f-8.95f;if(Mathf.Abs(p)>4.5f && Mathf.Abs(p)<7.5f && (p>0)==positive)peak=Mathf.Max(peak,histogram[i]);}
            if(peak<=0)throw new System.Exception("Missing native mosque wall plane");
            for(int i=0;i<histogram.Length;i++){float p=i*.1f-8.95f;if(Mathf.Abs(p)>4.5f && Mathf.Abs(p)<7.5f && (p>0)==positive && histogram[i]>=peak*.2f && Mathf.Abs(p)>Mathf.Abs(result))result=p;}
            return result;
        }
        private static void Front(GameObject root,Vector3 c,float floor,float height,float z,float width,Material mat,bool visible,float left=float.NaN,float right=float.NaN)
        {
            float start=float.IsNaN(left)?-width*.5f:left;float finish=float.IsNaN(right)?width*.5f:right;
            foreach(float door in new[]{-3.55f,0,3.55f})
            {
                float end=door-.95f;
                if(end>start)Panel(root,new Vector3(c.x+(start+end)*.5f,floor+height*.5f,c.z+z),new Vector3(end-start,height,.20f),mat,visible);
                Panel(root,new Vector3(c.x+door,floor+3.2f+(height-3.2f)*.5f,c.z+z),new Vector3(1.9f,height-3.2f,.20f),mat,visible);
                start=door+.95f;
            }
            if(start<finish)Panel(root,new Vector3(c.x+(start+finish)*.5f,floor+height*.5f,c.z+z),new Vector3(finish-start,height,.20f),mat,visible);
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
