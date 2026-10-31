# Changelog

All notable changes to this package will be documented in this file.

## [1.0.1] - 2026-10-31

### Fixed

- `Editor/LayeredImage.Editor.asmdef` did not reference the `LayeredImage.Runtime` assembly,
  causing `CS0246` errors (`NodeInfo`, `LayeredImageManifest` not found) in `LayeredImageBuilder.cs`.
  Added a GUID reference to the Runtime assembly.
- `Editor/LayeredImageImporter.cs` used `Newtonsoft.Json` with no declared dependency, causing a
  `CS0246` error (`Newtonsoft` not found) in consuming projects that don't already have the
  Newtonsoft package installed. Added `com.unity.nuget.newtonsoft-json` as a package dependency.

## [1.0.0] - 2026-10-30

### Fixed

- `Editor/LayeredImage.Editor.asmdef` had an inverted platform configuration
  (`includePlatforms: []` + `excludePlatforms: ["Editor"]`), which compiled the Editor-only
  assembly into every platform except the Editor, despite all 3 of its source files using
  `UnityEditor` APIs. Corrected to `includePlatforms: ["Editor"]`.

### Changed

- Converted to a standard Unity Package Manager package installable via Git URL.
- Package id set to `com.qcat.layeredimage`.
- `Documentation/` renamed to `Documentation~/` (UPM-hidden).

### Added

- Root `package.json`, `CHANGELOG.md`, `LICENSE`.
