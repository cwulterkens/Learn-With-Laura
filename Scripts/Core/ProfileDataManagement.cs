#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public enum ProgressResetScope
{
    Learning,
    Adventure,
    All
}

public sealed class ProfileTransferEnvelope
{
    public int TransferVersion { get; set; }
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public ProfileData? Profile { get; set; }
}

public sealed class ProfileTransferParseResult
{
    public bool Succeeded { get; init; }
    public ProfileData? Profile { get; init; }
    public string Error { get; init; } = "";
}

public sealed class LocalProfileActionResult
{
    public bool Succeeded { get; init; }
    public string Path { get; init; } = "";
    public string Error { get; init; } = "";
}

public static class ProfileDataManagement
{
    public const int CurrentTransferVersion = 1;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static ProfileTransferEnvelope CreateTransfer(
        ProfileData profile,
        DateTime? exportedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new ProfileTransferEnvelope
        {
            TransferVersion = CurrentTransferVersion,
            ExportedAtUtc = exportedAtUtc ?? DateTime.UtcNow,
            Profile = profile
        };
    }

    public static string SerializeTransfer(
        ProfileData profile,
        DateTime? exportedAtUtc = null)
    {
        return JsonSerializer.Serialize(CreateTransfer(profile, exportedAtUtc), WriteOptions);
    }

    public static ProfileData CloneProfile(ProfileData profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        string json = JsonSerializer.Serialize(profile, WriteOptions);
        return JsonSerializer.Deserialize<ProfileData>(json, ReadOptions)
            ?? throw new InvalidOperationException("The local profile could not be cloned safely.");
    }

    public static ProfileTransferParseResult ParseTransfer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Failure("The selected file is empty.");
        }

        try
        {
            ProfileTransferEnvelope? envelope = JsonSerializer.Deserialize<ProfileTransferEnvelope>(
                json,
                ReadOptions);
            if (envelope == null)
            {
                return Failure("The selected file does not contain a profile export.");
            }
            if (envelope.TransferVersion != CurrentTransferVersion)
            {
                return Failure(
                    $"Unsupported local transfer version {envelope.TransferVersion}. Expected version {CurrentTransferVersion}.");
            }
            if (envelope.Profile == null)
            {
                return Failure("The selected export does not contain learner progress.");
            }

            return new ProfileTransferParseResult
            {
                Succeeded = true,
                Profile = envelope.Profile
            };
        }
        catch (JsonException ex)
        {
            return Failure($"The selected file is not a valid Learn With Laura export: {ex.Message}");
        }
    }

    public static IReadOnlyList<string> GetRecentSubjectKeys(
        ProfileData profile,
        int maximum = 5)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return (profile.SubjectProgress ?? new Dictionary<string, SubjectProgress>())
            .Where(pair => pair.Value != null && pair.Value.TotalAttempted > 0)
            .OrderByDescending(pair => pair.Value.LastPlayedUtc)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, maximum))
            .Select(pair => pair.Key)
            .ToList();
    }

    public static void Reset(
        ProfileData profile,
        ProgressResetScope scope,
        int defaultMathTier)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.SubjectProgress ??= new Dictionary<string, SubjectProgress>();
        profile.OperationProgress ??= new Dictionary<string, OperationProgress>();
        (int removedCorrect, int removedAttempted) = GetRemovedQuestionTotals(profile, scope);

        if (scope is ProgressResetScope.Learning or ProgressResetScope.All)
        {
            ResetLearning(profile, defaultMathTier);
        }
        if (scope is ProgressResetScope.Adventure or ProgressResetScope.All)
        {
            ResetAdventure(profile);
        }

        if (scope == ProgressResetScope.All)
        {
            profile.TotalCorrect = 0;
            profile.TotalAttempted = 0;
            profile.UnlockedBadges = new List<string>();
            profile.UnlockedAwards = new List<string>();
            profile.PrairieStars = 0;
            profile.GameScore = 0;
            profile.LastPlayedSubject = "math";
            profile.SavedSession = new SavedSessionData();
        }
        else
        {
            // Preserve legacy totals that predate per-subject tracking while removing
            // only the question totals represented by the selected scope.
            profile.TotalCorrect = Math.Max(0, profile.TotalCorrect - removedCorrect);
            profile.TotalAttempted = Math.Max(0, profile.TotalAttempted - removedAttempted);
        }

        profile.LastUpdatedUtc = DateTime.UtcNow;
    }

    private static void ResetLearning(ProfileData profile, int defaultMathTier)
    {
        foreach (string subjectKey in profile.SubjectProgress.Keys
            .Where(key => !key.Equals("laura", StringComparison.OrdinalIgnoreCase))
            .ToList())
        {
            profile.SubjectProgress[subjectKey] = new SubjectProgress();
        }

        foreach (string operationKey in profile.OperationProgress.Keys.ToList())
        {
            profile.OperationProgress[operationKey] = new OperationProgress
            {
                Tier = Math.Max(0, defaultMathTier)
            };
        }

        profile.Outcomes = new LearningOutcomeCounts();
        profile.CorrectSinceMiniGame = 0;
        profile.CurrentStreak = 0;
        profile.BestStreak = 0;
        profile.SessionsCompleted = 0;
        profile.TotalLearningSeconds = 0;
    }

    private static void ResetAdventure(ProfileData profile)
    {
        profile.SubjectProgress["laura"] = new SubjectProgress();
        profile.MiniGamesCompleted = 0;
        profile.LauraChapterReplays = 0;
        profile.MasteredLauraAreas = new List<string>();
        profile.Journey = new JourneyProgress();
        profile.SavedSession ??= new SavedSessionData();
        if (profile.SavedSession.ScenePath == "res://Scenes/LauraMiniGame.tscn")
        {
            profile.SavedSession = new SavedSessionData();
        }
        else
        {
            profile.SavedSession.LauraAreaId = "";
            profile.SavedSession.LauraQuestionsAnswered = 0;
            profile.SavedSession.LauraCorrectAnswers = 0;
            profile.SavedSession.LauraActivitiesCompleted = 0;
            profile.SavedSession.LauraFreePlay = false;
        }
        if (profile.LastPlayedSubject == "laura")
        {
            profile.LastPlayedSubject = "math";
        }
    }

    private static (int Correct, int Attempted) GetRemovedQuestionTotals(
        ProfileData profile,
        ProgressResetScope scope)
    {
        IEnumerable<KeyValuePair<string, SubjectProgress>> selected = scope switch
        {
            ProgressResetScope.Learning => profile.SubjectProgress
                .Where(pair => !pair.Key.Equals("laura", StringComparison.OrdinalIgnoreCase)),
            ProgressResetScope.Adventure => profile.SubjectProgress
                .Where(pair => pair.Key.Equals("laura", StringComparison.OrdinalIgnoreCase)),
            _ => profile.SubjectProgress
        };

        return (
            selected.Where(pair => pair.Value != null)
                .Sum(pair => Math.Max(0, pair.Value.TotalCorrect)),
            selected.Where(pair => pair.Value != null)
                .Sum(pair => Math.Max(0, pair.Value.TotalAttempted)));
    }

    private static ProfileTransferParseResult Failure(string error)
    {
        return new ProfileTransferParseResult { Error = error };
    }
}
