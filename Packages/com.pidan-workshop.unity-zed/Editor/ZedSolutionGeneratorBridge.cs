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
        public static IGenerator CreateGenerator()
        {
            var projectDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            var asm = typeof(ProjectGeneration).Assembly;
            var sdkType = asm.GetType("Microsoft.Unity.VisualStudio.Editor.SdkStyleProjectGeneration");
            var sdkGenerator = TryCreateParameterlessGenerator(sdkType);
            if (sdkGenerator != null)
                return sdkGenerator;

            var legacyType = asm.GetType("Microsoft.Unity.VisualStudio.Editor.LegacyStyleProjectGeneration");
            var legacyGenerator = TryCreateProjectDirectoryGenerator(legacyType, projectDirectory);
            if (legacyGenerator != null)
                return legacyGenerator;

            return new ProjectGeneration(projectDirectory);
        }

        private static IGenerator TryCreateParameterlessGenerator(Type generatorType)
        {
            if (generatorType == null)
                return null;

            var ctor = generatorType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            return ctor != null ? (IGenerator)ctor.Invoke(Array.Empty<object>()) : null;
        }

        private static IGenerator TryCreateProjectDirectoryGenerator(Type generatorType, string projectDirectory)
        {
            if (generatorType == null)
                return null;

            var ctors = generatorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var stringCtor = ctors.FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 1 && p[0].ParameterType == typeof(string);
            });

            return stringCtor != null ? (IGenerator)stringCtor.Invoke(new object[] { projectDirectory }) : null;
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
