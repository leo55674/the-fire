using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
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
		const int size = 40;
		return new Rect2(content.End.X - size, content.Position.Y + 4, size, size);
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
		for (var i = 0; i < 2; i++)
		{
			var tab = SettingsTab(i);
			var selected = _settingsTab == i;
			DrawRect(tab, selected ? new Color("825e35", .92f) : new Color("101824", .76f));
			DrawRect(tab, selected ? new Color("ffd285") : new Color("53616f"), false, 1);
			DrawString(font, tab.Position + new Vector2(0, 24), i == 0 ? "音频与显示" : "按键绑定", HorizontalAlignment.Center, tab.Size.X, 16, selected ? new Color("ffe0a0") : new Color("b8c2cc"));
		}
		DrawString(font, content.Position + new Vector2(0, 106), _settingsTab == 0 ? "点击分辨率切换，拖动滑块调整全局音量" : _editingAction == null ? "点击操作后按下新按键" : "请按下新的按键或鼠标键", HorizontalAlignment.Left, content.Size.X, helpSize, new Color("d2d8df"));
		if (_settingsTab == 0) DrawGeneralSettings(font);
		else DrawBindingSettings(font);
		DrawString(font, new Vector2(content.Position.X, content.End.Y - 8), "Tab 切换分页  ·  F1 / Esc 关闭  ·  修改自动保存", HorizontalAlignment.Center, content.Size.X, helpSize, new Color("b8c2cc"));
	}

	private void DrawGeneralSettings(Font font)
	{
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
		var volumeRow = VolumeRow();
		var track = VolumeTrack();
		DrawRect(volumeRow, new Color("101824", .78f));
		DrawString(font, volumeRow.Position + new Vector2(12, baseline), "主音量", HorizontalAlignment.Left, volumeRow.Size.X * .34f, rowFontSize, Colors.White);
		DrawRect(track, new Color("394959"));
		DrawRect(new Rect2(track.Position, new Vector2(track.Size.X * _masterVolume, track.Size.Y)), new Color("d9a75c"));
		DrawCircle(new Vector2(track.Position.X + track.Size.X * _masterVolume, track.GetCenter().Y), 8, new Color("ffe0a0"));
		DrawString(font, volumeRow.Position + new Vector2(volumeRow.Size.X * .85f, baseline), $"{Mathf.RoundToInt(_masterVolume * 100)}%", HorizontalAlignment.Right, volumeRow.Size.X * .11f, rowFontSize, new Color("ffd285"));
	}

	private void DrawBindingSettings(Font font)
	{
		for (var i = 0; i < ActionBindings.Definitions.Length; i++)
		{
			var (action, label) = ActionBindings.Definitions[i]; var row = SettingRow(i); var selected = _editingAction == action;
			var fontSize = Mathf.Clamp(Mathf.RoundToInt(row.Size.Y * .66f), 13, 18);
			var textY = row.Size.Y / 2 + fontSize * .35f;
			DrawRect(row, selected ? new Color("825e35", .88f) : new Color("101824", .72f));
			DrawString(font, row.Position + new Vector2(12, textY), label, HorizontalAlignment.Left, row.Size.X * .52f, fontSize, Colors.White);
			DrawString(font, row.Position + new Vector2(row.Size.X * .58f, textY), _input.LabelFor(action), HorizontalAlignment.Left, row.Size.X * .36f, fontSize, new Color("ffd285"));
		}
	}
}
