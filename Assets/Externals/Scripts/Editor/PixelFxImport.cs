using UnityEditor;
using UnityEngine;

namespace FourCore.EditorTools
{
    // Imports PIXELFX sprite sheets as crisp, uncompressed pixel art.
    public sealed class PixelFxImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Externals/PixelFX/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || assetImporter is not TextureImporter importer) return;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.sRGBTexture = true;
        }

        [MenuItem("4CORE/PixelFX/Reimport Sheets")]
        private static void Reimport()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Externals/PixelFX" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log("PIXELFX sheets reimported (point filter, no compression).");
        }
    }
}
