using Godot;
using System;

public partial class ProfileSelection : Control
{
    private ItemList _profileList = null!;
    private LineEdit _nameInput = null!;
    private Label _statusLabel = null!;
    private Button _deleteButton = null!;
    private string _pendingDeleteName = "";

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        _profileList = GetNode<ItemList>("SafeArea/Center/Panel/VBox/Profiles");
        _nameInput = GetNode<LineEdit>("SafeArea/Center/Panel/VBox/NewNameInput");
        _statusLabel = GetNode<Label>("SafeArea/Center/Panel/VBox/StatusLabel");
        _deleteButton = GetNode<Button>("SafeArea/Center/Panel/VBox/DeleteButton");

        GetNode<Button>("SafeArea/Center/Panel/VBox/CreateButton").Pressed += OnCreatePressed;
        GetNode<Button>("SafeArea/Center/Panel/VBox/SelectButton").Pressed += OnSelectPressed;
        _deleteButton.Pressed += OnDeletePressed;
        GetNode<Button>("SafeArea/Center/Panel/VBox/BackButton").Pressed += OnBackPressed;
        _profileList.ItemSelected += OnProfileHighlighted;

        RefreshList();
    }

    private void RefreshList()
    {
        _profileList.Clear();
        foreach (string name in AppState.GetProfileNames())
        {
            _profileList.AddItem(name);
        }

        _statusLabel.Text = AppState.ActiveProfile != null
            ? $"Current: {AppState.ActiveProfile.FirstName}"
            : "Current: none";
    }

    private void OnCreatePressed()
    {
        ResetDeleteConfirmation();
        string name = _nameInput.Text.Trim();
        if (name.Length < 2)
        {
            _statusLabel.Text = "Enter a first name with at least 2 characters.";
            return;
        }

        if (AppState.ProfileExists(name))
        {
            _statusLabel.Text = $"A learner named {name} already exists. Select that profile, or add an initial to create another.";
            return;
        }

        if (AppState.SetActiveProfile(name))
        {
            _nameInput.Text = "";
            RefreshList();
            ContinueFlow();
        }
        else
        {
            _statusLabel.Text = "Could not create profile.";
        }
    }

    private void OnSelectPressed()
    {
        ResetDeleteConfirmation();
        int[] selected = _profileList.GetSelectedItems();
        if (selected.Length == 0)
        {
            _statusLabel.Text = "Select a profile first.";
            return;
        }

        string name = _profileList.GetItemText(selected[0]);
        if (AppState.SetActiveProfile(name))
        {
            ContinueFlow();
        }
        else
        {
            _statusLabel.Text = "Could not load selected profile.";
        }
    }

    private void OnDeletePressed()
    {
        int[] selected = _profileList.GetSelectedItems();
        if (selected.Length == 0)
        {
            _statusLabel.Text = "Select a profile to delete.";
            return;
        }

        string name = _profileList.GetItemText(selected[0]);
        if (_pendingDeleteName != name)
        {
            _pendingDeleteName = name;
            _deleteButton.Text = $"Confirm Delete {name}";
            _statusLabel.Text = $"Press the confirm button again to permanently delete {name}'s local progress.";
            return;
        }

        if (AppState.DeleteProfile(name))
        {
            ResetDeleteConfirmation();
            RefreshList();
            _statusLabel.Text = $"Deleted profile: {name}";
            return;
        }

        _statusLabel.Text = $"Could not delete profile: {name}";
    }

    private void OnProfileHighlighted(long index)
    {
        ResetDeleteConfirmation();
        string name = _profileList.GetItemText((int)index);
        _statusLabel.Text = AppState.ActiveProfile?.FirstName == name
            ? $"{name} is active · Grade {AppState.ActiveProfile.CurrentGrade} · Stars {AppState.ActiveProfile.PrairieStars} · Badges {AppState.ActiveProfile.UnlockedBadges.Count}"
            : $"Selected learner: {name}";
    }

    private void ResetDeleteConfirmation()
    {
        _pendingDeleteName = "";
        if (_deleteButton != null)
        {
            _deleteButton.Text = "Delete Selected Profile";
        }
    }

    private void OnBackPressed()
    {
        AppState.PendingSceneAfterProfile = "";
        AppState.PendingLauraFreePlay = false;
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void ContinueFlow()
    {
        string target = string.IsNullOrWhiteSpace(AppState.PendingSceneAfterProfile)
            ? "res://Scenes/MainMenu.tscn"
            : AppState.PendingSceneAfterProfile;

        AppState.PendingSceneAfterProfile = "";
        GetTree().ChangeSceneToFile(target);
    }
}
