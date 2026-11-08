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
        [Serializable] private class Usage { public string source,placement,calibration;public Vector3 size,position;public int instances; }
        [Serializable] private class Placement { public string name;public float x,z,yaw,height,width; }
        [Serializable] private class Anchor { public float x,z; }
        [Serializable] private class Layout { public string districtName;public int halfWidth,halfDepth;public Placement[] models;public Anchor[] treeAnchors; }
        private static Layout layout;
        private static bool Helper(string name) { return name.StartsWith("UCX_",StringComparison.OrdinalIgnoreCase)||name.IndexOf("Collision",StringComparison.OrdinalIgnoreCase)>=0||name.EndsWith("_LOD1",StringComparison.OrdinalIgnoreCase)||name.EndsWith("_LOD2",StringComparison.OrdinalIgnoreCase); }
        private static bool Helper(Transform t) {for(;t!=null;t=t.parent)if(Helper(t.name))return true;return false;}
        private static void FullDetailOnly(GameObject go)
        {
            // Authored alternate LOD/proxy objects are not additional visible houses.
            // Keep full detail; do not switch to a reduced LOD at distance.
            Directory.CreateDirectory("QA/runtime");
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))if(Helper(filter.transform)&&filter.sharedMesh!=null)
                File.AppendAllText("QA/runtime/helper-exclusions.txt",go.name+" / "+filter.name+" / vertices="+filter.sharedMesh.vertexCount+"; alternate LOD/proxy not drawn, full-detail source retained\n");
            foreach(var group in go.GetComponentsInChildren<LODGroup>())group.enabled=false;
            foreach(var t in go.GetComponentsInChildren<Transform>())if(t.name.StartsWith("UCX_",StringComparison.OrdinalIgnoreCase)||t.name.EndsWith("_LOD1",StringComparison.OrdinalIgnoreCase)||t.name.EndsWith("_LOD2",StringComparison.OrdinalIgnoreCase))t.gameObject.SetActive(false);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())if(Helper(renderer.transform))renderer.enabled=false;
        }
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
            layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/VillageLayout.json"));
            if(layout.models.Length!=18||layout.treeAnchors.Length!=24)throw new Exception("Incomplete compact layout");
            var oldGround=GameObject.Find("Sculpted ground");if(oldGround!=null)UnityEngine.Object.DestroyImmediate(oldGround);
            var stones=GameObject.Find("Batched small ground stones");
            if(stones!=null){var filter=stones.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i].y-=GroundBuilder.Height(vertices[i].x,vertices[i].z);mesh.vertices=vertices;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/Generated/Village/LevelledPebbles.asset");filter.sharedMesh=mesh;}
            var district=new GameObject(layout.districtName);
            CreateTerrain(district.transform,ground);CreatePond(district.transform);
            foreach(var pickup in UnityEngine.Object.FindObjectsOfType<WeaponPickup>()){var p=pickup.transform.position;pickup.transform.position-=Vector3.up*GroundBuilder.Height(p.x,p.z);}
            Slab("East connected neighbourhood lane",new Vector3(22,.025f,-8),new Vector3(4.2f,.05f,82),lane,district.transform);
            Slab("East residential alley",new Vector3(40,.025f,-9),new Vector3(2.4f,.05f,58),lane,district.transform);
            Slab("South cross gali",new Vector3(35,.025f,-21),new Vector3(30,.05f,2.4f),lane,district.transform);
            Slab("North cross gali",new Vector3(35,.025f,-2),new Vector3(30,.05f,2.4f),lane,district.transform);
            Slab("West courtyard connection",new Vector3(-25,.025f,10),new Vector3(24,.05f,2.4f),lane,district.transform);
            Slab("West uthan",new Vector3(-36,.025f,-28),new Vector3(7,.05f,11),lane,district.transform);
            Slab("West inner passage",new Vector3(-36,.025f,5),new Vector3(2.4f,.05f,55),lane,district.transform);
            var usage=new List<Usage>();int tree=0;
            foreach(var entry in catalog.models)
            {
                // Import under the actual generated URP, not the initial missing/default pipeline.
                AssetDatabase.ImportAsset(entry.assetPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(entry.assetPath);
                if(prefab==null)throw new Exception("glTF importer did not create model: "+entry.assetPath);
                Placement plan=null;foreach(var candidate in layout.models)if(candidate.name==entry.name){plan=candidate;break;}
                if(plan==null)throw new Exception("Unplaced supplied model: "+entry.name);
                int instances=entry.pack=="Tree"?8:1;GameObject first=null;string calibration="manual height reference; visual validation pending";
                for(int instance=0;instance<instances;instance++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=entry.name;go.transform.SetParent(district.transform,false);
                    FullDetailOnly(go);
                    var sourceRotation=go.transform.localRotation;
                    var bounds=Bounds(go);float footprint=Mathf.Max(bounds.size.x,bounds.size.z);if(footprint<.001f)throw new Exception("Degenerate supplied model: "+entry.name);
                    float scale=plan.height/Mathf.Max(.001f,bounds.size.y);
                    if(entry.pack=="Road") { scale=plan.width/bounds.size.x;calibration="uniform scale to4.2m street width, not85m blanket length"; }
                    if(entry.pack=="House")
                    {
                        var doors=new List<float>();
                        foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                        {
                            string name=renderer.name.ToLowerInvariant();float height=renderer.bounds.size.y;
                            if((name=="door"||name.StartsWith("door_"))&&!name.Contains("frame")&&!name.Contains("latch")&&height>bounds.size.y*.18f&&height<bounds.size.y*.75f)doors.Add(height);
                        }
                        if(doors.Count>0){doors.Sort();float doorScale=2.1f/doors[doors.Count/2];if(footprint*doorScale>=3&&footprint*doorScale<=13.5f){scale=doorScale;calibration="authored door panel median calibrated to2.1m";}}
                        if(footprint*scale>13.5f)throw new Exception("House exceeds compact parcel: "+entry.name);
                    }
                    go.transform.localScale*=scale;
                    Vector3 location=new Vector3(plan.x,.02f,plan.z);float yaw=plan.yaw;
                    if(entry.pack=="Tree") {var anchor=layout.treeAnchors[tree*8+instance];location=new Vector3(anchor.x,0,anchor.z);yaw=instance*47;}
                    go.transform.rotation=Quaternion.Euler(0,yaw,0)*sourceRotation;bounds=Bounds(go);
                    go.transform.position+=location-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        BindMissingVertexMaterial(renderer,entry.pack);
                        if(Helper(renderer.transform))continue;
                        VillageSurfaceRepair.Apply(renderer,entry.pack,entry.name);
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
                        foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&!Helper(filter.transform))
                        {var collider=filter.GetComponent<MeshCollider>();if(collider==null)collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;}
                    }
                    foreach(var t in go.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|(entry.pack=="House"?StaticEditorFlags.OccluderStatic:0));
                    if(first==null)first=go;
                }
                if(entry.pack=="Tree")tree++;
                var finalBounds=Bounds(first);usage.Add(new Usage{source=entry.pack+"/"+entry.sourcePath,placement=first.name,position=first.transform.position,size=finalBounds.size,instances=instances,calibration=calibration});
            }
            Slab("Compact north perimeter",new Vector3(0,1,60),new Vector3(120,2,1),ground,district.transform);
            Slab("Compact south perimeter",new Vector3(0,1,-60),new Vector3(120,2,1),ground,district.transform);
            Slab("Compact west perimeter",new Vector3(-60,1,0),new Vector3(1,2,120),ground,district.transform);
            Slab("Compact east perimeter",new Vector3(60,1,0),new Vector3(1,2,120),ground,district.transform);
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
                if(texture==null||normal==null)throw new Exception("Pinned glTF brick repair material contract unavailable");
                material.shader=Shader.Find("FPP/Metric Surface Lit");material.SetTexture("_MainTex",texture);material.SetTexture("_BumpMap",normal);material.SetColor("_Color",Color.white);material.SetFloat("_DetailScale",1/1.92f);material.SetFloat("_Smoothness",.14f);
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
            Bounds bounds=default(Bounds);bool found=false;foreach(var renderer in renderers)if(renderer.enabled&&!Helper(renderer.transform)){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}if(!found)throw new Exception("No visible source geometry");return bounds;
        }
        private static GameObject Slab(string name,Vector3 position,Vector3 size,Material material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;return go;
        }
        public static float SurfaceHeight(float x,float z)
        {
            float radius=Mathf.Sqrt(Mathf.Pow((x-46)/11,2)+Mathf.Pow((z-40)/9,2));
            return radius<1?-2.7f*Mathf.SmoothStep(0,1,1-radius):-.04f;
        }
        private static void CreateTerrain(Transform parent,Material material)
        {
            int width=layout.halfWidth*2+1,depth=layout.halfDepth*2+1;var vertices=new Vector3[width*depth];var uv=new Vector2[vertices.Length];var indices=new List<int>();
            for(int z=0;z<depth;z++)for(int x=0;x<width;x++){int i=z*width+x;float px=x-layout.halfWidth,pz=z-layout.halfDepth;vertices[i]=new Vector3(px,SurfaceHeight(px,pz),pz);uv[i]=new Vector2(px/4,pz/4);}
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
            var vertices=new Vector3[65];var triangles=new int[63*3];vertices[0]=new Vector3(46,-.3f,40);
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/63;vertices[i+1]=vertices[0]+new Vector3(Mathf.Cos(a)*9,0,Mathf.Sin(a)*7);if(i<63){triangles[i*3]=0;triangles[i*3+1]=i+2;triangles[i*3+2]=i+1;}}
            var mesh=new Mesh{vertices=vertices,triangles=triangles};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,"Assets/Generated/Village/Pond.asset");
            var pond=new GameObject("Village pond water (placeholder surface)",typeof(MeshFilter),typeof(MeshRenderer));pond.transform.SetParent(parent,false);pond.GetComponent<MeshFilter>().sharedMesh=mesh;pond.GetComponent<Renderer>().sharedMaterial=material;
            for(int i=0;i<5;i++)Slab("Pond ghat step "+i,new Vector3(46,-.12f-i*.18f,31+i*.5f),new Vector3(4,.25f,1),material,parent);
        }
    }
}
