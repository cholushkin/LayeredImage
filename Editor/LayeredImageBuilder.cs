using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UI;

namespace GameLib.LayeredImage
{
    public class LayeredImageBuilder
    {
        private readonly LayeredImageManifest _manifest;
        private readonly string _assetPath;
        private readonly float _ppu;
        private readonly bool _isUI;
        
        private int _sortingOrderTracker = 0;

        public LayeredImageBuilder(LayeredImageManifest manifest, string assetPath, float ppu, bool isUI)
        {
            _manifest = manifest;
            _assetPath = assetPath;
            _ppu = ppu;
            _isUI = isUI;
        }

        public GameObject Build(AssetImportContext ctx)
        {
            GameObject root = new GameObject(_manifest.name);
            
            if (_isUI)
            {
                var rect = root.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(_manifest.document.width, _manifest.document.height);
            }

            // Ordering Contract: Top-down traversal assigns increasing sorting layers.
            // Earlier children in JSON render behind later children.
            _sortingOrderTracker = 0; 

            if (_manifest.children != null)
            {
                foreach (var childNode in _manifest.children)
                {
                    BuildNode(childNode, root.transform);
                }
            }

            return root;
        }

        private void BuildNode(NodeInfo node, Transform parent)
        {
            GameObject go = new GameObject(node.name);
            go.transform.SetParent(parent, false);

            if (node.type == "group")
            {
                if (_isUI)
                {
                    var rect = go.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
            }
            else if (node.type == "image")
            {
                string directory = Path.GetDirectoryName(_assetPath);
                string imageFullPath = Path.Combine(directory, node.imagePath).Replace("\\", "/");

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imageFullPath);
                if (sprite == null)
                {
                    throw new Exception($"Image node '{node.name}' references a missing or invalid image at '{imageFullPath}'. Asset must exist and be imported as a Sprite.");
                }

                if (_isUI)
                {
                    var image = go.AddComponent<Image>();
                    image.sprite = sprite;
                    image.SetNativeSize();

                    var rect = go.GetComponent<RectTransform>();
                    
                    // Native UI Layout mapping (Top-Left origin assumption for 2D software)
                    rect.pivot = new Vector2(0, 1);
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(node.bounds.x, -node.bounds.y);
                }
                else
                {
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = _sortingOrderTracker++;

                    // World Space conversion utilizing PixelsPerUnit
                    float docHalfWidth = _manifest.document.width / 2f;
                    float docHalfHeight = _manifest.document.height / 2f;
                    
                    float centerX = (node.bounds.x + (node.bounds.width / 2f)) - docHalfWidth;
                    float centerY = docHalfHeight - (node.bounds.y + (node.bounds.height / 2f));
                    
                    go.transform.localPosition = new Vector3(centerX / _ppu, centerY / _ppu, 0);
                }
            }

            if (node.children != null)
            {
                foreach (var childNode in node.children)
                {
                    BuildNode(childNode, go.transform);
                }
            }
        }
    }
}