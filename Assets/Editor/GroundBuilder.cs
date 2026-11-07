using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Fpp.Editor
{
    public static class GroundBuilder
    {
        public static float Height(float x,float z)
        {
            float mask=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,Mathf.Abs(x)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(6.5f,10,Mathf.Abs(z)));
            for(int row=-1;row<=1;row++)for(int side=-1;side<=1;side+=2)
            {
                float outside=Mathf.Max(Mathf.Abs(x-side*20)-8,Mathf.Abs(z-row*26)-8);
                mask*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,3,outside));
            }
            float mosque=Mathf.Max(Mathf.Abs(x)-9.5f,Mathf.Abs(z-32)-10.5f);
            mask*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,3,mosque));
            return mask*(.1f+.65f*Mathf.PerlinNoise(x*.13f+51,z*.13f+91)+.13f*Mathf.PerlinNoise(x*.43f+7,z*.43f+9));
        }
        public static void Create(Material grass)
        {
            const int width=71,depth=91;var vertices=new Vector3[width*depth];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int z=0;z<depth;z++)for(int x=0;x<width;x++)
            {int i=z*width+x;float px=x-35,pz=z-45;vertices[i]=new Vector3(px,Height(px,pz),pz);uv[i]=new Vector2(x/4f,z/4f);}
            for(int z=0;z<depth-1;z++)for(int x=0;x<width-1;x++)
            {int i=z*width+x;triangles.AddRange(new[]{i,i+width,i+1,i+1,i+width,i+width+1});}
            var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            SaveMesh(mesh,"Terrain");var go=new GameObject("Sculpted ground",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=grass;go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;go.isStatic=true;
            var v=new List<Vector3>();var indices=new List<int>();var random=new System.Random(3107);
            for(int i=0;i<120;i++)
            {
                float x=(float)random.NextDouble()*20-10,z=(float)random.NextDouble()*65-30;
                if(Mathf.Abs(x)<5.4f || Mathf.Abs(z)<6.5f || z>21)continue;
                float radius=.05f+(float)random.NextDouble()*.1f,h=radius*.6f;Vector3 c=new Vector3(x,Height(x,z)-.01f,z);int b=v.Count;
                v.Add(c+Vector3.up*h);for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;v.Add(c+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.8f));}
                for(int k=0;k<6;k++)indices.AddRange(new[]{b,b+1+k,b+1+(k+1)%6});
            }
            var rocks=new Mesh();rocks.SetVertices(v);rocks.SetTriangles(indices,0);rocks.RecalculateNormals();rocks.RecalculateBounds();SaveMesh(rocks,"Pebbles");
            var stones=new GameObject("Batched small ground stones",typeof(MeshFilter),typeof(MeshRenderer));stones.GetComponent<MeshFilter>().sharedMesh=rocks;
            string path="Assets/Materials/TestMap/Ground stones.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=new Color(.3f,.29f,.25f);mat.SetFloat("_Glossiness",0);
            stones.GetComponent<Renderer>().sharedMaterial=mat;stones.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;stones.isStatic=true;
        }
        private static void SaveMesh(Mesh mesh,string name)
        {Directory.CreateDirectory("Assets/Generated/Ground");AssetDatabase.Refresh();string path="Assets/Generated/Ground/"+name+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);}
    }
}
