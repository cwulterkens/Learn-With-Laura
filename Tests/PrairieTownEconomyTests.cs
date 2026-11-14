using System.Collections.Generic;
using Xunit;

public class PrairieTownEconomyTests
{
    [Fact]
    public void LearningAnswersAwardPenniesWithoutSpendingStars()
    {
        var profile = new ProfileData { PrairieStars = 8 };

        int firstTry = PrairieTownEconomy.AwardLearningReward(profile, true);
        int withHint = PrairieTownEconomy.AwardLearningReward(profile, false);

        Assert.Equal(2, firstTry);
        Assert.Equal(1, withHint);
        Assert.Equal(3, profile.PrairiePennies);
        Assert.Equal(8, profile.PrairieStars);
    }

    [Fact]
    public void ExistingLearningSeedsStarterPurseOnlyOnce()
    {
        var profile = new ProfileData { PrairieStars = 20 };

        int first = PrairieTownEconomy.PrepareFirstTownVisit(profile);
        int second = PrairieTownEconomy.PrepareFirstTownVisit(profile);

        Assert.Equal(16, first);
        Assert.Equal(0, second);
        Assert.Equal(16, profile.PrairiePennies);
    }

    [Fact]
    public void PurchaseIsPermanentAndCannotBeBoughtTwice()
    {
        var profile = new ProfileData { PrairiePennies = 10 };

        TownPurchaseResult first = PrairieTownEconomy.TryPurchase(profile, "storybook");
        TownPurchaseResult second = PrairieTownEconomy.TryPurchase(profile, "storybook");

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(3, profile.PrairiePennies);
        Assert.Equal(new[] { "storybook" }, profile.TownInventory);
    }

    [Fact]
    public void FirstDiscoveryAwardsOnePennyButRepeatDoesNot()
    {
        var profile = new ProfileData();

        Assert.Equal(1, PrairieTownEconomy.RecordDiscovery(profile, "schoolhouse"));
        Assert.Equal(0, PrairieTownEconomy.RecordDiscovery(profile, "SCHOOLHOUSE"));
        Assert.Equal(1, profile.PrairiePennies);
        Assert.Equal(new[] { "schoolhouse" }, profile.TownDiscoveries);
    }

    [Fact]
    public void NormalizationRepairsLegacyCollections()
    {
        var profile = new ProfileData
        {
            PrairiePennies = -4,
            TownInventory = new List<string> { " storybook ", "STORYBOOK", "" },
            TownDiscoveries = null!
        };

        PrairieTownEconomy.Normalize(profile);

        Assert.Equal(0, profile.PrairiePennies);
        Assert.Equal(new[] { "storybook" }, profile.TownInventory);
        Assert.Empty(profile.TownDiscoveries);
    }
}
