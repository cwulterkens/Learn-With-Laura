using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GameSettings
{
    public int SettingsVersion { get; set; }
    public int SessionMinutes { get; set; } = 25;
    public int QuestionsPerEvaluation { get; set; } = 10;
    public int CorrectAnswersForMiniGame { get; set; } = 5;
    public double PromoteAccuracy { get; set; } = 0.85;
    public double DemoteAccuracy { get; set; } = 0.50;
    public double AuthoredQuestionChance { get; set; } = 0.65;

    public float MusicVolume { get; set; } = 0.6f;
    public float SfxVolume { get; set; } = 0.7f;
    public bool MasterMute { get; set; } = false;
    public bool Fullscreen { get; set; } = false;
    public bool LargeText { get; set; } = false;
    public bool HighContrast { get; set; } = false;
    public bool ReducedMotion { get; set; } = false;
    public string Resolution { get; set; } = "1536x1024";

    public string UiFontPath { get; set; } = "res://assets/fonts/AtkinsonHyperlegible-Regular.otf";
    public string ActiveProfileName { get; set; } = "";
    public int LastSelectedGrade { get; set; } = 1;
}

public class MathDifficultyConfig
{
    public Dictionary<string, GradeConfig> Grades { get; set; } = new();
}

public class GradeConfig
{
    public Dictionary<string, OperationConfig> Operations { get; set; } = new();
}

public class OperationConfig
{
    public bool Enabled { get; set; } = true;
    public List<int> MaxValuesByTier { get; set; } = new() { 10, 20, 50, 100 };
    public bool WholeNumberOnly { get; set; } = true;
}

public class MathQuestionContent
{
    public int ContentVersion { get; set; }
    public Dictionary<string, Dictionary<string, List<AuthoredQuestion>>> Questions { get; set; } = new();
}

public class SubjectQuestionContent
{
    public int ContentVersion { get; set; }
    public Dictionary<string, Dictionary<string, List<ChoiceQuestion>>> Subjects { get; set; } = new();
}

public class LauraMiniGameContent
{
    public int ContentVersion { get; set; }
    public List<LauraAreaDefinition> Areas { get; set; } = new();
}

public class LauraAreaDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string BackgroundPath { get; set; } = "";
    public List<ChoiceQuestion> Questions { get; set; } = new();
}

public class AuthoredQuestion
{
    public string Prompt { get; set; } = "";
    public int Answer { get; set; }
}

public class ChoiceQuestion
{
    public string Id { get; set; } = "";
    public int GradeMin { get; set; } = 1;
    public int GradeMax { get; set; } = 8;
    public string SkillId { get; set; } = "";
    public string Canon { get; set; } = "general";
    public string Prompt { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Hint { get; set; } = "";
    public string SuccessText { get; set; } = "";
    public string SourceTitle { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public class BadgeCatalog
{
    public int ContentVersion { get; set; }
    public List<BadgeDefinition> Badges { get; set; } = new();
}

public class BadgeDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Metric { get; set; } = "";
    public int Threshold { get; set; }
}

public class ProfileData
{
    // Version 0 is the historical, unversioned format. New profiles pass through the
    // same ordered migrations as old saves before they are persisted.
    public int SchemaVersion { get; set; }
    // Retained as the serialized property name for compatibility. The UI treats it
    // only as a local nickname.
    public string FirstName { get; set; } = "";
    [JsonIgnore]
    public string DisplayName
    {
        get => FirstName;
        set => FirstName = value;
    }
    public int CurrentGrade { get; set; } = 1;
    public Dictionary<string, OperationProgress> OperationProgress { get; set; } = new();
    public Dictionary<string, SubjectProgress> SubjectProgress { get; set; } = new();
    public List<string> UnlockedBadges { get; set; } = new();
    public List<string> UnlockedAwards { get; set; } = new();
    public int TotalCorrect { get; set; }
    public int TotalAttempted { get; set; }
    public LearningOutcomeCounts Outcomes { get; set; } = new();
    public int GameScore { get; set; }
    public int SessionsCompleted { get; set; }
    public int CorrectSinceMiniGame { get; set; }
    public int MiniGamesCompleted { get; set; }
    public int LauraChapterReplays { get; set; }
    public List<string> MasteredLauraAreas { get; set; } = new();
    public int PrairieStars { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public JourneyProgress Journey { get; set; } = new();
    public string LastPlayedSubject { get; set; } = "math";
    public SavedSessionData SavedSession { get; set; } = new();
    public double TotalLearningSeconds { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

public class JourneyProgress
{
    public int Day { get; set; } = 1;
    public int Miles { get; set; }
    public int Food { get; set; } = 24;
    public int Wood { get; set; } = 12;
    public int Morale { get; set; } = 8;
    public int Health { get; set; } = 8;
    public int LessonsAnswered { get; set; }
    public int FamilyActivitiesCompleted { get; set; }
    public string CurrentRegion { get; set; } = "big_woods";
}

public class SubjectProgress
{
    public int TotalCorrect { get; set; }
    public int TotalAttempted { get; set; }
    public LearningOutcomeCounts Outcomes { get; set; } = new();
    public DateTime LastPlayedUtc { get; set; } = DateTime.MinValue;
}

public class OperationProgress
{
    public int Tier { get; set; }
    public int WindowFirstTryCorrect { get; set; }
    // Retained for compatibility with pre-outcome saves. New adaptive windows do not use it.
    public int WindowCorrect { get; set; }
    public int WindowAttempted { get; set; }
    public LearningOutcomeCounts Outcomes { get; set; } = new();
}

public class LearningOutcomeCounts
{
    public int FirstTryCorrect { get; set; }
    public int HintAssistedCorrect { get; set; }
    public int Corrected { get; set; }
    public int Skipped { get; set; }
}

public class RuntimeQuestion
{
    public string Id { get; set; } = "";
    public string OperationKey { get; set; } = "addition";
    public string SubjectKey { get; set; } = "math";
    public string SkillId { get; set; } = "";
    public string Canon { get; set; } = "general";
    public string Prompt { get; set; } = "";
    public int Answer { get; set; }
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Hint { get; set; } = "";
    public string SuccessText { get; set; } = "";
    public string SourceTitle { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public class SavedSessionData
{
    public string ScenePath { get; set; } = "";
    public string SubjectKey { get; set; } = "";
    public string DisplayLabel { get; set; } = "";
    public string LauraAreaId { get; set; } = "";
    public int LauraQuestionsAnswered { get; set; }
    public int LauraCorrectAnswers { get; set; }
    public int LauraActivitiesCompleted { get; set; }
    public bool LauraFreePlay { get; set; }
    public bool HasResumePoint => !string.IsNullOrWhiteSpace(ScenePath);
}
