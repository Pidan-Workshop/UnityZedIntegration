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

