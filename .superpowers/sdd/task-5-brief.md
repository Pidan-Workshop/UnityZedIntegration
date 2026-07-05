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

