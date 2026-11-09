using System;
using System.Linq;

public enum ChapterMasteryResult
{
    FirstMastery,
    Replay
}

public static class AdventureProgress
{
    public static void Normalize(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        profile.MasteredLauraAreas ??= new();
        profile.MasteredLauraAreas = profile.MasteredLauraAreas
            .Where(areaId => !string.IsNullOrWhiteSpace(areaId))
            .Select(areaId => areaId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        profile.MiniGamesCompleted = Math.Max(0, profile.MiniGamesCompleted);
        profile.LauraChapterReplays = Math.Max(0, profile.LauraChapterReplays);
    }

    public static bool IsMastered(ProfileData profile, string areaId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return !string.IsNullOrWhiteSpace(areaId)
            && profile.MasteredLauraAreas?.Contains(areaId, StringComparer.OrdinalIgnoreCase) == true;
    }

    public static ChapterMasteryResult RecordMasteredRun(ProfileData profile, string areaId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(areaId))
        {
            throw new ArgumentException("A chapter area ID is required.", nameof(areaId));
        }

        Normalize(profile);
        profile.MiniGamesCompleted += 1;

        string cleanAreaId = areaId.Trim();
        if (!IsMastered(profile, cleanAreaId))
        {
            profile.MasteredLauraAreas.Add(cleanAreaId);
            return ChapterMasteryResult.FirstMastery;
        }

        profile.LauraChapterReplays += 1;
        return ChapterMasteryResult.Replay;
    }
}
