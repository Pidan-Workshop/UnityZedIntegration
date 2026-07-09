using System.IO;
using NUnit.Framework;

namespace Unity.Zed.Editor.Tests
{
    public class ZedProcessTests
    {
        private const string ProjectDirectory = @"C:\Unity Projects\Window Game";
        private static readonly string SourceFile = Path.Combine(ProjectDirectory, "Assets", "Scripts", "Player.cs");

        [Test]
        public void BuildArguments_SmartBeforeProjectKnownOpen_CreatesNewWorkspace()
        {
            var args = ZedProcess.BuildArguments(
                ProjectDirectory,
                SourceFile,
                12,
                4,
                ZedWindowBehavior.Smart,
                false);

            Assert.That(args, Is.EqualTo(@"""--new"" ""--wait"" ""C:\Unity Projects\Window Game"" ""Assets/Scripts/Player.cs:12:4"""));
        }

        [Test]
        public void BuildArguments_SmartAfterProjectKnownOpen_ReusesProjectWorkspace()
        {
            var args = ZedProcess.BuildArguments(
                ProjectDirectory,
                SourceFile,
                12,
                4,
                ZedWindowBehavior.Smart,
                true);

            Assert.That(args, Is.EqualTo(@"""C:\Unity Projects\Window Game"" ""Assets/Scripts/Player.cs:12:4"""));
        }

        [Test]
        public void BuildArguments_ReuseCurrentWindow_OpensInExistingWindow()
        {
            var args = ZedProcess.BuildArguments(
                ProjectDirectory,
                SourceFile,
                12,
                4,
                ZedWindowBehavior.ReuseCurrentWindow,
                false);

            Assert.That(args, Is.EqualTo(@"""--existing"" ""C:\Unity Projects\Window Game"" ""Assets/Scripts/Player.cs:12:4"""));
        }

        [Test]
        public void BuildArguments_AlwaysNewWorkspace_CreatesNewWorkspaceEveryTime()
        {
            var args = ZedProcess.BuildArguments(
                ProjectDirectory,
                SourceFile,
                12,
                4,
                ZedWindowBehavior.AlwaysNewWorkspace,
                true);

            Assert.That(args, Is.EqualTo(@"""--new"" ""C:\Unity Projects\Window Game"" ""Assets/Scripts/Player.cs:12:4"""));
        }

        [Test]
        public void BuildArguments_ProjectOnlyOpen_IncludesWindowBehaviorAndProjectDirectory()
        {
            var args = ZedProcess.BuildArguments(
                ProjectDirectory,
                null,
                0,
                0,
                ZedWindowBehavior.AlwaysNewWorkspace,
                false);

            Assert.That(args, Is.EqualTo(@"""--new"" ""C:\Unity Projects\Window Game"""));
        }
    }
}
