using Godot;

public partial class WulterkensSplash : Control
{
	private float _timer;
	private TextureRect _creditsImage = null!;

	public override void _Ready()
	{
		AppState.Initialize();
			UiHelpers.ApplyConfiguredFont(this);
			_creditsImage = GetNode<TextureRect>("CreditsImage");
			_creditsImage.Modulate = new Color(1, 1, 1, 0);
			if (AppState.Settings.ReducedMotion)
			{
				GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
			}
	}

	public override void _Process(double delta)
	{
		_timer += (float)delta;
		float fadeIn = Mathf.Clamp(_timer / 0.7f, 0f, 1f);
			float fadeOut = Mathf.Clamp((2.2f - _timer) / 0.5f, 0f, 1f);
		float alpha = Mathf.Min(fadeIn, fadeOut);
		_creditsImage.Modulate = new Color(1, 1, 1, alpha);

			if (_timer >= 2.0f)
		{
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey || @event is InputEventMouseButton)
		{
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		}
	}
}
