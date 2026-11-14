#nullable enable

using Godot;
using System;
using System.Collections.Generic;

public partial class PrairieTown : Control
{
	private const float MoveSpeed = 245f;
	private const float SaveIntervalSeconds = 2f;

	private sealed record TownLocation(
		string Id,
		string Name,
		string Action,
		string Message,
		Rect2 InteractionArea,
		bool OpensStore = false,
		string DestinationScene = "",
		string DestinationButton = "",
		bool LaunchLauraFreePlay = false);

	private readonly List<TownLocation> _locations = new()
	{
		new TownLocation(
			"prairie_house", "Prairie House", "Visit home",
			"Home feels cozier with every treasure you bring back from town. The garden is ready for a Prairie Seed Packet.",
			new Rect2(70, 810, 275, 135)),
		new TownLocation(
			"hunting_field", "Northwest Field", "Join Pa in the field",
			"Pa is studying tracks near the edge of the field. Join him for a calm lesson in observation, patience, and responsible choices.",
			new Rect2(55, 235, 220, 190), false,
			"res://Scenes/HuntingWithPa.tscn", "Go Hunting with Pa"),
		new TownLocation(
			"schoolhouse", "One-Room Schoolhouse", "Visit the schoolhouse",
			"The school bell is ready to ring. Step inside to choose your grade and begin today's learning.",
			new Rect2(370, 235, 235, 105), false,
			"res://Scenes/GradeSelection.tscn", "Enter School"),
		new TownLocation(
			"post_office", "Post Office", "Check the post office",
			"A letter waits for the next mail wagon: Dear learner, the town is proud of the work you have done.",
			new Rect2(565, 410, 215, 105)),
		new TownLocation(
			"general_store", "Town General Store", "Enter the general store",
			"The shelves are full of small treasures earned through learning.",
			new Rect2(870, 420, 365, 105), true),
		new TownLocation(
			"bakery", "Town Bakery", "Visit the bakery",
			"Warm bread is cooling by the window. The baker says careful measuring is both math and good cooking.",
			new Rect2(1120, 625, 300, 105)),
		new TownLocation(
			"community_garden", "Community Garden", "Explore the garden",
			"Sunflowers, cabbages, beans, and prairie flowers grow side by side. Every plant has a different job in the garden.",
			new Rect2(1145, 225, 350, 100)),
		new TownLocation(
			"notice_board", "Town Notice Board", "Read the notices",
			"Town challenge: discover every building. Each first discovery earns one Prairie Penny.",
			new Rect2(770, 470, 170, 155)),
		new TownLocation(
			"creek_bridge", "Creek Bridge", "Listen at the bridge",
			"Water slips around the stones below. You spot tracks in the mud leading back toward town.",
			new Rect2(1240, 690, 220, 155)),
		new TownLocation(
			"exploration_wagon", "Exploration Wagon", "Climb into the wagon",
			"The wagon is packed for a journey through Laura's world. Choose a chapter, explore a region, and return to town whenever you are ready.",
			new Rect2(410, 700, 220, 230), false,
			"res://Scenes/LauraMiniGame.tscn", "Explore Laura's World", true)
	};

	private readonly Dictionary<Button, string> _storeButtons = new();
	private Node2D _world = null!;
	private Sprite2D _player = null!;
	private Label _coinLabel = null!;
	private Label _locationLabel = null!;
	private Label _promptLabel = null!;
	private Label _messageLabel = null!;
	private Label _storeFeedbackLabel = null!;
	private Label _inventoryLabel = null!;
	private Button _messageActionButton = null!;
	private Panel _messagePanel = null!;
	private Panel _storePanel = null!;
	private Panel _inventoryPanel = null!;
	private AudioStreamPlayer _music = null!;
	private AudioStreamPlayer _selectSfx = null!;
	private TownLocation? _nearbyLocation;
	private TownLocation? _pendingTravelLocation;
	private Vector2 _lastDirection = Vector2.Down;
	private float _saveTimer;

	public override void _Ready()
	{
		AppState.Initialize();
		if (AppState.ActiveProfile == null)
		{
			AppState.PendingSceneAfterProfile = "res://Scenes/PrairieTown.tscn";
			GetTree().ChangeSceneToFile("res://Scenes/ProfileSelection.tscn");
			return;
		}

		_world = GetNode<Node2D>("World");
		_player = GetNode<Sprite2D>("World/Player");
		_coinLabel = GetNode<Label>("UI/HUD/TopBar/Margin/HBox/CoinLabel");
		_locationLabel = GetNode<Label>("UI/HUD/TopBar/Margin/HBox/LocationLabel");
		_promptLabel = GetNode<Label>("UI/HUD/PromptPanel/Margin/PromptLabel");
		_messagePanel = GetNode<Panel>("UI/HUD/MessagePanel");
		_messageLabel = GetNode<Label>("UI/HUD/MessagePanel/Margin/VBox/MessageLabel");
		_messageActionButton = GetNode<Button>("UI/HUD/MessagePanel/Margin/VBox/ActionButton");
		_storePanel = GetNode<Panel>("UI/HUD/StorePanel");
		_storeFeedbackLabel = GetNode<Label>("UI/HUD/StorePanel/Margin/Scroll/VBox/StoreFeedbackLabel");
		_inventoryPanel = GetNode<Panel>("UI/HUD/InventoryPanel");
		_inventoryLabel = GetNode<Label>("UI/HUD/InventoryPanel/Margin/VBox/InventoryLabel");

		_music = GetNode<AudioStreamPlayer>("Music");
		_selectSfx = GetNode<AudioStreamPlayer>("SelectSfx");
		AppState.ConfigureMusicPlayer(_music);
		AppState.ConfigureSfxPlayer(_selectSfx);
		if (!_music.Playing)
		{
			_music.Play();
		}

		GetNode<Button>("UI/HUD/TopBar/Margin/HBox/SatchelButton").Pressed += OpenInventory;
		GetNode<Button>("UI/HUD/TopBar/Margin/HBox/HomeButton").Pressed += ReturnHome;
		GetNode<Button>("UI/HUD/MessagePanel/Margin/VBox/CloseButton").Pressed += CloseMessage;
		_messageActionButton.Pressed += ActivateLocationDestination;
		GetNode<Button>("UI/HUD/StorePanel/Margin/Scroll/VBox/CloseButton").Pressed += CloseStore;
		GetNode<Button>("UI/HUD/InventoryPanel/Margin/VBox/CloseButton").Pressed += CloseInventory;

		BindStoreButton("SeedButton", "seed_packet");
		BindStoreButton("RibbonButton", "blue_ribbon");
		BindStoreButton("BookButton", "storybook");
		BindStoreButton("BonnetButton", "red_bonnet");
		BindStoreButton("LanternButton", "tin_lantern");
		BindStoreButton("HorseButton", "toy_horse");

		Resized += FitWorldToViewport;
		FitWorldToViewport();

		ProfileData profile = AppState.ActiveProfile;
		PrairieTownEconomy.Normalize(profile);
		Vector2 savedPosition = ClampToMap(new Vector2(profile.TownPositionX, profile.TownPositionY));
		_player.Position = HitsObstacle(savedPosition)
			? new Vector2(PrairieTownCollision.SafeStartX, PrairieTownCollision.SafeStartY)
			: savedPosition;
		profile.TownVisitCount += 1;
		int starterPurse = PrairieTownEconomy.PrepareFirstTownVisit(profile);
		AppState.SaveProfile();

		RefreshHud();
		RefreshStore();
		if (starterPurse > 0)
		{
			ShowMessage(
				$"Your earlier learning filled a starter purse with {starterPurse} Prairie Pennies! " +
				"Keep learning to earn more, then walk to the general store to spend them.");
		}
	}

	public override void _Process(double delta)
	{
		if (AppState.ActiveProfile == null)
		{
			return;
		}

		if (!IsModalOpen())
		{
			Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
			if (direction.LengthSquared() > 0.01f)
			{
				direction = direction.Normalized();
				_lastDirection = direction;
				MovePlayer(direction * MoveSpeed * (float)delta);
				UpdatePlayerFrame(direction);
			}
		}

		UpdateNearbyLocation();
		_saveTimer += (float)delta;
		if (_saveTimer >= SaveIntervalSeconds)
		{
			SaveTownPosition();
			_saveTimer = 0f;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport()?.SetInputAsHandled();
			if (_storePanel.Visible)
			{
				CloseStore();
			}
			else if (_inventoryPanel.Visible)
			{
				CloseInventory();
			}
			else if (_messagePanel.Visible)
			{
				CloseMessage();
			}
			else
			{
				ReturnHome();
			}
			return;
		}

		if (@event.IsActionPressed("ui_accept") && !IsModalOpen() && _nearbyLocation != null)
		{
			GetViewport()?.SetInputAsHandled();
			InteractWith(_nearbyLocation);
		}
	}

	private void MovePlayer(Vector2 motion)
	{
		Vector2 position = _player.Position;
		Vector2 nextX = ClampToMap(position + new Vector2(motion.X, 0));
		if (!HitsObstacle(nextX))
		{
			position.X = nextX.X;
		}

		Vector2 nextY = ClampToMap(position + new Vector2(0, motion.Y));
		nextY.X = position.X;
		if (!HitsObstacle(nextY))
		{
			position.Y = nextY.Y;
		}
		_player.Position = position;
	}

	private void FitWorldToViewport()
	{
		if (!GodotObject.IsInstanceValid(_world))
		{
			return;
		}
		Vector2 viewportSize = Size;
		float scale = Mathf.Min(viewportSize.X / 1536f, viewportSize.Y / 1024f);
		_world.Scale = Vector2.One * scale;
		_world.Position = (viewportSize - new Vector2(1536f, 1024f) * scale) / 2f;
	}

	private bool HitsObstacle(Vector2 position)
	{
		return PrairieTownCollision.IsBlocked(position.X, position.Y);
	}

	private static Vector2 ClampToMap(Vector2 position)
	{
		return new Vector2(
			PrairieTownCollision.ClampX(position.X),
			PrairieTownCollision.ClampY(position.Y));
	}

	private void UpdatePlayerFrame(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_player.Frame = direction.X < 0 ? 1 : 2;
		}
		else
		{
			_player.Frame = direction.Y < 0 ? 3 : 0;
		}
	}

	private void UpdateNearbyLocation()
	{
		TownLocation? found = null;
		foreach (TownLocation location in _locations)
		{
			if (location.InteractionArea.HasPoint(_player.Position))
			{
				found = location;
				break;
			}
		}

		_nearbyLocation = found;
		if (!IsModalOpen())
		{
			_promptLabel.Text = found == null
				? "Move with arrow keys or a controller · Press Enter/Space to interact · Esc returns home"
				: $"Press Enter/Space to {found.Action.ToLowerInvariant()}";
		}

		string area = _player.Position.X < 520 && _player.Position.Y > 560
			? "Prairie Homestead"
			: _player.Position.X < 760
				? "Prairie Trail"
				: "Walnut Grove Town";
		_locationLabel.Text = area;
	}

	private void InteractWith(TownLocation location)
	{
		ProfileData profile = AppState.ActiveProfile!;
		int discoveryReward = PrairieTownEconomy.RecordDiscovery(profile, location.Id);
		string discoveryText = discoveryReward > 0
			? "\nNew place discovered! +1 Prairie Penny"
			: "";
		_selectSfx.Play();
		AppState.SaveProfile();
		RefreshHud();

		if (location.OpensStore)
		{
			_storeFeedbackLabel.Text = location.Message + discoveryText;
			RefreshStore();
			_storePanel.Visible = true;
			FirstAvailableStoreButton()?.GrabFocus();
			return;
		}

		if (!string.IsNullOrWhiteSpace(location.DestinationScene))
		{
			_pendingTravelLocation = location;
			_messageActionButton.Text = location.DestinationButton;
			_messageActionButton.Visible = true;
			ShowMessage($"{location.Name}\n{location.Message}{discoveryText}", true);
			return;
		}

		ShowMessage($"{location.Name}\n{location.Message}{discoveryText}");
	}

	private void BindStoreButton(string nodeName, string itemId)
	{
		Button button = GetNode<Button>($"UI/HUD/StorePanel/Margin/Scroll/VBox/{nodeName}");
		_storeButtons[button] = itemId;
		button.Pressed += () => BuyItem(itemId);
	}

	private void BuyItem(string itemId)
	{
		TownPurchaseResult result = PrairieTownEconomy.TryPurchase(AppState.ActiveProfile!, itemId);
		_storeFeedbackLabel.Text = result.Message;
		_selectSfx.Play();
		if (result.Succeeded)
		{
			AppState.SaveProfile();
		}
		RefreshHud();
		RefreshStore();
	}

	private void RefreshStore()
	{
		if (AppState.ActiveProfile == null)
		{
			return;
		}

		foreach ((Button button, string itemId) in _storeButtons)
		{
			TownStoreItem item = null!;
			foreach (TownStoreItem candidate in PrairieTownEconomy.StoreItems)
			{
				if (candidate.Id == itemId)
				{
					item = candidate;
					break;
				}
			}
			bool owned = AppState.ActiveProfile.TownInventory.Contains(itemId);
			button.Text = owned
				? $"✓ {item.Name} · In your satchel"
				: $"{item.Name} · {item.Price} pennies\n{item.Description}";
			button.Disabled = owned;
		}
	}

	private Button? FirstAvailableStoreButton()
	{
		foreach (Button button in _storeButtons.Keys)
		{
			if (!button.Disabled)
			{
				return button;
			}
		}
		return GetNode<Button>("UI/HUD/StorePanel/Margin/Scroll/VBox/CloseButton");
	}

	private void OpenInventory()
	{
		_inventoryLabel.Text = PrairieTownEconomy.GetInventorySummary(AppState.ActiveProfile!);
		_inventoryPanel.Visible = true;
		GetNode<Button>("UI/HUD/InventoryPanel/Margin/VBox/CloseButton").GrabFocus();
	}

	private void CloseInventory()
	{
		_inventoryPanel.Visible = false;
		GetNode<Button>("UI/HUD/TopBar/Margin/HBox/SatchelButton").GrabFocus();
	}

	private void ShowMessage(string text, bool hasAction = false)
	{
		_messageLabel.Text = text;
		_messageActionButton.Visible = hasAction;
		if (!hasAction)
		{
			_pendingTravelLocation = null;
		}
		_messagePanel.Visible = true;
		(hasAction
			? _messageActionButton
			: GetNode<Button>("UI/HUD/MessagePanel/Margin/VBox/CloseButton")).GrabFocus();
	}

	private void CloseMessage()
	{
		_messagePanel.Visible = false;
		_messageActionButton.Visible = false;
		_pendingTravelLocation = null;
	}

	private void ActivateLocationDestination()
	{
		if (_pendingTravelLocation == null
			|| string.IsNullOrWhiteSpace(_pendingTravelLocation.DestinationScene))
		{
			return;
		}

		TownLocation destination = _pendingTravelLocation;
		SaveTownPosition();
		AppState.PendingLauraFreePlay = destination.LaunchLauraFreePlay;
		GetTree().ChangeSceneToFile(destination.DestinationScene);
	}

	private void CloseStore()
	{
		_storePanel.Visible = false;
		_storeFeedbackLabel.Text = "Choose something for your prairie life.";
	}

	private bool IsModalOpen()
	{
		return _messagePanel.Visible || _storePanel.Visible || _inventoryPanel.Visible;
	}

	private void RefreshHud()
	{
		ProfileData profile = AppState.ActiveProfile!;
		_coinLabel.Text = $"Prairie Pennies: {profile.PrairiePennies}";
	}

	private void SaveTownPosition()
	{
		if (AppState.ActiveProfile == null)
		{
			return;
		}
		AppState.ActiveProfile.TownPositionX = _player.Position.X;
		AppState.ActiveProfile.TownPositionY = _player.Position.Y;
		AppState.SaveProfile();
	}

	private void ReturnHome()
	{
		SaveTownPosition();
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}
}
