using Xunit;

public class HuntingWithPaProgressTests
{
    [Fact]
    public void ResponsibleHuntAddsFoodPenniesAndJournalEntry()
    {
        var profile = new ProfileData
        {
            PrairiePennies = 2,
            Journey = new JourneyProgress { Food = 20, Morale = 7 }
        };

        HuntingTripResult result = HuntingWithPaProgress.CompleteTrip(
            profile,
            "rabbit",
            "Rabbit — small paired tracks often land ahead of the front tracks.",
            true);

        Assert.Equal(4, result.FoodGained);
        Assert.Equal(1, result.MoraleGained);
        Assert.Equal(5, profile.PrairiePennies);
        Assert.Equal(1, profile.HuntingTripsCompleted);
        Assert.Single(profile.FieldJournalEntries);
    }

    [Fact]
    public void LettingAnimalPassRewardsJudgmentWithoutAddingFood()
    {
        var profile = new ProfileData
        {
            Journey = new JourneyProgress { Food = 12, Morale = 5 }
        };

        HuntingTripResult result = HuntingWithPaProgress.CompleteTrip(
            profile,
            "squirrel",
            "Squirrel — small front and hind prints often appear near trees.",
            false);

        Assert.Equal(0, result.FoodGained);
        Assert.Equal(2, result.MoraleGained);
        Assert.Equal(HuntingWithPaProgress.PennyReward, profile.PrairiePennies);
        Assert.Contains("good judgment", result.Summary);
    }

    [Fact]
    public void RepeatedAnimalDoesNotDuplicateJournalEntry()
    {
        var profile = new ProfileData();

        HuntingWithPaProgress.CompleteTrip(profile, "rabbit", "Rabbit — paired hind prints land ahead.", false);
        HuntingTripResult repeat = HuntingWithPaProgress.CompleteTrip(
            profile,
            "RABBIT",
            "Rabbit — paired hind prints land ahead.",
            false);

        Assert.False(repeat.NewJournalEntry);
        Assert.Single(profile.FieldJournalEntries);
        Assert.Equal(2, profile.HuntingTripsCompleted);
    }
}
