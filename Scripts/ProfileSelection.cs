using Godot;
using System;

public partial class ProfileSelection : Control
{
    private ItemList _profileList = null!;
    private LineEdit _nameInput = null!;
    private Label _statusLabel = null!;
    private Button _deleteButton = null!;
    private ConfirmationDialog _recoveryDialog = null!;
    private Button _startFreshButton = null!;
    private string _pendingDeleteName = "";
    private string _pendingRecoveryName = "";
    private bool _pendingBackupAvailable;

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);

        _profileList = GetNode<ItemList>("SafeArea/Center/Panel/VBox/Profiles");
        _nameInput = GetNode<LineEdit>("SafeArea/Center/Panel/VBox/NewNameInput");
        _statusLabel = GetNode<Label>("SafeArea/Center/Panel/VBox/StatusLabel");
        _deleteButton = GetNode<Button>("SafeArea/Center/Panel/VBox/DeleteButton");
        _recoveryDialog = new ConfirmationDialog
        {
            Title = "Recover Local Profile"
        };
        AddChild(_recoveryDialog);
        _startFreshButton = _recoveryDialog.AddButton("Start Fresh", true, "start_fresh");
        _recoveryDialog.Confirmed += OnRecoveryConfirmed;
        _recoveryDialog.Canceled += OnRecoveryCanceled;
        _recoveryDialog.CustomAction += OnRecoveryCustomAction;

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
            ? $"Current nickname: {AppState.ActiveProfile.DisplayName}"
            : "Current: none";
    }

    private void OnCreatePressed()
    {
        ResetDeleteConfirmation();
        string name = _nameInput.Text.Trim();
        if (name.Length < 2)
        {
            _statusLabel.Text = "Enter a nickname with at least 2 characters.";
            return;
        }

        if (AppState.ProfileExists(name))
        {
            _statusLabel.Text = $"The nickname {name} already exists. Select that learner, or choose another nickname.";
            return;
        }

        ProfileActivationResult result = AppState.TryActivateProfile(
            name,
            ProfileActivationChoice.StartFresh);
        if (result.Succeeded)
        {
            _nameInput.Text = "";
            RefreshList();
            ContinueFlow();
        }
        else
        {
            _statusLabel.Text = string.IsNullOrWhiteSpace(result.Error)
                ? "Could not create profile."
                : result.Error;
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
        ProfileActivationResult result = AppState.TryActivateProfile(name);
        if (result.Succeeded)
        {
            ContinueFlow();
        }
        else if (result.Status is ProfileActivationStatus.BackupAvailable
            or ProfileActivationStatus.Corrupt)
        {
            ShowRecoveryDialog(result);
        }
        else
        {
            _statusLabel.Text = result.Status == ProfileActivationStatus.NotFound
                ? "The selected local profile file is missing."
                : string.IsNullOrWhiteSpace(result.Error)
                    ? "Could not load selected profile."
                    : result.Error;
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
        _pendingRecoveryName = "";
        string name = _profileList.GetItemText((int)index);
        _statusLabel.Text = AppState.ActiveProfile?.DisplayName == name
            ? $"{name} is active · Grade {AppState.ActiveProfile.CurrentGrade} · Stars {AppState.ActiveProfile.PrairieStars} · Badges {AppState.ActiveProfile.UnlockedBadges.Count}"
            : $"Selected learner: {name}";
    }

    private void ResetDeleteConfirmation()
    {
        _pendingDeleteName = "";
        if (_deleteButton != null)
        {
            _deleteButton.Text = "Delete Selected Learner";
        }
    }

    private void OnBackPressed()
    {
        AppState.PendingSceneAfterProfile = "";
        AppState.PendingLauraFreePlay = false;
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void ShowRecoveryDialog(ProfileActivationResult result)
    {
        _pendingRecoveryName = result.ProfileName;
        _pendingBackupAvailable = result.Status == ProfileActivationStatus.BackupAvailable;
        _startFreshButton.Visible = _pendingBackupAvailable;
        _recoveryDialog.OkButtonText = _pendingBackupAvailable ? "Restore Backup" : "Start Fresh";
        _recoveryDialog.DialogText = _pendingBackupAvailable
            ? $"{result.ProfileName}'s current profile file could not be read, but a valid local backup is available.\n\nRestore Backup keeps the saved backup progress. Start Fresh archives the damaged file and resets this learner. Cancel leaves every file untouched."
            : $"{result.ProfileName}'s profile data could not be read, and no valid backup is available.\n\nStart Fresh archives the damaged file before resetting this learner. Cancel leaves every file untouched.";
        _recoveryDialog.PopupCentered(new Vector2I(760, 360));
    }

    private void OnRecoveryConfirmed()
    {
        ProfileActivationChoice choice = _pendingBackupAvailable
            ? ProfileActivationChoice.RestoreBackup
            : ProfileActivationChoice.StartFresh;
        CompleteRecovery(choice);
    }

    private void OnRecoveryCustomAction(StringName action)
    {
        if (action != "start_fresh")
        {
            return;
        }

        _recoveryDialog.Hide();
        CompleteRecovery(ProfileActivationChoice.StartFresh);
    }

    private void OnRecoveryCanceled()
    {
        if (!string.IsNullOrWhiteSpace(_pendingRecoveryName))
        {
            _statusLabel.Text = $"Recovery canceled. {_pendingRecoveryName}'s local files were not changed.";
        }
        _pendingRecoveryName = "";
    }

    private void CompleteRecovery(ProfileActivationChoice choice)
    {
        if (string.IsNullOrWhiteSpace(_pendingRecoveryName))
        {
            return;
        }

        string name = _pendingRecoveryName;
        _pendingRecoveryName = "";
        ProfileActivationResult result = AppState.TryActivateProfile(name, choice);
        if (result.Succeeded)
        {
            RefreshList();
            ContinueFlow();
            return;
        }

        _statusLabel.Text = string.IsNullOrWhiteSpace(result.Error)
            ? $"Could not recover profile: {name}."
            : result.Error;
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
