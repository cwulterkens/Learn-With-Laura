using Godot;
using System;

public partial class Recess : Control
{
    private Label _activityLabel = null!;
    private Button _stretchButton = null!;
    private Button _breathingButton = null!;
    private string[] _activePrompts = Array.Empty<string>();
    private int _promptIndex;
    private bool _breakActive;
    private LearningBreakChoice _activeBreakChoice;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _activityLabel = GetNode<Label>("SafeArea/Center/Panel/Scroll/VBox/ActivityLabel");
        _stretchButton = GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/HuntingButton");
        _breathingButton = GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/BakingButton");
        _stretchButton.Text = "Prairie Stretch · +2 health · +1 morale";
        _breathingButton.Text = "Quiet Fiddle Breathing · +1 health · +2 morale";
        _activityLabel.Text = BuildBreakIntro();

        _stretchButton.Pressed += () => StartOrAdvanceBreak("stretch");
        _breathingButton.Pressed += () => StartOrAdvanceBreak("breathing");

        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ReturnButton").Pressed += ReturnToLesson;
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/HomeButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://Scenes/PrairieTown.tscn");
    }

    private void StartOrAdvanceBreak(string mode)
    {
        if (!_breakActive)
        {
            _activeBreakChoice = mode == "stretch"
                ? LearningBreakChoice.Stretch
                : LearningBreakChoice.Breathing;
            _activePrompts = mode == "stretch"
                ? new[]
                {
                    "Stand or sit tall. Reach both hands toward the wide prairie sky.",
                    "Roll your shoulders slowly, then look left and right as if scanning the horizon.",
                    "March gently in place for eight counts, or tap your feet while seated."
                }
                : new[]
                {
                    "Breathe in gently for four fiddle beats: 1 · 2 · 3 · 4.",
                    "Hold softly for two beats: 1 · 2.",
                    "Breathe out slowly for four beats: 1 · 2 · 3 · 4."
                };
            _promptIndex = 0;
            _breakActive = true;
            _stretchButton.Disabled = mode != "stretch";
            _breathingButton.Disabled = mode != "breathing";
        }
        else
        {
            _promptIndex += 1;
        }

        if (_promptIndex >= _activePrompts.Length)
        {
            CompleteBreak();
            return;
        }

        _activityLabel.Text = $"Break step {_promptIndex + 1}/{_activePrompts.Length}\n{_activePrompts[_promptIndex]}";
        Button activeButton = _stretchButton.Disabled ? _breathingButton : _stretchButton;
        activeButton.Text = _promptIndex == _activePrompts.Length - 1 ? "Finish Break" : "Next Break Step";
    }

    private void CompleteBreak()
    {
        _breakActive = false;
        _stretchButton.Disabled = false;
        _breathingButton.Disabled = false;
        _stretchButton.Text = "Prairie Stretch · +2 health · +1 morale";
        _breathingButton.Text = "Quiet Fiddle Breathing · +1 health · +2 morale";

        if (AppState.ActiveProfile != null)
        {
            JourneyChangeResult result = JourneyResources.ApplyLearningBreak(
                AppState.ActiveProfile.Journey,
                _activeBreakChoice);
            _activityLabel.Text = $"Break complete! {result.Summary}\nYour next lesson is ready when you are.";
            AppState.SaveProfile();
            return;
        }

        _activityLabel.Text = "Break complete! Your next lesson is ready when you are.";
    }

    private string BuildBreakIntro()
    {
        if (AppState.ActiveProfile == null)
        {
            return "Choose a short movement or calming break before the next lesson.";
        }

        JourneyProgress journey = AppState.ActiveProfile.Journey;
        return $"You worked hard. Choose the kind of break your learner needs.\nHealth {journey.Health}/{JourneyResources.MaxHealth} · Morale {journey.Morale}/{JourneyResources.MaxMorale}";
    }

    private void ReturnToLesson()
    {
        string scene = AppState.ActiveProfile?.SavedSession?.ScenePath;
        GetTree().ChangeSceneToFile(string.IsNullOrWhiteSpace(scene)
            ? "res://Scenes/SubjectSelection.tscn"
            : scene);
    }
}
