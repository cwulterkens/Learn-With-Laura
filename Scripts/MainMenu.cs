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

		var welcome = GetNode<Label>("SafeArea/Center/Panel/Scroll/VBox/WelcomeLabel");
		var continueButton = GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ContinueButton");
		if (AppState.ActiveProfile == null)
		{
			welcome.Text = "No local learner selected";
			continueButton.Visible = false;
		}
			else
			{
				welcome.Text = $"Welcome, {AppState.ActiveProfile.DisplayName} · Prairie Stars {AppState.ActiveProfile.PrairieStars} · Prairie Pennies {AppState.ActiveProfile.PrairiePennies} · Badges {AppState.ActiveProfile.UnlockedAwards.Count}";
				continueButton.Visible = AppState.HasResumePoint();
				continueButton.Text = AppState.GetResumeLabel();
			}

		continueButton.Pressed += OnContinuePressed;
		GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/TownButton").Pressed += OnTownPressed;
		GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ProgressButton").Pressed += OnProgressPressed;
		GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ProfilesButton").Pressed += OnProfilesPressed;
		GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/SettingsButton").Pressed += OnOptionsPressed;
		GetNode<Button>("SafeArea/Center/Panel/Scroll/VBox/ExitButton").Pressed += () => GetTree().Quit();
	}

	private void OnTownPressed()
	{
		AppState.PendingLauraFreePlay = false;
		if (AppState.ActiveProfile == null)
		{
			AppState.PendingSceneAfterProfile = "res://Scenes/PrairieTown.tscn";
			GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/PrairieTown.tscn");
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
		AppState.PendingSceneAfterProfile = "res://Scenes/PrairieTown.tscn";
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
