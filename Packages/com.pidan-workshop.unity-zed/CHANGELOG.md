# Changelog

## [0.1.3] - 2026-07-09

### Added

- Configurable Zed window behavior with Smart, Reuse Current Window, and Always New Workspace modes

## [0.1.1] - 2026-07-05

### Added

- External Tools UI for Visual Studio Editor project generation flags and project file regeneration
- Project Settings page for Zed executable detection, file routing extensions, and Project Panel setup
- Project Settings actions for generating `.zed/settings.json` and Unity folder `.gitignore` entries for Zed Project Panel filtering

### Changed

- Move Zed executable configuration from a dedicated Preferences page into Project Settings
- Route only configured source file extensions to Zed instead of all Unity assets
- Remove custom additional CLI arguments from the Zed launch path

## [0.1.0] - 2026-07-05

### Added

- Initial package scaffold
- `Unity.Zed.Editor` assembly definition
- `com.unity.ide.visualstudio` dependency for C# project generation
- Zed executable auto-detection on Windows, macOS, and Linux
- External Tools integration with IExternalCodeEditor
- .sln/.csproj generation via Visual Studio Editor bridge
- Double-click script opens file at correct line/column in Zed
