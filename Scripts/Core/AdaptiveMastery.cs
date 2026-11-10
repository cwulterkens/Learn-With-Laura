using System;

public enum LearningOutcome
{
    FirstTryCorrect,
    HintAssistedCorrect,
    Corrected,
    Skipped
}

public enum AdaptiveTierChange
{
    None,
    Promoted,
    Demoted
}

public static class AdaptiveMastery
{
    public static bool ResetAmbiguousLegacyWindow(OperationProgress progress)
    {
        LearningOutcomeCounts outcomes = progress.Outcomes ??= new LearningOutcomeCounts();
        bool hasDetailedOutcomes = outcomes.FirstTryCorrect > 0
            || outcomes.HintAssistedCorrect > 0
            || outcomes.Corrected > 0
            || outcomes.Skipped > 0;
        bool containsLegacyData = progress.WindowCorrect > 0
            || (!hasDetailedOutcomes
                && progress.WindowAttempted > 0
                && progress.WindowFirstTryCorrect == 0);

        if (!containsLegacyData)
        {
            return false;
        }

        progress.WindowCorrect = 0;
        progress.WindowAttempted = 0;
        progress.WindowFirstTryCorrect = 0;
        return true;
    }

    public static void RecordOutcome(
        LearningOutcomeCounts counts,
        LearningOutcome outcome)
    {
        switch (outcome)
        {
            case LearningOutcome.FirstTryCorrect:
                counts.FirstTryCorrect++;
                break;
            case LearningOutcome.HintAssistedCorrect:
                counts.HintAssistedCorrect++;
                break;
            case LearningOutcome.Corrected:
                counts.Corrected++;
                break;
            case LearningOutcome.Skipped:
                counts.Skipped++;
                break;
        }
    }

    public static void RecordWindowOutcome(
        OperationProgress progress,
        LearningOutcome outcome)
    {
        progress.WindowAttempted++;
        if (outcome == LearningOutcome.FirstTryCorrect)
        {
            progress.WindowFirstTryCorrect++;
        }
    }

    public static bool TryEvaluate(
        OperationProgress progress,
        int windowSize,
        double promoteAccuracy,
        double demoteAccuracy,
        int maxTier,
        out AdaptiveTierChange change)
    {
        change = AdaptiveTierChange.None;
        windowSize = Math.Max(1, windowSize);
        if (progress.WindowAttempted < windowSize)
        {
            return false;
        }

        double firstTryRate = (double)progress.WindowFirstTryCorrect / progress.WindowAttempted;
        if (firstTryRate >= promoteAccuracy && progress.Tier < maxTier)
        {
            progress.Tier++;
            change = AdaptiveTierChange.Promoted;
        }
        else if (firstTryRate <= demoteAccuracy && progress.Tier > 0)
        {
            progress.Tier--;
            change = AdaptiveTierChange.Demoted;
        }

        progress.WindowAttempted = 0;
        progress.WindowFirstTryCorrect = 0;
        return true;
    }
}
