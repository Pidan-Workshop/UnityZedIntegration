using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    internal static class ZedSolutionGeneratorBridge
    {
        private static IGenerator CreateGenerator()
        {
            var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            var asm = typeof(ProjectGeneration).Assembly;
            var legacyType = asm.GetType("Microsoft.Unity.VisualStudio.Editor.LegacyStyleProjectGeneration");
            if (legacyType != null)
            {
                var ctors = legacyType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var stringCtor = ctors.FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 1 && p[0].ParameterType == typeof(string);
                });
                if (stringCtor != null)
                    return (IGenerator)stringCtor.Invoke(new object[] { projectDirectory });
            }

            return new ProjectGeneration(projectDirectory);
        }

        public static void SyncAll()
        {
            CreateGenerator().Sync();
        }

        public static void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            var affected = addedFiles
                .Union(deletedFiles)
                .Union(movedFiles)
                .Union(movedFromFiles);
            CreateGenerator().SyncIfNeeded(affected, importedFiles);
        }

        public static string GetOrGenerateSolutionFile()
        {
            var generator = CreateGenerator();
            generator.Sync();
            return generator.SolutionFile();
        }
    }
}
