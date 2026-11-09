using System.Runtime.CompilerServices;
using Xunit;

public class ResponsiveLayoutTests
{
    [Theory]
    [InlineData("Scenes/LearningModule.tscn")]
    [InlineData("Scenes/LauraMiniGame.tscn")]
    public void LongLessonScreensUseFocusFollowingVerticalScroll(string relativePath)
    {
        string scene = File.ReadAllText(RepositoryFile(relativePath));

        Assert.Contains("type=\"ScrollContainer\"", scene);
        Assert.Contains("follow_focus = true", scene);
        Assert.Contains("horizontal_scroll_mode = 0", scene);
        Assert.DoesNotContain("custom_minimum_size = Vector2(840, 64)", scene);
    }

    [Theory]
    [InlineData("Scripts/LearningModule.cs")]
    [InlineData("Scripts/LauraMiniGame.cs")]
    public void ScreenScriptsUseResponsiveContentPaths(string relativePath)
    {
        string script = File.ReadAllText(RepositoryFile(relativePath));

        Assert.Contains("Panel/Scroll/VBox", script);
        Assert.DoesNotContain("\"Panel/VBox", script);
    }

    private static string RepositoryFile(
        string relativePath,
        [CallerFilePath] string sourceFile = "")
    {
        string testsDirectory = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(testsDirectory, "..", relativePath));
    }
}
