using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class AppState
{
    public static readonly Dictionary<string, Vector2I> SupportedResolutions = new()
    {
        { "1024x768", new Vector2I(1024, 768) },
        { "1280x720", new Vector2I(1280, 720) },
        { "1280x800", new Vector2I(1280, 800) },
        { "1280x960", new Vector2I(1280, 960) },
        { "1366x768", new Vector2I(1366, 768) },
        { "1440x900", new Vector2I(1440, 900) },
        { "1536x1024", new Vector2I(1536, 1024) },
        { "1600x900", new Vector2I(1600, 900) },
        { "1680x1050", new Vector2I(1680, 1050) },
        { "1920x1080", new Vector2I(1920, 1080) }
    };

    public const string UserConfigPath = "user://config/game_settings.json";
    public const string UserDifficultyPath = "user://config/math_difficulty.json";
    public const string UserQuestionPath = "user://content/math_questions.json";
    public const string UserSubjectQuestionPath = "user://content/subject_questions.json";
    public const string UserLauraMiniGamePath = "user://content/laura_minigame.json";
    public const string UserBadgePath = "user://content/badges.json";

    public const string ResConfigPath = "res://config/game_settings.json";
    public const string ResDifficultyPath = "res://config/math_difficulty.json";
    public const string ResQuestionPath = "res://content/math_questions.json";
    public const string ResSubjectQuestionPath = "res://content/subject_questions.json";
    public const string ResLauraMiniGamePath = "res://content/laura_minigame.json";
    public const string ResBadgePath = "res://content/badges.json";

    public static bool Initialized { get; private set; }
    public static GameSettings Settings { get; private set; } = new();
    public static MathDifficultyConfig Difficulty { get; private set; } = new();
    public static MathQuestionContent QuestionContent { get; private set; } = new();
    public static SubjectQuestionContent SubjectQuestionContent { get; private set; } = new();
    public static LauraMiniGameContent LauraMiniGameContent { get; private set; } = new();
    public static BadgeCatalog Badges { get; private set; } = new();

    public static string PendingSceneAfterProfile { get; set; } = "";
    public static bool PendingLauraFreePlay { get; set; }
    public static int CurrentGrade { get; set; } = 1;
    public static string CurrentSubjectKey { get; set; } = "math";

    public static ProfileData ActiveProfile { get; private set; }

    public static void Initialize()
    {
        if (Initialized)
        {
            return;
        }

        EnsureDefaultDataFiles();
        ReloadData();
        LoadActiveProfileFromSettings();

        Initialized = true;
    }

    public static void ReloadData()
    {
        Settings = JsonStorage.LoadOrDefault(UserConfigPath, new GameSettings());
        bool settingsMigrated = MigrateSettings();
        Difficulty = JsonStorage.LoadOrDefault(UserDifficultyPath, new MathDifficultyConfig());
        QuestionContent = JsonStorage.LoadOrDefault(UserQuestionPath, new MathQuestionContent());
        SubjectQuestionContent = JsonStorage.LoadOrDefault(UserSubjectQuestionPath, new SubjectQuestionContent());
        LauraMiniGameContent = JsonStorage.LoadOrDefault(UserLauraMiniGamePath, new LauraMiniGameContent());
        Badges = JsonStorage.LoadOrDefault(UserBadgePath, new BadgeCatalog());
        if (string.IsNullOrWhiteSpace(Settings.UiFontPath))
        {
            Settings.UiFontPath = "res://assets/fonts/AtkinsonHyperlegible-Regular.otf";
        }
        NormalizeCatalogs();
        MergeDefaultCatalogs();

        if (settingsMigrated)
        {
            JsonStorage.Save(UserConfigPath, Settings);
        }

        ApplyWindowSettings();
    }

    private static bool MigrateSettings()
    {
        bool changed = false;
        if (Settings.SettingsVersion < 2)
        {
            Settings.SettingsVersion = 2;
            Settings.CorrectAnswersForMiniGame = 5;
            Settings.UiFontPath = "res://assets/fonts/AtkinsonHyperlegible-Regular.otf";
            changed = true;
        }

        if (Settings.SettingsVersion < 3)
        {
            Settings.SettingsVersion = 3;
            Settings.AuthoredQuestionChance = 0.65;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(Settings.UiFontPath)
            || !ResourceLoader.Exists(Settings.UiFontPath))
        {
            Settings.UiFontPath = "res://assets/fonts/AtkinsonHyperlegible-Regular.otf";
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(Settings.Resolution)
            || !SupportedResolutions.ContainsKey(Settings.Resolution))
        {
            Settings.Resolution = "1536x1024";
            changed = true;
        }

        int sessionMinutes = Math.Clamp(Settings.SessionMinutes, 5, 90);
        int evaluationQuestions = Math.Clamp(Settings.QuestionsPerEvaluation, 3, 30);
        int adventureTarget = Math.Clamp(Settings.CorrectAnswersForMiniGame, 1, 20);
        double authoredChance = Math.Clamp(Settings.AuthoredQuestionChance, 0.0, 1.0);
        float musicVolume = Math.Clamp(Settings.MusicVolume, 0f, 1f);
        float sfxVolume = Math.Clamp(Settings.SfxVolume, 0f, 1f);

        changed |= sessionMinutes != Settings.SessionMinutes
            || evaluationQuestions != Settings.QuestionsPerEvaluation
            || adventureTarget != Settings.CorrectAnswersForMiniGame
            || authoredChance != Settings.AuthoredQuestionChance
            || musicVolume != Settings.MusicVolume
            || sfxVolume != Settings.SfxVolume;
        Settings.SessionMinutes = sessionMinutes;
        Settings.QuestionsPerEvaluation = evaluationQuestions;
        Settings.CorrectAnswersForMiniGame = adventureTarget;
        Settings.AuthoredQuestionChance = authoredChance;
        Settings.MusicVolume = musicVolume;
        Settings.SfxVolume = sfxVolume;

        if (Settings.DemoteAccuracy < 0.0
            || Settings.PromoteAccuracy > 1.0
            || Settings.DemoteAccuracy >= Settings.PromoteAccuracy)
        {
            Settings.DemoteAccuracy = 0.50;
            Settings.PromoteAccuracy = 0.85;
            changed = true;
        }

        return changed;
    }

    public static void SaveSettings()
    {
        JsonStorage.Save(UserConfigPath, Settings);
        ApplyWindowSettings();
    }

    public static void ApplyWindowSettings()
    {
        Vector2I requestedSize = GetConfiguredResolution();
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, false);

        if (Settings.Fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            Rect2I usableScreen = DisplayServer.ScreenGetUsableRect();
            int maximumWidth = Math.Max(640, usableScreen.Size.X - 48);
            int maximumHeight = Math.Max(600, usableScreen.Size.Y - 72);
            float fitScale = Math.Min(
                1f,
                Math.Min(
                    (float)maximumWidth / requestedSize.X,
                    (float)maximumHeight / requestedSize.Y));
            Vector2I fittedSize = new(
                Math.Max(640, Mathf.RoundToInt(requestedSize.X * fitScale)),
                Math.Max(600, Mathf.RoundToInt(requestedSize.Y * fitScale)));

            DisplayServer.WindowSetSize(fittedSize);
            DisplayServer.WindowSetPosition(
                usableScreen.Position + (usableScreen.Size - fittedSize) / 2);
        }

        AudioServer.SetBusMute(0, Settings.MasterMute);
    }

    public static Vector2I GetConfiguredResolution()
    {
        if (string.IsNullOrWhiteSpace(Settings.Resolution)
            || !SupportedResolutions.TryGetValue(Settings.Resolution, out Vector2I size))
        {
            Settings.Resolution = "1536x1024";
            return SupportedResolutions[Settings.Resolution];
        }

        return size;
    }

    public static float ToDb(float linear)
    {
        if (linear <= 0f)
        {
            return -80f;
        }

        return Mathf.LinearToDb(Mathf.Clamp(linear, 0f, 1f));
    }

    public static void ConfigureMusicPlayer(AudioStreamPlayer player)
    {
        player.VolumeDb = Settings.MasterMute ? -80f : ToDb(Settings.MusicVolume);
    }

    public static void ConfigureSfxPlayer(AudioStreamPlayer player)
    {
        player.VolumeDb = Settings.MasterMute ? -80f : ToDb(Settings.SfxVolume);
    }

    public static List<string> GetProfileNames()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!DirAccess.DirExistsAbsolute("user://profiles"))
        {
            return result.ToList();
        }

        using var dir = DirAccess.Open("user://profiles");
        if (dir == null)
        {
            return result.ToList();
        }

        foreach (string file in dir.GetFiles())
        {
            if (file.EndsWith(".json.bak", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(file[..^9]);
            }
            else if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(file[..^5]);
            }
        }

        return result.OrderBy(x => x).ToList();
    }

    public static bool ProfileExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string clean = SanitizeName(name);
        return GetProfileNames().Any(existing =>
            string.Equals(existing, clean, StringComparison.OrdinalIgnoreCase));
    }

    public static bool DeleteProfile(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return false;
        }

        string clean = SanitizeName(firstName);
        string path = GetProfilePath(clean);
        string profileFileName = path.GetFile();
        bool found = false;
        bool allRemoved = true;

        using var dir = DirAccess.Open(path.GetBaseDir());
        if (dir == null)
        {
            return false;
        }

        foreach (string file in dir.GetFiles())
        {
            if (!file.Equals(profileFileName, StringComparison.OrdinalIgnoreCase)
                && !file.StartsWith($"{profileFileName}.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            found = true;
            string artifactPath = path.GetBaseDir().PathJoin(file);
            Error artifactError = DirAccess.RemoveAbsolute(artifactPath);
            if (artifactError != Error.Ok)
            {
                GD.PrintErr($"Failed to remove profile recovery file {artifactPath}: {artifactError}");
                allRemoved = false;
            }
        }

        if (!found || !allRemoved)
        {
            return false;
        }

        if (ActiveProfile != null && ActiveProfile.FirstName == clean)
        {
            ActiveProfile = null;
            Settings.ActiveProfileName = "";
            SaveSettings();
        }

        return true;
    }

    public static ProfileActivationResult TryActivateProfile(
        string firstName,
        ProfileActivationChoice choice = ProfileActivationChoice.Normal)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return new ProfileActivationResult { Status = ProfileActivationStatus.InvalidName };
        }

        string clean = SanitizeName(firstName);
        string path = GetProfilePath(clean);
        JsonRecoveryInspection<ProfileData> inspection = JsonStorage.InspectUserFile<ProfileData>(path);
        JsonRecoveryDecision decision = JsonRecoveryPolicy.Decide(
            inspection.PrimaryStatus,
            inspection.BackupStatus);

        ProfileData profile = null!;
        ProfileActivationStatus successStatus = ProfileActivationStatus.Activated;
        if (choice == ProfileActivationChoice.RestoreBackup)
        {
            if (inspection.BackupStatus != JsonFileStatus.Valid || inspection.BackupValue == null)
            {
                return new ProfileActivationResult
                {
                    Status = ProfileActivationStatus.Corrupt,
                    ProfileName = clean,
                    Error = inspection.BackupError
                };
            }

            profile = inspection.BackupValue;
            successStatus = ProfileActivationStatus.RecoveredBackup;
        }
        else if (choice == ProfileActivationChoice.StartFresh)
        {
            profile = new ProfileData { FirstName = clean };
            successStatus = ProfileActivationStatus.Created;
        }
        else
        {
            switch (decision)
            {
                case JsonRecoveryDecision.UsePrimary:
                    profile = inspection.PrimaryValue;
                    break;
                case JsonRecoveryDecision.OfferBackupRestore:
                    return new ProfileActivationResult
                    {
                        Status = ProfileActivationStatus.BackupAvailable,
                        ProfileName = clean,
                        Error = inspection.PrimaryError
                    };
                case JsonRecoveryDecision.OfferFreshStart:
                    return new ProfileActivationResult
                    {
                        Status = ProfileActivationStatus.Corrupt,
                        ProfileName = clean,
                        Error = string.IsNullOrWhiteSpace(inspection.PrimaryError)
                            ? inspection.BackupError
                            : inspection.PrimaryError
                    };
                default:
                    return new ProfileActivationResult
                    {
                        Status = ProfileActivationStatus.NotFound,
                        ProfileName = clean
                    };
            }
        }

        if (profile == null)
        {
            return new ProfileActivationResult
            {
                Status = ProfileActivationStatus.Corrupt,
                ProfileName = clean,
                Error = "Profile data could not be read."
            };
        }

        PrepareProfile(profile, clean);

        int previousLastGrade = Settings.LastSelectedGrade;
        int grade = ProfileSelectionRules.ActivateGrade(profile, Settings);
        profile.LastUpdatedUtc = DateTime.UtcNow;
        if (!JsonStorage.Save(path, profile))
        {
            Settings.LastSelectedGrade = previousLastGrade;
            return new ProfileActivationResult
            {
                Status = ProfileActivationStatus.SaveFailed,
                ProfileName = clean,
                Error = "The profile could not be saved safely."
            };
        }

        ActiveProfile = profile;
        Settings.ActiveProfileName = clean;
        CurrentGrade = grade;
        CurrentSubjectKey = profile.LastPlayedSubject;
        SaveSettings();

        return new ProfileActivationResult
        {
            Status = successStatus,
            ProfileName = clean
        };
    }

    public static bool SaveProfile()
    {
        if (ActiveProfile == null)
        {
            return false;
        }

        ActiveProfile.LastUpdatedUtc = DateTime.UtcNow;
        return JsonStorage.Save(GetProfilePath(ActiveProfile.FirstName), ActiveProfile);
    }

    public static LocalProfileActionResult CreateActiveProfileBackup()
    {
        if (ActiveProfile == null)
        {
            return ActionFailure("Select a local learner before creating a backup.");
        }

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        string path = $"user://profile_backups/{SanitizeName(ActiveProfile.DisplayName)}-{timestamp}.lwl-profile.json";
        ProfileTransferEnvelope transfer = ProfileDataManagement.CreateTransfer(ActiveProfile);
        return JsonStorage.Save(path, transfer)
            ? new LocalProfileActionResult { Succeeded = true, Path = path }
            : ActionFailure("The local backup could not be written safely.");
    }

    public static LocalProfileActionResult ExportActiveProfile(string path)
    {
        if (ActiveProfile == null)
        {
            return ActionFailure("Select a local learner before exporting progress.");
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            return ActionFailure("Choose a local destination for the export.");
        }

        string exportPath = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"{path}.json";
        string json = ProfileDataManagement.SerializeTransfer(ActiveProfile);
        using var file = FileAccess.Open(exportPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            return ActionFailure($"The export could not be opened for writing ({FileAccess.GetOpenError()}).");
        }

        file.StoreString(json);
        file.Flush();
        if (file.GetError() != Error.Ok)
        {
            return ActionFailure("The export could not be written completely.");
        }

        return new LocalProfileActionResult { Succeeded = true, Path = exportPath };
    }

    public static LocalProfileActionResult ImportActiveProfile(string path)
    {
        if (ActiveProfile == null)
        {
            return ActionFailure("Select the local learner that should receive the imported progress.");
        }
        if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
        {
            return ActionFailure("The selected local import file does not exist.");
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return ActionFailure($"The import could not be opened ({FileAccess.GetOpenError()}).");
        }

        ProfileTransferParseResult parsed = ProfileDataManagement.ParseTransfer(file.GetAsText());
        if (!parsed.Succeeded || parsed.Profile == null)
        {
            return ActionFailure(parsed.Error);
        }

        LocalProfileActionResult backup = CreateActiveProfileBackup();
        if (!backup.Succeeded)
        {
            return ActionFailure($"Import stopped because the current progress could not be backed up. {backup.Error}");
        }

        string nickname = ActiveProfile.DisplayName;
        ProfileData imported = parsed.Profile;
        PrepareProfile(imported, nickname);
        int previousLastGrade = Settings.LastSelectedGrade;
        int grade = ProfileSelectionRules.ActivateGrade(imported, Settings);
        imported.LastUpdatedUtc = DateTime.UtcNow;
        if (!JsonStorage.Save(GetProfilePath(nickname), imported))
        {
            Settings.LastSelectedGrade = previousLastGrade;
            return ActionFailure("The imported progress could not be installed safely; the current profile was left active.");
        }

        ActiveProfile = imported;
        Settings.ActiveProfileName = nickname;
        CurrentGrade = grade;
        CurrentSubjectKey = string.IsNullOrWhiteSpace(imported.LastPlayedSubject)
            ? "math"
            : imported.LastPlayedSubject;
        SaveSettings();
        return new LocalProfileActionResult
        {
            Succeeded = true,
            Path = backup.Path
        };
    }

    public static LocalProfileActionResult ResetActiveProfile(ProgressResetScope scope)
    {
        if (ActiveProfile == null)
        {
            return ActionFailure("Select a local learner before resetting progress.");
        }

        LocalProfileActionResult backup = CreateActiveProfileBackup();
        if (!backup.Succeeded)
        {
            return ActionFailure($"Reset stopped because a safety backup could not be created. {backup.Error}");
        }

        ProfileData original = ProfileDataManagement.CloneProfile(ActiveProfile);
        string previousSubjectKey = CurrentSubjectKey;
        ProfileDataManagement.Reset(
            ActiveProfile,
            scope,
            GetDefaultTierForGrade(ActiveProfile.CurrentGrade));
        EnsureOperationKeys(ActiveProfile);
        EnsureSubjectKeys(ActiveProfile);
        if (scope == ProgressResetScope.All
            || (scope == ProgressResetScope.Adventure && CurrentSubjectKey == "laura"))
        {
            CurrentSubjectKey = "math";
        }

        if (!SaveProfile())
        {
            ActiveProfile = original;
            CurrentSubjectKey = previousSubjectKey;
            return ActionFailure("The reset could not be saved; a safety backup is available.");
        }

        return new LocalProfileActionResult
        {
            Succeeded = true,
            Path = backup.Path
        };
    }

    public static OperationProgress GetOperationProgress(string operation)
    {
        if (ActiveProfile == null)
        {
            throw new InvalidOperationException("Active profile is required.");
        }

        if (!ActiveProfile.OperationProgress.TryGetValue(operation, out var value))
        {
            value = new OperationProgress { Tier = GetDefaultTierForGrade(CurrentGrade) };
            ActiveProfile.OperationProgress[operation] = value;
        }

        value.Outcomes ??= new LearningOutcomeCounts();

        return value;
    }

    public static SubjectProgress GetSubjectProgress(string subjectKey)
    {
        if (ActiveProfile == null)
        {
            throw new InvalidOperationException("Active profile is required.");
        }

        if (!ActiveProfile.SubjectProgress.TryGetValue(subjectKey, out var value))
        {
            value = new SubjectProgress();
            ActiveProfile.SubjectProgress[subjectKey] = value;
        }

        value.Outcomes ??= new LearningOutcomeCounts();

        return value;
    }

    public static void UnlockEligibleBadges()
    {
        if (ActiveProfile == null)
        {
            return;
        }

        foreach (BadgeDefinition badge in Badges.Badges)
        {
            if (ActiveProfile.UnlockedBadges.Contains(badge.Id))
            {
                continue;
            }

            int value = badge.Metric switch
            {
                "total_correct" => ActiveProfile.TotalCorrect,
                "total_attempted" => ActiveProfile.TotalAttempted,
                "sessions_completed" => ActiveProfile.SessionsCompleted,
                "game_score" => ActiveProfile.GameScore,
                "mini_games_completed" => ActiveProfile.MiniGamesCompleted,
                "unique_chapters_mastered" => ActiveProfile.MasteredLauraAreas?.Count ?? 0,
                "journey_miles" => ActiveProfile.Journey?.Miles ?? 0,
                "family_activities" => ActiveProfile.Journey?.FamilyActivitiesCompleted ?? 0,
                "prairie_stars" => ActiveProfile.PrairieStars,
                "best_streak" => ActiveProfile.BestStreak,
                "math_correct" => ActiveProfile.SubjectProgress.GetValueOrDefault("math")?.TotalCorrect ?? 0,
                _ => -1
            };

            if (value >= badge.Threshold)
            {
                ActiveProfile.UnlockedBadges.Add(badge.Id);
                if (!ActiveProfile.UnlockedAwards.Contains(badge.Name))
                {
                    ActiveProfile.UnlockedAwards.Add(badge.Name);
                }
            }
        }
    }

    public static string[] GetEnabledOperations(int grade)
    {
        if (!Difficulty.Grades.TryGetValue(grade.ToString(), out var gradeConfig))
        {
            gradeConfig = Difficulty.Grades.GetValueOrDefault("1", new GradeConfig());
        }

        return gradeConfig.Operations
            .Where(pair => pair.Value.Enabled)
            .Select(pair => pair.Key)
            .ToArray();
    }

    public static OperationConfig GetOperationConfig(int grade, string operation)
    {
        if (Difficulty.Grades.TryGetValue(grade.ToString(), out var gradeConfig)
            && gradeConfig.Operations.TryGetValue(operation, out var operationConfig))
        {
            return operationConfig;
        }

        if (Difficulty.Grades.TryGetValue("1", out var defaultGrade)
            && defaultGrade.Operations.TryGetValue(operation, out var fallback))
        {
            return fallback;
        }

        return new OperationConfig();
    }

    public static void SetCurrentSubject(string subjectKey)
    {
        CurrentSubjectKey = string.IsNullOrWhiteSpace(subjectKey) ? "math" : subjectKey;

        if (ActiveProfile != null)
        {
            ActiveProfile.LastPlayedSubject = CurrentSubjectKey;
        }
    }

    public static void SaveResumePoint(string scenePath, string displayLabel)
    {
        if (ActiveProfile == null)
        {
            return;
        }

        EnsureSavedSession(ActiveProfile);
        ActiveProfile.SavedSession.ScenePath = scenePath;
        ActiveProfile.SavedSession.SubjectKey = CurrentSubjectKey;
        ActiveProfile.SavedSession.DisplayLabel = displayLabel;
    }

    public static void ClearResumePoint()
    {
        if (ActiveProfile == null)
        {
            return;
        }

        ActiveProfile.SavedSession = new SavedSessionData();
    }

    public static bool HasResumePoint()
    {
        return ActiveProfile?.SavedSession?.HasResumePoint == true;
    }

    public static string GetResumeLabel()
    {
        if (!HasResumePoint())
        {
            return "Continue";
        }

        return string.IsNullOrWhiteSpace(ActiveProfile!.SavedSession.DisplayLabel)
            ? "Continue"
            : ActiveProfile.SavedSession.DisplayLabel;
    }

    public static string GetSubjectDisplayName(string subjectKey)
    {
        return subjectKey switch
        {
            "math" => "Math",
            "social_studies" => "Social Studies",
            "science" => "Science",
            "history" => "History",
            "geography" => "Geography",
            "english" => "English",
            "laura" => "Laura's Adventure",
            _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                subjectKey.Replace('_', ' '))
        };
    }

    public static string GetCanonDisplayName(string canon)
    {
        return canon switch
        {
            "actual_life" => "Actual Life",
            "book" => "Little House Books",
            "tv_1974" => "Classic TV Adaptation",
            "inspired" => "Imagined Prairie Scenario",
            _ => "School Concept"
        };
    }

    public static LauraAreaDefinition GetLauraArea(string areaId)
    {
        return LauraMiniGameContent.Areas.FirstOrDefault(area => area.Id == areaId);
    }

    public static bool HasMiniGameReady()
    {
        return ActiveProfile != null
            && ActiveProfile.CorrectSinceMiniGame >= Settings.CorrectAnswersForMiniGame;
    }

    public static void ConsumeMiniGameProgress()
    {
        if (ActiveProfile == null)
        {
            return;
        }

        ActiveProfile.CorrectSinceMiniGame = Math.Max(
            0,
            ActiveProfile.CorrectSinceMiniGame - Settings.CorrectAnswersForMiniGame);
    }

    private static void EnsureDefaultDataFiles()
    {
        JsonStorage.CopyIfMissing(ResConfigPath, UserConfigPath);
        JsonStorage.CopyIfMissing(ResDifficultyPath, UserDifficultyPath);
        JsonStorage.CopyIfMissing(ResQuestionPath, UserQuestionPath);
        JsonStorage.CopyIfMissing(ResSubjectQuestionPath, UserSubjectQuestionPath);
        JsonStorage.CopyIfMissing(ResLauraMiniGamePath, UserLauraMiniGamePath);
        JsonStorage.CopyIfMissing(ResBadgePath, UserBadgePath);
        DirAccess.MakeDirRecursiveAbsolute("user://profiles");
    }

    private static void MergeDefaultCatalogs()
    {
        MathQuestionContent defaultMath = JsonStorage.LoadOrDefault(ResQuestionPath, new MathQuestionContent());
        if (defaultMath.ContentVersion > QuestionContent.ContentVersion)
        {
            QuestionContent = defaultMath;
            JsonStorage.Save(UserQuestionPath, QuestionContent);
        }

        foreach (var gradePair in defaultMath.Questions)
        {
            if (!QuestionContent.Questions.TryGetValue(gradePair.Key, out var operations))
            {
                QuestionContent.Questions[gradePair.Key] = gradePair.Value;
                continue;
            }

            foreach (var operationPair in gradePair.Value)
            {
                if (!operations.TryGetValue(operationPair.Key, out var questions))
                {
                    operations[operationPair.Key] = operationPair.Value;
                    continue;
                }

                foreach (AuthoredQuestion question in operationPair.Value)
                {
                    if (questions.All(existing => existing.Prompt != question.Prompt))
                    {
                        questions.Add(question);
                    }
                }
            }
        }

        SubjectQuestionContent defaultSubjects = JsonStorage.LoadOrDefault(ResSubjectQuestionPath, new SubjectQuestionContent());
        if (defaultSubjects.ContentVersion > SubjectQuestionContent.ContentVersion)
        {
            SubjectQuestionContent = defaultSubjects;
            JsonStorage.Save(UserSubjectQuestionPath, SubjectQuestionContent);
        }

        foreach (var subjectPair in defaultSubjects.Subjects)
        {
            if (!SubjectQuestionContent.Subjects.TryGetValue(subjectPair.Key, out var grades))
            {
                SubjectQuestionContent.Subjects[subjectPair.Key] = subjectPair.Value;
                continue;
            }

            foreach (var gradePair in subjectPair.Value)
            {
                if (!grades.TryGetValue(gradePair.Key, out var questions))
                {
                    grades[gradePair.Key] = gradePair.Value;
                    continue;
                }

                foreach (ChoiceQuestion question in gradePair.Value)
                {
                    if (questions.All(existing => existing.Prompt != question.Prompt))
                    {
                        questions.Add(question);
                    }
                }
            }
        }

        LauraMiniGameContent defaultMiniGames = JsonStorage.LoadOrDefault(ResLauraMiniGamePath, new LauraMiniGameContent());
        if (defaultMiniGames.ContentVersion > LauraMiniGameContent.ContentVersion)
        {
            LauraMiniGameContent = defaultMiniGames;
            JsonStorage.Save(UserLauraMiniGamePath, LauraMiniGameContent);
        }

        foreach (LauraAreaDefinition area in defaultMiniGames.Areas)
        {
            if (LauraMiniGameContent.Areas.All(existing => existing.Id != area.Id))
            {
                LauraMiniGameContent.Areas.Add(area);
            }
        }

        BadgeCatalog defaultBadges = JsonStorage.LoadOrDefault(ResBadgePath, new BadgeCatalog());
        if (defaultBadges.ContentVersion > Badges.ContentVersion)
        {
            Badges = defaultBadges;
            JsonStorage.Save(UserBadgePath, Badges);
        }

        foreach (BadgeDefinition badge in defaultBadges.Badges)
        {
            if (Badges.Badges.All(existing => existing.Id != badge.Id))
            {
                Badges.Badges.Add(badge);
            }
        }
    }

    private static void NormalizeCatalogs()
    {
        QuestionContent.Questions ??= new Dictionary<string, Dictionary<string, List<AuthoredQuestion>>>();
        SubjectQuestionContent.Subjects ??= new Dictionary<string, Dictionary<string, List<ChoiceQuestion>>>();
        LauraMiniGameContent.Areas ??= new List<LauraAreaDefinition>();
        Badges.Badges ??= new List<BadgeDefinition>();
    }

    private static void LoadActiveProfileFromSettings()
    {
        if (!string.IsNullOrWhiteSpace(Settings.ActiveProfileName))
        {
            TryActivateProfile(Settings.ActiveProfileName);
        }

        if (ActiveProfile != null)
        {
            CurrentGrade = Math.Clamp(ActiveProfile.CurrentGrade, 1, 8);
            CurrentSubjectKey = string.IsNullOrWhiteSpace(ActiveProfile.LastPlayedSubject)
                ? "math"
                : ActiveProfile.LastPlayedSubject;
        }
        else
        {
            CurrentGrade = Math.Clamp(Settings.LastSelectedGrade, 1, 8);
            CurrentSubjectKey = "math";
        }
    }

    private static string GetProfilePath(string firstName)
    {
        return $"user://profiles/{SanitizeName(firstName)}.json";
    }

    private static string SanitizeName(string value)
    {
        char[] chars = value.Trim().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private static void PrepareProfile(ProfileData profile, string nickname)
    {
        profile.FirstName = SanitizeName(nickname);
        profile.OperationProgress ??= new Dictionary<string, OperationProgress>();
        profile.SubjectProgress ??= new Dictionary<string, SubjectProgress>();
        profile.Outcomes ??= new LearningOutcomeCounts();
        profile.UnlockedBadges ??= new List<string>();
        profile.UnlockedAwards ??= new List<string>();
        AdventureProgress.Normalize(profile);
        profile.Journey ??= new JourneyProgress();
        JourneyResources.Normalize(profile.Journey);
        EnsureOperationKeys(profile);
        EnsureSubjectKeys(profile);
        EnsureSavedSession(profile);
    }

    private static LocalProfileActionResult ActionFailure(string error)
    {
        return new LocalProfileActionResult { Error = error };
    }

    private static void EnsureOperationKeys(ProfileData profile)
    {
        string[] keys = { "addition", "subtraction", "multiplication", "division" };
        foreach (string key in keys)
        {
            if (!profile.OperationProgress.ContainsKey(key))
            {
                profile.OperationProgress[key] = new OperationProgress { Tier = GetDefaultTierForGrade(profile.CurrentGrade) };
            }

            profile.OperationProgress[key].Outcomes ??= new LearningOutcomeCounts();
            AdaptiveMastery.ResetAmbiguousLegacyWindow(profile.OperationProgress[key]);
        }
    }

    private static void EnsureSubjectKeys(ProfileData profile)
    {
        foreach (string subject in new[] { "math", "science", "history", "social_studies", "geography", "english", "laura" })
        {
            if (!profile.SubjectProgress.ContainsKey(subject))
            {
                profile.SubjectProgress[subject] = new SubjectProgress();
            }

            profile.SubjectProgress[subject].Outcomes ??= new LearningOutcomeCounts();
        }
    }

    private static void EnsureSavedSession(ProfileData profile)
    {
        profile.SavedSession ??= new SavedSessionData();
        if (string.IsNullOrWhiteSpace(profile.LastPlayedSubject))
        {
            profile.LastPlayedSubject = "math";
        }
    }

    private static int GetDefaultTierForGrade(int grade)
    {
        if (grade >= 6)
        {
            return 1;
        }

        return 0;
    }
}
