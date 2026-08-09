using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using Newtonsoft.Json;

namespace GameLib.LayeredImage
{
    public enum LayeredImageMode
    {
        SpriteRenderer,
        UI
    }

    [ScriptedImporter(1, "layeredimage")]
    public class LayeredImageImporter : ScriptedImporter
    {
        [Tooltip("Pixels Per Unit for SpriteRenderer mode.")]
        public float pixelsPerUnit = 100f;
        
        [Tooltip("Choose whether to build as world-space Sprites or Canvas UI Images.")]
        public LayeredImageMode renderMode = LayeredImageMode.SpriteRenderer;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                if (pixelsPerUnit <= 0f)
                {
                    throw new Exception($"Invalid pixelsPerUnit ({pixelsPerUnit}). Value must be greater than zero.");
                }

                string json = File.ReadAllText(ctx.assetPath);
                var manifest = JsonConvert.DeserializeObject<LayeredImageManifest>(json);

                if (manifest == null)
                {
                    ctx.LogImportError("Failed to parse .layeredimage JSON. Manifest is null.");
                    return;
                }

                manifest.Validate();

                string directory = Path.GetDirectoryName(ctx.assetPath);
                foreach (string relativePath in manifest.GetAllImagePaths())
                {
                    string fullPath = Path.Combine(directory, relativePath).Replace("\\", "/");
                    ctx.DependsOnSourceAsset(fullPath);
                }

                bool buildAsUI = renderMode == LayeredImageMode.UI;
                var builder = new LayeredImageBuilder(manifest, ctx.assetPath, pixelsPerUnit, buildAsUI);
                GameObject root = builder.Build(ctx);

                if (root != null)
                {
                    ctx.AddObjectToAsset("root", root);
                    ctx.SetMainObject(root);
                }
                else
                {
                    ctx.LogImportError("LayeredImageBuilder returned a null root object.");
                }
            }
            catch (Exception ex)
            {
                ctx.LogImportError($"LayeredImage Import Error: {ex.Message}");
            }
        }

        private void OnValidate()
        {
            if (pixelsPerUnit <= 0f)
            {
                pixelsPerUnit = 0.01f;
            }
        }
    }
}