# Changelog

All notable changes to this package will be documented in this file.

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
