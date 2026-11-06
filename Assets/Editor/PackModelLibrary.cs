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
                var c = part.color;
                material.color = c != null && c.Length == 4 ? new Color(c[0], c[1], c[2], c[3]) : Color.white;
                material.SetFloat("_Glossiness", .15f);
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
                    var materials = renderer.sharedMaterials;
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
                    var model = Create(entry, entry.weapon ? .8f : (entry.name.ToLowerInvariant().Contains("mosque") ? 18 : 6));
                    if (entry.weapon)
                    {
                        model.transform.position = new Vector3(-3 + weapon * 1.2f, .25f, -30);
                        var pickup = model.AddComponent<WeaponPickup>();
                        pickup.displayName = entry.name;
                        weapon++;
                    }
                    else
                    {
                        model.transform.position = new Vector3(prop % 2 == 0 ? -26 : 26, .05f, -16 + prop / 2 * 21);
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
