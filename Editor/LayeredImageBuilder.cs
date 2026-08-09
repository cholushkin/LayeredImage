// todo: Support SortingGroup for complex layering scenarios
// todo: Implement Z-depth stacking instead of sortingOrder
// idea: Add optional sprite atlas integration based on output folder

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using UnityEditor.AssetImporters;

public static class LayeredImageBuilder
{
    public enum ImportMode
    {
        UI,
        SpriteRenderer
    }

    #region Implementation
    
    public static GameObject Build(
        LayeredImageManifest manifest,
        string jsonAssetPath,
        ImportMode mode,
        float pixelsPerUnit,
        AssetImportContext ctx)
    {
        Debug.Assert(manifest != null, "Manifest cannot be null");
        
        string rootName = string.IsNullOrEmpty(manifest.Name) ? "LayeredImage" : manifest.Name;
        var root = new GameObject(rootName);

        if (mode == ImportMode.UI)
        {
            root.AddComponent<RectTransform>();
        }

        if (manifest.Children == null || manifest.Children.Count == 0)
        {
            Debug.LogWarning($"[LayeredImageBuilder] Root '{rootName}' has no children. Prefab will be empty.");
            return root;
        }

        int sortingOrder = 0;
        foreach (var child in manifest.Children)
        {
            sortingOrder = BuildNodeRecursive(child, root.transform, jsonAssetPath, mode, pixelsPerUnit, manifest, sortingOrder, ctx);
        }

        return root;
    }

    private static int BuildNodeRecursive(
        LayeredImageNode node,
        Transform parent,
        string jsonAssetPath,
        ImportMode mode,
        float ppu,
        LayeredImageManifest manifest,
        int sortingOrder,
        AssetImportContext ctx)
    {
        if (node.Children != null && node.Children.Count > 0)
        {
            var group = new GameObject(node.Name);
            group.transform.SetParent(parent, false);

            if (mode == ImportMode.UI)
            {
                group.AddComponent<RectTransform>();
            }

            foreach (var child in node.Children)
            {
                sortingOrder = BuildNodeRecursive(child, group.transform, jsonAssetPath, mode, ppu, manifest, sortingOrder, ctx);
            }

            return sortingOrder;
        }

        if (node.Type == "image")
        {
            string imagePath = Path.Combine(Path.GetDirectoryName(jsonAssetPath) ?? string.Empty, node.Image).Replace("\\", "/");
            
            ctx?.DependsOnSourceAsset(imagePath);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[LayeredImageBuilder] Sprite missing or unimported at: {imagePath}");
            }

            var go = new GameObject(node.Name);
            go.transform.SetParent(parent, false);

            float centerX = node.Bounds.X + node.Bounds.Width * 0.5f;
            float centerY = node.Bounds.Y + node.Bounds.Height * 0.5f;
            float docCenterX = manifest.Document?.Width * 0.5f ?? 0f;
            float docCenterY = manifest.Document?.Height * 0.5f ?? 0f;

            float localX = centerX - docCenterX;
            float localY = docCenterY - centerY;

            if (mode == ImportMode.UI)
            {
                var rect = go.AddComponent<RectTransform>();
                var image = go.AddComponent<Image>();
                
                if (sprite != null) image.sprite = sprite;

                rect.anchoredPosition = new Vector2(localX, localY);
                rect.sizeDelta = new Vector2(node.Bounds.Width, node.Bounds.Height);
            }
            else
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                
                if (sprite != null) renderer.sprite = sprite;
                
                renderer.sortingOrder = sortingOrder++;
                go.transform.localPosition = new Vector3(localX / ppu, localY / ppu, 0f);
            }
        }

        return sortingOrder;
    }
    
    #endregion
}