Below is a team-oriented README that combines **how to use the module** with the **implementation details needed to maintain or extend it**.

# LayeredImage

![Logo](Documentation~/Logo.jpg)

## Introduction

`LayeredImage` is a Unity Editor/runtime module for importing layered image documents into a hierarchy of Unity `GameObject`s.

The module uses a custom `.layeredimage` file format containing a JSON manifest that describes:

* The document dimensions.
* The hierarchy of image and group layers.
* The source image associated with each image layer.
* The pixel-space bounds of each layer.

During import, Unity reads the manifest, validates it, tracks its image dependencies, and builds a corresponding `GameObject` hierarchy.

The module supports two rendering modes:

1. **SpriteRenderer** — creates world-space `SpriteRenderer` objects and positions them using Pixels Per Unit.
2. **UI** — creates Unity UI `Image` components with `RectTransform`s positioned in document pixel coordinates.

This allows layered artwork produced by external tools or pipelines to be represented in Unity without manually assembling individual sprites and maintaining their layer ordering.

### Problem solved

Traditional layered artwork requires developers or artists to manually:

* Import each image as a Sprite.
* Create a GameObject for every layer.
* Reconstruct the layer hierarchy.
* Position every layer correctly.
* Preserve the original layer ordering.
* Keep imported textures configured consistently.

`LayeredImage` automates those steps from a single manifest file.

---

## Features

* Custom `.layeredimage` Unity asset importer.
* JSON-based layered document format.
* Nested group and image layer support.
* Automatic `GameObject` hierarchy generation.
* SpriteRenderer rendering for world-space content.
* UI `Image` rendering for Canvas-based content.
* Pixel-to-world coordinate conversion using configurable Pixels Per Unit.
* Automatic Sprite import configuration for referenced textures.
* Automatic source-asset dependency tracking.
* Manifest validation with descriptive import errors.
* Support for nested image paths and relative asset references.
* Preservation of layer ordering through generated sorting orders.
* Manifest versioning for future format changes.
* Automatic detection of explicitly referenced image assets.

---

# Using the Module

## Integration with Script Pipeline System

The `LayeredImage` module works in conjunction with the [`user-scripts`](https://github.com/cholushkin/user-scripts) repository, and more specifically with its [`KritaExport`](https://github.com/cholushkin/user-scripts/tree/master/KritaExport) tooling. The export scripts are responsible for converting layered artwork from the image-editing workflow into the `.layeredimage` format consumed by this Unity module.

The workflow is designed to be independent of the specific image editor used to create the artwork. Regardless of which supported image-editing application is used, the export process produces the same standardized layered-image representation. This allows the Unity side of the pipeline to remain consistent while artists can use the image editor that best fits their workflow.


## Basic workflow

A typical workflow is:

```text
Layered artwork
      │
      ▼
.layeredimage manifest
      │
      ├── image layer → PNG/Sprite
      ├── image layer → PNG/Sprite
      └── group
            ├── image layer
            └── image layer
      │
      ▼
LayeredImageImporter
      │
      ├── Validate manifest
      ├── Register image dependencies
      └── Build GameObject hierarchy
      │
      ▼
Unity asset
```

Place the `.layeredimage` file and its referenced images in the Unity project.

Unity automatically invokes `LayeredImageImporter` because the importer is registered for the `.layeredimage` extension.

---

## Importing a `.layeredimage` file

A `.layeredimage` file is a JSON document.

A minimal example is:

```json
{
  "version": "1",
  "name": "Character",
  "document": {
    "width": 1024,
    "height": 1024
  },
  "children": [
    {
      "name": "Body",
      "type": "image",
      "bounds": {
        "x": 100,
        "y": 200,
        "width": 400,
        "height": 600
      },
      "imagePath": "body.png"
    },
    {
      "name": "Details",
      "type": "group",
      "children": [
        {
          "name": "Eyes",
          "type": "image",
          "bounds": {
            "x": 250,
            "y": 300,
            "width": 100,
            "height": 50
          },
          "imagePath": "eyes.png"
        }
      ]
    }
  ]
}
```

The image paths are resolved relative to the directory containing the `.layeredimage` file.

For example:

```text
Assets/
└── Art/
    └── Character/
        ├── Character.layeredimage
        ├── body.png
        └── eyes.png
```

`Character.layeredimage` can reference:

```json
"imagePath": "body.png"
```

or a subdirectory:

```json
"imagePath": "Layers/body.png"
```

---

# Manifest Format

## Root manifest

The root object is represented by `LayeredImageManifest`.

| Property   | Type   | Required | Description                                  |
| ---------- | ------ | -------: | -------------------------------------------- |
| `version`  | string |      Yes | Manifest format version. Currently `"1"`.    |
| `name`     | string |      Yes | Name used for the generated root GameObject. |
| `document` | object |      Yes | Document dimensions in pixels.               |
| `children` | array  |       No | Root-level layers/groups.                    |

Example:

```json
{
  "version": "1",
  "name": "Example",
  "document": {
    "width": 2048,
    "height": 2048
  },
  "children": []
}
```

### Document dimensions

```json
"document": {
  "width": 2048,
  "height": 2048
}
```

Both dimensions must be greater than zero.

These dimensions are used for:

* UI root sizing.
* SpriteRenderer coordinate conversion.
* Calculating the document center.

---

# Nodes

Each entry in `children` is a `NodeInfo`.

There are currently two supported node types:

* `image`
* `group`

## Image node

An image node references a source Sprite:

```json
{
  "name": "Background",
  "type": "image",
  "bounds": {
    "x": 0,
    "y": 0,
    "width": 1024,
    "height": 1024
  },
  "imagePath": "background.png"
}
```

Required fields:

* `name`
* `type`
* `bounds`
* `imagePath`

The referenced image must exist and be imported by Unity as a Sprite.

## Group node

A group creates a `GameObject` that can contain child nodes:

```json
{
  "name": "Face",
  "type": "group",
  "children": [
    {
      "name": "Eyes",
      "type": "image",
      "bounds": {
        "x": 100,
        "y": 100,
        "width": 50,
        "height": 30
      },
      "imagePath": "eyes.png"
    }
  ]
}
```

Groups do not have image bounds of their own.

### Optional node type

`NodeInfo.Validate()` can infer the node type when `type` is omitted:

* A node with children becomes a `group`.
* A leaf node becomes an `image`.

For clarity and forward compatibility, explicitly specifying `type` is recommended.

---

# Coordinate System

Layer bounds are expressed in document pixel coordinates.

```text
(0, 0)
┌──────────────────────────────┐
│                              │
│       Image bounds           │
│          ┌─────────┐         │
│          │         │         │
│          │  Layer  │         │
│          │         │         │
│          └─────────┘         │
│                              │
└──────────────────────────────┘
                    (width,height)
```

The bounds represent:

```text
x      = left position
y      = top position
width  = layer width
height = layer height
```

The coordinate interpretation differs slightly between rendering modes.

---

# Rendering Modes

`LayeredImageImporter` exposes:

```csharp
public LayeredImageMode renderMode;
```

with:

```csharp
public enum LayeredImageMode
{
    SpriteRenderer,
    UI
}
```

## SpriteRenderer mode

This is the default mode.

Each image layer becomes:

```text
GameObject
└── SpriteRenderer
```

The importer converts document pixels to world units using:

```text
world position = document position / pixelsPerUnit
```

The document is centered around `(0, 0)`.

For an image layer, the center position is calculated as:

```csharp
float docHalfWidth = _manifest.document.width / 2f;
float docHalfHeight = _manifest.document.height / 2f;

float centerX =
    (node.bounds.x + (node.bounds.width / 2f)) - docHalfWidth;

float centerY =
    docHalfHeight -
    (node.bounds.y + (node.bounds.height / 2f));
```

The final Unity position is:

```csharp
new Vector3(
    centerX / _ppu,
    centerY / _ppu,
    0
);
```

### Pixels Per Unit

The importer exposes:

```csharp
public float pixelsPerUnit = 100f;
```

For example, with:

```text
pixelsPerUnit = 100
```

a 100-pixel distance becomes:

```text
1 Unity unit
```

The value must be greater than zero.

---

## UI mode

When `renderMode` is set to `UI`, image nodes become:

```text
GameObject
└── Image
    └── RectTransform
```

The root object receives a `RectTransform` sized to the document:

```csharp
rect.sizeDelta = new Vector2(
    _manifest.document.width,
    _manifest.document.height
);
```

Image layers use their document bounds directly:

```csharp
rect.sizeDelta =
    new Vector2(node.bounds.width, node.bounds.height);

rect.pivot = new Vector2(0, 1);
rect.anchorMin = new Vector2(0, 1);
rect.anchorMax = new Vector2(0, 1);

rect.anchoredPosition =
    new Vector2(node.bounds.x, -node.bounds.y);
```

The generated hierarchy can therefore be placed under a Unity `Canvas`.

---

# Layer Ordering

Layer ordering is handled by `LayeredImageBuilder`.

The builder traverses child collections in reverse order:

```csharp
foreach (var childNode in Enumerable.Reverse(_manifest.children))
{
    BuildNode(childNode, root.transform);
}
```

For SpriteRenderer mode, each image receives an incrementing sorting order:

```csharp
renderer.sortingOrder = _sortingOrderTracker++;
```

This provides deterministic ordering between generated SpriteRenderers.

When changing layer traversal logic, take care to preserve the relationship between manifest order, traversal order, and `sortingOrder`.

---

# Texture Importing

`LayeredImageSpritePostprocessor` automatically detects textures that are explicitly referenced by `.layeredimage` manifests in the same directory.

Referenced images are configured as:

```text
Texture Type       = Sprite
Sprite Mode        = Single
Texture Compression = Uncompressed
Mip Maps            = Disabled
```

This is important because layered artwork can be generated externally and may be sensitive to texture compression or mipmapping.

## Maximum texture size

The postprocessor reads the image dimensions and calculates the next power of two:

```text
512  → 512
700  → 1024
1024 → 1024
1500 → 2048
```

It then increases `maxTextureSize` when necessary.

The minimum calculated value is 32.

If the image cannot be measured, the postprocessor falls back to:

```text
4096 × 4096
```

and logs a warning.

---

# Asset Dependencies

`LayeredImageImporter` registers every referenced image as a source dependency:

```csharp
foreach (string relativePath in manifest.GetAllImagePaths())
{
    string fullPath =
        Path.Combine(directory, relativePath)
            .Replace("\\", "/");

    ctx.DependsOnSourceAsset(fullPath);
}
```

This allows Unity to reimport the `.layeredimage` asset when one of its referenced images changes.

`LayeredImageManifest.GetAllImagePaths()` recursively walks the entire node hierarchy, so nested group structures are supported.

---

# Validation

Before building the hierarchy, the importer calls:

```csharp
manifest.Validate();
```

Validation checks the following.

## Manifest

* `version` exists.
* `version` is supported.
* `name` exists.
* `document` exists.
* Document width is greater than zero.
* Document height is greater than zero.

## Nodes

* Node name exists.
* Node type is supported.
* Image nodes contain bounds.
* Image bounds have positive dimensions.
* Image nodes contain `imagePath`.
* Child nodes are recursively validated.

Currently supported manifest version:

```csharp
public const string SUPPORTED_VERSION = "1";
```

Invalid manifests are reported through Unity's importer error mechanism.

---

# Error Handling

The importer wraps the import process in a `try/catch`.

Errors are reported with:

```csharp
ctx.LogImportError(...)
```

Typical errors include:

### Invalid Pixels Per Unit

```text
Invalid pixelsPerUnit (...).
Value must be greater than zero.
```

### Invalid manifest

```text
Manifest validation failed: ...
```

### Missing image

```text
Image node '...' references a missing or invalid image ...
```

### Unsupported node type

```text
Unsupported node type '...'
```

When troubleshooting an import failure, inspect the Unity Console and the `.layeredimage` manifest first.

---

# Architecture

The module is intentionally split into Editor and Runtime responsibilities.

```text
LayeredImage/
├── DataAndSettings/
│   └── ModuleManifest(LayeredImage).asset
│
├── Editor/
│   ├── LayeredImageImporter.cs
│   ├── LayeredImageBuilder.cs
│   └── LayeredImageSpritePostprocessor.cs
│
├── Runtime/
│   └── LayeredImageManifest.cs
│
├── Documentation~/
│   └── Logo.jpg
│
└── Textures/
    └── layered-image-logo.png
```

## Installation

Install directly from Git via Unity Package Manager (**Window → Package Manager → + → Install package from git URL…**):

```
https://github.com/cholushkin/LayeredImage.git
```

This package has no required third-party dependencies.

## LayeredImageImporter

**File:**

```text
Editor/LayeredImageImporter.cs
```

Responsible for Unity asset importing.

Main responsibilities:

1. Validate importer settings.
2. Read the `.layeredimage` file.
3. Deserialize JSON.
4. Validate the manifest.
5. Register referenced images as dependencies.
6. Create a `LayeredImageBuilder`.
7. Build the GameObject hierarchy.
8. Register the generated root with Unity.

The importer is registered with:

```csharp
[ScriptedImporter(1, "layeredimage")]
```

The `1` is the Unity importer version. Incrementing this value may be appropriate when importer behavior changes in a way that requires Unity to reimport existing assets.

---

## LayeredImageBuilder

**File:**

```text
Editor/LayeredImageBuilder.cs
```

Responsible for converting the validated manifest into Unity objects.

The builder:

* Creates the root GameObject.
* Creates group GameObjects.
* Creates SpriteRenderer or UI Image components.
* Resolves image asset paths.
* Assigns sprites.
* Calculates positions.
* Applies sorting order.
* Recursively builds nested children.

The builder does not parse JSON or validate the manifest. That separation keeps importing and object construction independent.

---

## LayeredImageManifest

**File:**

```text
Runtime/LayeredImageManifest.cs
```

Defines the data model shared by the importer and runtime code.

Main types:

```text
LayeredImageManifest
├── DocumentInfo
├── NodeInfo
└── BoundsInfo
```

`LayeredImageManifest` also owns:

* Manifest validation.
* Recursive image-path collection.
* Manifest version information.

Although the importer itself is Editor-only, keeping the manifest data structures in `Runtime` makes the data model available to other project code when needed.

---

## LayeredImageSpritePostprocessor

**File:**

```text
Editor/LayeredImageSpritePostprocessor.cs
```

Responsible for preparing referenced image textures for use by the layered-image importer.

It runs during texture preprocessing and determines whether the current texture is explicitly referenced by a `.layeredimage` manifest in the same directory.

Only explicitly referenced images receive the special Sprite import settings.

This avoids globally changing import behavior for unrelated textures.

---

# Extension Guidelines

## Adding a new rendering mode

Rendering modes are currently represented by:

```csharp
public enum LayeredImageMode
{
    SpriteRenderer,
    UI
}
```

To add another mode:

1. Add a new enum value.
2. Update `LayeredImageImporter` to recognize the mode.
3. Add the corresponding build behavior to `LayeredImageBuilder`.
4. Keep manifest parsing independent of rendering mode.
5. Add tests/examples covering coordinate conversion and layer ordering.

Prefer keeping mode-specific behavior inside the builder rather than adding rendering concerns to `LayeredImageManifest`.

---

## Adding a new node type

Currently:

```text
image
group
```

To introduce another type:

1. Update `NodeInfo.Validate()`.
2. Add the corresponding construction logic in `LayeredImageBuilder.BuildNode()`.
3. Define the required JSON properties.
4. Update this README with an example.
5. Consider whether the new node contributes image dependencies.
6. Verify nested nodes and ordering behavior.

For example, a future `mask` node would require both manifest semantics and a corresponding Unity representation.

---

# Code Maintenance Guidelines

### Keep validation centralized

Manifest validation belongs in `LayeredImageManifest.Validate()`.

Avoid duplicating validation rules in the importer and builder.

### Keep asset construction in the builder

The importer should orchestrate the process rather than contain detailed GameObject construction logic.

### Preserve relative paths

Image paths are resolved relative to the `.layeredimage` file. Changes to path handling should preserve this behavior.

### Be careful with coordinate conversions

There are two coordinate systems:

```text
Document pixels
      │
      ├── SpriteRenderer → centered world coordinates / PPU
      │
      └── UI → RectTransform pixel coordinates
```

Changes to one mode should not unintentionally alter the other.

### Preserve deterministic ordering

Layer order is part of the visual result. Changes to recursive traversal or sorting-order assignment should be treated as rendering behavior changes.

---

# Adding Code That Uses the Module

Code generally interacts with the generated hierarchy rather than directly constructing the manifest classes.

For example, an imported asset can be treated as a normal Unity asset whose root object contains the generated layer hierarchy.

A generated SpriteRenderer hierarchy resembles:

```text
Character
├── Body
│   └── SpriteRenderer
├── Clothes
│   └── SpriteRenderer
└── Face
    ├── Eyes
    │   └── SpriteRenderer
    └── Mouth
        └── SpriteRenderer
```

When writing gameplay or presentation code, prefer referencing meaningful generated layer names rather than relying on hierarchy indices.

For example:

```csharp
Transform face = root.transform.Find("Face");
Transform eyes = face?.Find("Eyes");
```

Avoid depending on the internal `sortingOrder` values unless the code specifically needs to control rendering order.

---

# File and Folder Conventions

A recommended layout is:

```text
MyArtwork/
├── Character.layeredimage
├── body.png
├── clothes.png
├── face/
│   ├── eyes.png
│   └── mouth.png
└── ...
```

Keep referenced image assets close to their manifest. This makes relative paths easier to understand and allows the sprite postprocessor to identify owned assets reliably.

---

# Troubleshooting

## The `.layeredimage` asset fails to import

Check:

1. The JSON is valid.
2. `version` is `"1"`.
3. `name` is present.
4. `document.width` and `document.height` are positive.
5. Every image node has valid bounds.
6. Every image node has an `imagePath`.
7. Every referenced image exists.

---

## An image cannot be loaded

Verify that the referenced image:

* Exists at the path relative to the manifest.
* Is inside the Unity project.
* Has been imported as a Sprite.

The importer expects:

```csharp
AssetDatabase.LoadAssetAtPath<Sprite>(imageFullPath);
```

If Unity has imported the texture as a regular Texture rather than a Sprite, the builder will reject it.

---

## The artwork appears at the wrong scale

For SpriteRenderer mode, check:

```text
pixelsPerUnit
```

For example:

```text
100 px / 100 PPU = 1 Unity unit
```

If the artwork is twice as large as expected, review the PPU setting and the source document's intended scale.

---

## The artwork appears in the wrong position

Check the layer `bounds` values.

SpriteRenderer mode converts document coordinates into centered world coordinates, while UI mode uses top-left document coordinates through `RectTransform`.

Do not assume that the raw `x` and `y` values correspond directly to world-space coordinates.

---

## Layer ordering is incorrect

Check:

* The order of nodes in the manifest.
* Reverse traversal in `LayeredImageBuilder`.
* SpriteRenderer `sortingOrder`.
* Whether other SpriteRenderers outside the generated hierarchy are using conflicting sorting settings.

---

# Known Design Considerations

### Sprite postprocessor scope

The texture postprocessor searches for `.layeredimage` manifests in the **same directory** as the texture.

This means a texture referenced by a manifest in another directory may not be automatically recognized by the postprocessor.

If the project later needs cross-directory ownership detection, this behavior should be revisited.

### Two JSON serializers are used

`LayeredImageImporter` uses Newtonsoft JSON:

```csharp
JsonConvert.DeserializeObject<LayeredImageManifest>(json)
```

while the texture postprocessor uses Unity's:

```csharp
JsonUtility.FromJson<LayeredImageManifest>(json)
```

If the manifest schema becomes more complex, especially with serializer-specific features, these two parsing paths should be kept compatible or consolidated.

### Runtime/Editor boundary

The actual import/build pipeline is Editor-only, while the manifest data model lives in `Runtime`.

Changes should preserve this separation unless there is a deliberate requirement to expose additional runtime functionality.

---

# Summary

`LayeredImage` provides a lightweight bridge between layered artwork and Unity.

Its core workflow is:

```text
.layeredimage
    ↓
JSON manifest
    ↓
Validation
    ↓
Dependency registration
    ↓
GameObject hierarchy
    ↓
SpriteRenderer or UI Image
```

The module is most valuable when artwork consists of multiple independently rendered layers that need to retain their original hierarchy, positioning, and ordering inside Unity.

For maintainers, the primary extension points are:

* `LayeredImageManifest` — change the data model and validation.
* `LayeredImageImporter` — change Unity import behavior.
* `LayeredImageBuilder` — change generated GameObjects and rendering behavior.
* `LayeredImageSpritePostprocessor` — change texture preparation and ownership detection.

When extending the module, preserve the separation between **manifest data**, **import orchestration**, **GameObject construction**, and **texture preprocessing**. This keeps the importer predictable and makes new node types or rendering modes easier to introduce.

This is written for an **internal team tool**, so it focuses on both day-to-day usage and the implementation boundaries maintainers need to understand.
