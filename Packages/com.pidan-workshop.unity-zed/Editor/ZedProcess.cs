using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

            try
            {
                var additionalArgs = ZedEditorPrefs.GetAdditionalArgs();
                var argsBuilder = new StringBuilder();

                AppendRawArgument(argsBuilder, additionalArgs);
                AppendArgument(argsBuilder, projectDirectory);
                AppendArgument(argsBuilder, BuildFileArgument(projectDirectory, filePath, line, column));

                StartZed(zedPath, projectDirectory, argsBuilder.ToString());
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

        private static string BuildFileArgument(string projectDirectory, string filePath, int line, int column)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var relativePath = GetRelativePath(projectDirectory, filePath);
            var fileArg = relativePath.Replace('\\', '/');
            if (line > 0)
            {
                fileArg += $":{line}";
                if (column > 0)
                    fileArg += $":{column}";
            }

            return fileArg;
        }

        private static void AppendRawArgument(StringBuilder builder, string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
                return;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(argument);
        }

        private static void AppendArgument(StringBuilder builder, string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(QuoteArgument(argument));
        }

        private static string QuoteArgument(string argument)
        {
            var quoted = new StringBuilder();
            quoted.Append('"');

            var backslashes = 0;
            foreach (var c in argument)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (c == '"')
                {
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                    backslashes = 0;
                    continue;
                }

                if (backslashes > 0)
                {
                    quoted.Append('\\', backslashes);
                    backslashes = 0;
                }

                quoted.Append(c);
            }

            if (backslashes > 0)
                quoted.Append('\\', backslashes * 2);

            quoted.Append('"');
            return quoted.ToString();
        }

        private static void StartZed(string zedPath, string workingDir, string args)
        {
            Debug.Log($"[ZedEditor] {zedPath} {args}");
            var psi = new ProcessStartInfo
            {
                FileName = zedPath,
                Arguments = args,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var process = new Process { StartInfo = psi };
            process.Start();
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
                return fullPath;

            basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fullPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(basePath + Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase) &&
                !fullPath.StartsWith(basePath + Path.AltDirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }

            return fullPath[(basePath.Length + 1)..];
        }
    }
}
