using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Fpp;

namespace Fpp.Editor
{
    public static class AndroidBuild
    {
        [MenuItem("FPP/Create setup scene")]
        public static void CreateSetupScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Mobile bootstrap").AddComponent<MobileBootstrap>();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0, 2, -6);
            cameraObject.transform.rotation = Quaternion.Euler(12, 0, 0);
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.17f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.farClipPlane = 150;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Test ground";
            floor.transform.localScale = new Vector3(3, 1, 3);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.shadows = LightShadows.None;
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Setup.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Setup.unity", true) };
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        [MenuItem("FPP/Build Android APK")]
        public static void Build()
        {
            try{BuildChecked();}
            catch(Exception error){Directory.CreateDirectory("build");File.WriteAllText("build/fpp-diagnostic.txt",error.ToString());throw;}
        }
        // Gallery must never prevent the signed APK from being built/preserved first.
        // Recreate the SAME full-quality deterministic scene; do not lower preview settings.
        public static void CaptureReview()
        {
            TextureRepair.Ensure2D();CottageMap.Create();EnvironmentAudit.Validate();
            var target=RenderTexture.GetTemporary(16,9,24);var old=RenderTexture.active;
            try
            {
                var owner=UnityEngine.Object.FindObjectOfType<WeaponController>();
                RenderEvidence.Into(owner.view.GetComponent<Camera>(),owner.weaponCamera,target);
                RevisionAudit.Validate();BuildPreview.Capture();
            }
            finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);}
        }
        private static void BuildChecked()
        {
            TextureRepair.Ensure2D();
            CottageMap.Create();
            EnvironmentAudit.Validate();
            AppIconSetup.Apply();
            // The imported Shader Graph has Built-in and URP subshaders. Select the live
            // URP context before auditing its active RenderPipeline tag.
            var warmTarget=RenderTexture.GetTemporary(16,9,24);var previousTarget=RenderTexture.active;
            try
            {
                var owner=UnityEngine.Object.FindObjectOfType<WeaponController>();
                RenderEvidence.Into(owner.view.GetComponent<Camera>(),owner.weaponCamera,warmTarget);
            }
            finally { RenderTexture.active=previousTarget;RenderTexture.ReleaseTemporary(warmTarget); }
            RevisionAudit.Validate();
            PlayerSettings.companyName = "Black Glass Dev";
            PlayerSettings.productName = "FPP";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "dev.blackglass.fpp");
            PlayerSettings.bundleVersion = "0.0.2";
            // Opt-in diagnostic only: native GPU queries can add overhead on some APIs.
            if(Argument("-fppProfile")=="1")PlayerSettings.enableFrameTimingStats=true;
            var version = Argument("-fppVersionCode") ?? Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE");
            PlayerSettings.Android.bundleVersionCode = string.IsNullOrEmpty(version) ? 1 : int.Parse(version);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            // Build an isolated payload copy; never serialize source/debug references into APK.
            var world=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            AndroidWorldPayload.Prepare(world);
            const string payloadWorld="Assets/Generated/AndroidPayload/CottageTest.unity";
            EditorSceneManager.SaveScene(world,payloadWorld,true);
            AssetDatabase.SaveAssets();AndroidWorldPayload.PruneUnusedResourceMaterials(payloadWorld);
            var payloadScene=EditorSceneManager.OpenScene(payloadWorld,OpenSceneMode.Single);
            WorldDetailLod.ClearEditorCaches();EditorUtility.UnloadUnusedAssetsImmediate();
            LosslessWorldOptimizer.BakeVisibility();EditorSceneManager.SaveScene(payloadScene);
            StartupSceneBuilder.Create(payloadWorld);
            var output = Environment.GetEnvironmentVariable("APK_OUTPUT") ?? "build/Android/Fpp.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { StartupSceneBuilder.Path, "Assets/Generated/AndroidPayload/CottageTest.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android build failed: " + report.summary.result);
            var bytes=new FileInfo(output).Length;
            File.WriteAllText("QA/runtime/android-apk-budget.txt","bytes="+bytes+"; maximum=500000000; source87=631625961; ASTC retains image dimensions/mips, is not lossless pixel compression; phone startup still requires validation.");
            if(bytes>=500000000)throw new Exception("APK exceeds reduced-size gate: "+bytes+"; do not publish another larger APK");
        }
    }
}
