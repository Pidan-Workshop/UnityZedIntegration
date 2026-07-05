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

