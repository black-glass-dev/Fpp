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
            foreach(string root in new[]{"Assets/PackModels","Assets/MosquePack","Assets/Viewmodels","Assets/Art","Assets/Resources/Presentation","Assets/Generated/Village"})
            {
                foreach(string path in Directory.GetFiles(root,"*.png",SearchOption.AllDirectories))
                {
                    string assetPath=path.Replace('\\','/');
                    var importer=AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if(importer==null)throw new Exception("No texture importer: "+assetPath);
                    bool normal=assetPath.EndsWith("Normal.png");var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                    bool originalDetail=assetPath.Contains("/Generated/Village/");
                    bool ground=assetPath.Contains("/Ground/")||originalDetail;bool glyph=assetPath.Contains("/Presentation/");bool icon=assetPath.Contains("/Icon/")||glyph;
                    var filter=ground || assetPath.Contains("/PackModels/") || assetPath.Contains("/MosquePack/")?FilterMode.Trilinear:FilterMode.Bilinear;
                    int aniso=ground?4:icon?1:2;
                    if(importer.textureShape!=TextureImporterShape.Texture2D || importer.textureType!=type ||
                       importer.sRGBTexture==normal || importer.filterMode!=filter || importer.anisoLevel!=aniso ||
                       originalDetail&&(importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=2048||importer.wrapMode!=TextureWrapMode.Repeat) ||
                       glyph && (importer.mipmapEnabled || importer.npotScale!=TextureImporterNPOTScale.None || importer.textureCompression!=TextureImporterCompression.Uncompressed))
                    {
                        importer.textureType=type;importer.textureShape=TextureImporterShape.Texture2D;
                        importer.sRGBTexture=!normal;importer.filterMode=filter;importer.anisoLevel=aniso;
                        if(originalDetail){importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Repeat;}
                        if(glyph){importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;}
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
