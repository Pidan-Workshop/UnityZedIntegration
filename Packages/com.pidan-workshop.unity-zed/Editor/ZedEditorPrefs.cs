using UnityEditor;

namespace Unity.Zed.Editor
{
    internal static class ZedEditorPrefs
    {
        private const string Prefix = "ZedEditor_";

        public const string ExecutablePathKey = Prefix + "ExecutablePath";

        public static string GetExecutablePath()
        {
            return EditorPrefs.GetString(ExecutablePathKey, "");
        }

        public static void SetExecutablePath(string path)
        {
            EditorPrefs.SetString(ExecutablePathKey, path ?? "");
        }
    }
}
