using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    /// <summary>
    /// Adds project-configured Roslyn analyzer entries to generated C# project files for non-Unity IDE language servers.
    /// </summary>
    internal sealed class ZedAnalyzerProjectPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Unity editor source generators are optional because public package defaults must not inject analyzer dependencies.
        /// </summary>
        private static readonly string[] UnitySourceGeneratorRelativePaths =
        {
            "Data/Tools/Unity.SourceGenerators/Unity.SourceGenerators.dll",
            "Data/Tools/Unity.SourceGenerators/Unity.Properties.SourceGenerator.dll",
        };

        /// <summary>
        /// Unity invokes this hook after producing each C# project file, before writing the file to disk.
        /// </summary>
        public static string OnGeneratedCSProject(string path, string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var settings = ZedProjectSettings.instance;
            if (!settings.AnalyzerInjectionEnabled && !settings.UnitySourceGeneratorInjectionEnabled)
                return content;

            var analyzers = ResolveAnalyzerPaths(settings);
            if (analyzers.Count == 0)
                return content;

            string newline = content.Contains("\r\n") ? "\r\n" : "\n";
            string analyzerBlock = BuildAnalyzerBlock(content, analyzers, newline);
            if (string.IsNullOrEmpty(analyzerBlock))
                return content;

            return InsertAnalyzerBlock(content, analyzerBlock, newline);
        }

        private static List<string> ResolveAnalyzerPaths(ZedProjectSettings settings)
        {
            var analyzers = new List<string>();
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();

            if (settings.AnalyzerInjectionEnabled)
            {
                foreach (string configuredPath in settings.AnalyzerPaths)
                    AddConfiguredAnalyzerPath(analyzers, projectRoot, configuredPath);
            }

            if (settings.UnitySourceGeneratorInjectionEnabled)
            {
                string editorRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
                if (!string.IsNullOrEmpty(editorRoot))
                {
                    foreach (string relativePath in UnitySourceGeneratorRelativePaths)
                        AddIfFileExists(analyzers, Path.Combine(editorRoot, relativePath), relativePath);
                }
            }

            return analyzers;
        }

        private static void AddConfiguredAnalyzerPath(ICollection<string> analyzers, string projectRoot, string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return;

            string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
            string absolutePath = Path.IsPathRooted(expandedPath)
                ? expandedPath
                : Path.Combine(projectRoot, expandedPath);

            AddIfFileExists(analyzers, absolutePath, configuredPath);
        }

        private static void AddIfFileExists(ICollection<string> analyzers, string path, string displayPath)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string normalized = Path.GetFullPath(path).Replace('\\', '/');
            if (File.Exists(normalized))
            {
                analyzers.Add(normalized);
                return;
            }

            Debug.LogWarning("[ZedEditor] Analyzer path does not exist and will not be added to generated csproj files: " + displayPath);
        }

        private static string BuildAnalyzerBlock(string content, IEnumerable<string> analyzers, string newline)
        {
            var builder = new StringBuilder();

            foreach (string analyzer in analyzers)
            {
                string escapedPath = SecurityElement.Escape(analyzer);
                if (content.IndexOf("<Analyzer Include=\"" + escapedPath + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (builder.Length == 0)
                    builder.Append("  <ItemGroup>").Append(newline);

                builder.Append("    <Analyzer Include=\"").Append(escapedPath).Append("\" />").Append(newline);
            }

            if (builder.Length == 0)
                return string.Empty;

            builder.Append("  </ItemGroup>").Append(newline);
            return builder.ToString();
        }

        private static string InsertAnalyzerBlock(string content, string analyzerBlock, string newline)
        {
            string firstItemGroupMarker = "  <ItemGroup>" + newline;
            int firstItemGroupIndex = content.IndexOf(firstItemGroupMarker, StringComparison.Ordinal);
            if (firstItemGroupIndex >= 0)
                return content.Insert(firstItemGroupIndex, analyzerBlock);

            string projectEndMarker = "</Project>";
            int projectEndIndex = content.LastIndexOf(projectEndMarker, StringComparison.Ordinal);
            if (projectEndIndex >= 0)
                return content.Insert(projectEndIndex, analyzerBlock);

            return content + newline + analyzerBlock;
        }
    }
}
