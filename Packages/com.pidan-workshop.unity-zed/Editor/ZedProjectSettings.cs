using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Zed.Editor
{
    [FilePath("ProjectSettings/ZedEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ZedProjectSettings : ScriptableSingleton<ZedProjectSettings>
    {
        private const string DefaultSourceFileExtensions =
            ".cs\n" +
            ".shader\n" +
            ".compute\n" +
            ".hlsl\n" +
            ".cginc\n" +
            ".glsl";

        private const string DefaultGitignoreEntries =
            "[Ll]ibrary/\n" +
            "[Tt]emp/\n" +
            "[Oo]bj/\n" +
            "[Bb]uild/\n" +
            "[Bb]uilds/\n" +
            "[Ll]ogs/\n" +
            "[Uu]ser[Ss]ettings/";

        [SerializeField]
        private string m_SourceFileExtensions = DefaultSourceFileExtensions;

        [SerializeField]
        private bool m_ProjectPanelHidesGitignoredFiles = true;

        [SerializeField]
        private string m_GitignoreEntries = DefaultGitignoreEntries;

        public string SourceFileExtensionsText
        {
            get { return m_SourceFileExtensions; }
            set { m_SourceFileExtensions = value ?? string.Empty; }
        }

        public bool ProjectPanelHidesGitignoredFiles
        {
            get { return m_ProjectPanelHidesGitignoredFiles; }
            set { m_ProjectPanelHidesGitignoredFiles = value; }
        }

        public string GitignoreEntriesText
        {
            get { return m_GitignoreEntries; }
            set { m_GitignoreEntries = value ?? string.Empty; }
        }

        public bool ShouldOpenPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            var extension = NormalizeExtension(Path.GetExtension(path));
            return SourceFileExtensions.Contains(extension);
        }

        public IEnumerable<string> SourceFileExtensions
        {
            get { return ParseLines(m_SourceFileExtensions).Select(NormalizeExtension).Where(e => !string.IsNullOrEmpty(e)); }
        }

        public IEnumerable<string> GitignoreEntries
        {
            get { return ParseLines(m_GitignoreEntries); }
        }

        public void ResetSourceFileExtensions()
        {
            m_SourceFileExtensions = DefaultSourceFileExtensions;
        }

        public void ResetGitignoreEntries()
        {
            m_GitignoreEntries = DefaultGitignoreEntries;
        }

        public void SaveSettings()
        {
            Save(true);
        }

        private static IEnumerable<string> ParseLines(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;

            var separators = new[] { '\r', '\n', ';', ',' };
            foreach (var raw in value.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (!string.IsNullOrEmpty(line))
                    yield return line;
            }
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            extension = extension.Trim().ToLowerInvariant();
            return extension[0] == '.' ? extension : "." + extension;
        }
    }
}
