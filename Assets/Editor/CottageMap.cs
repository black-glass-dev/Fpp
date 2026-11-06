using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fpp.Editor
{
    public static class CottageMap
    {
        public const string ScenePath = "Assets/Scenes/CottageTest.unity";

        [MenuItem("FPP/Create cottage test map (replaces open scene)")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cottages/Cottage_FREE/Cottage_FREE.fbx");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cottages/Cottage_FREE.mat");
            if (model == null || material == null) throw new Exception("Cottage model/material missing. Import assets first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Materials/TestMap");
            var grass = MakeMaterial("Ground", new Color(.22f, .27f, .17f));
            var road = MakeMaterial("Road", new Color(.28f, .26f, .23f));
            var wall = MakeMaterial("Cover", new Color(.35f, .37f, .34f));
            Cube("Ground", new Vector3(0, -.3f, 0), new Vector3(70, .6f, 90), grass);
            Cube("Main lane", new Vector3(0, .015f, 0), new Vector3(10, .03f, 88), road);
            Cube("Courtyard", new Vector3(0, .02f, 0), new Vector3(35, .04f, 12), road);
            for (int row = 0; row < 3; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var house = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    house.name = "Cottage " + row + " " + side;
                    house.transform.rotation = Quaternion.Euler(0, side == -1 ? 90 : 270, 0);
                    Bounds bounds = BoundsOf(house);
                    // Fit export units to a roughly 10m footprint without modifying source mesh.
                    float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
                    if (footprint < .001f) throw new Exception("Invalid cottage bounds");
                    house.transform.localScale *= 10f / footprint;
                    bounds = BoundsOf(house);
                    house.transform.position += new Vector3(side * 17 - bounds.center.x,
                        -bounds.min.y, (row - 1) * 26 - bounds.center.z);
                    foreach (var renderer in house.GetComponentsInChildren<Renderer>())
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = material;
                        renderer.sharedMaterials = materials;
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    foreach (var filter in house.GetComponentsInChildren<MeshFilter>())
                        if (filter.sharedMesh != null && filter.GetComponent<Collider>() == null)
                            filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    house.isStatic = true;
                }
            for (int i = 0; i < 4; i++)
                Cube("Temporary cover " + i, new Vector3(i % 2 == 0 ? -7 : 7, .6f, -18 + i * 12), new Vector3(3, 1.2f, 1), wall);
            Cube("North boundary", new Vector3(0, 1.5f, 45), new Vector3(70, 3, 1), wall);
            Cube("South boundary", new Vector3(0, 1.5f, -45), new Vector3(70, 3, 1), wall);
            Cube("West boundary", new Vector3(-35, 1.5f, 0), new Vector3(1, 3, 90), wall);
            Cube("East boundary", new Vector3(35, 1.5f, 0), new Vector3(1, 3, 90), wall);
            PackModelLibrary.Populate();
            for (int i = 0; i < 5; i++)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "Shooting target " + i;
                target.transform.position = new Vector3(-4 + i * 2, 1.5f, -18);
                target.transform.localScale = new Vector3(.8f, 1.5f, .3f);
                target.GetComponent<Renderer>().sharedMaterial = wall;
                target.AddComponent<TestTarget>();
            }
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.shadows = LightShadows.None;
            RenderSettings.ambientLight = new Color(.6f, .63f, .68f);
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, .15f, -36);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .3f;
            controller.center = new Vector3(0, .9f, 0);
            var camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            var lens = camera.AddComponent<Camera>();
            lens.nearClipPlane = .1f;
            lens.farClipPlane = 120;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(.42f, .57f, .68f);
            camera.AddComponent<AudioListener>();
            player.AddComponent<MobileBootstrap>();
            player.AddComponent<TestPlayer>().view = camera.transform;
            player.AddComponent<WeaponController>().view = camera.transform;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = "Assets/Materials/TestMap/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetFloat("_Glossiness", 0);
            return material;
        }

        private static void Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            cube.isStatic = true;
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new Exception("Cottage has no renderers");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
