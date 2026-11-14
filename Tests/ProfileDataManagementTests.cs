using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

public class ProfileDataManagementTests
{
    [Fact]
    public void LocalTransferRoundTripsDetailedProgress()
    {
        var profile = new ProfileData
        {
            DisplayName = "Half-Pint",
            PrairieStars = 12,
            Journey = new JourneyProgress { Miles = 80 }
        };
        profile.Outcomes.FirstTryCorrect = 4;

        string json = ProfileDataManagement.SerializeTransfer(
            profile,
            new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc));
        ProfileTransferParseResult result = ProfileDataManagement.ParseTransfer(json);

        Assert.True(result.Succeeded);
        Assert.Equal("Half-Pint", result.Profile!.DisplayName);
        Assert.Equal(12, result.Profile.PrairieStars);
        Assert.Equal(80, result.Profile.Journey.Miles);
        Assert.Equal(4, result.Profile.Outcomes.FirstTryCorrect);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"Profile\":{}}")]
    [InlineData("{\"TransferVersion\":1}")]
    [InlineData("{\"TransferVersion\":99,\"Profile\":{}}")]
    public void InvalidOrUnsupportedTransferFailsSafely(string json)
    {
        ProfileTransferParseResult result = ProfileDataManagement.ParseTransfer(json);

        Assert.False(result.Succeeded);
        Assert.Null(result.Profile);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void LearningResetPreservesAdventureProgress()
    {
        ProfileData profile = BuildProgressedProfile();

        ProfileDataManagement.Reset(profile, ProgressResetScope.Learning, 1);

        Assert.Equal(0, profile.SubjectProgress["math"].TotalAttempted);
        Assert.Equal(1, profile.OperationProgress["addition"].Tier);
        Assert.Equal(3, profile.SubjectProgress["laura"].TotalAttempted);
        Assert.Equal(80, profile.Journey.Miles);
        Assert.Equal(new[] { "big_woods" }, profile.MasteredLauraAreas);
        Assert.Equal(3, profile.TotalAttempted);
    }

    [Fact]
    public void AdventureResetPreservesClassroomProgress()
    {
        ProfileData profile = BuildProgressedProfile();
        profile.LastPlayedSubject = "laura";
        profile.SavedSession = new SavedSessionData
        {
            ScenePath = "res://Scenes/LauraMiniGame.tscn",
            LauraAreaId = "big_woods"
        };

        ProfileDataManagement.Reset(profile, ProgressResetScope.Adventure, 1);

        Assert.Equal(8, profile.SubjectProgress["math"].TotalAttempted);
        Assert.Equal(0, profile.SubjectProgress["laura"].TotalAttempted);
        Assert.Equal(0, profile.Journey.Miles);
        Assert.Empty(profile.MasteredLauraAreas);
        Assert.Equal(8, profile.TotalAttempted);
        Assert.Equal("math", profile.LastPlayedSubject);
        Assert.False(profile.SavedSession.HasResumePoint);
    }

    [Fact]
    public void FullResetPreservesNicknameAndGradeButClearsRewards()
    {
        ProfileData profile = BuildProgressedProfile();

        ProfileDataManagement.Reset(profile, ProgressResetScope.All, 1);

        Assert.Equal("Half-Pint", profile.DisplayName);
        Assert.Equal(4, profile.CurrentGrade);
        Assert.Equal(0, profile.TotalAttempted);
        Assert.Equal(0, profile.PrairieStars);
        Assert.Equal(0, profile.PrairiePennies);
        Assert.Empty(profile.TownInventory);
        Assert.False(profile.TownRewardInitialized);
        Assert.Equal(0, profile.HuntingTripsCompleted);
        Assert.Empty(profile.FieldJournalEntries);
        Assert.Empty(profile.UnlockedBadges);
        Assert.Empty(profile.MasteredLauraAreas);
        Assert.False(profile.SavedSession.HasResumePoint);
    }

    [Fact]
    public void ScopedResetPreservesLegacyTotalsNotRepresentedBySubjectRows()
    {
        ProfileData profile = BuildProgressedProfile();
        profile.TotalCorrect = 10;
        profile.TotalAttempted = 15;

        ProfileDataManagement.Reset(profile, ProgressResetScope.Adventure, 1);

        Assert.Equal(8, profile.TotalCorrect);
        Assert.Equal(12, profile.TotalAttempted);
    }

    [Fact]
    public void RecentActivityIncludesOnlyPlayedSubjectsInNewestFirstOrder()
    {
        var profile = new ProfileData
        {
            SubjectProgress = new Dictionary<string, SubjectProgress>
            {
                ["science"] = new() { TotalAttempted = 2, LastPlayedUtc = new DateTime(2026, 11, 8) },
                ["math"] = new() { TotalAttempted = 4, LastPlayedUtc = new DateTime(2026, 11, 9) },
                ["english"] = new() { TotalAttempted = 0, LastPlayedUtc = new DateTime(2026, 11, 10) }
            }
        };

        IReadOnlyList<string> recent = ProfileDataManagement.GetRecentSubjectKeys(profile);

        Assert.Equal(new[] { "math", "science" }, recent);
    }

    [Fact]
    public void DisplayNameAliasDoesNotDuplicateNicknameInJson()
    {
        var profile = new ProfileData { DisplayName = "Bookworm" };

        string json = JsonSerializer.Serialize(profile);

        Assert.Contains("\"FirstName\":\"Bookworm\"", json);
        Assert.DoesNotContain("DisplayName", json);
    }

    private static ProfileData BuildProgressedProfile()
    {
        var profile = new ProfileData
        {
            DisplayName = "Half-Pint",
            CurrentGrade = 4,
            PrairieStars = 20,
            GameScore = 400,
            Journey = new JourneyProgress { Miles = 80 },
            MasteredLauraAreas = new List<string> { "big_woods" },
            UnlockedBadges = new List<string> { "first_correct" },
            HuntingTripsCompleted = 2,
            FieldJournalEntries = new List<string> { "rabbit|Rabbit tracks" },
            SavedSession = new SavedSessionData { ScenePath = "res://Scenes/LearningModule.tscn" }
        };
        profile.SubjectProgress["math"] = new SubjectProgress
        {
            TotalCorrect = 6,
            TotalAttempted = 8
        };
        profile.SubjectProgress["laura"] = new SubjectProgress
        {
            TotalCorrect = 2,
            TotalAttempted = 3
        };
        profile.OperationProgress["addition"] = new OperationProgress { Tier = 2 };
        profile.TotalCorrect = 8;
        profile.TotalAttempted = 11;
        return profile;
    }
}
