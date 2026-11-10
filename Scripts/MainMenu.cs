using Godot;

public partial class MainMenu : Control
{
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

		var welcome = GetNode<Label>("SafeArea/Center/Panel/VBox/WelcomeLabel");
		var continueButton = GetNode<Button>("SafeArea/Center/Panel/VBox/ContinueButton");
		if (AppState.ActiveProfile == null)
		{
			welcome.Text = "No local learner selected";
			continueButton.Visible = false;
		}
			else
			{
				welcome.Text = $"Welcome, {AppState.ActiveProfile.DisplayName} · Prairie Stars {AppState.ActiveProfile.PrairieStars} · Trail Score {AppState.ActiveProfile.GameScore} · Badges {AppState.ActiveProfile.UnlockedAwards.Count}";
				continueButton.Visible = AppState.HasResumePoint();
				continueButton.Text = AppState.GetResumeLabel();
			}

			GetNode<Button>("SafeArea/Center/Panel/VBox/TrailButton").Text = "Explore Laura's World (Free Play)";

		continueButton.Pressed += OnContinuePressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/LearnButton").Pressed += OnLearnPressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/TrailButton").Pressed += OnTrailPressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/ProgressButton").Pressed += OnProgressPressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/ProfilesButton").Pressed += OnProfilesPressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/SettingsButton").Pressed += OnOptionsPressed;
		GetNode<Button>("SafeArea/Center/Panel/VBox/ExitButton").Pressed += () => GetTree().Quit();
	}

	private void OnLearnPressed()
	{
		AppState.PendingLauraFreePlay = false;
		if (AppState.ActiveProfile == null)
		{
			AppState.PendingSceneAfterProfile = "res://Scenes/GradeSelection.tscn";
			GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/GradeSelection.tscn");
	}

	private void OnTrailPressed()
	{
		AppState.PendingLauraFreePlay = true;
		if (AppState.ActiveProfile == null)
		{
			AppState.PendingSceneAfterProfile = "res://Scenes/LauraMiniGame.tscn";
			GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/LauraMiniGame.tscn");
	}

	private void OnContinuePressed()
	{
		if (AppState.ActiveProfile?.SavedSession == null || !AppState.ActiveProfile.SavedSession.HasResumePoint)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(AppState.ActiveProfile.SavedSession.SubjectKey))
		{
			AppState.SetCurrentSubject(AppState.ActiveProfile.SavedSession.SubjectKey);
		}

		AppState.PendingLauraFreePlay = AppState.ActiveProfile.SavedSession.LauraFreePlay;

		GetTree().ChangeSceneToFile(AppState.ActiveProfile.SavedSession.ScenePath);
	}

	private void OnProfilesPressed()
	{
		AppState.PendingLauraFreePlay = false;
		AppState.PendingSceneAfterProfile = "res://Scenes/MainMenu.tscn";
		GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
	}

	private void OnProgressPressed()
	{
		if (AppState.ActiveProfile == null)
		{
			AppState.PendingSceneAfterProfile = "res://Scenes/ProgressScreen.tscn";
			GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/ProgressScreen.tscn");
	}

	private void OnOptionsPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/OptionsMenu.tscn");
	}
}
