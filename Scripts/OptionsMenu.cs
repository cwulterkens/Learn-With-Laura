using Godot;

public partial class OptionsMenu : Control
{
    private OptionButton _resolutionSelect = null!;
    private OptionButton _displayModeSelect = null!;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        var musicSlider = GetNode<HSlider>("SafeArea/Center/Panel/VBox/MusicSlider");
        var sfxSlider = GetNode<HSlider>("SafeArea/Center/Panel/VBox/SfxSlider");
        var muteToggle = GetNode<CheckBox>("SafeArea/Center/Panel/VBox/MuteToggle");
        var largeTextToggle = GetNode<CheckBox>("SafeArea/Center/Panel/VBox/LargeTextToggle");
        var highContrastToggle = GetNode<CheckBox>("SafeArea/Center/Panel/VBox/HighContrastToggle");
        var reducedMotionToggle = GetNode<CheckBox>("SafeArea/Center/Panel/VBox/ReducedMotionToggle");
        _resolutionSelect = GetNode<OptionButton>("SafeArea/Center/Panel/VBox/ResolutionSelect");
        _displayModeSelect = GetNode<OptionButton>("SafeArea/Center/Panel/VBox/DisplayModeSelect");

        musicSlider.Value = AppState.Settings.MusicVolume;
        sfxSlider.Value = AppState.Settings.SfxVolume;
        muteToggle.ButtonPressed = AppState.Settings.MasterMute;
        largeTextToggle.ButtonPressed = AppState.Settings.LargeText;
        highContrastToggle.ButtonPressed = AppState.Settings.HighContrast;
        reducedMotionToggle.ButtonPressed = AppState.Settings.ReducedMotion;
        PopulateDisplayOptions();

        musicSlider.ValueChanged += value =>
        {
            AppState.Settings.MusicVolume = (float)value;
            AppState.SaveSettings();
        };

        sfxSlider.ValueChanged += value =>
        {
            AppState.Settings.SfxVolume = (float)value;
            AppState.SaveSettings();
        };

        muteToggle.Toggled += value =>
        {
            AppState.Settings.MasterMute = value;
            AppState.SaveSettings();
        };

        largeTextToggle.Toggled += value =>
        {
            AppState.Settings.LargeText = value;
            AppState.SaveSettings();
            UiHelpers.ApplyConfiguredFont(this);
        };

        highContrastToggle.Toggled += value =>
        {
            AppState.Settings.HighContrast = value;
            AppState.SaveSettings();
            UiHelpers.ApplyConfiguredFont(this);
        };

        reducedMotionToggle.Toggled += value =>
        {
            AppState.Settings.ReducedMotion = value;
            AppState.SaveSettings();
        };

        _resolutionSelect.ItemSelected += OnResolutionSelected;
        _displayModeSelect.ItemSelected += OnDisplayModeSelected;
        GetNode<Button>("SafeArea/Center/Panel/VBox/BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void PopulateDisplayOptions()
    {
        _displayModeSelect.Clear();
        _displayModeSelect.AddItem("Windowed", 0);
        _displayModeSelect.AddItem("Fullscreen", 1);
        _displayModeSelect.Select(AppState.Settings.Fullscreen ? 1 : 0);

        _resolutionSelect.Clear();
        int selectedIndex = 0;
        int index = 0;
        foreach (string key in AppState.SupportedResolutions.Keys)
        {
            string label = key switch
            {
                "1024x768" or "1280x960" => $"{key} (4:3)",
                "1280x720" or "1366x768" or "1600x900" or "1920x1080" => $"{key} (16:9)",
                "1280x800" or "1440x900" or "1680x1050" => $"{key} (16:10)",
                "1536x1024" => $"{key} (native art)",
                _ => key
            };

            _resolutionSelect.AddItem(label);
            _resolutionSelect.SetItemMetadata(index, key);
            if (key == AppState.Settings.Resolution)
            {
                selectedIndex = index;
            }
            index++;
        }

        _resolutionSelect.Select(selectedIndex);
    }

    private void OnResolutionSelected(long itemIndex)
    {
        string key = _resolutionSelect.GetItemMetadata((int)itemIndex).AsString();
        AppState.Settings.Resolution = key;
        AppState.SaveSettings();
    }

    private void OnDisplayModeSelected(long itemIndex)
    {
        AppState.Settings.Fullscreen = itemIndex == 1;
        AppState.SaveSettings();
    }
}
