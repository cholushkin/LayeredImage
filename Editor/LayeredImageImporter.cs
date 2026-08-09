// todo: Add UI Inspector with reimport triggers
// todo: Add support for assigning Addressable groups directly via JSON directives
// idea: Implement runtime variant that loads via config text assets

using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

[ScriptedImporter(1, "layeredimage")]
public class LayeredImageImporter : ScriptedImporter
{
    #region Properties
    
    public LayeredImageBuilder.ImportMode ImportMode = LayeredImageBuilder.ImportMode.UI;
    public float PixelsPerUnit = 100f;
    
    #endregion

    #region Unity Lifecycle
    
    public override void OnImportAsset(AssetImportContext ctx)
    {
        Debug.Log($"[LayeredImageImporter] Parsing layered image: {ctx.assetPath}");

        string json = File.ReadAllText(ctx.assetPath);
        var manifest = JsonConvert.DeserializeObject<LayeredImageManifest>(json);

        if (manifest == null)
        {
            Debug.LogError($"[LayeredImageImporter] Failed to parse JSON at {ctx.assetPath}");
            return;
        }

        GameObject root = LayeredImageBuilder.Build(
            manifest,
            ctx.assetPath,
            ImportMode,
            PixelsPerUnit,
            ctx
        );

        ctx.AddObjectToAsset("prefab", root);
        ctx.SetMainObject(root);
        
        Debug.Log($"[LayeredImageImporter] Prefab '{root.name}' generated successfully.");
    }
    
    #endregion
}