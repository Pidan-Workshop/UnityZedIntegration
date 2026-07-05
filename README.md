# Zed Editor for Unity

Unity editor integration for [Zed](https://zed.dev).

This package registers Zed as an external script editor in Unity, generates C# project files through Unity's Visual Studio Editor package, and opens scripts from Unity with line and column navigation.

## Features

- Registers **Zed** in `Edit > Preferences > External Tools > External Script Editor`
- Opens the current Unity project folder as the Zed workspace
- Reuses an already-open Zed workspace when Zed can match the project folder
- Opens scripts at the requested line and column from Unity Console, Project window, and script double-clicks
- Generates and syncs `.sln` / `.csproj` files through `com.unity.ide.visualstudio`
- Auto-detects Zed installations on Windows, macOS, and Linux
- Provides a Preferences page for manual executable path and additional CLI arguments

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
3. If Zed is not detected automatically, open `Edit > Preferences > External Tools > Zed Editor` and set the executable path manually.

On Windows, either of these paths is accepted:

```text
%LOCALAPPDATA%\Programs\Zed\bin\zed.exe
%LOCALAPPDATA%\Programs\Zed\zed.exe
```

When the app executable is selected, the package resolves it to the CLI executable when available.

## Usage

Use Unity normally:

- Double-click a C# script in the Project window
- Click a C# stack trace entry in the Console
- Use `Assets > Open C# Project`

The package launches Zed with the Unity project folder and the selected script path. If the workspace is already open, Zed should focus or reuse that workspace; otherwise, it opens the project folder.

## Preferences

The package adds a settings page at:

```text
Edit > Preferences > External Tools > Zed Editor
```

Available options:

- **Zed Executable Path**: manually set the Zed executable if auto-detection fails
- **Detect Again**: clear the cached executable path and run detection again
- **Additional Arguments**: prepend custom arguments to the Zed CLI invocation

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

Open `Edit > Preferences > External Tools > Zed Editor` and set the executable path manually.

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
    ZedPreferences.cs
    ZedProcess.cs
    ZedScriptEditor.cs
    ZedSolutionGeneratorBridge.cs
  package.json
  README.md
  CHANGELOG.md
```
