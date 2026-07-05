# Unity Zed Editor Integration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `com.unity.ide.zededitor` UPM package that registers Zed as a code editor in Unity, auto-detects installations on Windows/macOS/Linux, leverages VS Editor package for .sln/.csproj generation, and opens scripts at correct line/column on double-click.

**Architecture:** Single UPM package inside `Packages/com.unity.ide.zededitor/`. Implements `IExternalCodeEditor` from `Unity.CodeEditor`, bridges `Microsoft.Unity.VisualStudio.Editor.ProjectGeneration` for project file generation, and registers a `SettingsProvider` for Preferences UI.

**Tech Stack:** C#, Unity 2021.2+, `Unity.CodeEditor` API, `com.unity.ide.visualstudio` as hard dependency.

## Global Constraints

- Unity 2021.2+ (for stable `IExternalCodeEditor` registration)
- Hard dependency on `com.unity.ide.visualstudio` (>= 2.0.0)
- Target platforms: Windows, macOS, Linux
- All code lives under `Editor/` (editor-only assembly)
- Asmdef name: `Unity.Zed.Editor`
- Namespace: `Unity.Zed.Editor`

---

### Task 1: Scaffold Package Structure

**Files:**
- Create: `Packages/com.unity.ide.zededitor/package.json`
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedScriptEditor.asmdef`
- Create: `Packages/com.unity.ide.zededitor/Editor/`
- Create: `Packages/com.unity.ide.zededitor/README.md`
- Create: `Packages/com.unity.ide.zededitor/CHANGELOG.md`

**Interfaces:**
- Produces: package structure, `Unity.Zed.Editor` assembly available for later tasks

- [ ] **Step 1: Create directory structure**

```powershell
New-Item -ItemType Directory -Path "Packages/com.unity.ide.zededitor/Editor" -Force
```

- [ ] **Step 2: Write package.json**

```json
{
  "name": "com.unity.ide.zededitor",
  "displayName": "Zed Editor",
  "description": "Code editor integration for supporting Zed as code editor for Unity. Adds support for generating csproj/sln files, auto discovery of installations, and script opening with line/column navigation.",
  "version": "0.1.0",
  "unity": "2021.2",
  "dependencies": {
    "com.unity.ide.visualstudio": "2.0.0"
  },
  "keywords": ["zed", "editor", "code"],
  "category": "Editor"
}
```

- [ ] **Step 3: Write asmdef**

```json
{
    "name": "Unity.Zed.Editor",
    "references": [
        "Unity.VisualStudio.Editor"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Verify Unity detects the package**

Open Unity, check Console for any assembly compilation errors. There should be none since the Editor/ directory is empty.

- [ ] **Step 5: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/
git commit -m "feat: scaffold com.unity.ide.zededitor package structure"
```

---

### Task 2: ZedEditorPrefs — EditorPrefs Key Constants

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedEditorPrefs.cs`

**Interfaces:**
- Produces: `ZedEditorPrefs` static class with `GetExecutablePath()`, `SetExecutablePath(string)`, `GetAdditionalArgs()`, `SetAdditionalArgs(string)`

- [ ] **Step 1: Write ZedEditorPrefs.cs**

```csharp
using UnityEditor;

namespace Unity.Zed.Editor
{
    internal static class ZedEditorPrefs
    {
        private const string Prefix = "ZedEditor_";

        public const string ExecutablePathKey = Prefix + "ExecutablePath";
        public const string AdditionalArgsKey = Prefix + "AdditionalArgs";

        public static string GetExecutablePath()
        {
            return EditorPrefs.GetString(ExecutablePathKey, "");
        }

        public static void SetExecutablePath(string path)
        {
            EditorPrefs.SetString(ExecutablePathKey, path ?? "");
        }

        public static string GetAdditionalArgs()
        {
            return EditorPrefs.GetString(AdditionalArgsKey, "");
        }

        public static void SetAdditionalArgs(string args)
        {
            EditorPrefs.SetString(AdditionalArgsKey, args ?? "");
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Return to Unity, wait for compilation. No errors should appear.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedEditorPrefs.cs
git commit -m "feat: add ZedEditorPrefs for EditorPrefs persistence"
```

---

### Task 3: ZedDiscovery — Cross-Platform Installation Detection

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedDiscovery.cs`

**Interfaces:**
- Consumes: `ZedEditorPrefs.GetExecutablePath()`
- Produces: `static string FindZed()`, `static void InvalidateCache()`

- [ ] **Step 1: Write ZedDiscovery.cs**

```csharp
using System.Collections.Generic;
using System.IO;

namespace Unity.Zed.Editor
{
    internal static class ZedDiscovery
    {
        private static string s_CachedPath;
        private static bool s_CacheValid;

        public static string FindZed()
        {
            if (s_CacheValid)
                return s_CachedPath;

            var userPath = ZedEditorPrefs.GetExecutablePath();
            if (!string.IsNullOrEmpty(userPath) && File.Exists(userPath))
            {
                s_CachedPath = userPath;
                s_CacheValid = true;
                return s_CachedPath;
            }

            foreach (var path in GetPlatformSearchPaths())
            {
                if (File.Exists(path))
                {
                    s_CachedPath = path;
                    s_CacheValid = true;
                    return s_CachedPath;
                }
            }

            var fromPath = FindInPath("zed");
            if (fromPath != null)
            {
                s_CachedPath = fromPath;
                s_CacheValid = true;
                return s_CachedPath;
            }

            s_CacheValid = true;
            return null;
        }

        public static void InvalidateCache()
        {
            s_CacheValid = false;
            s_CachedPath = null;
        }

        private static IEnumerable<string> GetPlatformSearchPaths()
        {
#if UNITY_EDITOR_WIN
            var localAppData = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
                yield return Path.Combine(localAppData, "Programs", "Zed", "zed.exe");
            yield return @"C:\Program Files\Zed\zed.exe";
#elif UNITY_EDITOR_OSX
            yield return "/Applications/Zed.app/Contents/MacOS/zed";
            var home = System.Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                yield return Path.Combine(home, "Applications", "Zed.app", "Contents", "MacOS", "zed");
#elif UNITY_EDITOR_LINUX
            var home = System.Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, ".local", "bin", "zed");
                yield return Path.Combine(home, ".local", "share", "flatpak", "exports", "bin", "dev.zed.Zed");
            }
            yield return "/usr/local/bin/zed";
            yield return "/usr/bin/zed";
            yield return "/var/lib/flatpak/exports/bin/dev.zed.Zed";
#endif
        }

        private static string FindInPath(string executable)
        {
#if UNITY_EDITOR_WIN
            executable += ".exe";
#endif
            var pathEnv = System.Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return null;

            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(dir, executable);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Return to Unity. No errors expected.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedDiscovery.cs
git commit -m "feat: add ZedDiscovery for cross-platform zed detection"
```

---

### Task 4: ZedSolutionGeneratorBridge — VS Editor Project Generation Bridge

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedSolutionGeneratorBridge.cs`

**Interfaces:**
- Consumes: `Microsoft.Unity.VisualStudio.Editor.ProjectGeneration`
- Produces: `static void SyncAll()`, `static void SyncIfNeeded(...)`, `static string GetOrGenerateSolutionFile()`

- [ ] **Step 1: Write ZedSolutionGeneratorBridge.cs**

```csharp
using System.IO;
using System.Linq;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal static class ZedSolutionGeneratorBridge
    {
        private static ProjectGeneration CreateGenerator()
        {
            return new ProjectGeneration(Directory.GetParent(Application.dataPath).FullName);
        }

        public static void SyncAll()
        {
            CreateGenerator().Sync();
        }

        public static void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            var affected = addedFiles
                .Union(deletedFiles)
                .Union(movedFiles)
                .Union(movedFromFiles);
            CreateGenerator().SyncIfNeeded(affected, importedFiles);
        }

        public static string GetOrGenerateSolutionFile()
        {
            var generator = CreateGenerator();
            generator.Sync();
            return generator.SolutionFile();
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Switch to Unity. The `Unity.VisualStudio.Editor` asmdef reference from Task 1 makes `ProjectGeneration` accessible. No errors expected.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedSolutionGeneratorBridge.cs
git commit -m "feat: add ZedSolutionGeneratorBridge for .sln/.csproj generation"
```

---

### Task 5: ZedProcess — Process Launch Wrapper

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedProcess.cs`

**Interfaces:**
- Consumes: `ZedDiscovery.FindZed()`, `ZedEditorPrefs.GetAdditionalArgs()`
- Produces: `static bool Launch(string projectDirectory, string filePath, int line, int column)`

- [ ] **Step 1: Write ZedProcess.cs**

```csharp
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal static class ZedProcess
    {
        public static bool Launch(string projectDirectory, string filePath, int line, int column)
        {
            var zedPath = ZedDiscovery.FindZed();
            if (string.IsNullOrEmpty(zedPath) || !File.Exists(zedPath))
            {
                Debug.LogError("Zed executable not found. Please configure the path in Edit > Preferences > External Tools > Zed Editor.");
                EditorUtility.DisplayDialog(
                    "Zed Not Found",
                    "Zed executable was not found. Please go to Edit > Preferences > External Tools > Zed Editor and specify the Zed executable path.",
                    "OK");
                return false;
            }

            var args = BuildArguments(projectDirectory, filePath, line, column);
            var additionalArgs = ZedEditorPrefs.GetAdditionalArgs();
            if (!string.IsNullOrEmpty(additionalArgs))
                args = additionalArgs + " " + args;

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = zedPath,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to launch Zed: {ex.Message}");
                EditorUtility.DisplayDialog(
                    "Failed to Launch Zed",
                    $"Failed to start Zed at path '{zedPath}'. Please check the path in Edit > Preferences > External Tools > Zed Editor.\n\nError: {ex.Message}",
                    "OK");
                return false;
            }
        }

        private static string BuildArguments(string projectDirectory, string filePath, int line, int column)
        {
            var args = $"\"{projectDirectory}\"";

            if (!string.IsNullOrEmpty(filePath))
            {
                args += $" \"{filePath}:{line}:{column}\"";
            }

            return args;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Return to Unity. No errors expected.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedProcess.cs
git commit -m "feat: add ZedProcess for launching zed executable"
```

---

### Task 6: ZedScriptEditor — IExternalCodeEditor Implementation

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedScriptEditor.cs`

**Interfaces:**
- Consumes: `ZedDiscovery.FindZed()`, `ZedProcess.Launch()`, `ZedSolutionGeneratorBridge.*`
- Produces: `[InitializeOnLoad] public class ZedScriptEditor : IExternalCodeEditor`
- Registers with `CodeEditor.Register()`; Unity picks it up in External Tools dropdown

- [ ] **Step 1: Write ZedScriptEditor.cs**

```csharp
using System;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    [InitializeOnLoad]
    public class ZedScriptEditor : IExternalCodeEditor
    {
        static ZedScriptEditor()
        {
            CodeEditor.Register(new ZedScriptEditor());
        }

        CodeEditor.Installation[] IExternalCodeEditor.Installations
        {
            get
            {
                var zedPath = ZedDiscovery.FindZed();
                return new[]
                {
                    new CodeEditor.Installation
                    {
                        Name = "Zed",
                        Path = zedPath ?? "zed"
                    }
                };
            }
        }

        public void Initialize(string editorInstallationPath)
        {
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly);
            if (package == null)
                return;

            var style = new GUIStyle
            {
                richText = true,
                margin = new RectOffset(0, 4, 0, 0)
            };

            GUILayout.Label($"<size=10><color=grey>{package.displayName} v{package.version} enabled</color></size>", style);
            GUILayout.EndHorizontal();
        }

        public bool OpenProject(string path, int line, int column)
        {
            var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            ZedSolutionGeneratorBridge.GetOrGenerateSolutionFile();
            return ZedProcess.Launch(projectDirectory, path, line, column);
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            ZedSolutionGeneratorBridge.SyncIfNeeded(addedFiles, deletedFiles, movedFiles, movedFromFiles, importedFiles);
        }

        public void SyncAll()
        {
            ZedSolutionGeneratorBridge.SyncAll();
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            var zedPath = ZedDiscovery.FindZed();

            if (string.IsNullOrEmpty(editorPath))
            {
                installation = default;
                return false;
            }

            var normalizedEditor = Path.GetFullPath(editorPath);
            var normalizedZed = !string.IsNullOrEmpty(zedPath) ? Path.GetFullPath(zedPath) : null;

            if (normalizedZed != null && string.Equals(normalizedEditor, normalizedZed, StringComparison.OrdinalIgnoreCase))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Zed",
                    Path = zedPath
                };
                return true;
            }

            if (editorPath == "zed")
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Zed",
                    Path = editorPath
                };
                return true;
            }

            installation = default;
            return false;
        }
    }
}
```

- [ ] **Step 2: Verify compilation and registration**

Return to Unity. Wait for compilation. Open `Edit > Preferences > External Tools`. The "External Script Editor" dropdown should show "Zed" as an option. Select it.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedScriptEditor.cs
git commit -m "feat: add ZedScriptEditor implementing IExternalCodeEditor"
```

---

### Task 7: ZedPreferences — SettingsProvider UI

**Files:**
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedPreferences.cs`

**Interfaces:**
- Consumes: `ZedDiscovery.FindZed()`, `ZedDiscovery.InvalidateCache()`, `ZedEditorPrefs.*`
- Produces: `SettingsProvider` at `Preferences/External Tools/Zed Editor`

- [ ] **Step 1: Write ZedPreferences.cs**

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal class ZedPreferences
    {
        private const string SettingsPath = "Preferences/External Tools/Zed Editor";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.User)
            {
                label = "Zed Editor",
                guiHandler = OnGUI,
                keywords = new HashSet<string> { "zed", "editor", "code" }
            };
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();

            DrawExecutablePathSection();
            DrawDetectionStatus();
            DrawAdditionalArgsSection();

            EditorGUILayout.Space();
        }

        private static void DrawExecutablePathSection()
        {
            EditorGUILayout.LabelField("Zed Executable Path", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            var currentPath = ZedEditorPrefs.GetExecutablePath();
            var newPath = EditorGUILayout.TextField("Path", currentPath);
            if (newPath != currentPath)
            {
                ZedEditorPrefs.SetExecutablePath(newPath);
                ZedDiscovery.InvalidateCache();
            }

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
#if UNITY_EDITOR_WIN
                var extension = "exe";
#else
                var extension = "";
#endif
                var selected = EditorUtility.OpenFilePanel("Select Zed Executable", "", extension);
                if (!string.IsNullOrEmpty(selected))
                {
                    ZedEditorPrefs.SetExecutablePath(selected);
                    ZedDiscovery.InvalidateCache();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawDetectionStatus()
        {
            var discoveredPath = ZedDiscovery.FindZed();
            if (!string.IsNullOrEmpty(discoveredPath))
            {
                EditorGUILayout.LabelField($"Found at: {discoveredPath}", EditorStyles.miniLabel);
            }
            else
            {
                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.red }
                };
                EditorGUILayout.LabelField("Not found. Please specify path manually.", errorStyle);
            }

            if (GUILayout.Button("Detect Again", GUILayout.Width(100)))
            {
                ZedDiscovery.InvalidateCache();
                ZedDiscovery.FindZed();
            }
        }

        private static void DrawAdditionalArgsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Arguments", EditorStyles.boldLabel);

            var currentArgs = ZedEditorPrefs.GetAdditionalArgs();
            var newArgs = EditorGUILayout.TextField("Args", currentArgs);
            if (newArgs != currentArgs)
            {
                ZedEditorPrefs.SetAdditionalArgs(newArgs);
            }
        }
    }
}
```

- [ ] **Step 2: Verify Preferences UI**

Return to Unity. Open `Edit > Preferences > External Tools > Zed Editor`. Verify the UI renders:
- Path text field with Browse button
- Detection status label
- "Detect Again" button
- Additional Args text field

If Zed is not installed, the status should show red "Not found" text. If installed, it should show the detected path.

- [ ] **Step 3: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/Editor/ZedPreferences.cs
git commit -m "feat: add ZedPreferences SettingsProvider for Zed configuration"
```

---

### Task 8: End-to-End Verification

**Files:**
- Create: `Assets/Scripts/TestScript.cs` (temporary, for testing)

**Interfaces:**
- Consumes: All previous tasks
- Produces: Verified working integration

- [ ] **Step 1: Create a test C# script**

Write `Assets/Scripts/TestScript.cs`:

```csharp
using UnityEngine;

public class TestScript : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hello from Zed!");
    }
}
```

If `Assets/Scripts/` doesn't exist, create it in Unity first (right-click in Project window > Create > Folder > Scripts).

- [ ] **Step 2: Verify External Tools dropdown**

Open `Edit > Preferences > External Tools`. Open the "External Script Editor" dropdown. Confirm "Zed" appears in the list. Select "Zed".

- [ ] **Step 3: Verify .sln generation**

With Zed selected as the external editor, go to `Assets > Open C# Project`. This should call `OpenProject("", 0, 0)`, which triggers solution generation. Verify that a `.sln` file appears in the project root (e.g., `UnityZed.sln`).

- [ ] **Step 4: Verify script open with double-click**

Double-click `TestScript.cs` in the Unity Project window. Verify:
- If Zed is installed: Zed opens with the project folder and the file at the correct position
- If Zed is NOT installed: An error dialog appears guiding the user to Preferences

- [ ] **Step 5: Verify project auto-open**

If Zed is installed but not running: double-clicking should launch Zed and open the project folder.
If Zed is already running with this project: the existing window should focus and open the file.

- [ ] **Step 6: Clean up test script**

Delete `Assets/Scripts/TestScript.cs` and `Assets/Scripts/TestScript.cs.meta`.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "chore: verification complete, remove test script"
```
