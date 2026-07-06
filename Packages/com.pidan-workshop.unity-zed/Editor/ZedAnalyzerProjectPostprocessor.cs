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
    /// Adds Unity and project Roslyn analyzers to generated C# project files so non-Unity IDE language servers can see source generators.
    /// </summary>
    internal sealed class ZedAnalyzerProjectPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// SmartShark's analyzer is stored in a package path instead of a Unity editor install path, so it must be resolved from the project root.
        /// </summary>
        private const string SmartSharkAnalyzerRelativePath = "Packages/SmartShark.Core/Analyzers/SmartShark.Generator.dll";

        /// <summary>
        /// Unity ships these source generators next to the editor executable; Zed needs them in csproj metadata for design-time generated code.
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

            var analyzers = ResolveAnalyzerPaths();
            if (analyzers.Count == 0)
                return content;

            string newline = content.Contains("\r\n") ? "\r\n" : "\n";
            string analyzerBlock = BuildAnalyzerBlock(content, analyzers, newline);
            if (string.IsNullOrEmpty(analyzerBlock))
                return content;

            return InsertAnalyzerBlock(content, analyzerBlock, newline);
        }

        private static List<string> ResolveAnalyzerPaths()
        {
            var analyzers = new List<string>();

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            AddIfFileExists(analyzers, Path.Combine(projectRoot, SmartSharkAnalyzerRelativePath));

            string editorRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (!string.IsNullOrEmpty(editorRoot))
            {
                foreach (string relativePath in UnitySourceGeneratorRelativePaths)
                    AddIfFileExists(analyzers, Path.Combine(editorRoot, relativePath));
            }

            return analyzers;
        }

        private static void AddIfFileExists(ICollection<string> analyzers, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string normalized = Path.GetFullPath(path).Replace('\\', '/');
            if (File.Exists(normalized))
                analyzers.Add(normalized);
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
