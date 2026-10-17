using Godot;
using System;

public partial class Main : Node2D
{
	private float GroundY => Resolutions[_resolutionIndex].Y * 7f / 9f;
	private static readonly Vector2I[] Resolutions =
	{
		new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440)
	};
	private Texture2D _menuButtonNormal;
	private Texture2D _menuButtonSelected;
	private Texture2D _settingsPageFrame;
	private Texture2D _settingsIcon;
	private Texture2D _displayIcon;
	private Texture2D _fullscreenIcon;
	private Texture2D _closeIcon;
	private Texture2D _statusBarsAtlas;
	private SystemFont _uiFont;
	[Export] private ActionBindings _input;
	[Export] private GameSession _session;
	[Export] private WorldPresentation _worldPresentation;
	[Export] private TextureRect _titleBackdropView;
	[Export] private TextureRect _gameBackdropView;
	[Export] private Control _mainMenuView;
	[Export] private TextureRect _startButtonView;
	[Export] private TextureRect _settingsButtonView;
	[Export] private TextureRect _exitButtonView;
	private bool _mainMenuOpen = true;
	private bool _settingsOpen;
	private bool _draggingVolume;
	private int _selectedMenuItem;
	private int _settingsTab;
	private int _resolutionIndex = 2;
	private float _masterVolume = .8f;
	private string _editingAction;
	private bool _inventorySplitMode;
	private bool _inventoryDragFromLoot;
	private int _inventoryDragSlot = -1;
	private int _inventorySplitSource = -1;
	private Inventory.Stack _inventoryDragStack;
	private float _finalNight => _session.FinalNight;

	public override void _Ready()
	{
		if (_input == null || _session == null || _worldPresentation == null)
			throw new InvalidOperationException("Main requires ActionBindings, GameSession, and WorldPresentation nodes.");
		_menuButtonNormal = GD.Load<Texture2D>("res://assets/art/runtime/ui/buttons/ui_button_menu_normal.png");
		_menuButtonSelected = GD.Load<Texture2D>("res://assets/art/runtime/ui/buttons/ui_button_menu_selected.png");
		_settingsPageFrame = GD.Load<Texture2D>("res://assets/art/runtime/ui/frames/ui_settings_page_frame.png");
		_settingsIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_settings.png");
		_displayIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_display.png");
		_fullscreenIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_fullscreen.png");
		_closeIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_close.png");
		_statusBarsAtlas = GD.Load<Texture2D>("res://assets/art/runtime/ui/frames/ui_status_bars_atlas.png");
		_uiFont = new SystemFont { FontNames = new[] { "Microsoft YaHei", "SimHei" } };
		LoadSettings();
		_session.Initialize(GroundY);
		SyncScreenState();
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_mainMenuOpen || _settingsOpen) { SyncScreenState(); QueueRedraw(); return; }
		var dt = (float)delta;
		_session.Tick(dt);
		_worldPresentation.Sync(_session, false);
		QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (_mainMenuOpen && !_settingsOpen)
		{
			if (@event is InputEventKey menuKey && menuKey.Pressed && !menuKey.Echo)
			{
				if (menuKey.Keycode == Key.F1) { _settingsOpen = true; SyncScreenState(); QueueRedraw(); return; }
				if (menuKey.Keycode == Key.Up || menuKey.Keycode == Key.W) { _selectedMenuItem = (_selectedMenuItem + 2) % 3; return; }
				if (menuKey.Keycode == Key.Down || menuKey.Keycode == Key.S) { _selectedMenuItem = (_selectedMenuItem + 1) % 3; return; }
				if (menuKey.Keycode == Key.Enter || menuKey.Keycode == Key.Space) { ActivateSelectedMenuItem(); return; }
			}
			if (@event is InputEventMouseMotion menuMotion)
				for (var i = 0; i < 3; i++) if (MenuButton(i).HasPoint(menuMotion.Position)) { _selectedMenuItem = i; break; }
			if (@event is InputEventMouseButton menuMouse && menuMouse.Pressed && menuMouse.ButtonIndex == MouseButton.Left)
			{
				var mouse = menuMouse.Position;
				for (var i = 0; i < 3; i++)
					if (MenuButton(i).HasPoint(mouse)) { _selectedMenuItem = i; ActivateSelectedMenuItem(); break; }
			}
			return;
		}
		if (_session.ComboOpen) { HandleComboInput(@event); return; }
		if (_settingsOpen && _draggingVolume && @event is InputEventMouseMotion volumeMotion) { SetMasterVolumeFromMouse(volumeMotion.Position.X); QueueRedraw(); return; }
		if (@event is InputEventMouseMotion swordMotion) { if (_session.InventoryOpen) return; if (!_settingsOpen) _session.CaptureSwordMouseMotion(swordMotion.Relative); }
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (!_settingsOpen && !_mainMenuOpen && _session.InventoryOpen && keyEvent.Keycode == Key.Escape) { _session.ToggleInventory(); return; }
			if (keyEvent.Keycode == Key.F1) { CloseVolumeDrag(); _editingAction = null; _settingsOpen = !_settingsOpen; SyncScreenState(); QueueRedraw(); return; }
			if (_settingsOpen && keyEvent.Keycode == Key.Escape) { CloseVolumeDrag(); _editingAction = null; _settingsOpen = false; SyncScreenState(); QueueRedraw(); return; }
			if (_settingsOpen && _editingAction != null) { _input.Rebind(_editingAction, keyEvent.Keycode); _editingAction = null; return; }
			if (_settingsOpen && keyEvent.Keycode == Key.Tab) { CloseVolumeDrag(); _settingsTab = 1 - _settingsTab; QueueRedraw(); return; }
			if (!_settingsOpen) _input.Capture(@event);
		}
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (!_settingsOpen && !_mainMenuOpen && _session.InventoryOpen) { HandleInventoryMouse(mouseEvent); return; }
			if (_settingsOpen)
			{
				if (!mouseEvent.Pressed) { CloseVolumeDrag(); return; }
				if (mouseEvent.ButtonIndex == MouseButton.Left && SettingsCloseButton().HasPoint(mouseEvent.Position)) { CloseVolumeDrag(); _editingAction = null; _settingsOpen = false; SyncScreenState(); QueueRedraw(); return; }
				for (var i = 0; i < 2; i++)
					if (mouseEvent.ButtonIndex == MouseButton.Left && SettingsTab(i).HasPoint(mouseEvent.Position)) { CloseVolumeDrag(); _settingsTab = i; _editingAction = null; QueueRedraw(); return; }
				if (_editingAction != null) { _input.Rebind(_editingAction, mouseEvent.ButtonIndex); _editingAction = null; }
				else SelectSettingRow(mouseEvent.Position, mouseEvent.ButtonIndex);
				QueueRedraw();
				return;
			}
			if (!mouseEvent.Pressed) return;
			if (mouseEvent.ButtonIndex == MouseButton.Left && GameExitButton().HasPoint(mouseEvent.Position))
			{
				GetTree().Quit();
				return;
			}
			_input.Capture(@event);
		}
	}

	private void LoadSettings()
	{
		var config = new ConfigFile();
		if (config.Load("user://controls.cfg") == Error.Ok)
		{
			_resolutionIndex = Mathf.Clamp(config.GetValue("display", "resolution", 2).AsInt32(), 0, Resolutions.Length - 1);
			_masterVolume = Mathf.Clamp(config.GetValue("audio", "master_volume", .8f).AsSingle(), 0, 1);
		}
		ApplyResolution();
		ApplyMasterVolume();
	}

	private void CycleResolution()
	{
		_resolutionIndex = (_resolutionIndex + 1) % Resolutions.Length;
		ApplyResolution();
		MoveWorldToGround();
		SaveSettings();
	}

	private void ApplyResolution() => GetWindow().ContentScaleSize = Resolutions[_resolutionIndex];
	private void ApplyMasterVolume()
	{
		AudioServer.SetBusMute(0, _masterVolume <= 0);
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Mathf.Max(_masterVolume, .0001f)));
	}
	private void SetMasterVolumeFromMouse(float mouseX)
	{
		var track = VolumeTrack();
		_masterVolume = Mathf.Clamp((mouseX - track.Position.X) / track.Size.X, 0, 1);
		ApplyMasterVolume();
	}
	private void CloseVolumeDrag()
	{
		if (!_draggingVolume) return;
		_draggingVolume = false;
		SaveSettings();
	}
	private void SaveSettings()
	{
		var config = new ConfigFile();
		config.Load("user://controls.cfg");
		config.SetValue("display", "resolution", _resolutionIndex);
		config.SetValue("audio", "master_volume", _masterVolume);
		config.Save("user://controls.cfg");
	}

	private void MoveWorldToGround()
	{
		_session.MoveToGround(GroundY);
		_worldPresentation.Sync(_session, _mainMenuOpen);
	}

	private void SelectSettingRow(Vector2 mouse, MouseButton button)
	{
		if (_settingsTab == 0)
		{
			if (button == MouseButton.Left && ResolutionRow().HasPoint(mouse)) CycleResolution();
			else
			{
				var track = VolumeTrack();
				var hitArea = new Rect2(track.Position.X, VolumeRow().Position.Y, track.Size.X, VolumeRow().Size.Y);
				if (button == MouseButton.Left && hitArea.HasPoint(mouse)) { _draggingVolume = true; SetMasterVolumeFromMouse(mouse.X); }
			}
			return;
		}
		for (var i = 0; i < ActionBindings.Definitions.Length; i++)
			if (SettingRow(i).HasPoint(mouse)) { _editingAction = ActionBindings.Definitions[i].Action; return; }
	}

	private Rect2 MenuButton(int index)
	{
		var size = GetViewportRect().Size;
		var width = Mathf.Clamp(size.X * .24f, 300, 420);
		var height = width * .275f;
		return new Rect2((size.X - width) / 2, size.Y * .4f + index * height * .92f, width, height);
	}

	private Rect2 GameExitButton() => new(GetViewportRect().Size.X - 128, GetViewportRect().Size.Y - 52, 110, 32);

	private void ActivateSelectedMenuItem()
	{
		if (_selectedMenuItem == 0) StartGame();
		else if (_selectedMenuItem == 1) { _settingsOpen = true; SyncScreenState(); QueueRedraw(); }
		else GetTree().Quit();
	}

	private void StartGame()
	{
		_mainMenuOpen = false;
		_session.Start();
		SyncScreenState();
		_worldPresentation.Sync(_session, false);
	}

	private void UpdateMenuView()
	{
		var menuVisible = _mainMenuOpen && !_settingsOpen;
		_mainMenuView.Visible = menuVisible;
		for (var i = 0; i < 3; i++)
		{
			var view = i == 0 ? _startButtonView : i == 1 ? _settingsButtonView : _exitButtonView;
			view.Position = MenuButton(i).Position;
			view.Size = MenuButton(i).Size;
			view.Texture = i == _selectedMenuItem ? _menuButtonSelected : _menuButtonNormal;
		}
	}

	private void SyncScreenState()
	{
		_titleBackdropView.Visible = _mainMenuOpen;
		_gameBackdropView.Visible = !_mainMenuOpen;
		_worldPresentation.Visible = !_mainMenuOpen;
		_mainMenuView.Visible = _mainMenuOpen && !_settingsOpen;
		if (!_mainMenuOpen) _worldPresentation.Sync(_session, false);
		QueueRedraw();
	}

	public override void _Draw()
	{
		var size = GetViewportRect().Size;
		if (_mainMenuOpen) UpdateMenuView();
		if (_mainMenuOpen)
		{
			if (_settingsOpen) DrawSettings(size);
			return;
		}
		DrawHud(size);
	}
}
