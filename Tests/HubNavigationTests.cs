using System.Runtime.CompilerServices;
using Xunit;

public class HubNavigationTests
{
    [Fact]
    public void TownOwnsLearningAndWorldExplorationEntrances()
    {
        string town = File.ReadAllText(RepositoryFile("Scripts/PrairieTown.cs"));

        Assert.Contains("res://Scenes/GradeSelection.tscn", town);
        Assert.Contains("res://Scenes/LauraMiniGame.tscn", town);
        Assert.Contains("res://Scenes/HuntingWithPa.tscn", town);
        Assert.Contains("exploration_wagon", town);
        Assert.Contains("hunting_field", town);
        Assert.Contains("LaunchLauraFreePlay", town);
    }

    [Fact]
    public void MainMenuNoLongerDuplicatesLearningOrAdventureButtons()
    {
        string menu = File.ReadAllText(RepositoryFile("Scenes/MainMenu.tscn"));

        Assert.Contains("TownButton", menu);
        Assert.DoesNotContain("LearnButton", menu);
        Assert.DoesNotContain("TrailButton", menu);
    }

    [Fact]
    public void HuntingWithPaUsesSlingshotSmallGameGallery()
    {
        string controller = File.ReadAllText(RepositoryFile("Scripts/HuntingWithPa.cs"));
        string field = File.ReadAllText(RepositoryFile("Scripts/HuntingField.cs"));
        string scene = File.ReadAllText(RepositoryFile("Scenes/HuntingWithPa.tscn"));

        Assert.Contains("rabbit", field);
        Assert.Contains("squirrel", field);
        Assert.Contains("YOUNG — LET PASS", field);
        Assert.Contains("prairie_slingshot.png", scene);
        Assert.Contains("hunting_field_rendered.png", scene);
        Assert.Contains("hunting_rabbit_rendered.png", scene);
        Assert.Contains("hunting_squirrel_rendered.png", scene);
        Assert.Contains("GetDepthScale", field);
        Assert.Contains("ShotsNeeded = 3", controller);
        Assert.Contains("responsiblyTookGame: true", controller);
        Assert.DoesNotContain("deer", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("turkey", controller, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Scripts/GradeSelection.cs")]
    [InlineData("Scripts/SubjectSelection.cs")]
    [InlineData("Scripts/LearningModule.cs")]
    [InlineData("Scripts/Recess.cs")]
    [InlineData("Scripts/LauraMiniGame.cs")]
    public void LearningAndAdventureHomeRoutesReturnToTown(string relativePath)
    {
        string script = File.ReadAllText(RepositoryFile(relativePath));

        Assert.Contains("res://Scenes/PrairieTown.tscn", script);
    }

    private static string RepositoryFile(
        string relativePath,
        [CallerFilePath] string sourceFile = "")
    {
        string testsDirectory = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(testsDirectory, "..", relativePath));
    }
}
