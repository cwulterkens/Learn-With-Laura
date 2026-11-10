using System.Runtime.CompilerServices;
using Xunit;

public class LessonArchitectureTests
{
    [Fact]
    public void RetiredMathClassroomFilesAreAbsent()
    {
        Assert.False(File.Exists(RepositoryFile("Scripts/MathClassroom.cs")));
        Assert.False(File.Exists(RepositoryFile("Scenes/MathClassroom.tscn")));
    }

    [Fact]
    public void RuntimeResourcesDoNotReferenceRetiredLessonFlow()
    {
        string repositoryRoot = RepositoryFile("");
        IEnumerable<string> runtimeFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "Scripts"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "Scenes"),
                "*.tscn",
                SearchOption.AllDirectories))
            .Append(Path.Combine(repositoryRoot, "project.godot"));

        foreach (string path in runtimeFiles)
        {
            Assert.DoesNotContain("MathClassroom", File.ReadAllText(path));
        }
    }

    [Fact]
    public void ActiveLessonRetainsRequiredSessionAndProgressBehavior()
    {
        string lesson = File.ReadAllText(RepositoryFile("Scripts/LearningModule.cs"));
        string subjectSelection = File.ReadAllText(RepositoryFile("Scripts/SubjectSelection.cs"));

        Assert.Contains("res://Scenes/LearningModule.tscn", subjectSelection);
        Assert.Contains("TotalLearningSeconds", lesson);
        Assert.Contains("SessionsCompleted", lesson);
        Assert.Contains("RecordQuestionOutcome", lesson);
        Assert.Contains("TryApplyAdaptiveProgress", lesson);
        Assert.Contains("UnlockEligibleBadges", lesson);
        Assert.Contains("SaveProfile", lesson);
        Assert.Contains("res://Scenes/Recess.tscn", lesson);
    }

    private static string RepositoryFile(
        string relativePath,
        [CallerFilePath] string sourceFile = "")
    {
        string testsDirectory = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(testsDirectory, "..", relativePath));
    }
}
