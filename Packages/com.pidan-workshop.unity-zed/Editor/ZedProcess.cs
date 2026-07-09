using System;
using System.Collections.Generic;
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
        private static readonly StringComparer ProjectPathComparer =
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            StringComparer.OrdinalIgnoreCase;
#else
            StringComparer.Ordinal;
#endif

        private static readonly Dictionary<string, Process> s_SmartWorkspaceWatchers = new Dictionary<string, Process>(ProjectPathComparer);
        private static readonly object s_SmartWorkspaceLock = new object();

        public static bool Launch(string projectDirectory, string filePath, int line, int column)
        {
            var zedPath = ZedDiscovery.FindZed();
            if (string.IsNullOrEmpty(zedPath) || !File.Exists(zedPath))
            {
                Debug.LogError("Zed executable not found. Please configure the path in Edit > Project Settings > Zed Editor.");
                EditorUtility.DisplayDialog(
                    "Zed Not Found",
                    "Zed executable was not found. Please go to Edit > Project Settings > Zed Editor and specify the Zed executable path.",
                    "OK");
                return false;
            }

            try
            {
                var windowBehavior = ZedProjectSettings.instance.WindowBehavior;
                var normalizedProjectDirectory = NormalizeProjectDirectory(projectDirectory);
                var smartProjectKnownOpen = IsSmartProjectKnownOpen(normalizedProjectDirectory);
                var args = BuildArguments(projectDirectory, filePath, line, column, windowBehavior, smartProjectKnownOpen);

                var process = StartZed(zedPath, projectDirectory, args);
                if (windowBehavior == ZedWindowBehavior.Smart && !smartProjectKnownOpen)
                    TrackSmartWorkspaceWatcher(normalizedProjectDirectory, process);
                else
                    process.Dispose();

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to launch Zed: {ex.Message}");
                EditorUtility.DisplayDialog(
                    "Failed to Launch Zed",
                    $"Failed to start Zed at path '{zedPath}'. Please check the path in Edit > Project Settings > Zed Editor.\n\nError: {ex.Message}",
                    "OK");
                return false;
            }
        }

        internal static string BuildArguments(
            string projectDirectory,
            string filePath,
            int line,
            int column,
            ZedWindowBehavior windowBehavior,
            bool smartProjectKnownOpen)
        {
            var argsBuilder = new StringBuilder();

            AppendWindowBehaviorArgument(argsBuilder, windowBehavior, smartProjectKnownOpen);
            AppendArgument(argsBuilder, projectDirectory);
            AppendArgument(argsBuilder, BuildFileArgument(projectDirectory, filePath, line, column));

            return argsBuilder.ToString();
        }

        private static void AppendWindowBehaviorArgument(StringBuilder builder, ZedWindowBehavior windowBehavior, bool smartProjectKnownOpen)
        {
            switch (windowBehavior)
            {
                case ZedWindowBehavior.Smart:
                    if (!smartProjectKnownOpen)
                    {
                        AppendArgument(builder, "--new");
                        AppendArgument(builder, "--wait");
                    }
                    break;

                case ZedWindowBehavior.ReuseCurrentWindow:
                    AppendArgument(builder, "--existing");
                    break;

                case ZedWindowBehavior.AlwaysNewWorkspace:
                    AppendArgument(builder, "--new");
                    break;
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

        private static Process StartZed(string zedPath, string workingDir, string args)
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
            var process = new Process { StartInfo = psi };
            process.Start();
            return process;
        }

        private static string NormalizeProjectDirectory(string projectDirectory)
        {
            if (string.IsNullOrEmpty(projectDirectory))
                return null;

            return Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsSmartProjectKnownOpen(string normalizedProjectDirectory)
        {
            if (string.IsNullOrEmpty(normalizedProjectDirectory))
                return false;

            lock (s_SmartWorkspaceLock)
            {
                if (!s_SmartWorkspaceWatchers.TryGetValue(normalizedProjectDirectory, out var watcher))
                    return false;

                if (!watcher.HasExited)
                    return true;

                RemoveSmartWorkspaceWatcher(normalizedProjectDirectory, watcher);
                return false;
            }
        }

        private static void TrackSmartWorkspaceWatcher(string normalizedProjectDirectory, Process process)
        {
            if (string.IsNullOrEmpty(normalizedProjectDirectory))
            {
                process.Dispose();
                return;
            }

            lock (s_SmartWorkspaceLock)
            {
                if (s_SmartWorkspaceWatchers.TryGetValue(normalizedProjectDirectory, out var existingWatcher))
                    RemoveSmartWorkspaceWatcher(normalizedProjectDirectory, existingWatcher);

                process.EnableRaisingEvents = true;
                process.Exited += (_, _) =>
                {
                    lock (s_SmartWorkspaceLock)
                    {
                        RemoveSmartWorkspaceWatcher(normalizedProjectDirectory, process);
                    }
                };
                s_SmartWorkspaceWatchers[normalizedProjectDirectory] = process;
                if (process.HasExited)
                    RemoveSmartWorkspaceWatcher(normalizedProjectDirectory, process);
            }
        }

        private static void RemoveSmartWorkspaceWatcher(string normalizedProjectDirectory, Process watcher)
        {
            if (s_SmartWorkspaceWatchers.TryGetValue(normalizedProjectDirectory, out var currentWatcher) &&
                ReferenceEquals(currentWatcher, watcher))
            {
                s_SmartWorkspaceWatchers.Remove(normalizedProjectDirectory);
            }

            watcher.Dispose();
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
