using Godot;
using System.Threading.Tasks;

public partial class SplashLogo : Control
{
	public override async void _Ready()
	{
		AppState.Initialize();
		UiHelpers.ApplyConfiguredFont(this);

		// Automatically proceed to the next splash after a few seconds.
			float delay = AppState.Settings.ReducedMotion ? 0.1f : 1.4f;
			await ToSignal(GetTree().CreateTimer(delay), "timeout");
		ChangeScene();
	}

	public override void _Input(InputEvent @event)
	{
		// Skip splash on any key or mouse button press.
		if (@event is InputEventKey || @event is InputEventMouseButton)
		{
			ChangeScene();
		}
	}

	private void ChangeScene()
	{
		// Ensure the scene only changes once.
		if (GetTree().CurrentScene.SceneFilePath.EndsWith("SplashLogo.tscn"))
		{
			GetTree().ChangeSceneToFile("res://Scenes/WulterkensSplash.tscn");
		}
	}
}
