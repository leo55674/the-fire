using Godot;
using System;

public partial class Main : Node2D
{
	private int _comboFocusColumn;
	private int _comboFocusIndex;
	private string _comboNotice = "点击左侧招式加入连招；右侧调整顺序。";

	private Rect2 ComboPanel(Vector2 size)
	{
		var panelSize = new Vector2(MathF.Min(1100, size.X - 40), MathF.Min(720, size.Y - 40));
		return new Rect2((size - panelSize) / 2, panelSize);
	}

	private Rect2 ComboLibraryArea(Rect2 panel) =>
		new(panel.Position + new Vector2(24, 102), new Vector2(panel.Size.X * .43f - 35, panel.Size.Y - 188));
	private Rect2 ComboOrderArea(Rect2 panel) =>
		new(panel.Position + new Vector2(panel.Size.X * .47f, 102), new Vector2(panel.Size.X * .53f - 24, panel.Size.Y - 188));
	private static Rect2 ComboRow(Rect2 area, int index)
	{
		var rowHeight = area.Size.Y / 11;
		return new Rect2(area.Position + new Vector2(0, index * rowHeight), new Vector2(area.Size.X, rowHeight - 3));
	}
	private static Rect2 ComboRowButton(Rect2 row, int button)
	{
		var side = MathF.Min(31, row.Size.Y - 8);
		return new Rect2(new Vector2(row.End.X - (3 - button) * (side + 5) - 5, row.Position.Y + (row.Size.Y - side) / 2), new Vector2(side, side));
	}
	private static Rect2 ComboPlayButton(Rect2 panel) => new(panel.End - new Vector2(207, 60), new Vector2(112, 38));
	private static Rect2 ComboCloseButton(Rect2 panel) => new(panel.End - new Vector2(86, 60), new Vector2(64, 38));

	private void DrawCombo(Vector2 size)
	{
		var font = _uiFont ?? ThemeDB.FallbackFont;
		var panel = ComboPanel(size);
		var library = ComboLibraryArea(panel);
		var order = ComboOrderArea(panel);
		DrawRect(new Rect2(Vector2.Zero, size), new Color("03070d", .76f));
		DrawRect(panel, new Color("111c27"));
		DrawRect(panel, new Color("a78960"), false, 2);
		DrawString(font, panel.Position + new Vector2(24, 42), "剑式搭配", HorizontalAlignment.Left, -1, 27, new Color("f3dfb5"));
		DrawString(font, panel.Position + new Vector2(24, 74), "固定招式 · 点击添加", HorizontalAlignment.Left, library.Size.X, 17, new Color("c8d7de"));
		DrawString(font, new Vector2(order.Position.X, panel.Position.Y + 74), $"出招顺序 · {_session.SwordCombo.Count}/{GameSession.MaxSwordComboSteps}", HorizontalAlignment.Left, order.Size.X, 17, new Color("c8d7de"));

		for (var i = 0; i <= GameSession.PresetSwordMoveCount; i++)
		{
			var row = ComboRow(library, i);
			var enabled = i != GameSession.CustomSwordMoveId || _session.HasRecordedSword;
			var focused = _comboFocusColumn == 0 && _comboFocusIndex == i;
			DrawRect(row, focused ? new Color("365262") : new Color("1d303d"));
			DrawRect(row, focused ? new Color("f1d396") : new Color("506875"), false, 1);
			var name = GameSession.SwordMoveName(i);
			if (!enabled) name += " · 先录制";
			DrawString(font, row.Position + new Vector2(12, row.Size.Y / 2 + 6), $"{i + 1:00}  {name}", HorizontalAlignment.Left, row.Size.X - 46, 16, enabled ? Colors.White : new Color("84939d"));
			DrawString(font, row.End - new Vector2(32, row.Size.Y / 2 - 6), "+", HorizontalAlignment.Left, 24, 20, enabled ? new Color("f6d48d") : new Color("72818a"));
		}

		for (var i = 0; i < _session.SwordCombo.Count; i++)
		{
			var row = ComboRow(order, i);
			var focused = _comboFocusColumn == 1 && _comboFocusIndex == i;
			DrawRect(row, focused ? new Color("594a37") : new Color("27313b"));
			DrawRect(row, focused ? new Color("f1d396") : new Color("556572"), false, 1);
			DrawString(font, row.Position + new Vector2(12, row.Size.Y / 2 + 6), $"{i + 1:00}  {GameSession.SwordMoveName(_session.SwordCombo[i])}", HorizontalAlignment.Left, row.Size.X - 125, 16, Colors.White);
			for (var button = 0; button < 3; button++)
			{
				var rect = ComboRowButton(row, button);
				DrawRect(rect, button == 2 ? new Color("643d42") : new Color("3a5360"));
				DrawString(font, rect.Position + new Vector2(0, rect.Size.Y / 2 + 6), button == 0 ? "上" : button == 1 ? "下" : "删", HorizontalAlignment.Center, rect.Size.X, 15, Colors.White);
			}
		}
		DrawString(font, panel.End - new Vector2(panel.Size.X - 24, 89), _comboNotice, HorizontalAlignment.Left, panel.Size.X - 48, 14, new Color("ddc89f"));
		DrawString(font, panel.End - new Vector2(panel.Size.X - 24, 42), "J/Esc 关闭  ·  方向键选中  ·  Enter 添加  ·  Shift+上下调序  ·  Delete 删除  ·  R 出招", HorizontalAlignment.Left, panel.Size.X - 240, 13, new Color("aebfc7"));
		DrawComboButton(ComboPlayButton(panel), "开始连招", new Color("765834"));
		DrawComboButton(ComboCloseButton(panel), "关闭", new Color("344b58"));
	}

	private void DrawComboButton(Rect2 rect, string label, Color color)
	{
		DrawRect(rect, color);
		DrawRect(rect, new Color("e4c992"), false, 1);
		DrawString(_uiFont ?? ThemeDB.FallbackFont, rect.Position + new Vector2(0, rect.Size.Y / 2 + 6), label, HorizontalAlignment.Center, rect.Size.X, 16, Colors.White);
	}

	private void HandleComboInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey key && key.Pressed && !key.Echo)
		{
			_input.Capture(inputEvent);
			var close = _input.Pressed("combo_menu") || key.Keycode == Key.Escape;
			var play = _input.Pressed("replay_sword");
			_input.ClearPressed();
			if (close) { _session.SetComboOpen(false); return; }
			if (play) { _session.PlaySwordCombo(); return; }
			if (key.Keycode == Key.Left || key.Keycode == Key.A) { _comboFocusColumn = 0; _comboFocusIndex = Mathf.Clamp(_comboFocusIndex, 0, GameSession.PresetSwordMoveCount); }
			else if (key.Keycode == Key.Right || key.Keycode == Key.D) { _comboFocusColumn = 1; _comboFocusIndex = Mathf.Clamp(_comboFocusIndex, 0, Math.Max(0, _session.SwordCombo.Count - 1)); }
			else if (key.Keycode == Key.Up || key.Keycode == Key.W) StepComboFocus(-1, key.ShiftPressed);
			else if (key.Keycode == Key.Down || key.Keycode == Key.S) StepComboFocus(1, key.ShiftPressed);
			else if (key.Keycode == Key.Enter || key.Keycode == Key.Space)
			{
				if (_comboFocusColumn == 0) AddFocusedComboMove();
			}
			else if (key.Keycode == Key.Delete || key.Keycode == Key.Backspace) RemoveFocusedComboMove();
			QueueRedraw();
			return;
		}
		if (inputEvent is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left) return;
		var panel = ComboPanel(GetViewportRect().Size);
		if (ComboCloseButton(panel).HasPoint(mouse.Position)) { _session.SetComboOpen(false); return; }
		if (ComboPlayButton(panel).HasPoint(mouse.Position)) { _session.PlaySwordCombo(); return; }
		var library = ComboLibraryArea(panel);
		for (var i = 0; i <= GameSession.PresetSwordMoveCount; i++)
			if (ComboRow(library, i).HasPoint(mouse.Position)) { _comboFocusColumn = 0; _comboFocusIndex = i; AddFocusedComboMove(); QueueRedraw(); return; }
		var order = ComboOrderArea(panel);
		for (var i = 0; i < _session.SwordCombo.Count; i++)
		{
			var row = ComboRow(order, i);
			if (!row.HasPoint(mouse.Position)) continue;
			_comboFocusColumn = 1;
			_comboFocusIndex = i;
			if (ComboRowButton(row, 0).HasPoint(mouse.Position)) StepComboFocus(-1, true);
			else if (ComboRowButton(row, 1).HasPoint(mouse.Position)) StepComboFocus(1, true);
			else if (ComboRowButton(row, 2).HasPoint(mouse.Position)) RemoveFocusedComboMove();
			QueueRedraw();
			return;
		}
	}

	private void StepComboFocus(int direction, bool reorder)
	{
		if (_comboFocusColumn == 1 && reorder)
		{
			if (_session.MoveComboStep(_comboFocusIndex, direction)) _comboFocusIndex += direction;
			return;
		}
		var count = _comboFocusColumn == 0 ? GameSession.PresetSwordMoveCount + 1 : _session.SwordCombo.Count;
		_comboFocusIndex = Mathf.Clamp(_comboFocusIndex + direction, 0, Math.Max(0, count - 1));
	}

	private void AddFocusedComboMove()
	{
		if (_session.AddComboMove(_comboFocusIndex))
		{
			_comboNotice = $"已加入 {GameSession.SwordMoveName(_comboFocusIndex)}，可在右侧调整顺序。";
			_comboFocusColumn = 1;
			_comboFocusIndex = _session.SwordCombo.Count - 1;
		}
		else _comboNotice = "无法添加：连招已满，或自录剑式尚未录制。";
	}

	private void RemoveFocusedComboMove()
	{
		if (_comboFocusColumn != 1 || !_session.RemoveComboStep(_comboFocusIndex)) return;
		_comboFocusIndex = Math.Max(0, Math.Min(_comboFocusIndex, _session.SwordCombo.Count - 1));
		_comboNotice = "已从连招中移除招式。";
	}
}
