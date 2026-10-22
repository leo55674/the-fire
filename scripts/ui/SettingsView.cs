using Godot;

public partial class SettingsView : Control
{
	[Signal] public delegate void CloseRequestedEventHandler();
	[Signal] public delegate void ResolutionCycleRequestedEventHandler();
	[Signal] public delegate void VolumeChangedEventHandler(float volume);
	[Signal] public delegate void VolumeCommittedEventHandler();

	public ActionBindings Input { get; set; }
	private int Tab { get; set; }
	private string EditingAction { get; set; }
	public Vector2I Resolution { get; set; } = new(1920, 1080);
	public float MasterVolume { get; set; } = .8f;
	private bool _draggingVolume;

	private ColorRect _backdrop;
	private TextureRect _panel;
	private Control _content;
	private ColorRect[] _tabs;
	private Label _help;
	private Label _footer;
	private TextureRect _close;
	private Control _generalPage;
	private Control _bindingPage;
	private ColorRect _resolutionRow;
	private Label _resolutionValue;
	private ColorRect _volumeRow;
	private ColorRect _volumeTrack;
	private ColorRect _volumeFill;
	private ColorRect _volumeKnob;
	private Label _volumeValue;
	private ColorRect[] _bindingRows;
	private Label[] _bindingValues;

	public override void _Ready()
	{
		_backdrop = GetNode<ColorRect>("Backdrop");
		_panel = GetNode<TextureRect>("Panel");
		_content = GetNode<Control>("Panel/Content");
		_tabs = new[] { GetNode<ColorRect>("Panel/Content/GeneralTab"), GetNode<ColorRect>("Panel/Content/BindingsTab") };
		_help = GetNode<Label>("Panel/Content/Help");
		_footer = GetNode<Label>("Panel/Content/Footer");
		_close = GetNode<TextureRect>("Panel/Content/Close");
		_generalPage = GetNode<Control>("Panel/Content/GeneralPage");
		_bindingPage = GetNode<Control>("Panel/Content/BindingPage");
		_resolutionRow = GetNode<ColorRect>("Panel/Content/GeneralPage/ResolutionRow");
		_resolutionValue = GetNode<Label>("Panel/Content/GeneralPage/ResolutionRow/Value");
		_volumeRow = GetNode<ColorRect>("Panel/Content/GeneralPage/VolumeRow");
		_volumeTrack = GetNode<ColorRect>("Panel/Content/GeneralPage/VolumeRow/Track");
		_volumeFill = GetNode<ColorRect>("Panel/Content/GeneralPage/VolumeRow/Track/Fill");
		_volumeKnob = GetNode<ColorRect>("Panel/Content/GeneralPage/VolumeRow/Track/Knob");
		_volumeValue = GetNode<Label>("Panel/Content/GeneralPage/VolumeRow/Value");
		_bindingRows = new ColorRect[ActionBindings.Definitions.Length];
		_bindingValues = new Label[_bindingRows.Length];
		for (var i = 0; i < _bindingRows.Length; i++)
		{
			_bindingRows[i] = GetNode<ColorRect>($"Panel/Content/BindingPage/Row{i:00}");
			_bindingValues[i] = _bindingRows[i].GetNode<Label>("Value");
		}
		Refresh();
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
		return new Rect2(panel.Position + panel.Size * new Vector2(.2f, .18f), panel.Size * new Vector2(.6f, .72f));
	}

	private Rect2 SettingsTab(int index)
	{
		var content = SettingsContent();
		var width = (content.Size.X - 8) / 2;
		return new Rect2(content.Position + new Vector2(index * (width + 8), 54), new Vector2(width, 36));
	}

	private float SettingsRowHeight() => Mathf.Clamp(Mathf.Floor((SettingsContent().Size.Y - 154) / ActionBindings.Definitions.Length), 17, 28);
	private Rect2 ResolutionRow()
	{
		var content = SettingsContent();
		return new Rect2(content.Position + new Vector2(0, 112), new Vector2(content.Size.X, 48));
	}
	private Rect2 VolumeRow() => new(ResolutionRow().Position + new Vector2(0, 58), ResolutionRow().Size);
	private Rect2 VolumeTrack()
	{
		var row = VolumeRow();
		return new Rect2(row.Position + new Vector2(row.Size.X * .4f, row.Size.Y / 2 - 3), new Vector2(row.Size.X * .42f, 6));
	}
	private Rect2 SettingRow(int index)
	{
		var content = SettingsContent();
		var height = SettingsRowHeight();
		return new Rect2(content.Position + new Vector2(0, 112 + index * height), new Vector2(content.Size.X, height - 2));
	}
	private Rect2 SettingsCloseButton()
	{
		var content = SettingsContent();
		return new Rect2(content.End - new Vector2(40, content.Size.Y - 4), new Vector2(40, 40));
	}

	public void HandleInput(InputEvent inputEvent)
	{
		if (_draggingVolume && inputEvent is InputEventMouseMotion motion)
		{
			SetVolumeFromMouse(motion.Position.X);
			return;
		}
		if (inputEvent is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F1 || key.Keycode == Key.Escape) { CloseVolumeDrag(); EditingAction = null; EmitSignal(SignalName.CloseRequested); return; }
			if (EditingAction != null) { Input.Rebind(EditingAction, key.Keycode); EditingAction = null; Refresh(); return; }
			if (key.Keycode == Key.Tab) { CloseVolumeDrag(); Tab = 1 - Tab; Refresh(); }
			return;
		}
		if (inputEvent is not InputEventMouseButton mouse) return;
		if (!mouse.Pressed) { CloseVolumeDrag(); return; }
		if (mouse.ButtonIndex == MouseButton.Left && SettingsCloseButton().HasPoint(mouse.Position))
		{
			CloseVolumeDrag(); EditingAction = null; EmitSignal(SignalName.CloseRequested); return;
		}
		for (var i = 0; i < 2; i++)
			if (mouse.ButtonIndex == MouseButton.Left && SettingsTab(i).HasPoint(mouse.Position))
			{
				CloseVolumeDrag(); Tab = i; EditingAction = null; Refresh(); return;
			}
		if (EditingAction != null)
		{
			if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) return;
			Input.Rebind(EditingAction, mouse.ButtonIndex); EditingAction = null;
		}
		else SelectRow(mouse.Position, mouse.ButtonIndex);
		Refresh();
	}

	private void SelectRow(Vector2 mouse, MouseButton button)
	{
		if (Tab == 0)
		{
			if (button == MouseButton.Left && ResolutionRow().HasPoint(mouse)) EmitSignal(SignalName.ResolutionCycleRequested);
			else
			{
				var track = VolumeTrack();
				var hitArea = new Rect2(track.Position.X, VolumeRow().Position.Y, track.Size.X, VolumeRow().Size.Y);
				if (button == MouseButton.Left && hitArea.HasPoint(mouse)) { _draggingVolume = true; SetVolumeFromMouse(mouse.X); }
			}
			return;
		}
		for (var i = 0; i < ActionBindings.Definitions.Length; i++)
			if (SettingRow(i).HasPoint(mouse)) { EditingAction = ActionBindings.Definitions[i].Action; return; }
	}

	private void SetVolumeFromMouse(float mouseX)
	{
		var track = VolumeTrack();
		MasterVolume = Mathf.Clamp((mouseX - track.Position.X) / track.Size.X, 0, 1);
		EmitSignal(SignalName.VolumeChanged, MasterVolume);
		Refresh();
	}

	private void CloseVolumeDrag()
	{
		if (!_draggingVolume) return;
		_draggingVolume = false;
		EmitSignal(SignalName.VolumeCommitted);
	}

	public void Refresh()
	{
		if (_panel == null) return;
		var panel = SettingsPanel();
		var content = SettingsContent();
		SetRect(_backdrop, new Rect2(Vector2.Zero, GetViewportRect().Size));
		SetRect(_panel, panel);
		SetRect(_content, new Rect2(content.Position - panel.Position, content.Size));
		SetRect(_close, new Rect2(SettingsCloseButton().Position - content.Position, new Vector2(40, 40)));
		for (var i = 0; i < 2; i++)
		{
			SetRect(_tabs[i], new Rect2(SettingsTab(i).Position - content.Position, SettingsTab(i).Size));
			_tabs[i].Color = i == Tab ? new Color("825e35", .92f) : new Color("101824", .76f);
		}
		_help.Position = new Vector2(0, 91);
		_help.Size = new Vector2(content.Size.X, 25);
		_help.Text = Tab == 0 ? "点击分辨率切换，拖动滑块调整全局音量" : EditingAction == null ? "点击操作后按下新按键（滚轮切换主剑）" : "请按下新的按键或鼠标键（滚轮保留给主剑）";
		_footer.Position = new Vector2(0, content.Size.Y - 29);
		_footer.Size = new Vector2(content.Size.X, 25);
		_generalPage.Visible = Tab == 0;
		_bindingPage.Visible = Tab == 1;
		SetRect(_resolutionRow, new Rect2(ResolutionRow().Position - content.Position, ResolutionRow().Size));
		_resolutionValue.Text = $"{Resolution.X} × {Resolution.Y}";
		SetRect(_volumeRow, new Rect2(VolumeRow().Position - content.Position, VolumeRow().Size));
		var track = VolumeTrack();
		SetRect(_volumeTrack, new Rect2(track.Position - VolumeRow().Position, track.Size));
		SetRect(_volumeFill, new Rect2(Vector2.Zero, new Vector2(track.Size.X * MasterVolume, track.Size.Y)));
		SetRect(_volumeKnob, new Rect2(new Vector2(track.Size.X * MasterVolume - 8, -5), new Vector2(16, 16)));
		_volumeValue.Text = $"{Mathf.RoundToInt(MasterVolume * 100)}%";
		for (var i = 0; i < _bindingRows.Length; i++)
		{
			var (action, _) = ActionBindings.Definitions[i];
			var row = SettingRow(i);
			SetRect(_bindingRows[i], new Rect2(row.Position - content.Position, row.Size));
			_bindingRows[i].Color = EditingAction == action ? new Color("825e35", .88f) : new Color("101824", .72f);
			var fontSize = Mathf.Clamp(Mathf.RoundToInt(row.Size.Y * .66f), 13, 18);
			_bindingRows[i].GetNode<Label>("Name").AddThemeFontSizeOverride("font_size", fontSize);
			_bindingValues[i].AddThemeFontSizeOverride("font_size", fontSize);
			_bindingValues[i].Text = Input?.LabelFor(action) ?? "未绑定";
		}
	}

	private static void SetRect(Control control, Rect2 rect)
	{
		control.Position = rect.Position;
		control.Size = rect.Size;
	}
}
