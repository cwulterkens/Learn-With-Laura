using Godot;
using System;

public partial class LearningModule : Control
{
    private readonly Button[] _optionButtons = new Button[4];

    private QuestionEngine _engine = null!;
    private RuntimeQuestion _currentQuestion = null!;
    private float _elapsedSeconds;
    private bool _questionResolved;
    private bool _attemptRecorded;
    private bool _missedOnce;
    private bool _adventureReady;
    private bool _transitioning;

    private TextureRect _background = null!;
    private Label _titleLabel = null!;
    private Label _questionLabel = null!;
    private Label _feedbackLabel = null!;
    private Label _statusLabel = null!;
    private Label _badgeLabel = null!;
    private Label _progressLabel = null!;
    private Button _nextButton = null!;
    private AudioStreamPlayer _successSfx = null!;
    private AudioStreamPlayer _wrongSfx = null!;
    private AudioStreamPlayer _awardSfx = null!;

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

        _background = GetNode<TextureRect>("Background");
        _titleLabel = GetNode<Label>("Panel/Scroll/VBox/TitleLabel");
        _questionLabel = GetNode<Label>("Panel/Scroll/VBox/QuestionLabel");
        _feedbackLabel = GetNode<Label>("Panel/Scroll/VBox/FeedbackLabel");
        _statusLabel = GetNode<Label>("Panel/Scroll/VBox/StatusLabel");
        _badgeLabel = GetNode<Label>("Panel/Scroll/VBox/BadgeLabel");
        _progressLabel = GetNode<Label>("Panel/Scroll/VBox/ProgressLabel");
        _nextButton = GetNode<Button>("Panel/Scroll/VBox/SkipButton");

        _optionButtons[0] = GetNode<Button>("Panel/Scroll/VBox/OptionsGrid/OptionA");
        _optionButtons[1] = GetNode<Button>("Panel/Scroll/VBox/OptionsGrid/OptionB");
        _optionButtons[2] = GetNode<Button>("Panel/Scroll/VBox/OptionsGrid/OptionC");
        _optionButtons[3] = GetNode<Button>("Panel/Scroll/VBox/OptionsGrid/OptionD");

        for (int index = 0; index < _optionButtons.Length; index++)
        {
            int localIndex = index;
            _optionButtons[index].Pressed += () => OnOptionSelected(localIndex);
        }

        _nextButton.Pressed += OnNextPressed;
        GetNode<Button>("Panel/Scroll/VBox/Footer/SubjectsButton").Pressed += OnSubjectsPressed;
        GetNode<Button>("Panel/Scroll/VBox/Footer/HomeButton").Pressed += OnHomePressed;

        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _successSfx = GetNode<AudioStreamPlayer>("SuccessSfx");
        _wrongSfx = GetNode<AudioStreamPlayer>("WrongSfx");
        _awardSfx = GetNode<AudioStreamPlayer>("AwardSfx");
        AppState.ConfigureSfxPlayer(_successSfx);
        AppState.ConfigureSfxPlayer(_wrongSfx);
        AppState.ConfigureSfxPlayer(_awardSfx);

        _engine = new QuestionEngine();
        SetBackgroundForSubject(AppState.CurrentSubjectKey);
        AppState.SaveResumePoint(
            "res://Scenes/LearningModule.tscn",
            $"Continue {AppState.GetSubjectDisplayName(AppState.CurrentSubjectKey)}");
        AppState.SaveProfile();

        UpdateHeader();
        UpdateStatus();
        NextQuestion();
    }

    public override void _Process(double delta)
    {
        if (_transitioning)
        {
            return;
        }

        _elapsedSeconds += (float)delta;
        float limitSeconds = Math.Max(1, AppState.Settings.SessionMinutes) * 60f;
        if (_elapsedSeconds < limitSeconds)
        {
            return;
        }

        _transitioning = true;
        if (AppState.ActiveProfile != null)
        {
            SaveElapsedLearningTime();
            AppState.ActiveProfile.SessionsCompleted += 1;
            AppState.SaveProfile();
        }

        GetTree().ChangeSceneToFile("res://Scenes/Recess.tscn");
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (_questionResolved || _currentQuestion == null || AppState.ActiveProfile == null)
        {
            return;
        }

        if (!_attemptRecorded)
        {
            RecordQuestionAttempt();
        }

        if (optionIndex == _currentQuestion.CorrectIndex)
        {
            ResolveCorrectAnswer();
            return;
        }

        ResolveWrongAnswer(optionIndex);
    }

    private void RecordQuestionAttempt()
    {
        var profile = AppState.ActiveProfile!;
        var subjectProgress = AppState.GetSubjectProgress(AppState.CurrentSubjectKey);
        profile.TotalAttempted += 1;
        subjectProgress.TotalAttempted += 1;
        subjectProgress.LastPlayedUtc = DateTime.UtcNow;

        if (_currentQuestion.SubjectKey == "math")
        {
            AppState.GetOperationProgress(_currentQuestion.OperationKey).WindowAttempted += 1;
        }

        _attemptRecorded = true;
    }

    private void ResolveCorrectAnswer()
    {
        var profile = AppState.ActiveProfile!;
        var subjectProgress = AppState.GetSubjectProgress(AppState.CurrentSubjectKey);
        bool firstTry = !_missedOnce;

        profile.TotalCorrect += 1;
        subjectProgress.TotalCorrect += 1;
        profile.CorrectSinceMiniGame += 1;
        profile.PrairieStars += firstTry ? 2 : 1;
        profile.CurrentStreak = firstTry ? profile.CurrentStreak + 1 : 0;
        profile.BestStreak = Math.Max(profile.BestStreak, profile.CurrentStreak);

        if (_currentQuestion.SubjectKey == "math")
        {
            AppState.GetOperationProgress(_currentQuestion.OperationKey).WindowCorrect += 1;
        }

        int awardCountBefore = profile.UnlockedAwards.Count;
        string adaptiveMessage = EvaluateAdaptiveProgress();
        AppState.UnlockEligibleBadges();

        _feedbackLabel.Text = BuildSuccessFeedback(firstTry, adaptiveMessage, awardCountBefore);
        _questionResolved = true;
        SetOptionButtonsEnabled(false);
        _optionButtons[_currentQuestion.CorrectIndex].Modulate = new Color(0.72f, 1f, 0.72f);

        if (profile.UnlockedAwards.Count > awardCountBefore)
        {
            _awardSfx.Play();
        }
        else
        {
            _successSfx.Play();
        }

        _adventureReady = AppState.HasMiniGameReady();
        _nextButton.Text = _adventureReady ? "Play Laura's Adventure" : "Next Question";
        AppState.SaveProfile();
        UpdateStatus();
        _nextButton.GrabFocus();
    }

    private void ResolveWrongAnswer(int optionIndex)
    {
        var profile = AppState.ActiveProfile!;
        profile.CurrentStreak = 0;
        _wrongSfx.Play();

        if (!_missedOnce)
        {
            _missedOnce = true;
            _optionButtons[optionIndex].Disabled = true;
            string hint = string.IsNullOrWhiteSpace(_currentQuestion.Hint)
                ? "Look for the choice that best fits the question, then try once more."
                : _currentQuestion.Hint;
            _feedbackLabel.Text = $"Good try. Hint: {hint}";
            AppState.SaveProfile();
            UpdateStatus();
            return;
        }

        _questionResolved = true;
        SetOptionButtonsEnabled(false);
        _optionButtons[_currentQuestion.CorrectIndex].Modulate = new Color(0.72f, 1f, 0.72f);
        string adaptiveMessage = EvaluateAdaptiveProgress();
        _feedbackLabel.Text = BuildCorrectionFeedback(adaptiveMessage);
        _nextButton.Text = "Next Question";
        AppState.SaveProfile();
        UpdateStatus();
        _nextButton.GrabFocus();
    }

    private void OnNextPressed()
    {
        if (_questionResolved)
        {
            if (_adventureReady)
            {
                LaunchLauraMiniGame();
                return;
            }

            NextQuestion();
            return;
        }

        if (!_attemptRecorded)
        {
            RecordQuestionAttempt();
        }

        AppState.ActiveProfile!.CurrentStreak = 0;
        _questionResolved = true;
        SetOptionButtonsEnabled(false);
        _optionButtons[_currentQuestion.CorrectIndex].Modulate = new Color(0.72f, 1f, 0.72f);
        string adaptiveMessage = EvaluateAdaptiveProgress();
        _feedbackLabel.Text = BuildCorrectionFeedback(adaptiveMessage);
        _nextButton.Text = "Next Question";
        AppState.SaveProfile();
        UpdateStatus();
    }

    private void NextQuestion()
    {
        _currentQuestion = _engine.NextQuestion(AppState.CurrentGrade, AppState.CurrentSubjectKey);
        _questionResolved = false;
        _attemptRecorded = false;
        _missedOnce = false;
        _adventureReady = false;
        _feedbackLabel.Text = "";
        _nextButton.Text = "Skip & Explain";

        string context = AppState.GetCanonDisplayName(_currentQuestion.Canon);
        _questionLabel.Text = $"{context}\n{_currentQuestion.Prompt}";

        for (int index = 0; index < _optionButtons.Length; index++)
        {
            bool hasOption = index < _currentQuestion.Options.Count;
            _optionButtons[index].Visible = hasOption;
            _optionButtons[index].Disabled = !hasOption;
            _optionButtons[index].Modulate = Colors.White;
            if (hasOption)
            {
                _optionButtons[index].Text = _currentQuestion.Options[index];
            }
        }

        UpdateHeader();
        UpdateStatus();
        _optionButtons[0].GrabFocus();
    }

    private string EvaluateAdaptiveProgress()
    {
        if (_currentQuestion.SubjectKey != "math")
        {
            return "";
        }

        _engine.TryApplyAdaptiveProgress(_currentQuestion.OperationKey, out string message);
        return message;
    }

    private string BuildSuccessFeedback(bool firstTry, string adaptiveMessage, int awardCountBefore)
    {
        string explanation = string.IsNullOrWhiteSpace(_currentQuestion.SuccessText)
            ? "Correct!"
            : _currentQuestion.SuccessText;
        string starText = firstTry ? "+2 Prairie Stars" : "+1 Prairie Star for learning from the hint";
        string result = $"{explanation}\n{starText}";

        if (!string.IsNullOrWhiteSpace(_currentQuestion.SourceTitle))
        {
            result += $" · Source: {_currentQuestion.SourceTitle}";
        }

        if (!string.IsNullOrWhiteSpace(adaptiveMessage))
        {
            result += $"\n{adaptiveMessage}";
        }

        if (AppState.ActiveProfile!.UnlockedAwards.Count > awardCountBefore)
        {
            string newest = AppState.ActiveProfile.UnlockedAwards[^1];
            result += $"\nNew badge earned: {newest}!";
        }

        return result;
    }

    private string BuildCorrectionFeedback(string adaptiveMessage)
    {
        string answer = _currentQuestion.Options[_currentQuestion.CorrectIndex];
        string explanation = string.IsNullOrWhiteSpace(_currentQuestion.SuccessText)
            ? "Review the idea, then try a new question."
            : _currentQuestion.SuccessText;
        string result = $"The best answer is {answer}. {explanation}";

        if (!string.IsNullOrWhiteSpace(_currentQuestion.SourceTitle))
        {
            result += $" · Source: {_currentQuestion.SourceTitle}";
        }

        if (!string.IsNullOrWhiteSpace(adaptiveMessage))
        {
            result += $"\n{adaptiveMessage}";
        }

        return result;
    }

    private void SetOptionButtonsEnabled(bool enabled)
    {
        foreach (Button button in _optionButtons)
        {
            if (button.Visible)
            {
                button.Disabled = !enabled;
            }
        }
    }

    private void UpdateHeader()
    {
        _titleLabel.Text = $"{AppState.GetSubjectDisplayName(AppState.CurrentSubjectKey)} Lesson";
    }

    private void UpdateStatus()
    {
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        var profile = AppState.ActiveProfile;
        var subjectProgress = AppState.GetSubjectProgress(AppState.CurrentSubjectKey);
        int target = Math.Max(1, AppState.Settings.CorrectAnswersForMiniGame);
        int towardAdventure = Math.Min(target, profile.CorrectSinceMiniGame);

        _statusLabel.Text =
            $"{profile.FirstName} · Grade {AppState.CurrentGrade} · Mastered {subjectProgress.TotalCorrect}/{subjectProgress.TotalAttempted}";
        _progressLabel.Text =
            $"Prairie Stars: {profile.PrairieStars} · Streak: {profile.CurrentStreak} (best {profile.BestStreak})";
        _badgeLabel.Text =
            $"Adventure meter: {towardAdventure}/{target} · Badges: {profile.UnlockedBadges.Count}";
    }

    private void LaunchLauraMiniGame()
    {
        _transitioning = true;
        SaveElapsedLearningTime();
        string returnSubject = AppState.CurrentSubjectKey;
        AppState.PendingLauraFreePlay = false;
        AppState.ConsumeMiniGameProgress();
        AppState.CurrentSubjectKey = "laura";
        AppState.SaveResumePoint("res://Scenes/LauraMiniGame.tscn", "Continue Laura's Adventure");
        AppState.ActiveProfile!.SavedSession.SubjectKey = returnSubject;
        AppState.ActiveProfile.SavedSession.LauraAreaId = "";
        AppState.ActiveProfile.SavedSession.LauraQuestionsAnswered = 0;
        AppState.ActiveProfile.SavedSession.LauraCorrectAnswers = 0;
        AppState.ActiveProfile.SavedSession.LauraActivitiesCompleted = 0;
        AppState.ActiveProfile.SavedSession.LauraFreePlay = false;
        AppState.SaveProfile();
        GetTree().ChangeSceneToFile("res://Scenes/LauraMiniGame.tscn");
    }

    private void OnSubjectsPressed()
    {
        SaveAndExit();
        GetTree().ChangeSceneToFile("res://Scenes/SubjectSelection.tscn");
    }

    private void OnHomePressed()
    {
        SaveAndExit();
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void SaveAndExit()
    {
        _transitioning = true;
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        SaveElapsedLearningTime();
        AppState.SetCurrentSubject(AppState.CurrentSubjectKey);
        AppState.SaveResumePoint(
            "res://Scenes/LearningModule.tscn",
            $"Continue {AppState.GetSubjectDisplayName(AppState.CurrentSubjectKey)}");
        AppState.SaveProfile();
    }

    private void SaveElapsedLearningTime()
    {
        if (AppState.ActiveProfile == null || _elapsedSeconds <= 0f)
        {
            return;
        }

        AppState.ActiveProfile.TotalLearningSeconds += _elapsedSeconds;
        _elapsedSeconds = 0f;
    }

    private void SetBackgroundForSubject(string subjectKey)
    {
        string path = subjectKey switch
        {
            "math" => "res://assets/math_classroom_bg.png",
            "social_studies" => "res://assets/social_studies_bg.png",
            "history" => "res://assets/history_study_bg.png",
            "geography" => "res://assets/geography_map_bg.png",
            "science" => "res://assets/science_lab_bg.png",
            "english" => "res://assets/english_reading_bg.png",
            _ => "res://assets/classroom.png"
        };

        if (ResourceLoader.Exists(path))
        {
            _background.Texture = GD.Load<Texture2D>(path);
        }
    }
}
