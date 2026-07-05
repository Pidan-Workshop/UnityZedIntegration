using UnityEditor;

namespace Unity.Zed.Editor
{
    internal static class ZedEditorPrefs
    {
        private const string Prefix = "ZedEditor_";

        public const string ExecutablePathKey = Prefix + "ExecutablePath";
        public const string AdditionalArgsKey = Prefix + "AdditionalArgs";

        public static string GetExecutablePath()
        {
            return EditorPrefs.GetString(ExecutablePathKey, "");
        }

        public static void SetExecutablePath(string path)
        {
            EditorPrefs.SetString(ExecutablePathKey, path ?? "");
        }

        public static string GetAdditionalArgs()
        {
            return EditorPrefs.GetString(AdditionalArgsKey, "");
        }

        public static void SetAdditionalArgs(string args)
        {
            EditorPrefs.SetString(AdditionalArgsKey, args ?? "");
        }
    }
}
