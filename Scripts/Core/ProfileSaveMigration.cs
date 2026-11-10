#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public enum ProfileMigrationStatus
{
    UpToDate,
    Migrated,
    UnsupportedVersion,
    Invalid
}

public sealed class ProfileMigrationResult
{
    public ProfileMigrationStatus Status { get; init; }
    public ProfileData? Profile { get; init; }
    public int OriginalVersion { get; init; }
    public int FinalVersion { get; init; }
    public string Error { get; init; } = "";
    public bool Succeeded => Status is ProfileMigrationStatus.UpToDate
        or ProfileMigrationStatus.Migrated;
    public bool RequiresSave => Status == ProfileMigrationStatus.Migrated;
}

public static class ProfileSaveMigration
{
    // 0: historical saves with no explicit version
    // 1: outcome-aware learning progress
    // 2: journey, mastery, and resumable-session progress
    public const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProfileMigrationResult Migrate(ProfileData source)
    {
        ArgumentNullException.ThrowIfNull(source);

        ProfileData profile;
        try
        {
            string json = JsonSerializer.Serialize(source);
            profile = JsonSerializer.Deserialize<ProfileData>(json, CloneOptions)
                ?? throw new InvalidOperationException("Profile JSON contained null.");
        }
        catch (Exception ex)
        {
            return Failure(ProfileMigrationStatus.Invalid, source.SchemaVersion,
                $"The profile could not be copied safely for migration: {ex.Message}");
        }

        int originalVersion = profile.SchemaVersion;
        if (originalVersion < 0)
        {
            return Failure(ProfileMigrationStatus.Invalid, originalVersion,
                $"Profile schema version {originalVersion} is invalid.");
        }
        if (originalVersion > CurrentVersion)
        {
            return Failure(ProfileMigrationStatus.UnsupportedVersion, originalVersion,
                $"This profile uses save schema {originalVersion}, but this build supports through schema {CurrentVersion}. The file was not changed.");
        }

        try
        {
            while (profile.SchemaVersion < CurrentVersion)
            {
                switch (profile.SchemaVersion)
                {
                    case 0:
                        MigrateUnversionedToOutcomeAware(profile);
                        break;
                    case 1:
                        MigrateOutcomeAwareToJourneyAware(profile);
                        break;
                    default:
                        return Failure(ProfileMigrationStatus.Invalid, originalVersion,
                            $"No migration is registered from profile schema {profile.SchemaVersion}.");
                }
            }
        }
        catch (Exception ex)
        {
            return Failure(ProfileMigrationStatus.Invalid, originalVersion,
                $"Profile migration stopped safely: {ex.Message}");
        }

        if (!TryValidateCurrent(profile, out string validationError))
        {
            return Failure(ProfileMigrationStatus.Invalid, originalVersion,
                $"The migrated profile did not pass validation: {validationError}");
        }

        return new ProfileMigrationResult
        {
            Status = originalVersion == CurrentVersion
                ? ProfileMigrationStatus.UpToDate
                : ProfileMigrationStatus.Migrated,
            Profile = profile,
            OriginalVersion = originalVersion,
            FinalVersion = profile.SchemaVersion
        };
    }

    public static bool TryValidateCurrent(ProfileData? profile, out string error)
    {
        if (profile == null)
        {
            error = "Profile data is missing.";
            return false;
        }
        if (profile.SchemaVersion != CurrentVersion)
        {
            error = $"Expected schema {CurrentVersion}, found {profile.SchemaVersion}.";
            return false;
        }
        if (profile.CurrentGrade is < 1 or > 8)
        {
            error = "Current grade must be between 1 and 8.";
            return false;
        }
        if (profile.OperationProgress == null || profile.SubjectProgress == null
            || profile.UnlockedBadges == null || profile.UnlockedAwards == null
            || profile.MasteredLauraAreas == null || profile.Outcomes == null
            || profile.Journey == null || profile.SavedSession == null)
        {
            error = "A required profile collection or progress section is missing.";
            return false;
        }
        if (HasNegativeProfileCounter(profile))
        {
            error = "Profile totals cannot be negative.";
            return false;
        }
        if (!ValidOutcomes(profile.Outcomes))
        {
            error = "Profile learning outcomes cannot be negative.";
            return false;
        }

        foreach ((string key, OperationProgress? progress) in profile.OperationProgress)
        {
            if (string.IsNullOrWhiteSpace(key) || progress == null || progress.Outcomes == null)
            {
                error = "Operation progress contains an invalid entry.";
                return false;
            }
            if (progress.Tier < 0 || progress.WindowFirstTryCorrect < 0
                || progress.WindowCorrect < 0 || progress.WindowAttempted < 0
                || progress.WindowFirstTryCorrect > progress.WindowAttempted
                || !ValidOutcomes(progress.Outcomes))
            {
                error = $"Operation progress for '{key}' contains invalid counters.";
                return false;
            }
        }

        foreach ((string key, SubjectProgress? progress) in profile.SubjectProgress)
        {
            if (string.IsNullOrWhiteSpace(key) || progress == null || progress.Outcomes == null)
            {
                error = "Subject progress contains an invalid entry.";
                return false;
            }
            if (progress.TotalCorrect < 0 || progress.TotalAttempted < 0
                || progress.TotalCorrect > progress.TotalAttempted
                || !ValidOutcomes(progress.Outcomes))
            {
                error = $"Subject progress for '{key}' contains invalid counters.";
                return false;
            }
        }

        JourneyProgress journey = profile.Journey;
        if (journey.Day < 1 || journey.Miles < 0 || journey.Food < 0 || journey.Wood < 0
            || journey.Health is < 0 or > JourneyResources.MaxHealth
            || journey.Morale is < 0 or > JourneyResources.MaxMorale
            || journey.LessonsAnswered < 0 || journey.FamilyActivitiesCompleted < 0
            || string.IsNullOrWhiteSpace(journey.CurrentRegion))
        {
            error = "Journey progress is outside its supported range.";
            return false;
        }

        error = "";
        return true;
    }

    private static void MigrateUnversionedToOutcomeAware(ProfileData profile)
    {
        NormalizeLearningProgress(profile);
        profile.SchemaVersion = 1;
    }

    private static void MigrateOutcomeAwareToJourneyAware(ProfileData profile)
    {
        NormalizeLearningProgress(profile);
        profile.CurrentGrade = Math.Clamp(profile.CurrentGrade, 1, 8);
        profile.UnlockedBadges ??= new List<string>();
        profile.UnlockedAwards ??= new List<string>();
        profile.MasteredLauraAreas ??= new List<string>();
        AdventureProgress.Normalize(profile);
        profile.Journey ??= new JourneyProgress();
        JourneyResources.Normalize(profile.Journey);
        profile.SavedSession ??= new SavedSessionData();
        profile.LastPlayedSubject = string.IsNullOrWhiteSpace(profile.LastPlayedSubject)
            ? "math"
            : profile.LastPlayedSubject.Trim();
        profile.PrairieStars = Math.Max(0, profile.PrairieStars);
        profile.CurrentStreak = Math.Max(0, profile.CurrentStreak);
        profile.BestStreak = Math.Max(profile.CurrentStreak, profile.BestStreak);
        profile.TotalLearningSeconds = Math.Max(0, profile.TotalLearningSeconds);
        profile.SchemaVersion = 2;
    }

    private static void NormalizeLearningProgress(ProfileData profile)
    {
        profile.OperationProgress ??= new Dictionary<string, OperationProgress>();
        profile.SubjectProgress ??= new Dictionary<string, SubjectProgress>();
        profile.Outcomes ??= new LearningOutcomeCounts();
        NormalizeOutcomes(profile.Outcomes);

        foreach (string key in profile.OperationProgress.Keys.ToList())
        {
            OperationProgress progress = profile.OperationProgress[key] ?? new OperationProgress();
            profile.OperationProgress[key] = progress;
            progress.Tier = Math.Max(0, progress.Tier);
            progress.Outcomes ??= new LearningOutcomeCounts();
            NormalizeOutcomes(progress.Outcomes);
            progress.WindowFirstTryCorrect = Math.Max(0, progress.WindowFirstTryCorrect);
            progress.WindowCorrect = Math.Max(0, progress.WindowCorrect);
            progress.WindowAttempted = Math.Max(0, progress.WindowAttempted);
            if (progress.WindowFirstTryCorrect > progress.WindowAttempted)
            {
                progress.WindowFirstTryCorrect = progress.WindowAttempted;
            }
            AdaptiveMastery.ResetAmbiguousLegacyWindow(progress);
        }

        foreach (string key in profile.SubjectProgress.Keys.ToList())
        {
            SubjectProgress progress = profile.SubjectProgress[key] ?? new SubjectProgress();
            profile.SubjectProgress[key] = progress;
            progress.TotalCorrect = Math.Max(0, progress.TotalCorrect);
            progress.TotalAttempted = Math.Max(progress.TotalCorrect, progress.TotalAttempted);
            progress.Outcomes ??= new LearningOutcomeCounts();
            NormalizeOutcomes(progress.Outcomes);
        }

        profile.TotalCorrect = Math.Max(0, profile.TotalCorrect);
        profile.TotalAttempted = Math.Max(profile.TotalCorrect, profile.TotalAttempted);
        profile.GameScore = Math.Max(0, profile.GameScore);
        profile.SessionsCompleted = Math.Max(0, profile.SessionsCompleted);
        profile.CorrectSinceMiniGame = Math.Max(0, profile.CorrectSinceMiniGame);
        profile.MiniGamesCompleted = Math.Max(0, profile.MiniGamesCompleted);
    }

    private static void NormalizeOutcomes(LearningOutcomeCounts outcomes)
    {
        outcomes.FirstTryCorrect = Math.Max(0, outcomes.FirstTryCorrect);
        outcomes.HintAssistedCorrect = Math.Max(0, outcomes.HintAssistedCorrect);
        outcomes.Corrected = Math.Max(0, outcomes.Corrected);
        outcomes.Skipped = Math.Max(0, outcomes.Skipped);
    }

    private static bool ValidOutcomes(LearningOutcomeCounts outcomes)
    {
        return outcomes.FirstTryCorrect >= 0
            && outcomes.HintAssistedCorrect >= 0
            && outcomes.Corrected >= 0
            && outcomes.Skipped >= 0;
    }

    private static bool HasNegativeProfileCounter(ProfileData profile)
    {
        return profile.TotalCorrect < 0 || profile.TotalAttempted < 0
            || profile.TotalCorrect > profile.TotalAttempted
            || profile.GameScore < 0 || profile.SessionsCompleted < 0
            || profile.CorrectSinceMiniGame < 0 || profile.MiniGamesCompleted < 0
            || profile.LauraChapterReplays < 0 || profile.PrairieStars < 0
            || profile.CurrentStreak < 0 || profile.BestStreak < profile.CurrentStreak
            || profile.TotalLearningSeconds < 0;
    }

    private static ProfileMigrationResult Failure(
        ProfileMigrationStatus status,
        int originalVersion,
        string error)
    {
        return new ProfileMigrationResult
        {
            Status = status,
            OriginalVersion = originalVersion,
            FinalVersion = originalVersion,
            Error = error
        };
    }
}
