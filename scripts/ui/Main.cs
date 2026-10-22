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
	private Texture2D _settingsIcon;
	private Texture2D _closeIcon;
	private Texture2D _statusBarsAtlas;
	private SystemFont _uiFont;
	[Export] private ActionBindings _input;
	[Export] private GameSession _session;
	[Export] private WorldPresentation _worldPresentation;
	[Export] private SettingsView _settingsView;
	[Export] private TextureRect _titleBackdropView;
	[Export] private TextureRect _gameBackdropView;
	[Export] private Control _mainMenuView;
	[Export] private TextureRect _startButtonView;
	[Export] private TextureRect _settingsButtonView;
	[Export] private TextureRect _exitButtonView;
	[Export] private LayeredPanelCanvas _panelCanvas;
	[Export] private LootPanel _lootPanel;
	[Export] private SwordCaseHud _swordCaseHud;
	[Export] private ForgePanel _forgePanel;
	[Export] private FrostRealm _frostRealm;
	private bool _mainMenuOpen = true;
	private bool _settingsOpen;
	private int _selectedMenuItem;
	private int _resolutionIndex = 2;
	private float _masterVolume = .8f;
	private float _finalNight => _session.FinalNight;

	public override void _Ready()
	{
		if (_input == null || _session == null || _worldPresentation == null || _settingsView == null || _panelCanvas == null || _lootPanel == null || _swordCaseHud == null || _forgePanel == null || _frostRealm == null)
			throw new InvalidOperationException("Main requires its input, session, world, realm, and interface nodes.");
		_panelCanvas.DrawPanels = DrawLayeredPanels;
		_lootPanel.Session = _session;
		_forgePanel.Session = _session;
		_session.RealmRunReset += _frostRealm.Reset;
		_menuButtonNormal = GD.Load<Texture2D>("res://assets/art/runtime/ui/buttons/ui_button_menu_normal.png");
		_menuButtonSelected = GD.Load<Texture2D>("res://assets/art/runtime/ui/buttons/ui_button_menu_selected.png");
		_settingsIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_settings.png");
		_closeIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/icons/ui_icon_close.png");
		_statusBarsAtlas = GD.Load<Texture2D>("res://assets/art/runtime/ui/frames/ui_status_bars_atlas.png");
		_uiFont = new SystemFont { FontNames = new[] { "Microsoft YaHei", "SimHei" } };
		_settingsView.Input = _input;
		_settingsView.CloseRequested += CloseSettings;
		_settingsView.ResolutionCycleRequested += CycleResolution;
		_settingsView.VolumeChanged += volume => { _masterVolume = volume; ApplyMasterVolume(); };
		_settingsView.VolumeCommitted += SaveSettings;
		LoadSettings();
		_session.Initialize(GroundY);
		SyncScreenState();
		QueueRedraw();
		_panelCanvas.QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_mainMenuOpen || _settingsOpen) { SyncScreenState(); QueueRedraw(); _panelCanvas.QueueRedraw(); return; }
		var dt = (float)delta;
		var mouse = GetViewport().GetMousePosition();
		var attackTarget = new Vector2(_session.CameraFocus.X + mouse.X - GetViewport().GetVisibleRect().Size.X / 2, mouse.Y);
		_session.Tick(dt, _input.ConsumeGameplay(attackTarget));
		_frostRealm.Tick(_session, _session.InventoryOpen || _session.ComboOpen ? 0 : dt);
		_gameBackdropView.Visible = !_session.IsInRealm;
		_forgePanel.Sync();
		SyncSwordCaseHud();
		var daylight = _session.IsNight || _session.FinalNight > 0 ? new Color(.48f, .58f, .78f) : Colors.White;
		_gameBackdropView.Modulate = daylight;
		_worldPresentation.Modulate = daylight;
		SyncMouseMode();
		_worldPresentation.Sync(_session, false);
		QueueRedraw();
		_panelCanvas.QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (_mainMenuOpen && !_settingsOpen)
		{
			if (@event is InputEventKey menuKey && menuKey.Pressed && !menuKey.Echo)
			{
				if (menuKey.Keycode == Key.F1) { OpenSettings(); return; }
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
		if (_settingsOpen) { _settingsView.HandleInput(@event); return; }
		if (_session.ForgeOpen)
		{
			if (@event is InputEventKey forgeKey && forgeKey.Pressed && !forgeKey.Echo && forgeKey.Keycode == Key.Escape)
			{ _session.CloseForge(); _forgePanel.Sync(); SyncMouseMode(); }
			return;
		}
		if (@event is InputEventMouseMotion swordMotion)
		{
			if (_session.InventoryOpen) { _lootPanel.HandleMotion(swordMotion); return; }
			_session.CaptureSwordMouseMotion(swordMotion.Relative);
		}
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (_session.InventoryOpen && keyEvent.Keycode == Key.Escape) { _session.ToggleInventory(); SyncMouseMode(); return; }
			if (keyEvent.Keycode == Key.F1) { OpenSettings(); return; }
			_input.Capture(@event);
		}
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (_session.InventoryOpen) { _lootPanel.HandleInput(mouseEvent); return; }
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
		_settingsView.Resolution = Resolutions[_resolutionIndex];
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
		else if (_selectedMenuItem == 1) OpenSettings();
		else GetTree().Quit();
	}

	private void StartGame()
	{
		_mainMenuOpen = false;
		_session.Start();
		SyncScreenState();
		_worldPresentation.Sync(_session, false);
	}

	private void OpenSettings() { _settingsOpen = true; SyncScreenState(); }
	private void CloseSettings() { _settingsOpen = false; SyncScreenState(); }

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
		SyncMouseMode();
		_titleBackdropView.Visible = _mainMenuOpen;
		_gameBackdropView.Visible = !_mainMenuOpen && !_session.IsInRealm;
		_frostRealm.Visible = !_mainMenuOpen && _session.IsInRealm;
		_worldPresentation.Visible = !_mainMenuOpen;
		_mainMenuView.Visible = _mainMenuOpen && !_settingsOpen;
		_settingsView.Visible = _settingsOpen;
		_forgePanel.Sync();
		_settingsView.Resolution = Resolutions[_resolutionIndex];
		_settingsView.MasterVolume = _masterVolume;
		_settingsView.Refresh();
		SyncSwordCaseHud();
		if (!_mainMenuOpen) { _frostRealm.Tick(_session, 0); _worldPresentation.Sync(_session, false); }
		QueueRedraw();
	}

	private void SyncSwordCaseHud()
	{
		_swordCaseHud.Visible = !_mainMenuOpen && !_settingsOpen && !_session.InventoryOpen && !_session.ComboOpen && !_session.ForgeOpen;
		if (_swordCaseHud.Visible) _swordCaseHud.Sync(_session);
	}

	private void SyncMouseMode() => Input.MouseMode = !_mainMenuOpen && !_settingsOpen && _session.WantsMouseCapture
		? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;

	private void DrawLayeredPanels(CanvasItem canvas)
	{
		var size = GetViewportRect().Size;
		if (_session.ComboOpen) DrawCombo(canvas, size);
	}

	public override void _Draw()
	{
		var size = GetViewportRect().Size;
		if (_mainMenuOpen) UpdateMenuView();
		if (_mainMenuOpen) return;
		if (!_session.IsInRealm) DrawVillageBoundary(size);
		DrawHud(size);
	}
}
