using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ProgressScreen : Control
{
    private enum PendingConfirmation
    {
        None,
        Import,
        ResetLearning,
        ResetAdventure,
        ResetAll
    }

    private Label _overviewLabel = null!;
    private Label _subjectsLabel = null!;
    private Label _recentLabel = null!;
    private Label _badgesLabel = null!;
    private Label _adventureLabel = null!;
    private Label _privacyLabel = null!;
    private Label _statusLabel = null!;
    private FileDialog _fileDialog = null!;
    private ConfirmationDialog _confirmationDialog = null!;
    private bool _fileDialogIsExport;
    private string _pendingImportPath = "";
    private PendingConfirmation _pendingConfirmation;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        if (AppState.ActiveProfile == null)
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/ProgressScreen.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
            return;
        }

        _overviewLabel = GetNode<Label>("Panel/Scroll/VBox/OverviewLabel");
        _subjectsLabel = GetNode<Label>("Panel/Scroll/VBox/SubjectsLabel");
        _recentLabel = GetNode<Label>("Panel/Scroll/VBox/RecentLabel");
        _badgesLabel = GetNode<Label>("Panel/Scroll/VBox/BadgesLabel");
        _adventureLabel = GetNode<Label>("Panel/Scroll/VBox/AdventureLabel");
        _privacyLabel = GetNode<Label>("Panel/Scroll/VBox/PrivacyLabel");
        _statusLabel = GetNode<Label>("Panel/Scroll/VBox/StatusLabel");

        GetNode<Button>("Panel/Scroll/VBox/DataButtons/BackupButton").Pressed += OnBackupPressed;
        GetNode<Button>("Panel/Scroll/VBox/DataButtons/ExportButton").Pressed += OnExportPressed;
        GetNode<Button>("Panel/Scroll/VBox/DataButtons/ImportButton").Pressed += OnImportPressed;
        GetNode<Button>("Panel/Scroll/VBox/ResetButtons/ResetLearningButton").Pressed += () =>
            AskToReset(PendingConfirmation.ResetLearning);
        GetNode<Button>("Panel/Scroll/VBox/ResetButtons/ResetAdventureButton").Pressed += () =>
            AskToReset(PendingConfirmation.ResetAdventure);
        GetNode<Button>("Panel/Scroll/VBox/ResetButtons/ResetAllButton").Pressed += () =>
            AskToReset(PendingConfirmation.ResetAll);
        GetNode<Button>("Panel/Scroll/VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        _fileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.json ; Learn With Laura local profile" },
            Title = "Local Profile File"
        };
        AddChild(_fileDialog);
        _fileDialog.FileSelected += OnFileSelected;

        _confirmationDialog = new ConfirmationDialog
        {
            Title = "Confirm Local Data Change"
        };
        AddChild(_confirmationDialog);
        _confirmationDialog.Confirmed += OnConfirmed;
        _confirmationDialog.Canceled += ClearPendingAction;

        RefreshProgress();
    }

    private void RefreshProgress()
    {
        ProfileData profile = AppState.ActiveProfile!;
        TimeSpan learningTime = TimeSpan.FromSeconds(Math.Max(0, profile.TotalLearningSeconds));
        string timeText = learningTime.TotalHours >= 1
            ? $"{(int)learningTime.TotalHours}h {learningTime.Minutes}m"
            : $"{learningTime.Minutes}m";

        _overviewLabel.Text =
            $"Nickname: {profile.DisplayName} · Grade {profile.CurrentGrade}\n" +
            $"Learning time: {timeText} · Sessions: {profile.SessionsCompleted} · Best first-try streak: {profile.BestStreak}\n" +
            $"Prairie Stars: {profile.PrairieStars} · Trail score: {profile.GameScore}";

        _subjectsLabel.Text = BuildSubjectProgress(profile);
        _recentLabel.Text = BuildRecentActivity(profile);
        _badgesLabel.Text = profile.UnlockedAwards.Count == 0
            ? "Badges\nNo badges earned yet."
            : $"Badges ({profile.UnlockedAwards.Count})\n• {string.Join("\n• ", profile.UnlockedAwards)}";
        _adventureLabel.Text = BuildAdventureProgress(profile);

        string localRoot = ProjectSettings.GlobalizePath("user://");
        _privacyLabel.Text =
            "Local data and privacy\n" +
            $"All progress stays on this machine under:\n{localRoot}\n" +
            "Use a nickname rather than a legal name. The game has no account, cloud sync, telemetry, advertising, or network transfer.";
    }

    private static string BuildSubjectProgress(ProfileData profile)
    {
        var lines = new List<string> { "Subject progress" };
        foreach (string key in new[] { "math", "science", "history", "social_studies", "geography", "english", "laura" })
        {
            if (!profile.SubjectProgress.TryGetValue(key, out SubjectProgress progress)
                || progress.TotalAttempted == 0)
            {
                continue;
            }

            lines.Add(key == "laura"
                ? $"{AppState.GetSubjectDisplayName(key)}: correct {progress.TotalCorrect}/{progress.TotalAttempted} knowledge stops"
                : $"{AppState.GetSubjectDisplayName(key)}: tried {progress.TotalAttempted} · " +
                  $"first try {progress.Outcomes.FirstTryCorrect} · with a hint {progress.Outcomes.HintAssistedCorrect} · " +
                  $"corrected {progress.Outcomes.Corrected} · skipped {progress.Outcomes.Skipped}");
        }

        if (lines.Count == 1)
        {
            lines.Add("No subject activity recorded yet.");
        }
        return string.Join("\n", lines);
    }

    private static string BuildRecentActivity(ProfileData profile)
    {
        IReadOnlyList<string> recentKeys = ProfileDataManagement.GetRecentSubjectKeys(profile);
        if (recentKeys.Count == 0)
        {
            return "Recent activity\nNo completed subject activity yet.";
        }

        IEnumerable<string> lines = recentKeys.Select(key =>
        {
            SubjectProgress progress = profile.SubjectProgress[key];
            string date = progress.LastPlayedUtc == DateTime.MinValue
                ? "date not recorded"
                : progress.LastPlayedUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
            return $"• {AppState.GetSubjectDisplayName(key)} · {date} · {progress.TotalAttempted} tried";
        });
        return $"Recent activity\n{string.Join("\n", lines)}";
    }

    private static string BuildAdventureProgress(ProfileData profile)
    {
        string chapters = profile.MasteredLauraAreas.Count == 0
            ? "none yet"
            : string.Join(", ", profile.MasteredLauraAreas.Select(areaId =>
                AppState.GetLauraArea(areaId)?.Name ?? areaId));
        JourneyProgress journey = profile.Journey;
        return
            $"Adventure mastery\nMastered chapters: {chapters}\n" +
            $"Day {journey.Day} · Miles {journey.Miles} · Food {journey.Food} · Wood {journey.Wood} · " +
            $"Health {journey.Health}/{JourneyResources.MaxHealth} · Morale {journey.Morale}/{JourneyResources.MaxMorale}";
    }

    private void OnBackupPressed()
    {
        LocalProfileActionResult result = AppState.CreateActiveProfileBackup();
        _statusLabel.Text = result.Succeeded
            ? $"Local backup created:\n{DisplayPath(result.Path)}"
            : result.Error;
    }

    private void OnExportPressed()
    {
        _fileDialogIsExport = true;
        _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.CurrentFile = $"{AppState.ActiveProfile!.DisplayName}-progress.lwl-profile.json";
        _fileDialog.PopupCenteredRatio(0.8f);
    }

    private void OnImportPressed()
    {
        _fileDialogIsExport = false;
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.CurrentFile = "";
        _fileDialog.PopupCenteredRatio(0.8f);
    }

    private void OnFileSelected(string path)
    {
        if (_fileDialogIsExport)
        {
            LocalProfileActionResult result = AppState.ExportActiveProfile(path);
            _statusLabel.Text = result.Succeeded
                ? $"Export created on this machine:\n{DisplayPath(result.Path)}"
                : result.Error;
            return;
        }

        _pendingImportPath = path;
        _pendingConfirmation = PendingConfirmation.Import;
        _confirmationDialog.OkButtonText = "Import Progress";
        _confirmationDialog.DialogText =
            $"Import this local file into {AppState.ActiveProfile!.DisplayName}?\n\n" +
            "Current progress will be backed up first. The imported nickname will not replace this learner's nickname. Cancel leaves everything unchanged.";
        _confirmationDialog.PopupCentered(new Vector2I(720, 340));
    }

    private void AskToReset(PendingConfirmation action)
    {
        _pendingConfirmation = action;
        (string label, string detail) = action switch
        {
            PendingConfirmation.ResetLearning => (
                "Reset Learning",
                "This clears classroom subject results, adaptive math windows, streaks, sessions, and learning time. Adventure progress remains."),
            PendingConfirmation.ResetAdventure => (
                "Reset Adventure",
                "This clears Laura chapter mastery, journey resources, and adventure activity. Classroom progress remains."),
            _ => (
                "Reset All Progress",
                "This clears classroom and adventure progress, badges, stars, score, and the resume point. The nickname and grade remain.")
        };
        _confirmationDialog.OkButtonText = label;
        _confirmationDialog.DialogText =
            $"{detail}\n\nA local safety backup will be created first. Cancel leaves everything unchanged.";
        _confirmationDialog.PopupCentered(new Vector2I(760, 380));
    }

    private void OnConfirmed()
    {
        if (_pendingConfirmation == PendingConfirmation.Import)
        {
            LocalProfileActionResult result = AppState.ImportActiveProfile(_pendingImportPath);
            _statusLabel.Text = result.Succeeded
                ? $"Progress imported. Previous progress was backed up to:\n{DisplayPath(result.Path)}"
                : result.Error;
            ClearPendingAction();
            RefreshProgress();
            return;
        }

        ProgressResetScope scope = _pendingConfirmation switch
        {
            PendingConfirmation.ResetLearning => ProgressResetScope.Learning,
            PendingConfirmation.ResetAdventure => ProgressResetScope.Adventure,
            PendingConfirmation.ResetAll => ProgressResetScope.All,
            _ => ProgressResetScope.Learning
        };
        if (_pendingConfirmation == PendingConfirmation.None)
        {
            return;
        }

        LocalProfileActionResult reset = AppState.ResetActiveProfile(scope);
        _statusLabel.Text = reset.Succeeded
            ? $"Progress reset completed. Safety backup:\n{DisplayPath(reset.Path)}"
            : reset.Error;
        ClearPendingAction();
        RefreshProgress();
    }

    private void ClearPendingAction()
    {
        _pendingConfirmation = PendingConfirmation.None;
        _pendingImportPath = "";
    }

    private static string DisplayPath(string path)
    {
        return path.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
            ? ProjectSettings.GlobalizePath(path)
            : path;
    }
}
