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
                    bool ground=assetPath.Contains("/Ground/");bool icon=assetPath.Contains("/Icon/");
                    var filter=ground || assetPath.Contains("/PackModels/") || assetPath.Contains("/MosquePack/")?FilterMode.Trilinear:FilterMode.Bilinear;
                    int aniso=ground?4:icon?1:2;
                    if(importer.textureShape!=TextureImporterShape.Texture2D || importer.textureType!=TextureImporterType.Default ||
                       !importer.sRGBTexture || importer.filterMode!=filter || importer.anisoLevel!=aniso)
                    {
                        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                        importer.sRGBTexture=true;importer.filterMode=filter;importer.anisoLevel=aniso;
                        importer.SaveAndReimport();
                    }
                    if(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)==null)
                        throw new Exception("Texture did not import as 2D: "+assetPath);
                }
            }
            const string fontPath="Assets/Resources/Fonts/Hud.ttf";
            var fontImporter=AssetImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
            if(fontImporter==null)throw new Exception("HUD font importer missing");
            if(fontImporter.fontTextureCase!=FontTextureCase.Dynamic || !fontImporter.includeFontData)
            {
                fontImporter.fontTextureCase=FontTextureCase.Dynamic; fontImporter.includeFontData=true;
                fontImporter.fontSize=24; fontImporter.SaveAndReimport();
            }
            var font=AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if(font==null || !font.dynamic)throw new Exception("Bundled dynamic HUD font is not available");
            Debug.Log("FPP 2D texture and bundled font repair and validation passed");
        }
    }
}
