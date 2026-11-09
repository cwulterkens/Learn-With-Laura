using System.Collections.Generic;
using System.Text.Json;
using Xunit;

public class AdventureProgressTests
{
    [Fact]
    public void FirstMasteryAndReplayAreRecordedSeparately()
    {
        var profile = new ProfileData();

        ChapterMasteryResult first = AdventureProgress.RecordMasteredRun(profile, "big_woods");
        ChapterMasteryResult replay = AdventureProgress.RecordMasteredRun(profile, "BIG_WOODS");

        Assert.Equal(ChapterMasteryResult.FirstMastery, first);
        Assert.Equal(ChapterMasteryResult.Replay, replay);
        Assert.Equal(new[] { "big_woods" }, profile.MasteredLauraAreas);
        Assert.Equal(2, profile.MiniGamesCompleted);
        Assert.Equal(1, profile.LauraChapterReplays);
    }

    [Fact]
    public void NormalizationPreservesLegacyCompletionCount()
    {
        var profile = new ProfileData
        {
            MiniGamesCompleted = 4,
            MasteredLauraAreas = new List<string> { " prairie ", "PRAIRIE", "", "town" }
        };

        AdventureProgress.Normalize(profile);

        Assert.Equal(4, profile.MiniGamesCompleted);
        Assert.Equal(new[] { "prairie", "town" }, profile.MasteredLauraAreas);
    }

    [Fact]
    public void LegacyJsonWithoutMasteryFieldsLoadsWithoutLosingTotals()
    {
        ProfileData profile = JsonSerializer.Deserialize<ProfileData>(
            "{\"FirstName\":\"Laura\",\"MiniGamesCompleted\":3}")!;

        AdventureProgress.Normalize(profile);

        Assert.Equal("Laura", profile.FirstName);
        Assert.Equal(3, profile.MiniGamesCompleted);
        Assert.Empty(profile.MasteredLauraAreas);
        Assert.Equal(0, profile.LauraChapterReplays);
    }

    [Fact]
    public void UniqueMasteryCountCannotBeIncreasedByReplays()
    {
        var profile = new ProfileData();

        AdventureProgress.RecordMasteredRun(profile, "big_woods");
        AdventureProgress.RecordMasteredRun(profile, "big_woods");
        AdventureProgress.RecordMasteredRun(profile, "prairie");

        Assert.Equal(2, profile.MasteredLauraAreas.Count);
        Assert.Equal(3, profile.MiniGamesCompleted);
        Assert.Equal(1, profile.LauraChapterReplays);
    }
}
