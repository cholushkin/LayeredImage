using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLib.LayeredImage
{
    public class LayeredImageSpritePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory)) return;

            string[] manifests = Directory.GetFiles(directory, "*.layeredimage", SearchOption.TopDirectoryOnly);
            bool isExplicitlyOwnedLayer = false;

            // Determine if the current PNG is explicitly referenced by ANY manifest in the folder.
            foreach (string manifestPath in manifests)
            {
                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = JsonUtility.FromJson<LayeredImageManifest>(json);
                    
                    if (manifest != null && manifest.children != null)
                    {
                        var allPaths = manifest.GetAllImagePaths();
                        string targetRelPath = assetPath.Replace("\\", "/");

                        if (allPaths.Any(p => Path.Combine(directory, p).Replace("\\", "/") == targetRelPath))
                        {
                            isExplicitlyOwnedLayer = true;
                            break;
                        }
                    }
                }
                catch
                {
                    // Ignore parse failures at this stage; LayeredImageImporter will catch and report them.
                }
            }

            if (isExplicitlyOwnedLayer)
            {
                if (textureImporter.textureType != TextureImporterType.Sprite)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                }
                
                int width, height;
                GetImageDimensions(assetPath, out width, out height);
                
                int maxDimension = Mathf.Max(width, height);
                int nextPo2 = GetNextPowerOfTwo(maxDimension);
                
                if (textureImporter.maxTextureSize < nextPo2)
                {
                    textureImporter.maxTextureSize = nextPo2;
                    Debug.Log($"LayeredImage: Set max texture size for '{Path.GetFileName(assetPath)}' to {nextPo2}.");
                }
                
                // Ensure Krita/Python output isn't degraded by default Unity compression
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.mipmapEnabled = false;
            }
        }

        private int GetNextPowerOfTwo(int value)
        {
            int result = 1;
            while (result < value)
            {
                result <<= 1;
            }
            return Mathf.Max(result, 32); 
        }

        private void GetImageDimensions(string path, out int width, out int height)
        {
            width = 4096;
            height = 4096;
            
            try 
            {
                // Efficiently read dimensions without loading the texture into Unity's main memory
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    width = tex.width;
                    height = tex.height;
                }
                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LayeredImage: Could not accurately measure dimensions for '{Path.GetFileName(path)}', defaulting to 4096. ({e.Message})");
            }
        }
    }
}