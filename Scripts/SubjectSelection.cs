using Godot;

public partial class SubjectSelection : Control
{
    private Label _profileLabel = null!;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        if (AppState.ActiveProfile == null)
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/SubjectSelection.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
            return;
        }

        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _profileLabel = GetNode<Label>("SafeArea/Center/Panel/Scroll/VBox/ProfileLabel");
        _profileLabel.Text =
            $"{AppState.ActiveProfile.DisplayName} | Grade {AppState.CurrentGrade} | Choose your lesson";

        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/MathButton").Pressed += () => StartSubject("math");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ScienceButton").Pressed += () => StartSubject("science");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/HistoryButton").Pressed += () => StartSubject("history");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/SocialStudiesButton").Pressed += () => StartSubject("social_studies");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/GeographyButton").Pressed += () => StartSubject("geography");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/EnglishButton").Pressed += () => StartSubject("english");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ProfileButton").Pressed += () =>
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/SubjectSelection.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
        };
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/GradeButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://Scenes/GradeSelection.tscn");
        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://Scenes/PrairieTown.tscn");
    }

    private void StartSubject(string subjectKey)
    {
        AppState.SetCurrentSubject(subjectKey);
        AppState.SaveResumePoint(
            "res://Scenes/LearningModule.tscn",
            $"Continue {AppState.GetSubjectDisplayName(subjectKey)}");
        AppState.SaveProfile();
        GetTree().ChangeSceneToFile("res://Scenes/LearningModule.tscn");
    }
}
