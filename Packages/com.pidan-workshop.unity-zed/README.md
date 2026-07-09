# Zed Editor for Unity

Unity editor integration for [Zed](https://zed.dev).

This package registers Zed as an external script editor in Unity, generates C# project files through Unity's Visual Studio Editor package, and opens C# / shader source files from Unity with line and column navigation.

## Features

- Registers **Zed** in `Edit > Preferences > External Tools > External Script Editor`
- Opens the current Unity project folder as the Zed workspace
- Configures whether Unity opens Zed smartly, in the current window, or in a new workspace
- Opens C# and shader source files at the requested line and column from Unity Console, Project window, and double-clicks
- Reuses Unity's Visual Studio Editor project generator and exposes the same `.csproj` generation toggles
- Generates and syncs `.sln` / `.csproj` files through `com.unity.ide.visualstudio`
- Configures which file extensions Unity should route to Zed
- Optionally injects project-configured Roslyn analyzer paths into generated `.csproj` files
- Can create `.zed/settings.json` and `.gitignore` entries so Zed's Project Panel hides generated Unity folders
- Auto-detects Zed installations on Windows, macOS, and Linux
- Provides a Project Settings page for executable path, file routing, analyzer paths, and Project Panel setup

## Requirements

- Unity 2021.2 or newer
- Zed installed locally
- `com.unity.ide.visualstudio` 2.0.0 or newer

The Visual Studio Editor package is used only for Unity project file generation. You do not need to use Visual Studio as your editor.

## Installation

### Install From Git

Open Unity Package Manager, choose **Add package from git URL...**, and enter the repository URL:

```text
https://github.com/<owner>/<repo>.git
```

### Install Locally

Copy this package into your Unity project:

```text
Packages/com.pidan-workshop.unity-zed
```

Unity should detect the package automatically after the editor refreshes.

## Setup

1. Open `Edit > Preferences > External Tools`.
2. Set **External Script Editor** to **Zed**.
3. If Zed is not detected automatically, open `Edit > Project Settings > Zed Editor` and set the executable path manually.

On Windows, either of these paths is accepted:

```text
%LOCALAPPDATA%\Programs\Zed\bin\zed.exe
%LOCALAPPDATA%\Programs\Zed\zed.exe
```

When the app executable is selected, the package resolves it to the CLI executable when available.

## Usage

Use Unity normally:

- Double-click a C# or shader source file in the Project window
- Click a C# stack trace entry in the Console
- Use `Assets > Open C# Project`

The package launches Zed with the Unity project folder and the selected source file path. Window behavior is configured in:

```text
Edit > Project Settings > Zed Editor
```

Available behaviors:

- **Smart**: opens a new Zed workspace for the current project, watches it until that workspace window closes, then lets later opens reuse the project workspace while it is alive.
- **Reuse Current Window**: asks Zed to open Unity requests in an existing Zed window.
- **Always New Workspace**: asks Zed to create a new workspace for every Unity open request.

Supported source file extensions are configured in:

```text
Edit > Project Settings > Zed Editor
```

Defaults:

```text
.cs
.shader
.compute
.hlsl
.cginc
.glsl
```

Other Unity assets are ignored so Unity can handle them normally.

## Project Generation

When Zed is selected as the external script editor, Unity's External Tools panel shows the same project generation options used by the Visual Studio Editor package:

- Embedded packages
- Local packages
- Registry packages
- Git packages
- Built-in packages
- Local tarball
- Packages from unknown sources
- Player projects

The **Regenerate project files** button calls Unity's Visual Studio Editor project generator. Zed does not maintain a separate `.sln` / `.csproj` generator.

## Roslyn Analyzers And Source Generators

Some Unity projects rely on Roslyn analyzers or source generators that need explicit `<Analyzer Include="...">` entries in generated `.csproj` files for Zed's C# language server to see generated code.

This package can add those entries during Unity project file generation. The feature is disabled by default and the package does not ship with any analyzer paths configured.

Open:

```text
Edit > Project Settings > Zed Editor
```

In **C# Project Analyzers**:

- Enable **Inject Analyzer Paths**
- Add one analyzer DLL path per line in **Analyzer Paths**
- Use either project-relative paths, such as `Packages/Example.Package/Analyzers/Example.Generator.dll`, or absolute paths
- Optionally enable **Inject Unity Source Generators** to add Unity's editor-provided source generator DLLs

After changing these settings, regenerate project files from Unity so the `.csproj` files are rewritten:

```text
Assets > Open C# Project
```

or use the **Regenerate project files** button in Unity's External Tools panel.

If a configured analyzer path does not exist, the package skips that entry and logs a warning in the Unity Console.

## Zed Project Panel

Open:

```text
Edit > Project Settings > Zed Editor
```

The Zed Project Panel section can:

- Create `.zed/settings.json` with `project_panel.hide_gitignore` enabled
- Add common Unity generated folders to `.gitignore`
- Let you customize the ignore entries before writing them

This uses Zed's current Project Panel behavior: when `project_panel.hide_gitignore` is enabled, files matched by `.gitignore` are hidden from the Project Panel.

## Settings

The package adds a settings page at:

```text
Edit > Project Settings > Zed Editor
```

Available options:

- **Zed Executable Path**: manually set the Zed executable if auto-detection fails
- **Detect Again**: clear the cached executable path and run detection again
- **Window Behavior**: configure whether Unity opens Zed smartly, reuses the current window, or always creates a new workspace
- **Open in Zed**: configure source file extensions that Unity should route to Zed
- **C# Project Analyzers**: configure analyzer DLL paths to inject into generated `.csproj` files
- **Zed Project Panel**: create `.zed/settings.json` and `.gitignore` entries for hiding generated Unity folders

## Auto-Detection Paths

The package searches common install locations before falling back to `PATH`.

Windows:

```text
%LOCALAPPDATA%\Programs\Zed\bin\zed.exe
%LOCALAPPDATA%\Programs\Zed\zed.exe
C:\Program Files\Zed\bin\zed.exe
C:\Program Files\Zed\zed.exe
PATH
```

macOS:

```text
/Applications/Zed.app/Contents/MacOS/zed
~/Applications/Zed.app/Contents/MacOS/zed
PATH
```

Linux:

```text
~/.local/bin/zed
~/.local/share/flatpak/exports/bin/dev.zed.Zed
/usr/local/bin/zed
/usr/bin/zed
/var/lib/flatpak/exports/bin/dev.zed.Zed
PATH
```

## Troubleshooting

### Zed Does Not Appear In External Script Editor

Make sure the package is installed under `Packages/com.pidan-workshop.unity-zed` and Unity has finished compiling scripts.

### Zed Executable Not Found

Open `Edit > Project Settings > Zed Editor` and set the executable path manually.

On Windows, prefer:

```text
%LOCALAPPDATA%\Programs\Zed\bin\zed.exe
```

### Scripts Open Without Correct Line Navigation

Regenerate project files from Unity:

```text
Assets > Open C# Project
```

Then try opening the script or Console entry again.

## Package Layout

```text
Packages/com.pidan-workshop.unity-zed/
  Editor/
    ZedDiscovery.cs
    ZedEditorPrefs.cs
    ZedProcess.cs
    ZedProjectSettings.cs
    ZedProjectSettingsProvider.cs
    ZedScriptEditor.cs
    ZedAnalyzerProjectPostprocessor.cs
    ZedSolutionGeneratorBridge.cs
    ZedWorkspaceSettings.cs
  package.json
  README.md
  CHANGELOG.md
```
