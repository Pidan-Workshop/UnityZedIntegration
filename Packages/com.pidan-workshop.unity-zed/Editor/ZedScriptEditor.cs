using System;
using System.IO;
using Microsoft.Unity.VisualStudio.Editor;
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

            DrawProjectGenerationSettings();
        }

        public bool OpenProject(string path, int line, int column)
        {
            var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            if (!string.IsNullOrEmpty(path) && !Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(projectDirectory, path));
            }

            if (!ShouldOpenPath(path))
                return false;

            ZedSolutionGeneratorBridge.GetOrGenerateSolutionFile();
            return ZedProcess.Launch(projectDirectory, path, line, column);
        }

        private static bool ShouldOpenPath(string path)
        {
            return ZedProjectSettings.instance.ShouldOpenPath(path);
        }

        private static void DrawProjectGenerationSettings()
        {
            var generator = ZedSolutionGeneratorBridge.CreateGenerator();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generate .csproj files for:");
            EditorGUI.indentLevel++;
            SettingsButton(generator, ProjectGenerationFlag.Embedded, "Embedded packages", "");
            SettingsButton(generator, ProjectGenerationFlag.Local, "Local packages", "");
            SettingsButton(generator, ProjectGenerationFlag.Registry, "Registry packages", "");
            SettingsButton(generator, ProjectGenerationFlag.Git, "Git packages", "");
            SettingsButton(generator, ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
            SettingsButton(generator, ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
            SettingsButton(generator, ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
            SettingsButton(generator, ProjectGenerationFlag.PlayerAssemblies, "Player projects", "For each player project generate an additional csproj with the name 'project-player.csproj'");
            DrawRegenerateProjectFilesButton(generator);
            EditorGUI.indentLevel--;
        }

        private static void SettingsButton(IGenerator generator, ProjectGenerationFlag preference, string guiMessage, string toolTip)
        {
            var prevValue = generator.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);
            var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
            if (newValue != prevValue)
                generator.AssemblyNameProvider.ToggleProjectGeneration(preference);
        }

        private static void DrawRegenerateProjectFilesButton(IGenerator generator)
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
                generator.Sync();
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
