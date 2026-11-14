#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

public static class AccessibilitySupport
{
    public static bool ShouldReduceMotion(bool localSetting, int systemPreference)
    {
        return localSetting || systemPreference == 1;
    }

    public static bool ShouldUseHighContrast(bool localSetting, int systemPreference)
    {
        return localSetting || systemPreference == 1;
    }

    public static string BuildStaticPattern(
        IReadOnlyList<int> pattern,
        IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(labels);

        if (pattern.Count == 0)
        {
            return "Static pattern is empty.";
        }

        string[] steps = pattern.Select((labelIndex, stepIndex) =>
        {
            if (labelIndex < 0 || labelIndex >= labels.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pattern),
                    $"Pattern step {stepIndex + 1} has no matching label.");
            }

            return $"{stepIndex + 1}. {labels[labelIndex]}";
        }).ToArray();

        return $"Static pattern: {string.Join(" → ", steps)}. Your turn — choose these clues in order. The pattern stays on screen.";
    }
}
