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
	private int _selectedMenuItem;
	private int _resolutionIndex = 2;
	private string _editingAction;
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
		_uiFont = new SystemFont { FontNames = new[] { "Microsoft YaHei", "SimHei" } };
		LoadResolution();
		_session.Initialize(GroundY);
		_worldPresentation.Sync(_session, true);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_mainMenuOpen || _settingsOpen) { QueueRedraw(); return; }
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
				if (menuKey.Keycode == Key.F1) { _settingsOpen = true; return; }
				if (menuKey.Keycode == Key.Up || menuKey.Keycode == Key.W) { _selectedMenuItem = (_selectedMenuItem + 2) % 3; return; }
				if (menuKey.Keycode == Key.Down || menuKey.Keycode == Key.S) { _selectedMenuItem = (_selectedMenuItem + 1) % 3; return; }
				if (menuKey.Keycode == Key.Enter || menuKey.Keycode == Key.Space) { ActivateSelectedMenuItem(); return; }
			}
			if (@event is InputEventMouseMotion menuMotion)
			{
				for (var i = 0; i < 3; i++)
					if (MenuButton(i).HasPoint(menuMotion.Position)) { _selectedMenuItem = i; break; }
			}
			if (@event is InputEventMouseButton menuMouse && menuMouse.Pressed && menuMouse.ButtonIndex == MouseButton.Left)
			{
				var mouse = GetViewport().GetMousePosition();
				for (var i = 0; i < 3; i++)
					if (MenuButton(i).HasPoint(mouse)) { _selectedMenuItem = i; ActivateSelectedMenuItem(); break; }
			}
			return;
		}
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.F1) { _editingAction = null; _settingsOpen = !_settingsOpen; return; }
			if (_settingsOpen && keyEvent.Keycode == Key.Escape) { _editingAction = null; _settingsOpen = false; return; }
			if (_settingsOpen && _editingAction != null) { _input.Rebind(_editingAction, keyEvent.Keycode); _editingAction = null; return; }
			if (!_settingsOpen) _input.Capture(@event);
		}
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (_settingsOpen)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && SettingsCloseButton().HasPoint(GetViewport().GetMousePosition())) { _editingAction = null; _settingsOpen = false; return; }
				if (_editingAction != null) { _input.Rebind(_editingAction, mouseEvent.ButtonIndex); _editingAction = null; }
				else SelectSettingRow(GetViewport().GetMousePosition());
				return;
			}
			if (mouseEvent.ButtonIndex == MouseButton.Left && GameExitButton().HasPoint(GetViewport().GetMousePosition()))
			{
				GetTree().Quit();
				return;
			}
			_input.Capture(@event);
		}
	}

	private void LoadResolution()
	{
		var config = new ConfigFile();
		if (config.Load("user://controls.cfg") == Error.Ok)
			_resolutionIndex = Mathf.Clamp(config.GetValue("display", "resolution", 2).AsInt32(), 0, Resolutions.Length - 1);
		ApplyResolution();
	}

	private void CycleResolution()
	{
		_resolutionIndex = (_resolutionIndex + 1) % Resolutions.Length;
		ApplyResolution();
		MoveWorldToGround();
		var config = new ConfigFile();
		config.Load("user://controls.cfg");
		config.SetValue("display", "resolution", _resolutionIndex);
		config.Save("user://controls.cfg");
	}

	private void ApplyResolution() => GetWindow().ContentScaleSize = Resolutions[_resolutionIndex];

	private void MoveWorldToGround()
	{
		_session.MoveToGround(GroundY);
		_worldPresentation.Sync(_session, _mainMenuOpen);
	}
	private Rect2 SettingsPanel()
	{
		var viewport = GetViewportRect().Size;
		var height = Mathf.Min(viewport.Y * .9f, 900);
		var width = height * 1.5f;
		if (width > viewport.X * .92f) { width = viewport.X * .92f; height = width / 1.5f; }
		return new Rect2((viewport.X - width) / 2, (viewport.Y - height) / 2, width, height);
	}

	private Rect2 SettingsContent()
	{
		var panel = SettingsPanel();
		return new Rect2(panel.Position + panel.Size * new Vector2(.2f, .18f), panel.Size * new Vector2(.6f, .64f));
	}

	private float SettingsRowHeight() => Mathf.Clamp(Mathf.Floor((SettingsContent().Size.Y - 110) / 14), 20, 28);
	private Rect2 ResolutionRow()
	{
		var content = SettingsContent();
		return new Rect2(content.Position + new Vector2(0, 68), new Vector2(content.Size.X, SettingsRowHeight()));
	}
	private Rect2 SettingRow(int index)
	{
		var resolution = ResolutionRow();
		var height = SettingsRowHeight();
		return new Rect2(resolution.Position + new Vector2(0, resolution.Size.Y + 6 + index * height), new Vector2(resolution.Size.X, height - 2));
	}

	private void SelectSettingRow(Vector2 mouse)
	{
		if (ResolutionRow().HasPoint(mouse)) { CycleResolution(); return; }
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
	private Rect2 MenuStartButton() => MenuButton(0);
	private Rect2 MenuSettingsButton() => MenuButton(1);
	private Rect2 MenuExitButton() => MenuButton(2);
	private Rect2 GameExitButton() => new(GetViewportRect().Size.X - 128, GetViewportRect().Size.Y - 52, 110, 32);
	private Rect2 SettingsCloseButton()
	{
		var content = SettingsContent();
		var size = Mathf.Clamp(SettingsRowHeight() + 8, 32, 40);
		return new Rect2(content.End.X - size, content.Position.Y + 4, size, size);
	}

	private void ActivateSelectedMenuItem()
	{
		if (_selectedMenuItem == 0) StartGame();
		else if (_selectedMenuItem == 1) _settingsOpen = true;
		else GetTree().Quit();
	}

	private void StartGame()
	{
		_mainMenuOpen = false;
		_session.Start();
		_worldPresentation.Sync(_session, false);
	}

	public override void _Draw()
	{
		var size = GetViewportRect().Size;
		_titleBackdropView.Visible = _mainMenuOpen;
		_gameBackdropView.Visible = !_mainMenuOpen;
		UpdateMenuView();
		if (_mainMenuOpen)
		{
			if (_settingsOpen) DrawSettings(size);
			return;
		}
		DrawHud(size);
	}

	private void UpdateMenuView()
	{
		_mainMenuView.Visible = _mainMenuOpen && !_settingsOpen;
		for (var i = 0; i < 3; i++)
		{
			var view = i == 0 ? _startButtonView : i == 1 ? _settingsButtonView : _exitButtonView;
			view.Position = MenuButton(i).Position;
			view.Size = MenuButton(i).Size;
			view.Texture = i == _selectedMenuItem ? _menuButtonSelected : _menuButtonNormal;
		}
	}

	private void DrawHud(Vector2 size)
	{
		var font = ThemeDB.FallbackFont;
		DrawRect(new Rect2(18, 18, 380, 110), new Color("080b12", .78f));
		DrawString(font, new Vector2(32, 44), $"\u4e3b\u706b {MathF.Ceiling(_session.Fuel),3}%   \u7b49\u7ea7 {_session.FireLevel}/3   \u68a6\u7070 {_session.DreamAsh:0}", HorizontalAlignment.Left, -1, 18, new Color("ffd797"));
		DrawString(font, new Vector2(32, 69), $"\u751f\u547d {_session.Health:0}/5   \u4f59\u706b {_session.Ember:0}%   \u8bb0\u5fc6\u6728 {_session.Wood}", HorizontalAlignment.Left, -1, 17, Colors.White);
		DrawString(font, new Vector2(32, 97), _session.Message, HorizontalAlignment.Left, 560, 14, new Color("b9c5d2"));
		DrawString(font, new Vector2(18, size.Y - 42), "WASD \u79fb\u52a8/\u8df3\u8dc3 | \u5de6\u952e\u6325\u65a7 | \u53f3\u952e\u706b\u79cd | F \u780d\u6811 | E \u6dfb\u706b | U \u5347\u7ea7 | Q \u706b\u70ac | C \u91ce\u8425\u706b", HorizontalAlignment.Left, -1, 14, new Color("d0d8df"));
		DrawTextureRect(_settingsIcon, new Rect2(size.X - 250, size.Y - 39, 24, 24), false);
		DrawString(font, new Vector2(size.X - 220, size.Y - 18), "F1 \u8bbe\u7f6e", HorizontalAlignment.Left, -1, 14, new Color("aebbc8"));
		DrawIconButton(GameExitButton(), _closeIcon, "\u9000\u51fa\u6e38\u620f", new Color("442939"));
		if (_session.NameFound && _session.Player.DistanceTo(new Vector2(430, GroundY)) < 80)
			DrawString(font, new Vector2(size.X / 2 - 165, size.Y - 72), "\u540d\u5b57\u788e\u7247\uff1aB \u71c3\u70e7\uff08+\u706b\u52bf/+\u68a6\u7070\uff09  G \u5f52\u8fd8", HorizontalAlignment.Left, -1, 15, new Color("d9e8ff"));
		DrawMiniMap(size);
		if (_finalNight > 0) DrawString(font, new Vector2(size.X - 218, 142), $"\u70fd\u706b\u957f\u591c\uff1a{MathF.Ceiling(_finalNight)}", HorizontalAlignment.Left, -1, 17, new Color("ffb4a2"));
		if (_settingsOpen) DrawSettings(size);
	}

	private void DrawMiniMap(Vector2 size)
	{
		var panel = new Rect2(size.X - 222, 18, 204, 110);
		DrawRect(panel, new Color("080b12", .82f)); DrawRect(panel, new Color("607080"), false, 1);
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 19), "\u8ff7\u96fe\u5730\u56fe", HorizontalAlignment.Left, -1, 14, new Color("c9d7e6"));
		const float worldWidth = 3600;
		Vector2 Marker(Vector2 world) => new(panel.Position.X + 9 + (world.X + worldWidth / 2) / worldWidth * (panel.Size.X - 18), panel.Position.Y + 63);
		DrawRect(new Rect2(panel.Position + new Vector2(9, 62), new Vector2(panel.Size.X - 18, 2)), new Color("314150"));
		DrawCircle(Marker(_session.MainFire), 5, new Color("ff9d4d"));
		foreach (var fire in _session.Fires) DrawCircle(Marker(fire.Position), 3, new Color("ffd46b"));
		foreach (var enemy in _session.Enemies) if (_session.IsVisible(enemy.Position)) DrawCircle(Marker(enemy.Position), 3, new Color("c66a77"));
		var sight = (155 + _session.Ember * 2.1f - _session.DreamAsh * 10) / worldWidth * (panel.Size.X - 18);
		DrawLine(Marker(_session.Player) - Vector2.Right * sight, Marker(_session.Player) + Vector2.Right * sight, new Color("dfeeff", .28f), 5);
		DrawCircle(Marker(_session.Player), 4, new Color("ecf4ff"));
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 97), "\u767d\u8272\uff1a\u4f60  \u6a59\u8272\uff1a\u4e3b\u706b", HorizontalAlignment.Left, -1, 11, new Color("9faebb"));
	}

	private void DrawMenuButton(Rect2 rect, string text, Color color)
	{
		DrawRect(rect, color);
		DrawRect(rect, new Color("d5b476"), false, 1);
		var fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.Size.Y * .45f), 18, 24);
		DrawString(_uiFont ?? ThemeDB.FallbackFont, rect.Position + new Vector2(0, rect.Size.Y / 2 + fontSize * .35f), text, HorizontalAlignment.Center, rect.Size.X, fontSize, Colors.White);
	}

	private void DrawIconButton(Rect2 rect, Texture2D icon, string text, Color color)
	{
		DrawMenuButton(rect, text, color);
		if (icon != null) DrawTextureRect(icon, new Rect2(rect.Position + new Vector2(5, 4), new Vector2(rect.Size.Y - 8, rect.Size.Y - 8)), false);
	}

	private void DrawSettings(Vector2 size)
	{
		var panel = SettingsPanel();
		var content = SettingsContent();
		var font = _uiFont ?? ThemeDB.FallbackFont;
		DrawRect(new Rect2(Vector2.Zero, size), new Color("05070b", .62f));
		if (_settingsPageFrame != null) DrawTextureRect(_settingsPageFrame, panel, false);
		else { DrawRect(panel, new Color("0b1019", .96f)); DrawRect(panel, new Color("d3a65a"), false, 2); }
		var titleSize = Mathf.Clamp(Mathf.RoundToInt(panel.Size.Y * .04f), 24, 34);
		var helpSize = Mathf.Clamp(Mathf.RoundToInt(panel.Size.Y * .021f), 14, 18);
		var titleIconSize = titleSize + 6;
		DrawTextureRect(_settingsIcon, new Rect2(content.Position + new Vector2(0, 3), new Vector2(titleIconSize, titleIconSize)), false);
		DrawStringOutline(font, content.Position + new Vector2(titleIconSize + 10, titleSize), "\u8bbe\u7f6e", HorizontalAlignment.Left, -1, titleSize, 2, new Color("1b100b", .9f));
		DrawString(font, content.Position + new Vector2(titleIconSize + 10, titleSize), "\u8bbe\u7f6e", HorizontalAlignment.Left, -1, titleSize, new Color("ffe0a0"));
		if (_closeIcon != null) DrawTextureRect(_closeIcon, SettingsCloseButton(), false);
		DrawString(font, content.Position + new Vector2(0, 58), _editingAction == null ? "\u70b9\u51fb\u5206\u8fa8\u7387\u5207\u6362\uff1b\u70b9\u51fb\u64cd\u4f5c\u540e\u6309\u65b0\u6309\u952e" : "\u8bf7\u6309\u4e0b\u65b0\u7684\u6309\u952e\u6216\u9f20\u6807\u952e", HorizontalAlignment.Left, content.Size.X, helpSize, new Color("d2d8df"));
		var resolution = Resolutions[_resolutionIndex];
		var resolutionRow = ResolutionRow();
		var rowFontSize = Mathf.Clamp(Mathf.RoundToInt(resolutionRow.Size.Y * .62f), 13, 18);
		var baseline = resolutionRow.Size.Y / 2 + rowFontSize * .35f;
		var rowIconSize = Mathf.Max(14, resolutionRow.Size.Y - 6);
		DrawRect(resolutionRow, new Color("101824", .78f));
		DrawTextureRect(_displayIcon, new Rect2(resolutionRow.Position + new Vector2(6, 3), new Vector2(rowIconSize, rowIconSize)), false);
		DrawString(font, resolutionRow.Position + new Vector2(rowIconSize + 14, baseline), "\u5206\u8fa8\u7387", HorizontalAlignment.Left, -1, rowFontSize, Colors.White);
		DrawString(font, resolutionRow.Position + new Vector2(resolutionRow.Size.X * .58f, baseline), $"{resolution.X} \u00d7 {resolution.Y}", HorizontalAlignment.Left, resolutionRow.Size.X * .3f, rowFontSize, new Color("ffd285"));
		DrawTextureRect(_fullscreenIcon, new Rect2(resolutionRow.End - new Vector2(rowIconSize + 4, rowIconSize + 3), new Vector2(rowIconSize, rowIconSize)), false);
		for (var i = 0; i < ActionBindings.Definitions.Length; i++)
		{
			var (action, label) = ActionBindings.Definitions[i]; var row = SettingRow(i); var selected = _editingAction == action;
			var fontSize = Mathf.Clamp(Mathf.RoundToInt(row.Size.Y * .66f), 13, 18);
			var textY = row.Size.Y / 2 + fontSize * .35f;
			DrawRect(row, selected ? new Color("825e35", .88f) : new Color("101824", .72f));
			DrawString(font, row.Position + new Vector2(12, textY), label, HorizontalAlignment.Left, row.Size.X * .52f, fontSize, Colors.White);
			DrawString(font, row.Position + new Vector2(row.Size.X * .58f, textY), _input.LabelFor(action), HorizontalAlignment.Left, row.Size.X * .36f, fontSize, new Color("ffd285"));
		}
		DrawString(font, new Vector2(content.Position.X, content.End.Y - 8), "F1 / Esc \u5173\u95ed\u8bbe\u7f6e\u3002\u4fee\u6539\u4f1a\u81ea\u52a8\u4fdd\u5b58\u3002", HorizontalAlignment.Center, content.Size.X, helpSize, new Color("b8c2cc"));
	}
}
