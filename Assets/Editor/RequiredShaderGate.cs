using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fpp.Editor
{
    /// <summary>
    /// The phone report showed "ArgumentNullException: Parameter name: shader" from runtime code.
    /// That happens when Shader.Find cannot see a shader in the player build. This gate fails the
    /// build instead of shipping a build that crashes on the device, records exactly which shaders
    /// the payload contains, and forces every runtime-by-name shader into Always Included Shaders.
    /// </summary>
    public sealed class RequiredShaderGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        private const string SettingsPath = "ProjectSettings/GraphicsSettings.asset";

        // Every shader name any runtime script may ask for. Keep in sync with RuntimeShader callers.
        private static readonly string[] Required =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "FPP/Layered Ground",
            "FPP/World Brick",
            "FPP/Metric Surface Lit",
            "FPP/Mobile Scan",
            "FPP/Muzzle",
            "FPP/Smoke",
            "FPP/Source Vertex Colour Lit",
            "Hidden/FPP/Actual Canvas Composite",
            "Sprites/Default",
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            var missing = new List<string>();
            var found = new List<string>();
            var included = new List<string>();
            foreach (var name in Required)
            {
                var shader = Shader.Find(name);
                if (shader == null) { missing.Add(name); continue; }
                found.Add(name + " -> " + AssetDatabase.GetAssetPath(shader));
                if (IsAlwaysIncluded(shader)) included.Add(name);
            }
            int added = missing.Count == 0 ? AddToAlwaysIncluded() : 0;
            Directory.CreateDirectory("QA/runtime");
            File.WriteAllText("QA/runtime/required-shaders.txt",
                "AlwaysIncludedShaders before build: " + included.Count + "/" + Required.Length +
                "; newly added by this gate: " + added + Environment.NewLine +
                "A null result below is a build error, not a warning; the phone cannot Shader.Find a stripped shader." + Environment.NewLine +
                "present=" + string.Join("; ", found) + Environment.NewLine +
                "missing=" + (missing.Count == 0 ? "none" : string.Join("; ", missing)) + Environment.NewLine);
            if (missing.Count > 0)
                throw new BuildFailedException("Runtime shader(s) missing before build: " + string.Join(", ", missing) + ". Add them to a built scene material or to Project Settings > Graphics > Always Included Shaders; otherwise Shader.Find returns null on the phone and Material(null) throws ArgumentNullException.");
        }

        private static bool IsAlwaysIncluded(Shader shader)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(SettingsPath);
            if (assets == null || assets.Length == 0) return false;
            var serialized = new SerializedObject(assets[0]);
            var list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) throw new BuildFailedException("GraphicsSettings always-included list contract changed");
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return true;
            return false;
        }

        /// <summary>Ensure every required shader survives into the player, independent of which scene references it.</summary>
        public static int AddToAlwaysIncluded()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(SettingsPath);
            if (assets == null || assets.Length == 0) throw new BuildFailedException("GraphicsSettings.asset is not readable");
            var serialized = new SerializedObject(assets[0]);
            var list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) throw new BuildFailedException("GraphicsSettings always-included list contract changed");
            int added = 0;
            foreach (var name in Required)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue; // reported as an error by the caller
                bool present = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                added++;
            }
            if (added > 0) { serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); }
            return added;
        }
    }
}
