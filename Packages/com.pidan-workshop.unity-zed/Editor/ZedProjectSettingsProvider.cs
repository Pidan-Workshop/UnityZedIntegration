using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal static class ZedProjectSettingsProvider
    {
        private const string SettingsPath = "Project/Zed Editor";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Zed Editor",
                guiHandler = OnGUI,
                keywords = new HashSet<string> { "zed", "editor", "shader", "ignore", "gitignore", "window", "workspace" }
            };
        }

        private static void OnGUI(string searchContext)
        {
            var settings = ZedProjectSettings.instance;

            DrawExecutablePathSection();
            DrawDetectionStatus();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Open In Zed", EditorStyles.boldLabel);
            var windowBehavior = (ZedWindowBehavior)EditorGUILayout.EnumPopup("Window Behavior", settings.WindowBehavior);
            EditorGUILayout.LabelField("Smart opens a project workspace, reuses it while alive, and resets after the Zed window closes.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("File extensions that Unity should route to Zed.", EditorStyles.miniLabel);
            var sourceExtensions = EditorGUILayout.TextArea(settings.SourceFileExtensionsText, GUILayout.MinHeight(86));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Extensions", GUILayout.Width(128)))
            {
                settings.ResetSourceFileExtensions();
                settings.SaveSettings();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("C# Project Analyzers", EditorStyles.boldLabel);
            var analyzerInjectionEnabled = EditorGUILayout.Toggle("Inject Analyzer Paths", settings.AnalyzerInjectionEnabled);
            var unitySourceGeneratorInjectionEnabled = EditorGUILayout.Toggle("Inject Unity Source Generators", settings.UnitySourceGeneratorInjectionEnabled);
            EditorGUILayout.LabelField("Analyzer DLL paths to add to generated csproj files. Use one absolute or project-relative path per line.", EditorStyles.miniLabel);
            var analyzerPaths = EditorGUILayout.TextArea(settings.AnalyzerPathsText, GUILayout.MinHeight(64));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Analyzer Paths", GUILayout.Width(152)))
            {
                settings.ResetAnalyzerPaths();
                settings.SaveSettings();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Zed Project Panel", EditorStyles.boldLabel);
            var hideGitignore = EditorGUILayout.Toggle("Hide .gitignore Matches", settings.ProjectPanelHidesGitignoredFiles);
            EditorGUILayout.LabelField("Gitignore entries used to hide generated Unity folders from Zed.", EditorStyles.miniLabel);
            var gitignoreEntries = EditorGUILayout.TextArea(settings.GitignoreEntriesText, GUILayout.MinHeight(92));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create .zed/settings.json", GUILayout.Width(176)))
            {
                var created = ZedWorkspaceSettings.WriteZedSettings(false);
                Debug.Log(created
                    ? $"[ZedEditor] Created {ZedWorkspaceSettings.ZedSettingsPath}."
                    : $"[ZedEditor] Skipped creating {ZedWorkspaceSettings.ZedSettingsPath} because it already exists.");
            }
            if (GUILayout.Button("Overwrite .zed/settings.json", GUILayout.Width(184)))
            {
                if (EditorUtility.DisplayDialog(
                        "Overwrite Zed Settings",
                        "This will replace .zed/settings.json with the settings managed by this Unity package.",
                        "Overwrite",
                        "Cancel"))
                {
                    ZedWorkspaceSettings.WriteZedSettings(true);
                    Debug.Log($"[ZedEditor] Wrote {ZedWorkspaceSettings.ZedSettingsPath}.");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add Ignore Entries To .gitignore", GUILayout.Width(216)))
            {
                var added = ZedWorkspaceSettings.EnsureGitignoreEntries(settings.GitignoreEntries);
                Debug.Log($"[ZedEditor] Added {added} .gitignore entries for Zed project panel filtering.");
            }

            if (ZedWorkspaceSettings.ZedSettingsExists())
                EditorGUILayout.LabelField($".zed/settings.json: {ZedWorkspaceSettings.ZedSettingsPath}", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField(".zed/settings.json has not been created for this project.", EditorStyles.miniLabel);

            if (EditorGUI.EndChangeCheck())
            {
                settings.SourceFileExtensionsText = sourceExtensions;
                settings.WindowBehavior = windowBehavior;
                settings.AnalyzerInjectionEnabled = analyzerInjectionEnabled;
                settings.UnitySourceGeneratorInjectionEnabled = unitySourceGeneratorInjectionEnabled;
                settings.AnalyzerPathsText = analyzerPaths;
                settings.ProjectPanelHidesGitignoredFiles = hideGitignore;
                settings.GitignoreEntriesText = gitignoreEntries;
                settings.SaveSettings();
            }
        }

        private static void DrawExecutablePathSection()
        {
            EditorGUILayout.LabelField("Zed Executable", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            var currentPath = ZedEditorPrefs.GetExecutablePath();
            var newPath = EditorGUILayout.TextField("Path", currentPath);
            if (newPath != currentPath)
            {
                ZedEditorPrefs.SetExecutablePath(newPath);
                ZedDiscovery.InvalidateCache();
            }

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
#if UNITY_EDITOR_WIN
                var extension = "exe";
#else
                var extension = "";
#endif
                var selected = EditorUtility.OpenFilePanel("Select Zed Executable", "", extension);
                if (!string.IsNullOrEmpty(selected))
                {
                    ZedEditorPrefs.SetExecutablePath(selected);
                    ZedDiscovery.InvalidateCache();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawDetectionStatus()
        {
            var discoveredPath = ZedDiscovery.FindZed();
            if (!string.IsNullOrEmpty(discoveredPath))
            {
                EditorGUILayout.LabelField($"Found at: {discoveredPath}", EditorStyles.miniLabel);
            }
            else
            {
                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.red }
                };
                EditorGUILayout.LabelField("Not found. Specify the executable path manually.", errorStyle);
            }

            if (GUILayout.Button("Detect Again", GUILayout.Width(100)))
            {
                ZedDiscovery.InvalidateCache();
                ZedDiscovery.FindZed();
            }
        }
    }
}
