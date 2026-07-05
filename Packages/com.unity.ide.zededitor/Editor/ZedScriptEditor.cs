using System;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using static Unity.CodeEditor.CodeEditor;

namespace Unity.Zed.Editor
{
    [InitializeOnLoad]
    public class ZedScriptEditor : IExternalCodeEditor
    {
        static ZedScriptEditor()
        {
            Register(new ZedScriptEditor());
        }

        Installation[] IExternalCodeEditor.Installations
        {
            get
            {
                var zedPath = ZedDiscovery.FindZed();
                return new[]
                {
                    new Installation
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
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly);
            if (package == null)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

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
            var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            Debug.Log(projectDirectory);
            if (!string.IsNullOrEmpty(path) && !Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(projectDirectory, path));
            }

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

        public bool TryGetInstallationForPath(string editorPath, out Installation installation)
        {
            var zedPath = ZedDiscovery.FindZed();

            if (string.IsNullOrEmpty(editorPath))
            {
                installation = default;
                return false;
            }

            var normalizedEditor = Path.GetFullPath(ZedDiscovery.ResolveCliPath(editorPath));
            var normalizedZed = !string.IsNullOrEmpty(zedPath) ? Path.GetFullPath(zedPath) : null;

            if (normalizedZed != null && string.Equals(normalizedEditor, normalizedZed, StringComparison.OrdinalIgnoreCase))
            {
                installation = new Installation
                {
                    Name = "Zed",
                    Path = zedPath
                };
                return true;
            }

            if (editorPath == "zed")
            {
                installation = new Installation
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
