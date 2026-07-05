# Unity Zed Editor Integration — Design Spec

**Date:** 2026-07-05
**Project:** UnityZed (C:\Users\cloud\UnityProjects\UnityZed)
**Unity Version:** 2022.3.62f3c1

---

## 1. Overview

Create a Unity Package Manager (UPM) package `com.unity.ide.zededitor` that integrates the [Zed code editor](https://zed.dev) as a first-class external code editor inside Unity, at parity with the existing Rider and Visual Studio Editor packages.

### Goals

- Auto-detect Zed installation on Windows, macOS, and Linux
- Register Zed in Unity's **Edit > Preferences > External Tools** dropdown
- Leverage `com.unity.ide.visualstudio` (hard dependency) to generate `.sln` and `.csproj` project files
- Double-clicking a C# script in Unity opens the file in Zed at the correct line/column
- If the project is not already open in Zed, Zed opens the project folder automatically

### Non-Goals (for this version)

- Debugger integration
- C# LSP / language server management (Zed handles this via its own extensions)
- Support for Unity versions before 2021.2

---

## 2. Package Structure

```
Packages/com.unity.ide.zededitor/
├── package.json
├── README.md
├── LICENSE.md
├── CHANGELOG.md
└── Editor/
    ├── ZedScriptEditor.asmdef
    ├── ZedScriptEditor.cs              # [InitializeOnLoad] entry + IExternalCodeEditor impl
    ├── ZedDiscovery.cs                 # Cross-platform Zed installation detection
    ├── ZedPreferences.cs               # SettingsProvider for Preferences > External Tools
    ├── ZedProcess.cs                   # Process.Start wrapper for Zed CLI
    ├── ZedSolutionGeneratorBridge.cs   # Wrapper that calls VS Editor package's ProjectGeneration
    └── EditorPrefs/
        └── ZedEditorPrefs.cs           # EditorPrefs key constants and helpers
```

---

## 3. Component Design

### 3.1 ZedScriptEditor.cs — `IExternalCodeEditor` Entry Point

Implements `Unity.CodeEditor.IExternalCodeEditor`. Decorated with `[InitializeOnLoad]`, registers itself on startup via `CodeEditor.Register()`.

**Interface methods:**

| Method | Behavior |
|--------|----------|
| `Installations` (property) | Returns `CodeEditor.Installation[]` with a single entry pointing to the discovered Zed path |
| `Initialize(string path)` | No-op (Zed has no per-project initialization ceremony) |
| `OnGUI()` | Renders version info label in External Tools preferences |
| `OpenProject(string filePath, int line, int column)` | Core method: generates solution files, discovers Zed, launches `zed <project-dir> <file>:<line>:<column>` |
| `SyncIfNeeded(...)` | Delegates to VS Editor's `ProjectGeneration.SyncIfNeeded()` |
| `SyncAll()` | Delegates to VS Editor's `ProjectGeneration.Sync()` |
| `TryGetInstallationForPath(string path, out Installation)` | Validates a Zed path and returns an `Installation` struct |

### 3.2 ZedDiscovery.cs — Detection Logic

Searches well-known installation paths per platform:

| Platform | Search Paths (in order) |
|----------|------------------------|
| Windows | `%LOCALAPPDATA%\Programs\Zed\zed.exe`, `C:\Program Files\Zed\zed.exe`, registry `HKLM\Software\Zed`, `PATH` |
| macOS | `/Applications/Zed.app/Contents/MacOS/zed`, `~/Applications/Zed.app/Contents/MacOS/zed`, `PATH` |
| Linux | `~/.local/bin/zed`, `/usr/local/bin/zed`, `/usr/bin/zed`, Flatpak path, `XDG_DATA_DIRS` .desktop entries, `PATH` |

Returns `null` if not found. Provides a public `static string FindZed()` method that caches the result until `InvalidateCache()` is called.

### 3.3 ZedSolutionGeneratorBridge.cs — Project File Generation

Wraps the VS Editor package's `ProjectGeneration` class from `Microsoft.Unity.VisualStudio.Editor` namespace.

- On `SyncAll()`: calls `new ProjectGeneration().Sync()` which generates `.sln` and `.csproj` files in the project root
- On `SyncIfNeeded()`: calls `new ProjectGeneration().SyncIfNeeded()`
- On `OpenProject()`: calls `GetOrGenerateSolutionFile()` (syncs, then returns `.SolutionFile()` path)

The generated solution file path is passed to Zed as the project root.

### 3.4 ZedProcess.cs — Process Launch

Launches `zed` via `System.Diagnostics.Process`:

```
zed <project-directory> <file>:<line>:<column>
```

- Uses `CreateNoWindow = true`, `UseShellExecute = false`
- Does NOT wait for exit (fire-and-forget)
- On failure, logs error and shows an EditorUtility.DisplayDialog suggesting the user check the path in Preferences

Cross-platform path handling:
- Windows: file paths with backslashes are normalized for the Zed CLI
- macOS: must launch via absolute path to the `zed` binary inside the `.app` bundle
- Linux: direct invocation of `zed` binary

### 3.5 ZedPreferences.cs — Settings UI

Registers a `SettingsProvider` at path `Preferences/External Tools` with label "Zed Editor". Displays:

1. **Zed Executable Path** — text field + "Browse" button (uses `EditorUtility.OpenFilePanel`)
2. **Auto-detection Status** — read-only label showing `✔ Found at: <path>` (green) or `✘ Not found. Please specify path manually.` (red)
3. **Additional Arguments** — optional text field for power users
4. **Detect Again** — button that calls `ZedDiscovery.InvalidateCache()` + `ZedDiscovery.FindZed()`

Saved via `EditorPrefs` with scoped keys (see ZedEditorPrefs.cs). Path changes auto-trigger an `Installations` property refresh.

---

## 4. Data Flow

### 4.1 Startup

```
Unity Editor loads
  → [InitializeOnLoad] fires on ZedScriptEditor
    → ZedDiscovery.FindZed() searches for zed binary
    → CodeEditor.Register(new ZedScriptEditor())
    → Zed shows in Preferences > External Tools dropdown
```

### 4.2 Script Open (double-click)

```
User double-clicks C# asset in Project window
  → Unity.CodeEditor calls IExternalCodeEditor.OpenProject(filePath, line, column)
    → ZedScriptEditor.OpenProject()
      → ZedSolutionGeneratorBridge.GetOrGenerateSolutionFile()
        → VS Editor's ProjectGeneration.Sync()    [generates .sln/.csproj]
      → ZedDiscovery.FindZed() checks cached path
      → ZedProcess.Launch(projectDir, filePath, line, column)
        → Process.Start("zed", "<project> <file>:<line>:<column>")
```

### 4.3 Asset Changes

```
User adds/removes C# files
  → Unity calls IExternalCodeEditor.SyncIfNeeded(added, deleted, moved, movedFrom, imported)
    → ZedScriptEditor.SyncIfNeeded() delegates to VS Editor's ProjectGeneration
```

---

## 5. Error Handling

| Condition | Handling |
|-----------|----------|
| Zed not installed / not found | Exposed as "Not found" in Preferences UI; `OpenProject()` shows dialog prompting manual path configuration |
| VS Editor package missing | Impossible due to hard dependency; `PackageInfo.FindForAssembly` check as defense |
| `ProjectGeneration` fails | Log warning, continue to launch Zed (Zed still opens with basic file support) |
| Zed process fails to start | `Debug.LogError` + `EditorUtility.DisplayDialog` with error message and suggested fix |
| Invalid Zed path in Preferences | `ZedDiscovery.FindZed()` returns null on next check, Preferences reflects this |
| Platform with no known paths | Fall back to `PATH` environment variable lookup |

---

## 6. Dependencies

- `com.unity.ide.visualstudio` (hard dependency, declared in `package.json`) — for `.sln`/`.csproj` generation
- Unity 2021.2+ — for `IExternalCodeEditor` registration API stability (the interface existed earlier but was finalized in this range)
- No external NuGet packages

---

## 7. Testing Strategy

- **Unit tests** (Editor tests): `ZedDiscovery` path detection with mock filesystem, `ZedProcess` argument building, `ZedEditorPrefs` serialization
- **Integration tests**: Verify `OpenProject` triggers project generation and creates a process start (mocked Process)
- **Manual tests**: Real Unity project — verify detection on each OS, verify double-click opens Zed, verify Preferences UI renders correctly

---

## 8. Asmdef Configuration

`ZedScriptEditor.asmdef`:
- `name`: `Unity.Zed.Editor`
- `includePlatforms`: `["Editor"]`
- `references`: `["Unity.VisualStudio.Editor"]` (to access ProjectGeneration)
- `autoReferenced`: `true`
- `noEngineReferences`: `false`

---

## 9. Package Registry & Distribution

- Hosted as GitHub repository
- UPM install via `https://github.com/<github-user>/com.unity.ide.zededitor.git` with appropriate branch/tag (GitHub username to be filled at repo creation time)
- `package.json` declares `"type": "tool"` per UPM convention for editor integration packages
