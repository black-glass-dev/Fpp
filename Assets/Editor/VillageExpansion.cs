using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Fpp.Editor
{
    // Scene-only placement. Original GLBs, material images and mesh topology remain unchanged.
    public static class VillageExpansion
    {
        [Serializable] private class Catalog { public Entry[] models; }
        [Serializable] private class Entry { public string pack,name,assetPath,sourcePath; }
        [Serializable] private class Usage { public string source,placement;public Vector3 size,position;public int instances; }
        [Serializable] private class Receipt { public Usage[] files;public string status="Placed all18 originals; visual/interior/device acceptance pending"; }
        public static void Build(Material ground,Material lane)
        {
            const string path="Assets/Generated/Village/catalog.json";
            if(!File.Exists(path))throw new Exception("All18 supplied village models must be staged before scene generation");
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(path));
            if(catalog.models==null||catalog.models.Length!=18)throw new Exception("Village inventory must contain all18 models");
            foreach(string boundary in new[]{"North boundary","South boundary","West boundary","East boundary"})
            {var old=GameObject.Find(boundary);if(old!=null)UnityEngine.Object.DestroyImmediate(old);}
            // glTFast caches the first material generator before generated URP assets exist.
            // Pin the inspected5.0.4 generator explicitly before reimport, not a stale Built-in one.
            var generator=typeof(GLTFast.Materials.MaterialGenerator).GetField("s_DefaultMaterialGenerator",BindingFlags.Static|BindingFlags.NonPublic);
            if(generator==null)throw new Exception("Pinned glTF material-generator contract changed");
            generator.SetValue(null,new GLTFast.Materials.UniversalRPMaterialGenerator((UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline));
            var district=new GameObject("Bangladesh courtyard and pond district (600 x 440m)");
            CreateTerrain(district.transform,ground);CreatePond(district.transform);
            // Connection paths lead between the old playable centre and courtyard clusters.
            Slab("East courtyard lane",new Vector3(105,.025f,0),new Vector3(170,.05f,5),lane,district.transform);
            Slab("West courtyard lane",new Vector3(-105,.025f,-30),new Vector3(170,.05f,5),lane,district.transform);
            Slab("Village north-south lane",new Vector3(-45,.025f,0),new Vector3(5,.05f,310),lane,district.transform);
            var usage=new List<Usage>();int house=0,tree=0,road=0;
            var courtyardCentres=new[]{new Vector3(-100,0,-115),new Vector3(75,0,-100),new Vector3(-135,0,10),new Vector3(70,0,115),new Vector3(160,0,-70),new Vector3(-100,0,125)};
            foreach(var entry in catalog.models)
            {
                // Import under the actual generated URP, not the initial missing/default pipeline.
                AssetDatabase.ImportAsset(entry.assetPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(entry.assetPath);
                if(prefab==null)throw new Exception("glTF importer did not create model: "+entry.assetPath);
                int instances=entry.pack=="Tree"?8:1;GameObject first=null;
                for(int instance=0;instance<instances;instance++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=entry.name;go.transform.SetParent(district.transform,false);
                    var sourceRotation=go.transform.localRotation;
                    var bounds=Bounds(go);float footprint=Mathf.Max(bounds.size.x,bounds.size.z);if(footprint<.001f)throw new Exception("Degenerate supplied model: "+entry.name);
                    float target=entry.pack=="House"?18:entry.pack=="Tree"?9:85;
                    float scale=target/footprint;
                    if(entry.pack=="House")foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                        if(renderer.name.Equals("Door",StringComparison.OrdinalIgnoreCase)&&renderer.bounds.size.y>.01f)
                        {float doorScale=2.2f/renderer.bounds.size.y;if(footprint*doorScale>=12&&footprint*doorScale<=32)scale=doorScale;break;}
                    go.transform.localScale*=scale;
                    Vector3 location;float yaw;
                    if(entry.pack=="House")
                    {
                        int cluster=house/2;var centre=courtyardCentres[Mathf.Min(cluster,courtyardCentres.Length-1)];
                        bool shop=entry.name.ToLowerInvariant().Contains("shop");
                        location=shop?new Vector3(-65,0,-15):centre+new Vector3(house%2==0?-18:18,0,0);
                        yaw=house%2==0?90:270;house++;
                    }
                    else if(entry.pack=="Tree")
                    {
                        float angle=(instance*45+tree*13)*Mathf.Deg2Rad;
                        location=new Vector3(Mathf.Cos(angle)*(185+tree*12),0,Mathf.Sin(angle)*155);yaw=instance*47;
                    }
                    else {location=road==0?new Vector3(-45,.06f,-100):new Vector3(-45,.06f,100);yaw=0;road++;}
                    go.transform.rotation=Quaternion.Euler(0,yaw,0)*sourceRotation;bounds=Bounds(go);
                    go.transform.position+=location-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        BindMissingVertexMaterial(renderer,entry.pack);
                        if(entry.pack=="Road"&&entry.name.StartsWith("Brick road",StringComparison.OrdinalIgnoreCase))RepairBlackBrick(renderer);
                        renderer.shadowCastingMode=ShadowCastingMode.On;
                        renderer.receiveShadows=true;
                        // Explicit source collision helpers must not appear as a duplicate visible trunk.
                        if(renderer.name.IndexOf("Collision",StringComparison.OrdinalIgnoreCase)>=0)renderer.enabled=false;
                    }
                    if(entry.pack=="Tree")
                    {
                        bool authored=false;
                        foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
                            if(filter.name.IndexOf("Collision",StringComparison.OrdinalIgnoreCase)>=0&&filter.sharedMesh!=null)
                            {var trunk=filter.gameObject.AddComponent<MeshCollider>();trunk.sharedMesh=filter.sharedMesh;authored=true;}
                        // Narrow fallback trunk only when no authored collision helper exists.
                        if(!authored)
                        {
                        bounds=Bounds(go);var collider=go.AddComponent<CapsuleCollider>();collider.radius=.22f/go.transform.lossyScale.x;
                        collider.height=Mathf.Max(.5f,bounds.size.y*.55f)/go.transform.lossyScale.y;
                        collider.center=go.transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.275f,bounds.center.z));
                        }
                    }
                    else
                    {
                        // Non-convex triangle collisions retain authored door/window/interior openings.
                        foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)
                        {var collider=filter.GetComponent<MeshCollider>();if(collider==null)collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;}
                    }
                    foreach(var t in go.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
                    if(first==null)first=go;
                }
                if(entry.pack=="Tree")tree++;
                var finalBounds=Bounds(first);usage.Add(new Usage{source=entry.pack+"/"+entry.sourcePath,placement=first.name,position=first.transform.position,size=finalBounds.size,instances=instances});
            }
            // Visible low perimeter, outside all new routes rather than the old70x90m enclosure.
            Slab("Expanded north perimeter",new Vector3(0,1,220),new Vector3(600,2,1),ground,district.transform);
            Slab("Expanded south perimeter",new Vector3(0,1,-220),new Vector3(600,2,1),ground,district.transform);
            Slab("Expanded west perimeter",new Vector3(-300,1,0),new Vector3(1,2,440),ground,district.transform);
            Slab("Expanded east perimeter",new Vector3(300,1,0),new Vector3(1,2,440),ground,district.transform);
            Directory.CreateDirectory("QA/runtime");File.WriteAllText("QA/runtime/village-placement.json",JsonUtility.ToJson(new Receipt{files=usage.ToArray()},true));
        }
        private static void BindMissingVertexMaterial(Renderer renderer,string pack)
        {
            var materials=renderer.sharedMaterials;var filter=renderer.GetComponent<MeshFilter>();
            if(materials.Length==0)materials=new Material[Mathf.Max(1,filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.subMeshCount:1)];
            bool vertexColour=filter!=null&&filter.sharedMesh!=null&&filter.sharedMesh.HasVertexAttribute(VertexAttribute.Color);
            for(int i=0;i<materials.Length;i++)if(materials[i]==null)
            {
                string path="Assets/Generated/Village/"+(vertexColour?"PreservedVertexColours":pack+"_NO_SOURCE_MATERIAL_PLACEHOLDER")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null)
                {
                    var shader=Shader.Find(vertexColour?"FPP/Source Vertex Colour Lit":"Universal Render Pipeline/Lit");
                    if(shader==null)throw new Exception("Missing source-colour fallback shader");
                    material=new Material(shader);material.color=vertexColour?Color.white:pack=="Tree"?new Color(.25f,.38f,.16f):new Color(.7f,.67f,.6f);AssetDatabase.CreateAsset(material,path);
                }
                materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
        private static void RepairBlackBrick(Renderer renderer)
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                if(materials[i]==null||!materials[i].name.Contains("HBB_Road_Final"))continue;
                const string path="Assets/Generated/Village/FiredBrickRepair.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(materials[i]){name="Fired brick repair (original procedural maps)"};AssetDatabase.CreateAsset(material,path);}
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/Village/BrickTextures/FiredBrick_BaseColor.png");
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Generated/Village/BrickTextures/FiredBrick_Normal.png");
                if(texture==null||normal==null||!material.HasProperty("baseColorTexture"))throw new Exception("Pinned glTF brick repair material contract unavailable");
                material.SetTexture("baseColorTexture",texture);material.SetVector("baseColorFactor",Vector4.one);
                if(material.HasProperty("normalTexture"))material.SetTexture("normalTexture",normal);
                if(material.HasProperty("roughnessFactor"))material.SetFloat("roughnessFactor",.86f);
                if(material.HasProperty("metallicFactor"))material.SetFloat("metallicFactor",0);
                EditorUtility.SetDirty(material);materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
        private static Bounds Bounds(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Empty village model "+go.name);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
        }
        private static GameObject Slab(string name,Vector3 position,Vector3 size,Material material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;return go;
        }
        private static float Height(float x,float z)
        {
            float radius=Mathf.Sqrt(Mathf.Pow((x-115)/28,2)+Mathf.Pow((z-35)/20,2));
            return radius<1?-2.7f*Mathf.SmoothStep(0,1,1-radius):-.04f;
        }
        private static void CreateTerrain(Transform parent,Material material)
        {
            const int width=301,depth=221;var vertices=new Vector3[width*depth];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int z=0;z<depth;z++)for(int x=0;x<width;x++){int i=z*width+x;float px=x*2-300,pz=z*2-220;vertices[i]=new Vector3(px,Height(px,pz),pz);uv[i]=new Vector2(px/4,pz/4);}
            for(int z=0;z<depth-1;z++)for(int x=0;x<width-1;x++){int i=z*width+x;indices.AddRange(new[]{i,i+width,i+1,i+1,i+width,i+width+1});}
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            string path="Assets/Generated/Village/Ground.asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject("Expanded ground and sloped pond basin",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
        }
        private static void CreatePond(Transform parent)
        {
            // New water placeholder: original supplied textures remain untouched; no swimming claim.
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.11f,.25f,.23f);material.SetFloat("_Smoothness",.85f);
            string matPath="Assets/Generated/Village/Pond.mat";AssetDatabase.DeleteAsset(matPath);AssetDatabase.CreateAsset(material,matPath);
            var vertices=new Vector3[65];var triangles=new int[63*3];vertices[0]=new Vector3(115,-.3f,35);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/63;vertices[i+1]=vertices[0]+new Vector3(Mathf.Cos(a)*24,0,Mathf.Sin(a)*17);if(i<63){triangles[i*3]=0;triangles[i*3+1]=i+2;triangles[i*3+2]=i+1;}}
            var mesh=new Mesh{vertices=vertices,triangles=triangles};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,"Assets/Generated/Village/Pond.asset");
            var pond=new GameObject("Village pond water (placeholder surface)",typeof(MeshFilter),typeof(MeshRenderer));pond.transform.SetParent(parent,false);pond.GetComponent<MeshFilter>().sharedMesh=mesh;pond.GetComponent<Renderer>().sharedMaterial=material;
            for(int i=0;i<5;i++)Slab("Pond ghat step "+i,new Vector3(115,-.12f-i*.18f,14+i*.7f),new Vector3(4,.25f,1),material,parent);
        }
    }
}
