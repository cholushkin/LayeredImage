// todo: Add serialization validation to catch missing JSON fields
// idea: Support animation frames or multipage exports in the future

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public sealed class LayeredImageManifest
{
    [JsonProperty("version")] 
    public int Version { get; set; }
    
    [JsonProperty("name")] 
    public string Name { get; set; }
    
    [JsonProperty("document")] 
    public LayeredImageDocument Document { get; set; }
    
    [JsonProperty("children")] 
    public List<LayeredImageNode> Children { get; set; }
}

[Serializable]
public sealed class LayeredImageDocument
{
    [JsonProperty("width")] 
    public int Width { get; set; }
    
    [JsonProperty("height")] 
    public int Height { get; set; }
}

[Serializable]
public sealed class LayeredImageNode
{
    [JsonProperty("name")] 
    public string Name { get; set; }
    
    [JsonProperty("type")] 
    public string Type { get; set; }
    
    [JsonProperty("bounds")] 
    public LayeredImageBounds Bounds { get; set; }
    
    [JsonProperty("image")] 
    public string Image { get; set; }
    
    [JsonProperty("children")] 
    public List<LayeredImageNode> Children { get; set; }
}

[Serializable]
public sealed class LayeredImageBounds
{
    [JsonProperty("x")] 
    public int X { get; set; }
    
    [JsonProperty("y")] 
    public int Y { get; set; }
    
    [JsonProperty("width")] 
    public int Width { get; set; }
    
    [JsonProperty("height")] 
    public int Height { get; set; }
}