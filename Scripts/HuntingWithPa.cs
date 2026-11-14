using Godot;

public partial class HuntingWithPa : Control
{
    private const int ShotsNeeded = 3;

    private HuntingField _field = null!;
    private Label _roundLabel = null!;
    private Label _feedbackLabel = null!;
    private Label _statusLabel = null!;
    private Button _startButton = null!;
    private Panel _journalPanel = null!;
    private Label _journalLabel = null!;
    private AudioStreamPlayer _successSfx = null!;
    private AudioStreamPlayer _wrongSfx = null!;
    private int _successfulShots;
    private bool _roundActive;
    private string _lastAnimalId = "rabbit";
    private string _lastJournalEntry = "Rabbit — paired hind prints often land ahead of the smaller front prints.";

    public override void _Ready()
    {
        AppState.Initialize();
        UiHelpers.ApplyConfiguredFont(this);
        if (AppState.ActiveProfile == null)
        {
            AppState.PendingSceneAfterProfile = "res://Scenes/HuntingWithPa.tscn";
            GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
            return;
        }

        _field = GetNode<HuntingField>("Panel/Scroll/VBox/GameRow/FieldArea");
        _roundLabel = GetNode<Label>("Panel/Scroll/VBox/RoundLabel");
        _feedbackLabel = GetNode<Label>("Panel/Scroll/VBox/FeedbackLabel");
        _statusLabel = GetNode<Label>("Panel/Scroll/VBox/StatusLabel");
        _startButton = GetNode<Button>("Panel/Scroll/VBox/StartButton");
        _journalPanel = GetNode<Panel>("JournalPanel");
        _journalLabel = GetNode<Label>("JournalPanel/Margin/VBox/JournalLabel");
        _successSfx = GetNode<AudioStreamPlayer>("SuccessSfx");
        _wrongSfx = GetNode<AudioStreamPlayer>("WrongSfx");

        AppState.ConfigureSfxPlayer(_successSfx);
        AppState.ConfigureSfxPlayer(_wrongSfx);
        var music = GetNode<AudioStreamPlayer>("Music");
        AppState.ConfigureMusicPlayer(music);
        if (!music.Playing)
        {
            music.Play();
        }

        _field.SafeTargetHit += OnSafeTargetHit;
        _field.YoungAnimalPassed += OnYoungAnimalSelected;
        _field.ShotMissed += OnShotMissed;
        _startButton.Pressed += StartRound;
        GetNode<Button>("Panel/Scroll/VBox/JournalButton").Pressed += OpenJournal;
        GetNode<Button>("Panel/Scroll/VBox/ReturnButton").Pressed += ReturnToTown;
        GetNode<Button>("JournalPanel/Margin/VBox/CloseButton").Pressed += CloseJournal;

        HuntingWithPaProgress.Normalize(AppState.ActiveProfile);
        _roundLabel.Text = $"Field hunt · {ShotsNeeded} careful shots";
        _feedbackLabel.Text = "Pa says: Choose only adult rabbits and squirrels. Let every young animal pass.";
        UpdateStatus();
        _startButton.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel"))
        {
            return;
        }

        GetViewport()?.SetInputAsHandled();
        if (_journalPanel.Visible)
        {
            CloseJournal();
        }
        else
        {
            ReturnToTown();
        }
    }

    private void StartRound()
    {
        _successfulShots = 0;
        _roundActive = true;
        _startButton.Visible = false;
        _roundLabel.Text = $"Successful shots 0/{ShotsNeeded}";
        _feedbackLabel.Text = "The hunt has begun. Aim carefully and watch for the YOUNG — LET PASS marker.";
        _field.StartRound();
        UpdateStatus();
    }

    private void OnSafeTargetHit(string animalId, string journalEntry)
    {
        if (!_roundActive)
        {
            return;
        }

        _successfulShots += 1;
        _lastAnimalId = animalId;
        _lastJournalEntry = journalEntry;
        _successSfx.Play();
        string animalName = animalId == "rabbit" ? "adult rabbit" : "adult squirrel";
        _feedbackLabel.Text = $"Careful shot: {animalName}. Pa reminds Laura to take only what the family needs.";
        _roundLabel.Text = $"Successful shots {_successfulShots}/{ShotsNeeded}";
        UpdateStatus();

        if (_successfulShots >= ShotsNeeded)
        {
            CompleteRound();
        }
    }

    private void OnYoungAnimalSelected(string animalName)
    {
        if (!_roundActive)
        {
            return;
        }

        _wrongSfx.Play();
        _feedbackLabel.Text = $"Let the {animalName} pass. Young animals do not count, and there is no penalty—choose another target.";
    }

    private void OnShotMissed()
    {
        if (_roundActive)
        {
            _feedbackLabel.Text = "Missed—steady the slingshot and try again. There is no penalty.";
        }
    }

    private void CompleteRound()
    {
        _roundActive = false;
        _field.StopRound();
        HuntingTripResult result = HuntingWithPaProgress.CompleteTrip(
            AppState.ActiveProfile!,
            _lastAnimalId,
            _lastJournalEntry,
            responsiblyTookGame: true);
        AppState.SaveProfile();

        _roundLabel.Text = "Hunt complete";
        _feedbackLabel.Text = $"Laura and Pa head home after taking only what the family needs. {result.Summary}";
        _startButton.Text = "Start Another Hunt";
        _startButton.Visible = true;
        _startButton.GrabFocus();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        ProfileData profile = AppState.ActiveProfile!;
        string round = _roundActive ? $" · Shots {_successfulShots}/{ShotsNeeded}" : string.Empty;
        _statusLabel.Text =
            $"Food {profile.Journey.Food}/{JourneyResources.FoodCapacity} · " +
            $"Prairie Pennies {profile.PrairiePennies} · Hunts {profile.HuntingTripsCompleted}{round}";
    }

    private void OpenJournal()
    {
        _journalLabel.Text = HuntingWithPaProgress.BuildJournal(AppState.ActiveProfile!);
        _journalPanel.Visible = true;
        GetNode<Button>("JournalPanel/Margin/VBox/CloseButton").GrabFocus();
    }

    private void CloseJournal()
    {
        _journalPanel.Visible = false;
        GetNode<Button>("Panel/Scroll/VBox/JournalButton").GrabFocus();
    }

    private void ReturnToTown()
    {
        AppState.SaveProfile();
        GetTree().ChangeSceneToFile("res://Scenes/PrairieTown.tscn");
    }
}
