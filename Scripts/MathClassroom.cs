using Godot;
using System;

public partial class MathClassroom : Control
{
    private QuestionEngine _engine = null!;
    private RuntimeQuestion _currentQuestion = null!;
    private float _elapsedSeconds;

    private Label _questionLabel = null!;
    private LineEdit _answerInput = null!;
    private Label _feedbackLabel = null!;
    private Label _statusLabel = null!;
    private Label _badgeLabel = null!;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        if (AppState.ActiveProfile == null)
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/GradeSelection.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
            return;
        }

        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _questionLabel = GetNode<Label>("Panel/VBox/QuestionLabel");
        _answerInput = GetNode<LineEdit>("Panel/VBox/AnswerInput");
        _feedbackLabel = GetNode<Label>("Panel/VBox/FeedbackLabel");
        _statusLabel = GetNode<Label>("Panel/VBox/StatusLabel");
        _badgeLabel = GetNode<Label>("Panel/VBox/BadgeLabel");

        GetNode<Button>("Panel/VBox/SubmitButton").Pressed += OnSubmit;
        GetNode<Button>("Panel/VBox/SkipButton").Pressed += NextQuestion;
        GetNode<Button>("Panel/VBox/BackButton").Pressed += OnBackPressed;

        _engine = new QuestionEngine();
        NextQuestion();
        UpdateStatus();
    }

    public override void _Process(double delta)
    {
        _elapsedSeconds += (float)delta;

        float limitSeconds = AppState.Settings.SessionMinutes * 60f;
        if (_elapsedSeconds >= limitSeconds)
        {
            if (AppState.ActiveProfile != null)
            {
                AppState.ActiveProfile.TotalLearningSeconds += _elapsedSeconds;
                AppState.ActiveProfile.SessionsCompleted += 1;
                AppState.SaveProfile();
            }

            GetTree().ChangeSceneToFile("res://Scenes/Recess.tscn");
        }
    }

    private void OnSubmit()
    {
        if (_currentQuestion == null)
        {
            return;
        }

        if (!int.TryParse(_answerInput.Text.Trim(), out int parsed))
        {
            _feedbackLabel.Text = "Enter a whole number answer.";
            return;
        }

        var profile = AppState.ActiveProfile!;
        var opProgress = AppState.GetOperationProgress(_currentQuestion.OperationKey);

        profile.TotalAttempted += 1;
        opProgress.WindowAttempted += 1;

        if (parsed == _currentQuestion.Answer)
        {
            profile.TotalCorrect += 1;
            opProgress.WindowCorrect += 1;
            _feedbackLabel.Text = "Correct!";
            _answerInput.Text = "";

            _engine.TryApplyAdaptiveProgress(_currentQuestion.OperationKey, out string adaptMessage);
            AppState.UnlockEligibleBadges();
            AppState.SaveProfile();

            if (!string.IsNullOrWhiteSpace(adaptMessage))
            {
                _feedbackLabel.Text = adaptMessage;
            }

            UpdateBadgeStatus();
            NextQuestion();
        }
        else
        {
            _feedbackLabel.Text = "Not quite. Try again!";
            AppState.SaveProfile();
        }

        UpdateStatus();
    }

    private void NextQuestion()
    {
        _currentQuestion = _engine.NextQuestion(AppState.CurrentGrade, "math");
        _questionLabel.Text = _currentQuestion.Prompt;
        _answerInput.Text = "";
        _answerInput.GrabFocus();
    }

    private void UpdateStatus()
    {
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        var profile = AppState.ActiveProfile;
        _statusLabel.Text =
            $"Profile: {profile.FirstName} | Grade: {AppState.CurrentGrade} | Correct: {profile.TotalCorrect}/{profile.TotalAttempted}";
    }

    private void UpdateBadgeStatus()
    {
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        int count = AppState.ActiveProfile.UnlockedBadges.Count;
        _badgeLabel.Text = $"Badges earned: {count}";
    }

    private void OnBackPressed()
    {
        if (AppState.ActiveProfile != null)
        {
            AppState.ActiveProfile.TotalLearningSeconds += _elapsedSeconds;
            AppState.SaveProfile();
        }

        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
