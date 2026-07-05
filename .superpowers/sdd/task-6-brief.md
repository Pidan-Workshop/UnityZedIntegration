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

