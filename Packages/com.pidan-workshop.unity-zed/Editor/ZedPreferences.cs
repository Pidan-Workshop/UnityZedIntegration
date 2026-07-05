using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal class ZedPreferences
    {
        private const string SettingsPath = "Preferences/External Tools/Zed Editor";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.User)
            {
                label = "Zed Editor",
                guiHandler = OnGUI,
                keywords = new HashSet<string> { "zed", "editor", "code" }
            };
        }

        private static void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();

            DrawExecutablePathSection();
            DrawDetectionStatus();
            DrawAdditionalArgsSection();

            EditorGUILayout.Space();
        }

        private static void DrawExecutablePathSection()
        {
            EditorGUILayout.LabelField("Zed Executable Path", EditorStyles.boldLabel);

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
                EditorGUILayout.LabelField("Not found. Please specify path manually.", errorStyle);
            }

            if (GUILayout.Button("Detect Again", GUILayout.Width(100)))
            {
                ZedDiscovery.InvalidateCache();
                ZedDiscovery.FindZed();
            }
        }

        private static void DrawAdditionalArgsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Arguments", EditorStyles.boldLabel);

            var currentArgs = ZedEditorPrefs.GetAdditionalArgs();
            var newArgs = EditorGUILayout.TextField("Args", currentArgs);
            if (newArgs != currentArgs)
            {
                ZedEditorPrefs.SetAdditionalArgs(newArgs);
            }
        }
    }
}
