using UnityEditor;

namespace Fpp.Editor
{
    public sealed class CottageTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Cottages/Cottage_FREE/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            importer.isReadable = false;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (assetPath.Contains("Normal")) importer.textureType = TextureImporterType.NormalMap;
            if (assetPath.Contains("Normal") || assetPath.Contains("AO") || assetPath.Contains("Metallic"))
                importer.sRGBTexture = false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ETC2_RGBA8
            });
        }
    }
}
