using Xunit;

public class JourneyResourcesTests
{
    [Fact]
    public void NormalizationKeepsResourcesValidAndPreservesEarnedSupplies()
    {
        var journey = new JourneyProgress
        {
            Day = -3,
            Miles = -40,
            Food = 99,
            Wood = -2,
            Health = 12,
            Morale = -1,
            CurrentRegion = ""
        };

        JourneyResources.Normalize(journey);

        Assert.Equal(1, journey.Day);
        Assert.Equal(0, journey.Miles);
        Assert.Equal(99, journey.Food);
        Assert.Equal(0, journey.Wood);
        Assert.Equal(JourneyResources.MaxHealth, journey.Health);
        Assert.Equal(0, journey.Morale);
        Assert.Equal("big_woods", journey.CurrentRegion);
    }

    [Fact]
    public void WellSuppliedTravelAdvancesDayMilesAndConsumesSupplies()
    {
        var journey = new JourneyProgress
        {
            Food = 10,
            Wood = 5,
            Health = 8,
            Morale = 8
        };

        JourneyChangeResult result = JourneyResources.AdvanceTravelStop(journey, 12, 2, 1);

        Assert.Equal(2, journey.Day);
        Assert.Equal(12, journey.Miles);
        Assert.Equal(8, journey.Food);
        Assert.Equal(4, journey.Wood);
        Assert.Equal(12, result.MilesChange);
    }

    [Fact]
    public void LowSuppliesCreateAGentlerPaceAndExplainConsequences()
    {
        var journey = new JourneyProgress
        {
            Food = JourneyResources.LowFood,
            Wood = JourneyResources.LowWood,
            Health = 8,
            Morale = 8
        };

        JourneyChangeResult result = JourneyResources.AdvanceTravelStop(journey, 12, 3, 1);

        Assert.Equal(7, journey.Miles);
        Assert.Equal(0, journey.Food);
        Assert.Equal(0, journey.Wood);
        Assert.Equal(7, journey.Health);
        Assert.Equal(7, journey.Morale);
        Assert.Contains("gentler pace", result.Summary);
        Assert.Contains("food basket is empty", result.Summary);
        Assert.Contains("wood pile is empty", result.Summary);
    }

    [Fact]
    public void EmptyResourcesReceiveAutomaticSupportAndJourneyContinues()
    {
        var journey = new JourneyProgress
        {
            Food = 0,
            Wood = 0,
            Health = 0,
            Morale = 0
        };

        JourneyChangeResult result = JourneyResources.AdvanceTravelStop(journey, 10, 2, 1);

        Assert.True(journey.Miles > 0);
        Assert.True(journey.Food > 0);
        Assert.True(journey.Wood > 0);
        Assert.True(journey.Health > 0);
        Assert.True(journey.Morale > 0);
        Assert.Contains("shared pantry", result.Summary);
        Assert.Contains("supported rest", result.Summary);
    }

    [Fact]
    public void GatheringRewardsReflectCurrentRegion()
    {
        var woods = new JourneyProgress { Food = 5, Wood = 5, CurrentRegion = "big_woods" };
        var prairie = new JourneyProgress { Food = 5, Wood = 5, CurrentRegion = "prairie" };

        JourneyResources.ApplyCampChoice(woods, CampChoice.GatherSupplies);
        JourneyResources.ApplyCampChoice(prairie, CampChoice.GatherSupplies);

        Assert.Equal(8, woods.Food);
        Assert.Equal(10, woods.Wood);
        Assert.Equal(10, prairie.Food);
        Assert.Equal(8, prairie.Wood);
        Assert.Equal(2, woods.Day);
        Assert.Equal(2, prairie.Day);
    }

    [Fact]
    public void RestChoiceRecoversWithoutAllowingAZeroFoodDeadEnd()
    {
        var journey = new JourneyProgress
        {
            Food = 0,
            Health = 1,
            Morale = 1
        };

        JourneyChangeResult result = JourneyResources.ApplyCampChoice(
            journey,
            CampChoice.RestTogether);

        Assert.Equal(1, journey.Food);
        Assert.Equal(4, journey.Health);
        Assert.Equal(4, journey.Morale);
        Assert.Equal(2, journey.Day);
        Assert.Contains("rest day", result.Summary);
    }

    [Fact]
    public void BreakChoicesHaveDistinctRestorativeEffects()
    {
        var stretch = new JourneyProgress { Health = 4, Morale = 4 };
        var breathing = new JourneyProgress { Health = 4, Morale = 4 };

        JourneyResources.ApplyLearningBreak(stretch, LearningBreakChoice.Stretch);
        JourneyResources.ApplyLearningBreak(breathing, LearningBreakChoice.Breathing);

        Assert.Equal((6, 5), (stretch.Health, stretch.Morale));
        Assert.Equal((5, 6), (breathing.Health, breathing.Morale));
    }

    [Fact]
    public void FamilyActivityRewardsAreCappedAndReportedAccurately()
    {
        var journey = new JourneyProgress
        {
            Food = JourneyResources.FoodCapacity - 1,
            Wood = JourneyResources.WoodCapacity,
            Morale = JourneyResources.MaxMorale
        };

        JourneyChangeResult result = JourneyResources.ApplyFamilyActivityReward(
            journey,
            "baking");

        Assert.Equal(JourneyResources.FoodCapacity, journey.Food);
        Assert.Equal(JourneyResources.MaxMorale, journey.Morale);
        Assert.Equal(1, result.FoodChange);
        Assert.Equal(0, result.MoraleChange);
        Assert.Contains("food +1", result.Summary);
    }
}
