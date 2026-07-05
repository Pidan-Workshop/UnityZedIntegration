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

