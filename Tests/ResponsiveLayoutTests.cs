using System.Runtime.CompilerServices;
using Xunit;

public class ResponsiveLayoutTests
{
    [Theory]
    [InlineData("Scenes/MainMenu.tscn")]
    [InlineData("Scenes/ProfileSelection.tscn")]
    [InlineData("Scenes/GradeSelection.tscn")]
    [InlineData("Scenes/SubjectSelection.tscn")]
    [InlineData("Scenes/OptionsMenu.tscn")]
    [InlineData("Scenes/LearningModule.tscn")]
    [InlineData("Scenes/LauraMiniGame.tscn")]
    [InlineData("Scenes/HuntingWithPa.tscn")]
    [InlineData("Scenes/ProgressScreen.tscn")]
    [InlineData("Scenes/Recess.tscn")]
    public void LongLessonScreensUseFocusFollowingVerticalScroll(string relativePath)
    {
        string scene = File.ReadAllText(RepositoryFile(relativePath));

        Assert.Contains("type=\"ScrollContainer\"", scene);
        Assert.Contains("follow_focus = true", scene);
        Assert.Contains("horizontal_scroll_mode = 0", scene);
        Assert.DoesNotContain("custom_minimum_size = Vector2(840, 64)", scene);
    }

    [Theory]
    [InlineData("Scripts/MainMenu.cs")]
    [InlineData("Scripts/ProfileSelection.cs")]
    [InlineData("Scripts/GradeSelection.cs")]
    [InlineData("Scripts/SubjectSelection.cs")]
    [InlineData("Scripts/OptionsMenu.cs")]
    [InlineData("Scripts/LearningModule.cs")]
    [InlineData("Scripts/LauraMiniGame.cs")]
    [InlineData("Scripts/HuntingWithPa.cs")]
    [InlineData("Scripts/ProgressScreen.cs")]
    [InlineData("Scripts/Recess.cs")]
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
