using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fpp.Editor
{
    public static class TextureRepair
    {
        // GUID-only metas in the old conversion could deserialize as Cubemap importers.
        // Repair old Library/cache state as well as defining correct settings for new imports.
        public static void Ensure2D()
        {
            AssetDatabase.Refresh();
            foreach(string root in new[]{"Assets/PackModels","Assets/MosquePack","Assets/Viewmodels","Assets/Art"})
            {
                foreach(string path in Directory.GetFiles(root,"*.png",SearchOption.AllDirectories))
                {
                    string assetPath=path.Replace('\\','/');
                    var importer=AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if(importer==null)throw new Exception("No texture importer: "+assetPath);
                    if(importer.textureShape!=TextureImporterShape.Texture2D || importer.textureType!=TextureImporterType.Default)
                    {
                        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                        importer.SaveAndReimport();
                    }
                    if(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)==null)
                        throw new Exception("Texture did not import as 2D: "+assetPath);
                }
            }
            Debug.Log("FPP 2D texture import repair and validation passed");
        }
    }
}
