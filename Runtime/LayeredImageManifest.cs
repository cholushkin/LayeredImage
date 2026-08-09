using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLib.LayeredImage
{
    [Serializable]
    public class LayeredImageManifest
    {
        public string version;
        public string name;
        public DocumentInfo document;
        public List<NodeInfo> children = new List<NodeInfo>();

        public void Validate()
        {
            if (string.IsNullOrEmpty(version)) 
                throw new Exception("Manifest validation failed: 'version' is missing.");
            if (string.IsNullOrEmpty(name)) 
                throw new Exception("Manifest validation failed: 'name' is missing.");
            if (document == null) 
                throw new Exception("Manifest validation failed: 'document' is missing.");
            if (document.width <= 0 || document.height <= 0) 
                throw new Exception($"Manifest validation failed: Invalid document dimensions ({document.width}x{document.height}). Dimensions must be > 0.");

            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Validate();
                }
            }
        }

        public IEnumerable<string> GetAllImagePaths()
        {
            var paths = new List<string>();
            CollectPaths(children, paths);
            return paths;
        }

        private void CollectPaths(List<NodeInfo> nodes, List<string> paths)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                // Fallback support if 'type' is missing but it has an image path
                bool isImage = node.type == "image" || (!string.IsNullOrEmpty(node.imagePath) && string.IsNullOrEmpty(node.type));
                if (isImage && !string.IsNullOrEmpty(node.imagePath))
                {
                    paths.Add(node.imagePath);
                }
                CollectPaths(node.children, paths);
            }
        }
    }

    [Serializable]
    public class DocumentInfo
    {
        public int width;
        public int height;
    }

    [Serializable]
    public class NodeInfo
    {
        public string name;
        public string type;
        public BoundsInfo bounds;
        public string imagePath;
        public List<NodeInfo> children = new List<NodeInfo>();

        public void Validate()
        {
            if (string.IsNullOrEmpty(name)) 
                throw new Exception("Node validation failed: Node name is missing.");

            // Legacy compatibility: Infer "group" if children exist but no type is specified.
            if (string.IsNullOrEmpty(type))
            {
                type = (children != null && children.Count > 0) ? "group" : "image";
            }

            if (type == "image")
            {
                if (bounds == null) 
                    throw new Exception($"Image node '{name}' is missing 'bounds'.");
                if (bounds.width <= 0 || bounds.height <= 0) 
                    throw new Exception($"Image node '{name}' has invalid bounds ({bounds.width}x{bounds.height}).");
                if (string.IsNullOrEmpty(imagePath)) 
                    throw new Exception($"Image node '{name}' is missing 'imagePath'.");
            }
            else if (type != "group")
            {
                throw new Exception($"Unsupported node type '{type}' on node '{name}'. Only 'image' and 'group' are currently supported.");
            }

            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Validate();
                }
            }
        }
    }

    [Serializable]
    public class BoundsInfo
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }
}