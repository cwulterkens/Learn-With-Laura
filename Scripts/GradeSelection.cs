using Godot;
using System;

public partial class GradeSelection : Control
{
    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        var gradeSpin = GetNode<SpinBox>("SafeArea/Center/Panel/Scroll/VBox/GradeSpin");
        var message = GetNode<Label>("SafeArea/Center/Panel/Scroll/VBox/MessageLabel");

        int initial = AppState.ActiveProfile?.CurrentGrade ?? AppState.Settings.LastSelectedGrade;
        gradeSpin.Value = Math.Clamp(initial, 1, 8);

        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/StartButton").Pressed += () =>
        {
            int grade = (int)Math.Round(gradeSpin.Value);
            grade = Math.Clamp(grade, 1, 8);

            AppState.CurrentGrade = grade;
            AppState.Settings.LastSelectedGrade = grade;

            if (AppState.ActiveProfile != null)
            {
                AppState.ActiveProfile.CurrentGrade = grade;
                AppState.SaveProfile();
            }

            AppState.SaveSettings();
            GetTree().ChangeSceneToFile("res://Scenes/SubjectSelection.tscn");
        };

        GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/PrairieTown.tscn");

        string name = AppState.ActiveProfile?.DisplayName ?? "Learner";
        message.Text = $"{name}, what grade are you in today?";
    }
}
