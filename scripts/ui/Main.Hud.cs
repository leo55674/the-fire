using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
	private void DrawHud(Vector2 size)
	{
		var font = ThemeDB.FallbackFont;
		DrawRect(new Rect2(18, 18, 450, 118), new Color("080b12", .78f));
		DrawString(font, new Vector2(32, 44), $"\u4e3b\u706b {MathF.Ceiling(_session.Fuel),3}%   \u7b49\u7ea7 {_session.FireLevel}/3   \u68a6\u7070 {_session.DreamAsh:0}", HorizontalAlignment.Left, -1, 18, new Color("ffd797"));
		DrawString(font, new Vector2(32, 65), "生命", HorizontalAlignment.Left, -1, 14, Colors.White);
		var healthBar = new Rect2(72, 48, 190, 22);
		DrawStatusBar(healthBar, new Rect2(0, 64, 1774, 214), new Rect2(143, 500, 1488, 120), _session.Health, _session.Balance.MaxHealth);
		DrawString(font, new Vector2(72, 64), $"{_session.Health:0}/{_session.Balance.MaxHealth:0}", HorizontalAlignment.Center, healthBar.Size.X, 13, Colors.White);
		DrawString(font, new Vector2(32, 91), "神识", HorizontalAlignment.Left, -1, 14, Colors.White);
		var emberBar = new Rect2(72, 74, 190, 22);
		DrawStatusBar(emberBar, new Rect2(0, 281, 1774, 214), new Rect2(143, 654, 1488, 120), _session.Ember, _session.Balance.MaxEmber);
		DrawString(font, new Vector2(72, 90), $"{_session.Ember:0}%", HorizontalAlignment.Center, emberBar.Size.X, 13, Colors.White);
		DrawString(font, new Vector2(276, 91), $"剑骸 {_session.Wood} · 铸剑铁料 {_session.SwordIron}", HorizontalAlignment.Left, 230, 14, Colors.White);
		DrawString(font, new Vector2(32, 119), _session.Message, HorizontalAlignment.Left, 560, 14, new Color("b9c5d2"));
		var swordRecordStatus = _session.IsSwordControlled ? $"录制剑式 {_session.SwordRecordingDuration:0.0} 秒"
			: _session.IsReplaySwordActive ? "从剑正在连招" : $"连招 {_session.SwordCombo.Count} 式 · J 搭配 · R 出招";
		DrawString(font, new Vector2(480, 44), swordRecordStatus, HorizontalAlignment.Left, 390, 16, new Color("f4d795"));
		DrawString(font, new Vector2(480, 68), $"飞剑 {_session.SwordCount}/3：{_session.SwordRoster} · 滚轮切主剑", HorizontalAlignment.Left, 500, 15, new Color("cfe6f4"));
		DrawString(font, new Vector2(480, 92), _session.IsInRealm ? "霜蚀秘境 · 左侧出口" : $"{(_session.IsNight ? "夜晚" : "白天")} · 剩余 {MathF.Ceiling(_session.PhaseRemaining)} 秒", HorizontalAlignment.Left, 390, 15, new Color("f4d795"));
		DrawString(font, new Vector2(18, size.Y - 40), "WASD 移动 | 左键攻击 | 右键御剑", HorizontalAlignment.Left, size.X / 2 - 160, 14, new Color("d0d8df"));
		DrawString(font, new Vector2(18, size.Y - 18), "滚轮切主剑 | J 搭配 | R 连招", HorizontalAlignment.Left, size.X / 2 - 160, 14, new Color("d0d8df"));
		DrawString(font, new Vector2(size.X / 2 + 145, size.Y - 40), "F 交互/搜刮 | E 补充 | U 升级 | I 背包", HorizontalAlignment.Left, size.X / 2 - 160, 14, new Color("d0d8df"));
		DrawTextureRect(_settingsIcon, new Rect2(size.X - 250, size.Y - 39, 24, 24), false);
		DrawString(font, new Vector2(size.X - 220, size.Y - 18), "F1 \u8bbe\u7f6e", HorizontalAlignment.Left, -1, 14, new Color("aebbc8"));
		DrawIconButton(GameExitButton(), _closeIcon, "\u9000\u51fa\u6e38\u620f", new Color("442939"));
		if (_session.NameFound && _session.Player.DistanceTo(new Vector2(430, GroundY)) < 80)
			DrawString(font, new Vector2(size.X / 2 - 165, size.Y - 72), "\u540d\u5b57\u788e\u7247\uff1aB \u71c3\u70e7\uff08+\u706b\u52bf/+\u68a6\u7070\uff09  G \u5f52\u8fd8", HorizontalAlignment.Left, -1, 15, new Color("d9e8ff"));
		if (!_session.IsInRealm) DrawMiniMap(size);
		if (_finalNight > 0) DrawString(font, new Vector2(size.X - 218, 142), $"\u70fd\u706b\u957f\u591c\uff1a{MathF.Ceiling(_finalNight)}", HorizontalAlignment.Left, -1, 17, new Color("ffb4a2"));
		if (!string.IsNullOrEmpty(_session.SearchPrompt)) DrawSearchPrompt(size);
	}

	private void DrawStatusBar(Rect2 rect, Rect2 frameRegion, Rect2 fillRegion, float value, float maximum)
	{
		var inset = rect.Size.X * .085f;
		var inner = new Rect2(rect.Position + new Vector2(inset, rect.Size.Y * .25f), new Vector2(rect.Size.X - inset * 2, rect.Size.Y * .5f));
		DrawRect(inner, new Color("17171b"));
		var fillRatio = maximum > 0 ? Mathf.Clamp(value / maximum, 0, 1) : 0;
		var fill = new Rect2(inner.Position, new Vector2(inner.Size.X * fillRatio, inner.Size.Y));
		if (fill.Size.X > 0) DrawTextureRectRegion(_statusBarsAtlas, fill, fillRegion, Colors.White);
		DrawTextureRectRegion(_statusBarsAtlas, rect, frameRegion, Colors.White);
	}

	private void DrawSearchPrompt(Vector2 size)
	{
		var center = new Vector2(size.X / 2 + _session.Player.X - _session.CameraFocus.X, _session.Player.Y - 112);
		var color = new Color("f7d88d");
		DrawArc(center, 17, 0, Mathf.Tau, 48, new Color("111821", .95f), 4, true);
		if (_session.IsSearching) DrawArc(center, 17, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * _session.SearchProgress, 48, color, 4, true);
		DrawString(_uiFont, center + new Vector2(30, 6), _session.IsSearching ? "搜刮中。。。。" : _session.SearchPrompt, HorizontalAlignment.Left, 500, 17, color);
	}

	private void DrawMiniMap(Vector2 size)
	{
		var panel = new Rect2(size.X - 222, 18, 204, 110);
		DrawRect(panel, new Color("080b12", .82f)); DrawRect(panel, new Color("607080"), false, 1);
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 19), "\u8ff7\u96fe\u5730\u56fe", HorizontalAlignment.Left, -1, 14, new Color("c9d7e6"));
		var worldWidth = _session.WorldHalfWidth * 2;
		Vector2 Marker(Vector2 world) => new(panel.Position.X + 9 + (world.X + worldWidth / 2) / worldWidth * (panel.Size.X - 18), panel.Position.Y + 63);
		DrawRect(new Rect2(panel.Position + new Vector2(9, 62), new Vector2(panel.Size.X - 18, 2)), new Color("314150"));
		DrawRect(new Rect2(Marker(new Vector2(-_session.VillageRadius, 0)) - new Vector2(0, 3), new Vector2(_session.VillageRadius * 2 / worldWidth * (panel.Size.X - 18), 8)), new Color("69b7b0", .55f));
		DrawCircle(Marker(_session.MainFire), 5, new Color("ff9d4d"));
		DrawCircle(Marker(new Vector2(GameSession.RealmEntranceX, 0)), 5, new Color("6de0ff"));
		foreach (var fire in _session.Fires) DrawCircle(Marker(fire.Position), 3, new Color("ffd46b"));
		foreach (var enemy in _session.Enemies) if (_session.IsVisible(enemy.Position)) DrawCircle(Marker(enemy.Position), 3, new Color("c66a77"));
		var sight = (155 + _session.Ember * 2.1f - _session.DreamAsh * 10) / worldWidth * (panel.Size.X - 18);
		DrawLine(Marker(_session.Player) - Vector2.Right * sight, Marker(_session.Player) + Vector2.Right * sight, new Color("dfeeff", .28f), 5);
		DrawCircle(Marker(_session.Player), 4, new Color("ecf4ff"));
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 97), "\u767d\u8272\uff1a\u4f60  \u6a59\u8272\uff1a\u4e3b\u706b", HorizontalAlignment.Left, -1, 11, new Color("9faebb"));
	}

	private void DrawVillageBoundary(Vector2 size)
	{
		foreach (var side in new[] { -1, 1 })
		{
			var x = size.X / 2 + side * _session.VillageRadius - _session.CameraFocus.X;
			if (x < 0 || x > size.X) continue;
			DrawLine(new Vector2(x, _session.GroundY - 130), new Vector2(x, _session.GroundY), new Color("8bd2c4", .65f), 2);
			DrawString(ThemeDB.FallbackFont, new Vector2(x + 8, _session.GroundY - 100), "村庄边界", HorizontalAlignment.Left, -1, 14, new Color("b9e4dc"));
		}
	}

	private void DrawIconButton(Rect2 rect, Texture2D icon, string text, Color color)
	{
		DrawMenuButton(rect, text, color);
		if (icon != null) DrawTextureRect(icon, new Rect2(rect.Position + new Vector2(5, 4), new Vector2(rect.Size.Y - 8, rect.Size.Y - 8)), false);
	}

	private void DrawMenuButton(Rect2 rect, string text, Color color)
	{
		DrawRect(rect, color);
		DrawRect(rect, new Color("d5b476"), false, 1);
		var fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.Size.Y * .45f), 18, 24);
		DrawString(_uiFont ?? ThemeDB.FallbackFont, rect.Position + new Vector2(0, rect.Size.Y / 2 + fontSize * .35f), text, HorizontalAlignment.Center, rect.Size.X, fontSize, Colors.White);
	}
}
