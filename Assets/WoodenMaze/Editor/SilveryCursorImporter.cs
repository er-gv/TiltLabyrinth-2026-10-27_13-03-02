
using UnityEngine;
using UnityEditor;

public class SilveryCursorImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("silver_pointing_finger"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Cursor;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; // no mip chains
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            
            // Force RGBA32, no compression
            TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
            settings.overridden = true;
            settings.format = TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.compressionQuality = 100;
            importer.SetPlatformTextureSettings(settings);
            
            Debug.Log($"Configured cursor texture: {assetPath} as RGBA32, Readable, alphaIsTransparency, no mips");
        }
    }
}
