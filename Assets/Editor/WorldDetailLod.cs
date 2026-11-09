using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

namespace Fpp.Editor
{
    // Editor-only derivatives. LOD0 and all original collision meshes stay untouched.
    public static class WorldDetailLod
    {
        public const float NearScreenHeight=.24f, MediumScreenHeight=.09f;
        private const int MinimumTriangles=12000;
        private const string Folder="Assets/Generated/Village/DistanceMeshes";
        [Serializable] private class Catalog { public Entry[] models; }
        [Serializable] private class Entry { public string name; }
        [Serializable] private class Row
        {
            public string model; public long nearTriangles,mediumTriangles,farTriangles;
            public bool crossFade; public string fadeNote;
        }
        [Serializable] private class Report
        {
            public string status="Generated geometry LOD, NOT measured device FPS or visual acceptance. Original near meshes, materials/textures and collision retained. All distant meshes stay available to frustum/occlusion culling; no distance disappearance. Unsupported dither shaders use hard transitions requiring visual review.";
            public float nearScreenHeight=NearScreenHeight,mediumScreenHeight=MediumScreenHeight;
            public int groups,generatedMeshes,cacheHits,unreducedMeshes;
            public Row[] models;
        }
        private static readonly Dictionary<string,Mesh> Cache=new Dictionary<string,Mesh>();
        private static Report report;
        public static long Triangles(Mesh mesh)
        {
            long n=0;for(int i=0;i<mesh.subMeshCount;i++)if(mesh.GetTopology(i)==MeshTopology.Triangles)n+=(long)mesh.GetIndexCount(i)/3;return n;
        }
        public static void Build()
        {
            SmokeTest();Cache.Clear();report=new Report();var rows=new List<Row>();Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var names=new HashSet<string>();
            foreach(var entry in JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Generated/Village/catalog.json")).models)names.Add(entry.name);
            var roots=new List<GameObject>();
            foreach(var sceneRoot in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if(sceneRoot.name.StartsWith("Cottage ",StringComparison.Ordinal))roots.Add(sceneRoot);
                if(sceneRoot.name=="Bangladesh compact para (120 x 120m)")
                    foreach(Transform child in sceneRoot.transform)if(names.Contains(child.name))roots.Add(child.gameObject);
            }
            Directory.CreateDirectory("QA/runtime");File.AppendAllText("QA/runtime/build-progress.txt","DISTANCE LOD BUILD START\n");
            foreach(var root in roots)
            {
                foreach(var old in root.GetComponentsInChildren<LODGroup>())if(!old.enabled)UnityEngine.Object.DestroyImmediate(old);
                var levels=new[]{new List<Renderer>(),new List<Renderer>(),new List<Renderer>()};bool changed=false;
                var row=new Row{model=root.name};bool fade=true;
                // Snapshot before creating derivative renderers; never recurse over our new children.
                foreach(var source in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!source.enabled||source.gameObject.layer==8||source.GetComponentInParent<LODGroup>()!=null||source.additionalVertexStreams!=null)continue;
                    var filter=source.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                    var original=filter.sharedMesh;levels[0].Add(source);row.nearTriangles+=Triangles(original);
                    foreach(var material in source.sharedMaterials)
                        if(material==null||!material.shader.keywordSpace.FindKeyword("LOD_FADE_CROSSFADE").isValid)fade=false;
                    Mesh previous=original;
                    for(int level=1;level<3;level++)
                    {
                        Mesh mesh=Triangles(original)>=MinimumTriangles?Derivative(original,level):original;
                        if(Triangles(mesh)>Triangles(previous))mesh=previous;previous=mesh;
                        Renderer selected=source;
                        if(mesh!=original)
                        {
                            var child=new GameObject(level==1?"Generated distance medium":"Generated distance far");child.layer=source.gameObject.layer;
                            child.transform.SetParent(source.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                            var renderer=child.AddComponent<MeshRenderer>();
                            renderer.shadowCastingMode=source.shadowCastingMode;renderer.receiveShadows=source.receiveShadows;
                            renderer.lightProbeUsage=source.lightProbeUsage;renderer.reflectionProbeUsage=source.reflectionProbeUsage;
                            renderer.probeAnchor=source.probeAnchor;renderer.motionVectorGenerationMode=source.motionVectorGenerationMode;
                            renderer.allowOcclusionWhenDynamic=source.allowOcclusionWhenDynamic;renderer.renderingLayerMask=source.renderingLayerMask;
                            renderer.lightmapIndex=source.lightmapIndex;renderer.lightmapScaleOffset=source.lightmapScaleOffset;
                            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;
                            renderer.sharedMaterials=source.sharedMaterials;renderer.enabled=true;
                            // Never attach or replace collision on a visual LOD, nor bake it as an occluder.
                            GameObjectUtility.SetStaticEditorFlags(child,StaticEditorFlags.OccludeeStatic);
                            selected=renderer;changed=true;
                        }
                        levels[level].Add(selected);
                        if(level==1)row.mediumTriangles+=Triangles(mesh);else row.farTriangles+=Triangles(mesh);
                    }
                }
                if(!changed)continue;
                var group=root.AddComponent<LODGroup>();
                group.fadeMode=fade?LODFadeMode.CrossFade:LODFadeMode.None;group.animateCrossFading=fade;
                var lods=new[]{new LOD(NearScreenHeight,levels[0].ToArray()),new LOD(MediumScreenHeight,levels[1].ToArray()),new LOD(0f,levels[2].ToArray())};
                for(int i=0;i<lods.Length;i++)lods[i].fadeTransitionWidth=.12f;
                group.SetLODs(lods);group.RecalculateBounds();group.ForceLOD(-1);
                foreach(var level in levels)foreach(var renderer in level)
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&~StaticEditorFlags.BatchingStatic);
                row.crossFade=fade;row.fadeNote=fade?"All material shaders declare LOD_FADE_CROSSFADE; validate real transitions":"Dither unsupported by at least one shader; no fake crossfade, review threshold popping";
                rows.Add(row);report.groups++;
            }
            var main=GameObject.Find("Main Camera");if(main!=null)main.GetComponent<Camera>().useOcclusionCulling=true;
            report.models=rows.ToArray();File.WriteAllText("QA/runtime/world-lod-audit.json",JsonUtility.ToJson(report,true));
            File.AppendAllText("QA/runtime/build-progress.txt","DISTANCE LOD BUILD END groups="+report.groups+" meshes="+report.generatedMeshes+"\n");
            if(report.groups==0)throw new Exception("Expected heavy world geometry to have actual generated LODs");
            new GameObject("World LOD visual mesh residency").AddComponent<Fpp.WorldLodMeshResidency>();
            AssetDatabase.SaveAssets();
        }
        private static void SmokeTest()
        {
            const int n=32;var positions=new Vector3[(n+1)*(n+1)];var normals=new Vector3[positions.Length];
            var uv=new Vector2[positions.Length];var colors=new Color[positions.Length];var indices=new List<int>();
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {int i=z*(n+1)+x;positions[i]=new Vector3(x,0,z);normals[i]=Vector3.up;uv[i]=new Vector2(x/(float)n,z/(float)n);colors[i]=new Color(uv[i].x,.5f,uv[i].y,1);}
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*(n+1)+x;indices.AddRange(new[]{i,i+n+1,i+1,i+1,i+n+1,i+n+2});}
            var source=new Mesh{name="LOD pipeline smoke grid"};source.vertices=positions;source.normals=normals;source.uv=uv;source.colors=colors;source.SetTriangles(indices,0);source.RecalculateBounds();
            source.UploadMeshData(true);Mesh copy=null,reduced=null;
            try
            {
                copy=ReadableCopy(source);var simplifier=new MeshSimplifier(copy);var options=SimplificationOptions.Default;
                options.PreserveBorderEdges=true;options.PreserveUVSeamEdges=true;options.PreserveUVFoldoverEdges=true;simplifier.SimplificationOptions=options;
                simplifier.SimplifyMesh(.35f);reduced=simplifier.ToMesh();Validate(source,reduced);
                if(Triangles(reduced)>=Triangles(source))throw new Exception("LOD smoke test failed to reduce planar grid");
                if(source.isReadable)throw new Exception("LOD generator unexpectedly enabled source CPU readability");
            }
            finally {if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);if(reduced!=null)UnityEngine.Object.DestroyImmediate(reduced);UnityEngine.Object.DestroyImmediate(source);}
        }
        private static Mesh Derivative(Mesh source,int level)
        {
            // Never silently generate LODs for animated/blendshape or non-triangle topology.
            if(source.blendShapeCount!=0)return source;
            for(int sub=0;sub<source.subMeshCount;sub++)if(source.GetTopology(sub)!=MeshTopology.Triangles)return source;
            string asset=AssetDatabase.GetAssetPath(source);string guid;long id;
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out guid,out id))throw new Exception("Missing stable source identity: "+source.name);
            string key=Hash128.Compute("lod-v2-curvature-uv-border-preserved/"+guid+"/"+id+"/"+AssetDatabase.GetAssetDependencyHash(asset)+"/"+level).ToString();
            Mesh result;if(Cache.TryGetValue(key,out result)){report.cacheHits++;return result;}
            string path=Folder+"/"+key+".asset";result=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(result==null)
            {
                var readable=ReadableCopy(source);
                try
                {
                    var simplifier=new MeshSimplifier();var options=SimplificationOptions.Default;
                    options.PreserveBorderEdges=true;options.PreserveUVSeamEdges=true;options.PreserveUVFoldoverEdges=true;
                    options.PreserveSurfaceCurvature=true;options.EnableSmartLink=true;options.MaxIterationCount=100;
                    simplifier.SimplificationOptions=options;simplifier.Initialize(readable);
                    simplifier.SimplifyMesh(level==1?.5f:.15f);result=simplifier.ToMesh();result.name=source.name+(level==1?" distance medium":" distance far");
                }
                finally {UnityEngine.Object.DestroyImmediate(readable);}
                Validate(source,result);
                if(Triangles(result)>=Triangles(source))
                {UnityEngine.Object.DestroyImmediate(result);report.unreducedMeshes++;Cache.Add(key,source);return source;}
                // Serialize complete geometry first. Runtime releases ONLY visual-LOD CPU copies.
                AssetDatabase.CreateAsset(result,path);report.generatedMeshes++;
            }
            Validate(source,result);Cache.Add(key,result);return result;
        }
        private static Mesh ReadableCopy(Mesh source)
        {
            // Editor read-only API also supports non-readable imported meshes. Never change importer flags.
            var copy=new Mesh{name=source.name+" (temporary simplification input)"};
            using(var data=MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var read=data[0];var writable=Mesh.AllocateWritableMeshData(1);var write=writable[0];
                write.SetVertexBufferParams(read.vertexCount,source.GetVertexAttributes());
                for(int stream=0;stream<source.vertexBufferCount;stream++)write.GetVertexData<byte>(stream).CopyFrom(read.GetVertexData<byte>(stream));
                write.SetIndexBufferParams(read.GetIndexData<byte>().Length/(source.indexFormat==IndexFormat.UInt32?4:2),source.indexFormat);
                write.GetIndexData<byte>().CopyFrom(read.GetIndexData<byte>());write.subMeshCount=read.subMeshCount;
                for(int sub=0;sub<read.subMeshCount;sub++)write.SetSubMesh(sub,read.GetSubMesh(sub),MeshUpdateFlags.DontRecalculateBounds);
                Mesh.ApplyAndDisposeWritableMeshData(writable,copy,MeshUpdateFlags.DontRecalculateBounds);copy.bounds=source.bounds;
            }
            return copy;
        }
        private static void Validate(Mesh source,Mesh result)
        {
            if(result.vertexCount==0||Triangles(result)==0||result.subMeshCount!=source.subMeshCount)throw new Exception("Invalid LOD/submesh layout: "+source.name);
            foreach(var attribute in new[]{VertexAttribute.Normal,VertexAttribute.Tangent,VertexAttribute.Color,VertexAttribute.TexCoord0,VertexAttribute.TexCoord1})
                if(source.HasVertexAttribute(attribute)&&!result.HasVertexAttribute(attribute))throw new Exception("LOD lost original attribute "+attribute+": "+source.name);
            var bounds=source.bounds;bounds.Expand(Mathf.Max(.001f,source.bounds.size.magnitude*.002f));
            if(!bounds.Contains(result.bounds.min)||!bounds.Contains(result.bounds.max))throw new Exception("LOD escaped original bounds: "+source.name);
        }
    }
}
