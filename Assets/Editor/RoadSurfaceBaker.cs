using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fpp.Editor
{
    public static class RoadSurfaceBaker
    {
        // A complete rectangular low-poly surface; source UV islands are rasterized BEFORE
        // reducing geometry. Never interpolate across an atlas seam during decimation.
        public static Mesh Build(MeshFilter filter,out Material material)
        {
            var source=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
            if(source.subMeshCount!=1||renderer.sharedMaterials.Length!=1)throw new Exception("Expected single-material cleaned road scan");
            var input=renderer.sharedMaterial;var b=renderer.bounds;const int width=2048,height=4096;
            var shader=Shader.Find("Hidden/FPP/Road Surface Bake");if(shader==null)throw new Exception("Missing road bake shader");
            var readable=WorldDetailLod.ReadableCopy(source);var vertices=readable.vertices;var triangles=readable.triangles;
            var order=new int[triangles.Length/3];for(int i=0;i<order.Length;i++)order[i]=i;
            Array.Sort(order,(a,c)=>((vertices[triangles[a*3]].y+vertices[triangles[a*3+1]].y+vertices[triangles[a*3+2]].y).CompareTo(vertices[triangles[c*3]].y+vertices[triangles[c*3+1]].y+vertices[triangles[c*3+2]].y)));
            var sorted=new int[triangles.Length];for(int i=0;i<order.Length;i++)Array.Copy(triangles,order[i]*3,sorted,i*3,3);readable.triangles=sorted;
            var bake=new Material(shader);bake.SetVector("_Rect",new Vector4(b.min.x,b.min.z,b.size.x,b.size.z));bake.SetVector("_Height",new Vector4(b.min.y,Mathf.Max(.001f,b.size.y),0,0));
            var albedo=input.GetTexture("baseColorTexture");if(albedo==null)throw new Exception("Source road albedo missing; never replace it with generic brick art");
            bake.SetTexture("_Base",albedo);bake.SetTexture("_Normal",input.GetTexture("normalTexture"));bake.SetTexture("_Roughness",input.GetTexture("metallicRoughnessTexture"));
            bake.SetVector("_Tint",input.HasProperty("baseColorFactor")?input.GetVector("baseColorFactor"):Vector4.one);
            bake.SetFloat("_RoughnessFactor",input.HasProperty("roughnessFactor")?input.GetFloat("roughnessFactor"):1);
            bake.SetFloat("_HasNormal",input.GetTexture("normalTexture")!=null&&source.HasVertexAttribute(VertexAttribute.Tangent)?1:0);
            const string folder="Assets/Generated/Village/RoadSurface";Directory.CreateDirectory(folder);
            try{Capture(filter,readable,bake,0,width,height,folder+"/AlbedoSmoothness.png");Capture(filter,readable,bake,1,width,height,folder+"/Normal.png");}
            finally{UnityEngine.Object.DestroyImmediate(bake);UnityEngine.Object.DestroyImmediate(readable);}
            AssetDatabase.Refresh();
            foreach(string name in new[]{"AlbedoSmoothness","Normal"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+name+".png");
                importer.textureType=name=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=name!="Normal";importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Actual road scan projected surface (original image retained)";
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/AlbedoSmoothness.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Normal.png"));material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_SmoothnessTextureChannel",1);material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");material.SetFloat("_Smoothness",1);material.enableInstancing=true;
            string matPath=folder+"/Road.mat";AssetDatabase.DeleteAsset(matPath);AssetDatabase.CreateAsset(material,matPath);
            const int nx=4,nz=12;var points=new Vector3[(nx+1)*(nz+1)];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];var tangents=new Vector4[points.Length];var ix=new int[nx*nz*6];int index=0;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){int i=z*(nx+1)+x;uv[i]=new Vector2(x/(float)nx,z/(float)nz);points[i]=new Vector3(b.min.x+b.size.x*uv[i].x,.045f,b.min.z+b.size.z*uv[i].y);normals[i]=Vector3.up;tangents[i]=new Vector4(1,0,0,-1);}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*(nx+1)+x;foreach(int v in new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2})ix[index++]=v;}
            var result=new Mesh{vertices=points,normals=normals,uv=uv,tangents=tangents,triangles=ix};result.RecalculateBounds();
            File.WriteAllText("QA/runtime/road-surface-bake.txt","Actual ORIGINAL cleaned scan projected to "+width+"x"+height+" albedo/smoothness + normal atlases; no lighting baked into albedo. Continuous rectangular96triangle tile replaces unsafe scan UV seam decimation. Texture projection, normal orientation/coverage and device appearance still require visual review; source images/assets retained. Additional atlas residency is not a memory optimization claim.");
            return result;
        }
        private static void BlendEdges(Color32[] colors,int a,int b,float strength,int mode)
        {
            Color ca=colors[a],cb=colors[b],target;
            if(mode==0){target=(ca.linear+cb.linear)*.5f;target=target.gamma;target.a=(ca.a+cb.a)*.5f;}
            else {var normal=new Vector3(ca.r+cb.r-1,ca.g+cb.g-1,ca.b+cb.b-1).normalized;target=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);}
            colors[a]=Color.Lerp(ca,target,strength);colors[b]=Color.Lerp(cb,target,strength);
        }
        private static void Capture(MeshFilter source,Mesh mesh,Material material,int mode,int width,int height,string path)
        {
            var old=RenderTexture.active;bool srgb=GL.sRGBWrite;
            var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,mode==0?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear);
            Texture2D pixels=null;
            try
            {
                Graphics.SetRenderTarget(rt);GL.Clear(true,true,mode==0?new Color(.3f,.25f,.2f,1):new Color(.5f,.5f,1,0));GL.sRGBWrite=mode==0;
                material.SetFloat("_Mode",mode);if(!material.SetPass(0))throw new Exception("Road bake pass failed");Graphics.DrawMeshNow(mesh,source.transform.localToWorldMatrix);
                pixels=new Texture2D(width,height,TextureFormat.RGBA32,false,mode!=0);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                if(mode==1){var data=pixels.GetPixels32();int covered=0;foreach(var p in data)if(p.a>127)covered++;float coverage=covered/(float)data.Length;File.WriteAllText("QA/runtime/road-bake-coverage.txt",coverage.ToString("R",System.Globalization.CultureInfo.InvariantCulture));if(coverage<.98f)throw new Exception("Road bake has holes: coverage="+coverage+"; do not publish corrupted atlas");}
                // Make projected derivative repeatable without stretching the source image.
                // Only a16pixel border is feathered; opposite edges become exactly equal.
                var colors=pixels.GetPixels32();
                for(int y=0;y<height;y++)for(int x=0;x<16;x++)BlendEdges(colors,y*width+x,y*width+width-1-x,1-x/16f,mode);
                for(int x=0;x<width;x++)for(int y=0;y<16;y++)BlendEdges(colors,y*width+x,(height-1-y)*width+x,1-y/16f,mode);
                pixels.SetPixels32(colors);pixels.Apply();
                File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{GL.sRGBWrite=srgb;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
        }
    }
}
