using System.Text.Json;
using Xunit;

public class AdaptiveMasteryTests
{
    [Theory]
    [InlineData(LearningOutcome.FirstTryCorrect, 1, 0, 0, 0, 1)]
    [InlineData(LearningOutcome.HintAssistedCorrect, 0, 1, 0, 0, 0)]
    [InlineData(LearningOutcome.Corrected, 0, 0, 1, 0, 0)]
    [InlineData(LearningOutcome.Skipped, 0, 0, 0, 1, 0)]
    public void EachOutcomeHasDistinctSavedAndAdaptiveMeaning(
        LearningOutcome outcome,
        int firstTry,
        int assisted,
        int corrected,
        int skipped,
        int adaptiveCredit)
    {
        var counts = new LearningOutcomeCounts();
        var progress = new OperationProgress();

        AdaptiveMastery.RecordOutcome(counts, outcome);
        AdaptiveMastery.RecordWindowOutcome(progress, outcome);

        Assert.Equal(firstTry, counts.FirstTryCorrect);
        Assert.Equal(assisted, counts.HintAssistedCorrect);
        Assert.Equal(corrected, counts.Corrected);
        Assert.Equal(skipped, counts.Skipped);
        Assert.Equal(1, progress.WindowAttempted);
        Assert.Equal(adaptiveCredit, progress.WindowFirstTryCorrect);
    }

    [Fact]
    public void FirstTryMasteryAtPromotionThresholdRaisesTier()
    {
        var progress = new OperationProgress
        {
            Tier = 1,
            WindowAttempted = 20,
            WindowFirstTryCorrect = 17
        };

        bool evaluated = AdaptiveMastery.TryEvaluate(
            progress, 20, 0.85, 0.50, 3, out AdaptiveTierChange change);

        Assert.True(evaluated);
        Assert.Equal(AdaptiveTierChange.Promoted, change);
        Assert.Equal(2, progress.Tier);
        Assert.Equal(0, progress.WindowAttempted);
        Assert.Equal(0, progress.WindowFirstTryCorrect);
    }

    [Theory]
    [InlineData(LearningOutcome.HintAssistedCorrect)]
    [InlineData(LearningOutcome.Corrected)]
    [InlineData(LearningOutcome.Skipped)]
    public void NonMasteryOutcomesAtDemotionThresholdLowerTier(LearningOutcome outcome)
    {
        var progress = new OperationProgress { Tier = 2 };
        for (int index = 0; index < 10; index++)
        {
            AdaptiveMastery.RecordWindowOutcome(progress, outcome);
        }

        bool evaluated = AdaptiveMastery.TryEvaluate(
            progress, 10, 0.85, 0.50, 3, out AdaptiveTierChange change);

        Assert.True(evaluated);
        Assert.Equal(AdaptiveTierChange.Demoted, change);
        Assert.Equal(1, progress.Tier);
    }

    [Theory]
    [InlineData(LearningOutcome.HintAssistedCorrect, 17, 3, AdaptiveTierChange.Promoted, 2)]
    [InlineData(LearningOutcome.Corrected, 17, 3, AdaptiveTierChange.Promoted, 2)]
    [InlineData(LearningOutcome.Skipped, 17, 3, AdaptiveTierChange.Promoted, 2)]
    [InlineData(LearningOutcome.HintAssistedCorrect, 10, 10, AdaptiveTierChange.Demoted, 0)]
    [InlineData(LearningOutcome.Corrected, 10, 10, AdaptiveTierChange.Demoted, 0)]
    [InlineData(LearningOutcome.Skipped, 10, 10, AdaptiveTierChange.Demoted, 0)]
    public void PromotionAndDemotionBoundariesUseFirstTryRateForEveryOtherOutcome(
        LearningOutcome otherOutcome,
        int firstTryCount,
        int otherCount,
        AdaptiveTierChange expectedChange,
        int expectedTier)
    {
        var progress = new OperationProgress { Tier = 1 };
        for (int index = 0; index < firstTryCount; index++)
        {
            AdaptiveMastery.RecordWindowOutcome(progress, LearningOutcome.FirstTryCorrect);
        }
        for (int index = 0; index < otherCount; index++)
        {
            AdaptiveMastery.RecordWindowOutcome(progress, otherOutcome);
        }

        AdaptiveMastery.TryEvaluate(
            progress, 20, 0.85, 0.50, 3, out AdaptiveTierChange change);

        Assert.Equal(expectedChange, change);
        Assert.Equal(expectedTier, progress.Tier);
    }

    [Fact]
    public void RateBetweenThresholdsKeepsTier()
    {
        var progress = new OperationProgress
        {
            Tier = 1,
            WindowAttempted = 10,
            WindowFirstTryCorrect = 7
        };

        bool evaluated = AdaptiveMastery.TryEvaluate(
            progress, 10, 0.85, 0.50, 3, out AdaptiveTierChange change);

        Assert.True(evaluated);
        Assert.Equal(AdaptiveTierChange.None, change);
        Assert.Equal(1, progress.Tier);
    }

    [Fact]
    public void OutcomeDetailRoundTripsAtProfileSubjectAndOperationLevels()
    {
        var profile = new ProfileData();
        profile.SubjectProgress["math"] = new SubjectProgress();
        profile.OperationProgress["addition"] = new OperationProgress();

        AdaptiveMastery.RecordOutcome(profile.Outcomes, LearningOutcome.FirstTryCorrect);
        AdaptiveMastery.RecordOutcome(
            profile.SubjectProgress["math"].Outcomes,
            LearningOutcome.HintAssistedCorrect);
        AdaptiveMastery.RecordOutcome(
            profile.OperationProgress["addition"].Outcomes,
            LearningOutcome.Corrected);

        string json = JsonSerializer.Serialize(profile);
        ProfileData restored = JsonSerializer.Deserialize<ProfileData>(json)!;

        Assert.Equal(1, restored.Outcomes.FirstTryCorrect);
        Assert.Equal(1, restored.SubjectProgress["math"].Outcomes.HintAssistedCorrect);
        Assert.Equal(1, restored.OperationProgress["addition"].Outcomes.Corrected);
    }

    [Fact]
    public void LegacyPartialWindowIsDiscardedWithoutChangingTierOrLifetimeOutcomes()
    {
        var progress = new OperationProgress
        {
            Tier = 2,
            WindowAttempted = 6,
            WindowCorrect = 5
        };
        progress.Outcomes.FirstTryCorrect = 12;

        bool reset = AdaptiveMastery.ResetAmbiguousLegacyWindow(progress);

        Assert.True(reset);
        Assert.Equal(2, progress.Tier);
        Assert.Equal(12, progress.Outcomes.FirstTryCorrect);
        Assert.Equal(0, progress.WindowAttempted);
        Assert.Equal(0, progress.WindowCorrect);
        Assert.Equal(0, progress.WindowFirstTryCorrect);
    }
}
