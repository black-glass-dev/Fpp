using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    [Serializable] public class PackCatalog { public PackModel[] models; }
    [Serializable] public class PackModel
    {
        public string name;
        public bool weapon;
        public PackPart[] parts;
    }
    [Serializable] public class PackPart
    {
        public string model;
        public string texture;
        public float[] color;
        public bool cutout;
    }

    public static class PackModelLibrary
    {
        public static GameObject Create(PackModel entry, float size)
        {
            var root = new GameObject(entry.name);
            foreach (var part in entry.parts)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(part.model);
                if (asset == null) throw new Exception("Missing model: " + part.model);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                instance.transform.SetParent(root.transform, false);
                string materialPath = Path.ChangeExtension(part.model, ".mat");
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.mainTexture = string.IsNullOrEmpty(part.texture) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(part.texture);
                if (!string.IsNullOrEmpty(part.texture) && material.mainTexture == null)
                    throw new Exception("Diffuse texture was not imported: " + part.texture);
                var c = part.color;
                material.color = c != null && c.Length == 4 ? new Color(c[0], c[1], c[2], c[3]) : Color.white;
                material.SetFloat("_Glossiness", .05f);
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_SpecularHighlights", 0);
                material.SetFloat("_GlossyReflections", 0);
                material.enableInstancing = true;
                if (part.cutout)
                {
                    material.SetFloat("_Mode", 1);
                    material.SetFloat("_Cutoff", .4f);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = 2450;
                }
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    int slots = filter != null && filter.sharedMesh != null ? Mathf.Max(1, filter.sharedMesh.subMeshCount) : 1;
                    var materials = new Material[slots];
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception("Empty model: " + entry.name);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            root.transform.localScale = Vector3.one * (size / Mathf.Max(.001f, max));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var pivot = new GameObject(entry.name + " pivot");
            root.transform.SetParent(pivot.transform, true);
            root.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return pivot;
        }

        public static void Populate()
        {
            string[] catalogs = { "Assets/PackModels/catalog.json", "Assets/MosquePack/catalog.json" };
            int weapon = 0;
            int prop = 0;
            foreach (string path in catalogs)
            {
                if (!File.Exists(path)) continue;
                var catalog = JsonUtility.FromJson<PackCatalog>(File.ReadAllText(path));
                foreach (var entry in catalog.models)
                {
                    if (entry.name.ToLowerInvariant().Contains("kriss") || entry.name.ToLowerInvariant().Contains("mark_23")) continue;
                    var model = Create(entry, entry.weapon ? .8f : (entry.name.ToLowerInvariant().Contains("mosque") ? 18 : (entry.name.ToLowerInvariant().Contains("haystack") ? 2.5f : 6)));
                    if (entry.weapon)
                    {
                        Vector3[] spots = { new Vector3(-5, .08f, -17), new Vector3(6, .08f, 9), new Vector3(-10, .08f, 20) };
                        var random = new System.Random(841 + weapon);
                        var rs = model.GetComponentsInChildren<Renderer>();
                        Bounds bound = rs[0].bounds;
                        foreach (var r in rs) bound.Encapsulate(r.bounds);
                        Vector3 thin = bound.size.x < bound.size.y && bound.size.x < bound.size.z ? Vector3.right :
                            (bound.size.y < bound.size.z ? Vector3.up : Vector3.forward);
                        model.transform.rotation = Quaternion.AngleAxis(random.Next(0, 360), Vector3.up) * Quaternion.FromToRotation(thin, Vector3.up);
                        bound = rs[0].bounds;
                        foreach (var r in rs) bound.Encapsulate(r.bounds);
                        Vector3 scatter = new Vector3((float)random.NextDouble()*2-1,0,(float)random.NextDouble()*2-1);
                        model.transform.position += spots[weapon % spots.Length] + scatter - new Vector3(bound.center.x, bound.min.y, bound.center.z);
                        var pickup = model.AddComponent<WeaponPickup>();
                        pickup.displayName = entry.name.Replace("Gun pistol m", "Field pistol").Replace("Glock 17", "Glock 17");
                        weapon++;
                    }
                    else
                    {
                        string name = entry.name.ToLowerInvariant();
                        model.transform.position = name.Contains("mosque") ? new Vector3(0,.05f,32) :
                            name.Contains("haystack") ? new Vector3(-10,.05f,12) :
                            new Vector3(prop % 2 == 0 ? -29 : 29,.05f,-32+prop*6);
                        var collider = model.AddComponent<BoxCollider>();
                        var renderers = model.GetComponentsInChildren<Renderer>();
                        Bounds bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        collider.center = model.transform.InverseTransformPoint(bounds.center);
                        collider.size = bounds.size;
                        model.isStatic = true;
                        prop++;
                    }
                }
            }
        }
    }

    public sealed class PackModelImport : AssetPostprocessor
    {
        private bool IsPack => assetPath.StartsWith("Assets/PackModels/") || assetPath.StartsWith("Assets/MosquePack/");
        private void OnPreprocessModel()
        {
            if (!IsPack) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1;
            importer.useFileScale = false;
            importer.importAnimation = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
        private void OnPreprocessTexture()
        {
            if (!IsPack) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ETC2_RGBA8
            });
        }
    }
}
