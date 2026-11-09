using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class LauraMiniGame : Control
{
    private const int KnowledgeGoal = 4;
    private const int ActivityGoal = 2;
    private const int MasteryCorrect = 3;

    private readonly Button[] _optionButtons = new Button[4];
    private readonly HashSet<int> _usedQuestionIndexes = new();
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<int> _pattern = new();

    private QuestionEngine _engine = null!;
    private RuntimeQuestion _currentQuestion = null!;
    private string _activeAreaId = "";
    private string _currentFamilyActivity = "";
    private string[] _patternLabels = Array.Empty<string>();
    private int _patternInputIndex;
    private int _patternPlaybackVersion;
    private bool _acceptPatternInput;
    private bool _familyActivityActive;
    private bool _familyActivityResolved;
    private bool _questionResolved;
    private bool _completionShown;
    private bool _completionMastered;
    private bool _freePlayEntry;

    private TextureRect _background = null!;
    private Label _introLabel = null!;
    private Label _areaLabel = null!;
    private Label _questionLabel = null!;
    private Label _feedbackLabel = null!;
    private Label _progressLabel = null!;
    private VBoxContainer _areaButtons = null!;
    private VBoxContainer _quizBox = null!;
    private GridContainer _optionsGrid = null!;
    private Button _continueButton = null!;
    private Button _returnButton = null!;
    private AudioStreamPlayer _successSfx = null!;
    private AudioStreamPlayer _wrongSfx = null!;
    private AudioStreamPlayer _awardSfx = null!;
    private AudioStreamPlayer _patternSfx = null!;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);
        _rng.Randomize();

        if (AppState.ActiveProfile == null)
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/LauraMiniGame.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
            return;
        }

        _background = GetNode<TextureRect>("Background");
        _introLabel = GetNode<Label>("Panel/VBox/IntroLabel");
        _areaLabel = GetNode<Label>("Panel/VBox/QuizBox/AreaLabel");
        _questionLabel = GetNode<Label>("Panel/VBox/QuizBox/QuestionLabel");
        _feedbackLabel = GetNode<Label>("Panel/VBox/QuizBox/FeedbackLabel");
        _progressLabel = GetNode<Label>("Panel/VBox/QuizBox/ProgressLabel");
        _areaButtons = GetNode<VBoxContainer>("Panel/VBox/AreaButtons");
        _quizBox = GetNode<VBoxContainer>("Panel/VBox/QuizBox");
        _optionsGrid = GetNode<GridContainer>("Panel/VBox/QuizBox/OptionsGrid");
        _continueButton = GetNode<Button>("Panel/VBox/QuizBox/ContinueButton");

        _optionButtons[0] = GetNode<Button>("Panel/VBox/QuizBox/OptionsGrid/OptionA");
        _optionButtons[1] = GetNode<Button>("Panel/VBox/QuizBox/OptionsGrid/OptionB");
        _optionButtons[2] = GetNode<Button>("Panel/VBox/QuizBox/OptionsGrid/OptionC");
        _optionButtons[3] = GetNode<Button>("Panel/VBox/QuizBox/OptionsGrid/OptionD");

        for (int index = 0; index < _optionButtons.Length; index++)
        {
            int localIndex = index;
            _optionButtons[index].Pressed += () => OnOptionSelected(localIndex);
        }

        _continueButton.Pressed += ContinueAdventure;
        _returnButton = GetNode<Button>("Panel/VBox/Footer/ReturnButton");
        _returnButton.Pressed += ReturnFromAdventure;
        GetNode<Button>("Panel/VBox/Footer/HomeButton").Pressed += GoHome;

        _freePlayEntry = AppState.PendingLauraFreePlay
            || AppState.ActiveProfile.SavedSession.LauraFreePlay;
        AppState.PendingLauraFreePlay = false;
        _returnButton.Text = _freePlayEntry ? "Back To Chapter Map" : "Return To Lesson";

        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _successSfx = GetNode<AudioStreamPlayer>("SuccessSfx");
        _wrongSfx = GetNode<AudioStreamPlayer>("WrongSfx");
        _awardSfx = GetNode<AudioStreamPlayer>("AwardSfx");
        _patternSfx = GetNode<AudioStreamPlayer>("PatternSfx");
        AppState.ConfigureSfxPlayer(_successSfx);
        AppState.ConfigureSfxPlayer(_wrongSfx);
        AppState.ConfigureSfxPlayer(_awardSfx);
        AppState.ConfigureSfxPlayer(_patternSfx);

        _engine = new QuestionEngine();
        BuildRouteButtons();
        ShowTrailMap();

        if (!string.IsNullOrWhiteSpace(AppState.ActiveProfile.SavedSession.LauraAreaId))
        {
            StartArea(AppState.ActiveProfile.SavedSession.LauraAreaId, false);
        }
    }

    private void BuildRouteButtons()
    {
        foreach (Node child in _areaButtons.GetChildren())
        {
            child.QueueFree();
        }

        int index = 0;
        int miles = AppState.ActiveProfile!.Journey.Miles;
        foreach (LauraAreaDefinition area in AppState.LauraMiniGameContent.Areas)
        {
            int requiredMiles = index * 40;
            bool unlocked = miles >= requiredMiles;
            var button = new Button
            {
                Text = unlocked
                    ? $"Visit {area.Name}"
                    : $"{area.Name} · unlocks at {requiredMiles} trail miles",
                CustomMinimumSize = new Vector2(640, 70),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                Alignment = HorizontalAlignment.Center,
                Disabled = !unlocked
            };

            string areaId = area.Id;
            button.Pressed += () => StartArea(areaId, true);
            _areaButtons.AddChild(button);
            UiHelpers.StyleNewControl(button);
            index++;
        }
    }

    private void ShowTrailMap()
    {
        _areaButtons.Visible = true;
        _quizBox.Visible = false;
        _introLabel.Visible = true;
        _returnButton.Visible = !_freePlayEntry;
        _introLabel.Text = BuildStatusLine(
            "Choose a chapter from Laura's world. Knowledge stops and interactive memory mini-games move the journey forward.");
    }

    private void StartArea(string areaId, bool resetProgress)
    {
        LauraAreaDefinition area = AppState.GetLauraArea(areaId);
        if (area == null)
        {
            ShowTrailMap();
            return;
        }

        _activeAreaId = areaId;
        _usedQuestionIndexes.Clear();
        _areaButtons.Visible = false;
        _quizBox.Visible = true;
        _introLabel.Visible = false;
        _returnButton.Visible = true;
        _completionShown = false;
        _completionMastered = false;
        _familyActivityActive = false;
        _familyActivityResolved = false;
        _questionResolved = false;
        _patternPlaybackVersion++;

        JourneyProgress journey = AppState.ActiveProfile!.Journey;
        journey.CurrentRegion = areaId;
        if (resetProgress)
        {
            ResetSavedAdventureProgress();
        }

        string returnSubject = GetReturnSubject();
        AppState.CurrentSubjectKey = "laura";
        AppState.SaveResumePoint("res://Scenes/LauraMiniGame.tscn", "Continue Laura's Adventure");
        AppState.ActiveProfile.SavedSession.SubjectKey = returnSubject;
        AppState.ActiveProfile.SavedSession.LauraAreaId = areaId;
        AppState.ActiveProfile.SavedSession.LauraFreePlay = _freePlayEntry;
        AppState.SaveProfile();

        _areaLabel.Text = area.Name;
        _feedbackLabel.Text = area.Description;
        ApplyAreaBackground(area);
        PresentNextAdventureStep();
    }

    private void PresentNextAdventureStep()
    {
        SavedSessionData saved = AppState.ActiveProfile!.SavedSession;
        if (saved.LauraQuestionsAnswered >= KnowledgeGoal
            && saved.LauraActivitiesCompleted >= ActivityGoal)
        {
            FinishMiniGame();
            return;
        }

        bool activityDue = (saved.LauraQuestionsAnswered >= 2 && saved.LauraActivitiesCompleted == 0)
            || (saved.LauraQuestionsAnswered >= KnowledgeGoal && saved.LauraActivitiesCompleted == 1);
        if (activityDue)
        {
            PresentFamilyActivity();
            return;
        }

        PresentTrailQuestion();
    }

    private void PresentTrailQuestion()
    {
        _familyActivityActive = false;
        _familyActivityResolved = false;
        _questionResolved = false;
        _acceptPatternInput = false;
        _continueButton.Visible = false;
        _optionsGrid.Visible = true;

        _currentQuestion = _engine.NextLauraQuestion(_activeAreaId, _usedQuestionIndexes);
        string context = AppState.GetCanonDisplayName(_currentQuestion.Canon);
        _questionLabel.Text = $"{RandomTrailEvent()}\n\n{context}\n{_currentQuestion.Prompt}";
        _feedbackLabel.Text = "Choose the best answer. The explanation and source will appear before you move on.";
        ApplyQuestionOptions();
        UpdateProgress();
    }

    private void ApplyQuestionOptions()
    {
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

        if (_optionButtons[0].Visible)
        {
            _optionButtons[0].GrabFocus();
        }
    }

    private void OnOptionSelected(int optionIndex)
    {
        if (_familyActivityActive)
        {
            HandlePatternInput(optionIndex);
            return;
        }

        if (_questionResolved || AppState.ActiveProfile == null || _currentQuestion == null)
        {
            return;
        }

        var profile = AppState.ActiveProfile;
        var journey = profile.Journey;
        var subjectProgress = AppState.GetSubjectProgress("laura");

        profile.TotalAttempted += 1;
        subjectProgress.TotalAttempted += 1;
        subjectProgress.LastPlayedUtc = DateTime.UtcNow;
        profile.SavedSession.LauraQuestionsAnswered += 1;
        journey.LessonsAnswered += 1;

        bool correct = optionIndex == _currentQuestion.CorrectIndex;
        int awardCountBefore = profile.UnlockedAwards.Count;
        if (correct)
        {
            profile.TotalCorrect += 1;
            subjectProgress.TotalCorrect += 1;
            profile.SavedSession.LauraCorrectAnswers += 1;
            profile.GameScore += 100;
            profile.PrairieStars += 2;
            journey.Morale = Math.Min(10, journey.Morale + 1);
            _feedbackLabel.Text = BuildQuestionFeedback(true);
            _successSfx.Play();
        }
        else
        {
            journey.Health = Math.Max(0, journey.Health - 1);
            journey.Morale = Math.Max(0, journey.Morale - 1);
            _feedbackLabel.Text = BuildQuestionFeedback(false);
            _wrongSfx.Play();
        }

        AdvanceJourneyAfterStop();
        AppState.UnlockEligibleBadges();
        if (profile.UnlockedAwards.Count > awardCountBefore)
        {
            _awardSfx.Play();
            _feedbackLabel.Text += $"\nNew badge: {profile.UnlockedAwards[^1]}!";
        }

        _questionResolved = true;
        SetOptionButtonsEnabled(false);
        _optionButtons[_currentQuestion.CorrectIndex].Modulate = new Color(0.72f, 1f, 0.72f);
        _continueButton.Text = "Continue the Journey";
        _continueButton.Visible = true;
        _continueButton.GrabFocus();
        AppState.SaveProfile();
        UpdateProgress();
    }

    private string BuildQuestionFeedback(bool correct)
    {
        string explanation = string.IsNullOrWhiteSpace(_currentQuestion.SuccessText)
            ? "Keep this idea in mind for the next stop."
            : _currentQuestion.SuccessText;
        string result = correct
            ? $"Correct! {explanation} +2 Prairie Stars"
            : $"The best answer is {_currentQuestion.Options[_currentQuestion.CorrectIndex]}. {explanation}";

        if (!string.IsNullOrWhiteSpace(_currentQuestion.SourceTitle))
        {
            result += $" · Source: {_currentQuestion.SourceTitle}";
        }

        return result;
    }

    private void PresentFamilyActivity()
    {
        string[] chapterActivities = _activeAreaId switch
        {
            "big_woods" => new[] { "tracking", "fiddle" },
            "prairie" => new[] { "shared_chores", "baking" },
            "plum_creek" => new[] { "baking", "tracking" },
            "schoolhouse" => new[] { "shared_chores", "fiddle" },
            _ => new[] { "fiddle", "baking" }
        };
        int activityIndex = Math.Clamp(
            AppState.ActiveProfile!.SavedSession.LauraActivitiesCompleted,
            0,
            chapterActivities.Length - 1);
        _currentFamilyActivity = chapterActivities[activityIndex];
        _familyActivityActive = true;
        _familyActivityResolved = false;
        _questionResolved = false;
        _continueButton.Visible = false;
        _optionsGrid.Visible = true;

        (_patternLabels, string title, string instructions) = _currentFamilyActivity switch
        {
            "tracking" => (
                new[] { "Deer Track", "Rabbit Track", "Creek", "Camp" },
                "Laura's Tracking Memory",
                "Watch the trail clues light up, then repeat them in the same order."),
            "baking" => (
                new[] { "Measure", "Pour", "Mix", "Knead" },
                "Kitchen Rhythm",
                "Watch the bread-making pattern, then repeat it in the same order."),
            "shared_chores" => (
                new[] { "Water", "Sweep", "Wood", "Garden" },
                "Family Chore Round",
                "The whole family shares the work. Watch the chore pattern, then repeat it."),
            _ => (
                new[] { "Low Note", "High Note", "Clap", "Rest" },
                "Pa's Fiddle Memory",
                "Watch the beat pattern, then play it back from memory.")
        };

        _questionLabel.Text = $"Family Mini-Game: {title}\n{instructions}";
        _feedbackLabel.Text = "Get ready — watch first!";
        _pattern.Clear();
        _patternInputIndex = 0;
        int length = 3 + Math.Min(1, AppState.ActiveProfile!.SavedSession.LauraActivitiesCompleted);
        for (int index = 0; index < length; index++)
        {
            _pattern.Add(_rng.RandiRange(0, _optionButtons.Length - 1));
        }

        for (int index = 0; index < _optionButtons.Length; index++)
        {
            _optionButtons[index].Visible = true;
            _optionButtons[index].Disabled = true;
            _optionButtons[index].Modulate = Colors.White;
            _optionButtons[index].Text = _patternLabels[index];
        }

        int playbackVersion = ++_patternPlaybackVersion;
        _ = PlayPatternAsync(playbackVersion);
        UpdateProgress();
    }

    private async Task PlayPatternAsync(int playbackVersion)
    {
        _acceptPatternInput = false;
        SetOptionButtonsEnabled(false);
        await ToSignal(GetTree().CreateTimer(0.55f), SceneTreeTimer.SignalName.Timeout);

        foreach (int buttonIndex in _pattern)
        {
            if (!IsInsideTree() || playbackVersion != _patternPlaybackVersion)
            {
                return;
            }

            Button button = _optionButtons[buttonIndex];
            button.Modulate = new Color(1f, 0.86f, 0.38f);
            button.Text = $"★ {_patternLabels[buttonIndex]} ★";
            PlayPatternTone(buttonIndex);
            await ToSignal(GetTree().CreateTimer(0.48f), SceneTreeTimer.SignalName.Timeout);

            if (!IsInsideTree() || playbackVersion != _patternPlaybackVersion)
            {
                return;
            }

            button.Modulate = Colors.White;
            button.Text = _patternLabels[buttonIndex];
            await ToSignal(GetTree().CreateTimer(0.16f), SceneTreeTimer.SignalName.Timeout);
        }

        if (!IsInsideTree() || playbackVersion != _patternPlaybackVersion)
        {
            return;
        }

        _patternInputIndex = 0;
        _acceptPatternInput = true;
        SetOptionButtonsEnabled(true);
        _feedbackLabel.Text = "Your turn — repeat the pattern!";
        _optionButtons[0].GrabFocus();
    }

    private void HandlePatternInput(int optionIndex)
    {
        if (!_acceptPatternInput || _familyActivityResolved)
        {
            return;
        }

        if (optionIndex != _pattern[_patternInputIndex])
        {
            _acceptPatternInput = false;
            SetOptionButtonsEnabled(false);
            _feedbackLabel.Text = "That clue was out of order. No penalty — watch the pattern and try again.";
            _wrongSfx.Play();
            int playbackVersion = ++_patternPlaybackVersion;
            _ = PlayPatternAsync(playbackVersion);
            return;
        }

        PlayPatternTone(optionIndex);
        _patternInputIndex += 1;
        _feedbackLabel.Text = _patternInputIndex < _pattern.Count
            ? $"Nice! {_patternInputIndex}/{_pattern.Count} clues remembered."
            : "Pattern complete!";

        if (_patternInputIndex < _pattern.Count)
        {
            return;
        }

        CompleteFamilyActivity();
    }

    private void PlayPatternTone(int optionIndex)
    {
        _patternSfx.PitchScale = 0.88f + (0.08f * Math.Clamp(optionIndex, 0, 3));
        _patternSfx.Play();
    }

    private void CompleteFamilyActivity()
    {
        var profile = AppState.ActiveProfile!;
        _acceptPatternInput = false;
        _familyActivityResolved = true;
        profile.SavedSession.LauraActivitiesCompleted += 1;
        profile.Journey.FamilyActivitiesCompleted += 1;
        profile.PrairieStars += 3;
        profile.GameScore += 75;
        AwardFamilyActivitySupplies();
        AppState.UnlockEligibleBadges();
        AppState.SaveProfile();

        SetOptionButtonsEnabled(false);
        _feedbackLabel.Text = $"Pattern complete! {GetActivityRewardText()} +3 Prairie Stars";
        _successSfx.Play();
        _continueButton.Text = "Continue the Journey";
        _continueButton.Visible = true;
        _continueButton.GrabFocus();
        UpdateProgress();
    }

    private void AwardFamilyActivitySupplies()
    {
        JourneyProgress journey = AppState.ActiveProfile!.Journey;
        switch (_currentFamilyActivity)
        {
            case "tracking":
                journey.Food += 4;
                break;
            case "baking":
                journey.Food += 3;
                journey.Morale = Math.Min(10, journey.Morale + 1);
                break;
            case "shared_chores":
                journey.Wood += 4;
                break;
            default:
                journey.Morale = Math.Min(10, journey.Morale + 2);
                break;
        }
    }

    private string GetActivityRewardText()
    {
        return _currentFamilyActivity switch
        {
            "tracking" => "Careful observation found useful trail food.",
            "baking" => "The bread is ready and the food basket is fuller.",
            "shared_chores" => "Shared work added wood and kept the camp tidy.",
            _ => "The fiddle tune lifted everyone's morale."
        };
    }

    private void ContinueAdventure()
    {
        if (_completionShown)
        {
            if (_completionMastered)
            {
                ReturnFromAdventure();
            }
            else
            {
                StartArea(_activeAreaId, true);
            }
            return;
        }

        if (!_questionResolved && !_familyActivityResolved)
        {
            return;
        }

        _questionResolved = false;
        _familyActivityResolved = false;
        _familyActivityActive = false;
        _continueButton.Visible = false;
        PresentNextAdventureStep();
    }

    private void AdvanceJourneyAfterStop()
    {
        JourneyProgress journey = AppState.ActiveProfile!.Journey;
        journey.Day += 1;
        journey.Miles += _rng.RandiRange(8, 18);
        journey.Food = Math.Max(0, journey.Food - _rng.RandiRange(1, 3));
        journey.Wood = Math.Max(0, journey.Wood - _rng.RandiRange(0, 1));
    }

    private void FinishMiniGame()
    {
        if (AppState.ActiveProfile == null || _completionShown)
        {
            return;
        }

        var profile = AppState.ActiveProfile;
        int correct = profile.SavedSession.LauraCorrectAnswers;
        _completionMastered = correct >= MasteryCorrect;
        _completionShown = true;
        _familyActivityActive = false;
        _acceptPatternInput = false;
        _patternPlaybackVersion++;

        int awardCountBefore = profile.UnlockedAwards.Count;
        if (_completionMastered)
        {
            profile.MiniGamesCompleted += 1;
            profile.GameScore += 250;
            profile.PrairieStars += 10;
            profile.Journey.Miles += 20;
            profile.Journey.Health = Math.Min(10, profile.Journey.Health + 1);
        }
        else
        {
            profile.PrairieStars += 2;
        }

        AppState.UnlockEligibleBadges();
        if (profile.UnlockedAwards.Count > awardCountBefore)
        {
            _awardSfx.Play();
        }
        else if (_completionMastered)
        {
            _successSfx.Play();
        }

        string returnSubject = GetReturnSubject();
        AppState.SetCurrentSubject(returnSubject);
        AppState.SaveResumePoint(
            "res://Scenes/LearningModule.tscn",
            $"Continue {AppState.GetSubjectDisplayName(returnSubject)}");
        profile.SavedSession.LauraAreaId = "";
        profile.SavedSession.LauraQuestionsAnswered = 0;
        profile.SavedSession.LauraCorrectAnswers = 0;
        profile.SavedSession.LauraActivitiesCompleted = 0;
        profile.SavedSession.LauraFreePlay = false;
        AppState.SaveProfile();

        _optionsGrid.Visible = false;
        _areaLabel.Text = _completionMastered ? "Adventure Mastered!" : "Almost There";
        _questionLabel.Text = _completionMastered
            ? $"You mastered {correct}/{KnowledgeGoal} knowledge stops and completed both memory mini-games."
            : $"You completed both memory mini-games and mastered {correct}/{KnowledgeGoal} knowledge stops. Master {MasteryCorrect} to finish this chapter.";
        _feedbackLabel.Text = _completionMastered
            ? "+10 Prairie Stars, a 250-point trail bonus, and 20 journey miles!"
            : "+2 Prairie Stars for your effort. Review the explanations, then try this chapter once more.";
        _progressLabel.Text = BuildStatusLine(_completionMastered ? "Chapter complete." : "Practice makes progress.");
        _continueButton.Text = _completionMastered
            ? (_freePlayEntry ? "Back To Chapter Map" : "Return to Lesson")
            : "Try This Chapter Again";
        _continueButton.Visible = true;
        _continueButton.GrabFocus();
    }

    private void ReturnFromAdventure()
    {
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        _patternPlaybackVersion++;
        string returnSubject = GetReturnSubject();
        AppState.SetCurrentSubject(returnSubject);
        AppState.SaveResumePoint(
            "res://Scenes/LearningModule.tscn",
            $"Continue {AppState.GetSubjectDisplayName(returnSubject)}");
        AppState.ActiveProfile.SavedSession.LauraAreaId = "";
        AppState.ActiveProfile.SavedSession.LauraQuestionsAnswered = 0;
        AppState.ActiveProfile.SavedSession.LauraCorrectAnswers = 0;
        AppState.ActiveProfile.SavedSession.LauraActivitiesCompleted = 0;
        AppState.ActiveProfile.SavedSession.LauraFreePlay = false;
        AppState.SaveProfile();

        if (_freePlayEntry)
        {
            _completionShown = false;
            _completionMastered = false;
            BuildRouteButtons();
            ShowTrailMap();
            return;
        }

        GetTree().ChangeSceneToFile("res://Scenes/LearningModule.tscn");
    }

    private void GoHome()
    {
        _patternPlaybackVersion++;
        AppState.SaveProfile();
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private string GetReturnSubject()
    {
        string savedSubject = AppState.ActiveProfile!.SavedSession.SubjectKey;
        if (!string.IsNullOrWhiteSpace(savedSubject) && savedSubject != "laura")
        {
            return savedSubject;
        }

        return string.IsNullOrWhiteSpace(AppState.ActiveProfile.LastPlayedSubject)
            || AppState.ActiveProfile.LastPlayedSubject == "laura"
            ? "math"
            : AppState.ActiveProfile.LastPlayedSubject;
    }

    private void ResetSavedAdventureProgress()
    {
        SavedSessionData saved = AppState.ActiveProfile!.SavedSession;
        saved.LauraQuestionsAnswered = 0;
        saved.LauraCorrectAnswers = 0;
        saved.LauraActivitiesCompleted = 0;
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

    private void UpdateProgress()
    {
        if (AppState.ActiveProfile == null)
        {
            return;
        }

        SavedSessionData saved = AppState.ActiveProfile.SavedSession;
        _progressLabel.Text = BuildStatusLine(
            $"Knowledge {Math.Min(saved.LauraQuestionsAnswered, KnowledgeGoal)}/{KnowledgeGoal} · Memory games {Math.Min(saved.LauraActivitiesCompleted, ActivityGoal)}/{ActivityGoal} · Mastery {saved.LauraCorrectAnswers}/{KnowledgeGoal}");
    }

    private string BuildStatusLine(string prefix)
    {
        JourneyProgress journey = AppState.ActiveProfile!.Journey;
        return $"{prefix}\nDay {journey.Day} · Miles {journey.Miles} · Food {journey.Food} · Wood {journey.Wood}\nHealth {journey.Health}/10 · Morale {journey.Morale}/10 · Stars {AppState.ActiveProfile.PrairieStars} · Badges {AppState.ActiveProfile.UnlockedAwards.Count}";
    }

    private string RandomTrailEvent()
    {
        string[] events =
        {
            "Imagined adventure moment: a creek crossing slows the family.",
            "Imagined adventure moment: clouds gather over the prairie grass.",
            "Imagined adventure moment: everyone stops to repair a loose wagon peg.",
            "Imagined adventure moment: a neighbor shares news from the next settlement.",
            "Imagined adventure moment: the family checks supplies before moving on."
        };

        return events[_rng.RandiRange(0, events.Length - 1)];
    }

    private void ApplyAreaBackground(LauraAreaDefinition area)
    {
        if (area == null || string.IsNullOrWhiteSpace(area.BackgroundPath) || !ResourceLoader.Exists(area.BackgroundPath))
        {
            return;
        }

        _background.Texture = GD.Load<Texture2D>(area.BackgroundPath);
    }
}
