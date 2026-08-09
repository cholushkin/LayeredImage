// todo: Add quality presets based on target platform logic
// idea: Apply automatic tightly-packed physics bounding boxes for world mode

using UnityEditor;
using UnityEngine;
using System.IO;

public class LayeredImageSpritePostprocessor : AssetPostprocessor
{
    #region Unity Lifecycle
    
    void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith(".png") || !IsLayeredImageTexture(assetPath)) return;

        var importer = (TextureImporter)assetImporter;
        
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int targetSize = GetClosestPowerOfTwo(Mathf.Max(width, height));

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = targetSize;
        
        Debug.Log($"[LayeredImageSpritePostprocessor] Snapped {Path.GetFileName(assetPath)} to {targetSize}px");
    }
    
    #endregion

    #region Implementation
    
    private int GetClosestPowerOfTwo(int value)
    {
        int size = 32;
        while (size < value) size *= 2;
        return Mathf.Clamp(size, 32, 8192);
    }

    private bool IsLayeredImageTexture(string path)
    {
        string currentDir = Path.GetDirectoryName(path);

        while (!string.IsNullOrEmpty(currentDir) && currentDir.Replace('\\', '/').StartsWith("Assets"))
        {
            if (Directory.GetFiles(currentDir, "*.layeredimage").Length > 0)
            {
                return true;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        return false;
    }
    
    #endregion
}