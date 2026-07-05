using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal static class ZedWorkspaceSettings
    {
        public static string ProjectDirectory
        {
            get { return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath; }
        }

        public static string ZedSettingsPath
        {
            get { return Path.Combine(ProjectDirectory, ".zed", "settings.json"); }
        }

        public static string GitignorePath
        {
            get { return Path.Combine(ProjectDirectory, ".gitignore"); }
        }

        public static bool ZedSettingsExists()
        {
            return File.Exists(ZedSettingsPath);
        }

        public static bool WriteZedSettings(bool overwrite)
        {
            if (ZedSettingsExists() && !overwrite)
                return false;

            var settings = ZedProjectSettings.instance;
            var zedDirectory = Path.GetDirectoryName(ZedSettingsPath);
            if (!string.IsNullOrEmpty(zedDirectory))
                Directory.CreateDirectory(zedDirectory);

            File.WriteAllText(ZedSettingsPath, BuildZedSettingsJson(settings), Encoding.UTF8);
            return true;
        }

        public static int EnsureGitignoreEntries(IEnumerable<string> entries)
        {
            var existingLines = File.Exists(GitignorePath)
                ? File.ReadAllLines(GitignorePath).ToList()
                : new List<string>();

            var existing = new HashSet<string>(existingLines.Select(l => l.Trim()), System.StringComparer.OrdinalIgnoreCase);
            var missing = entries
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .Where(e => !existing.Contains(e))
                .ToList();

            if (missing.Count == 0)
                return 0;

            if (existingLines.Count > 0 && !string.IsNullOrWhiteSpace(existingLines[existingLines.Count - 1]))
                existingLines.Add(string.Empty);

            existingLines.Add("# Unity folders hidden from Zed project panel");
            existingLines.AddRange(missing);
            File.WriteAllLines(GitignorePath, existingLines, Encoding.UTF8);
            return missing.Count;
        }

        private static string BuildZedSettingsJson(ZedProjectSettings settings)
        {
            var hideGitignore = settings.ProjectPanelHidesGitignoredFiles ? "true" : "false";
            return "{\n" +
                   "  \"project_panel\": {\n" +
                   "    \"hide_gitignore\": " + hideGitignore + "\n" +
                   "  }\n" +
                   "}\n";
        }
    }
}
