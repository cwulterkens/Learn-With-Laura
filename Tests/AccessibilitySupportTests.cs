using System.Runtime.CompilerServices;
using Xunit;

public class AccessibilitySupportTests
{
    [Theory]
    [InlineData(false, -1, false)]
    [InlineData(false, 0, false)]
    [InlineData(false, 1, true)]
    [InlineData(true, 0, true)]
    public void ReducedMotionHonorsLocalAndSystemPreferences(
        bool localSetting,
        int systemPreference,
        bool expected)
    {
        Assert.Equal(
            expected,
            AccessibilitySupport.ShouldReduceMotion(localSetting, systemPreference));
    }

    [Theory]
    [InlineData(false, -1, false)]
    [InlineData(false, 0, false)]
    [InlineData(false, 1, true)]
    [InlineData(true, 0, true)]
    public void HighContrastHonorsLocalAndSystemPreferences(
        bool localSetting,
        int systemPreference,
        bool expected)
    {
        Assert.Equal(
            expected,
            AccessibilitySupport.ShouldUseHighContrast(localSetting, systemPreference));
    }

    [Fact]
    public void StaticPatternIsNumberedTextAndDoesNotDependOnAudio()
    {
        string result = AccessibilitySupport.BuildStaticPattern(
            new[] { 2, 0, 3 },
            new[] { "Deer Track", "Rabbit Track", "Creek", "Camp" });

        Assert.Equal(
            "Static pattern: 1. Creek → 2. Deer Track → 3. Camp. Your turn — choose these clues in order. The pattern stays on screen.",
            result);
    }

    [Fact]
    public void InvalidStaticPatternFailsInsteadOfAnnouncingWrongInstructions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AccessibilitySupport.BuildStaticPattern(new[] { 4 }, new[] { "One" }));
    }

    [Fact]
    public void NativeAccessibilityAndStaticMemoryAlternativeAreEnabled()
    {
        string project = File.ReadAllText(RepositoryFile("project.godot"));
        string miniGame = File.ReadAllText(RepositoryFile("Scripts/LauraMiniGame.cs"));
        string helpers = File.ReadAllText(RepositoryFile("Scripts/Core/UiHelpers.cs"));

        Assert.Contains("general/accessibility_support=1", project);
        Assert.Contains("AccessibilityLiveMode.Polite", helpers);
        Assert.Contains("AccessibilityName", helpers);
        Assert.Contains("AccessibilityShouldReduceAnimation", helpers);
        Assert.Contains("BuildStaticPattern", miniGame);
    }

    private static string RepositoryFile(
        string relativePath,
        [CallerFilePath] string sourceFile = "")
    {
        string testsDirectory = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(testsDirectory, "..", relativePath));
    }
}
