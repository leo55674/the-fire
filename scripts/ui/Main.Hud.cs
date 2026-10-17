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
		DrawString(font, new Vector2(276, 91), $"剑骸 {_session.Wood}", HorizontalAlignment.Left, 160, 15, Colors.White);
		DrawString(font, new Vector2(32, 119), _session.Message, HorizontalAlignment.Left, 560, 14, new Color("b9c5d2"));
		DrawString(font, new Vector2(18, size.Y - 42), "WASD 移动 | 左键攻击 | 右键御剑 | F 搜刮 | E 补充 | U 升级 | I 背包", HorizontalAlignment.Left, -1, 14, new Color("d0d8df"));
		DrawTextureRect(_settingsIcon, new Rect2(size.X - 250, size.Y - 39, 24, 24), false);
		DrawString(font, new Vector2(size.X - 220, size.Y - 18), "F1 \u8bbe\u7f6e", HorizontalAlignment.Left, -1, 14, new Color("aebbc8"));
		DrawIconButton(GameExitButton(), _closeIcon, "\u9000\u51fa\u6e38\u620f", new Color("442939"));
		if (_session.NameFound && _session.Player.DistanceTo(new Vector2(430, GroundY)) < 80)
			DrawString(font, new Vector2(size.X / 2 - 165, size.Y - 72), "\u540d\u5b57\u788e\u7247\uff1aB \u71c3\u70e7\uff08+\u706b\u52bf/+\u68a6\u7070\uff09  G \u5f52\u8fd8", HorizontalAlignment.Left, -1, 15, new Color("d9e8ff"));
		DrawMiniMap(size);
		if (_finalNight > 0) DrawString(font, new Vector2(size.X - 218, 142), $"\u70fd\u706b\u957f\u591c\uff1a{MathF.Ceiling(_finalNight)}", HorizontalAlignment.Left, -1, 17, new Color("ffb4a2"));
		if (!string.IsNullOrEmpty(_session.SearchPrompt)) DrawSearchPrompt(size);
		if (_session.InventoryOpen) DrawInventory(size);
		if (_settingsOpen) DrawSettings(size);
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

	private void DrawInventory(Vector2 size)
	{
		var font = _uiFont ?? ThemeDB.FallbackFont;
		DrawRect(new Rect2(Vector2.Zero, size), new Color("03060b", .68f));
		var bagPanel = InventoryPanel(size, false);
		DrawInventoryPanel(bagPanel, _session.Bag.Slots, false, font);
		if (_session.LootOpen)
		{
			var lootPanel = InventoryPanel(size, true);
			var lootSlots = Enumerable.Repeat<Inventory.Stack>(null, _session.Bag.Capacity).ToArray();
			for (var i = 0; i < Math.Min(lootSlots.Length, _session.LootItems.Count); i++) lootSlots[i] = _session.LootItems[i];
			DrawInventoryPanel(lootPanel, lootSlots, true, font);
		}
		else DrawString(font, bagPanel.Position + new Vector2(26, bagPanel.Size.Y - 18), "E 使用疗伤丹    Tab 切换剑胚    I / Esc 关闭", HorizontalAlignment.Left, -1, 14, new Color("ffe0a0"));
		if (_inventoryDragStack != null)
		{
			var ghost = new Rect2(GetViewport().GetMousePosition() - new Vector2(36, 36), new Vector2(72, 72));
			DrawRect(ghost, new Color("b8d1e3", .42f));
			DrawInventoryIcon(_inventoryDragStack.Id, ghost);
			DrawRect(ghost, new Color("07101a", .46f));
			DrawRect(ghost, new Color("dcecff", .72f), false, 2);
			DrawString(font, ghost.End - new Vector2(18, 4), _inventoryDragStack.Count.ToString(), HorizontalAlignment.Right, 16, 13, Colors.White);
		}
	}

	private Rect2 InventoryPanel(Vector2 size, bool loot)
	{
		var panelSize = new Vector2(size.X * (_session.LootOpen ? .43f : .56f), size.Y * .68f);
		var x = _session.LootOpen ? size.X * (loot ? .04f : .53f) : (size.X - panelSize.X) / 2;
		return new Rect2(new Vector2(x, (size.Y - panelSize.Y) / 2), panelSize);
	}

	private Rect2 InventoryGrid(Rect2 panel)
	{
		var area = new Vector2(panel.Size.X - 40, panel.Size.Y - 195);
		var gap = 7f;
		var cell = Mathf.Floor(Mathf.Min((area.X - gap * 5) / 6, (area.Y - gap * 3) / 4));
		var used = new Vector2(cell * 6 + gap * 5, cell * 4 + gap * 3);
		return new Rect2(panel.Position + new Vector2((panel.Size.X - used.X) / 2, 56 + (area.Y - used.Y) / 2), used);
	}

	private Rect2 InventorySlot(Rect2 panel, int index)
	{
		var grid = InventoryGrid(panel);
		var cell = (grid.Size.X - 35) / 6;
		return new Rect2(grid.Position + new Vector2(index % 6 * (cell + 7), index / 6 * (cell + 7)), new Vector2(cell, cell));
	}

	private Rect2 InventoryButton(Rect2 panel, int index, bool lootPanel)
	{
		var width = (panel.Size.X - 56) / (lootPanel ? 3 : 2);
		return new Rect2(panel.Position + new Vector2(20 + index * (width + 8), panel.Size.Y - 55), new Vector2(width, 34));
	}

	private void DrawInventoryPanel(Rect2 panel, IReadOnlyList<Inventory.Stack> slots, bool loot, Font font)
	{
		DrawRect(panel, new Color("0c121c", .97f));
		DrawRect(panel, new Color("c69b5c"), false, 2);
		DrawString(font, panel.Position + new Vector2(20, 36), loot ? "搜刮物资" : "行囊", HorizontalAlignment.Left, -1, 24, new Color("ffe0a0"));
		if (!loot) DrawString(font, panel.Position + new Vector2(panel.Size.X - 170, 34), $"物品 {_session.Bag.Items.Count}/{_session.Bag.Capacity}", HorizontalAlignment.Right, 150, 14, new Color("b8c2cc"));
		var mouse = GetViewport().GetMousePosition();
		var hovered = -1;
		for (var i = 0; i < _session.Bag.Capacity; i++)
		{
			var rect = InventorySlot(panel, i);
			var stack = i < slots.Count ? slots[i] : null;
			var over = rect.HasPoint(mouse);
			if (over && stack != null) hovered = i;
			DrawRect(rect, over ? new Color("5d4930") : stack != null ? new Color("202a34") : new Color("111923"));
			DrawRect(rect, over ? new Color("ffe0a0") : new Color("46535e"), false, over ? 2 : 1);
			if (stack == null) continue;
			DrawInventoryIcon(stack.Id, rect);
			DrawString(font, rect.End - new Vector2(20, 4), stack.Count.ToString(), HorizontalAlignment.Right, 17, 13, Colors.White);
		}
		var descriptionRect = new Rect2(panel.Position + new Vector2(18, panel.Size.Y - 106), new Vector2(panel.Size.X - 36, 40));
		DrawRect(descriptionRect, new Color("151e28"));
		if (hovered >= 0)
		{
			var stack = slots[hovered];
			DrawString(font, descriptionRect.Position + new Vector2(8, 15), ItemName(stack.Id), HorizontalAlignment.Left, -1, 14, new Color("ffe0a0"));
			DrawString(font, descriptionRect.Position + new Vector2(8, 33), ItemDescription(stack.Id), HorizontalAlignment.Left, descriptionRect.Size.X - 16, 12, new Color("d0d8df"));
		}
		else DrawString(font, descriptionRect.Position + new Vector2(8, 25), "拖动物品可自由摆放；悬停查看详情", HorizontalAlignment.Left, -1, 13, new Color("9daab5"));
		var buttonCount = loot ? 3 : 2;
		for (var i = 0; i < buttonCount; i++)
		{
			var rect = InventoryButton(panel, i, loot);
			var label = loot && i == 2 ? "全部拿取" : i == 0 ? (_inventorySplitMode ? "拆分：已开启" : "拆分") : "整理";
			DrawRect(rect, rect.HasPoint(mouse) ? new Color("725230") : new Color("26313c"));
			DrawRect(rect, new Color("9b8059"), false, 1);
			DrawString(font, rect.Position + new Vector2(0, 22), label, HorizontalAlignment.Center, rect.Size.X, 14, Colors.White);
		}
	}

	private string ItemName(string id) => id switch { "wood" => "剑骸", "light_sword" => "轻剑胚", "heavy_sword" => "重剑胚", "healing_potion" => "疗伤丹", _ => id };

	private string ItemDescription(string id) => id switch
	{
		"wood" => "用于补充灵蕴、升阶及布置剑阵。",
		"light_sword" => "轻盈灵敏，攻击伤害 35。",
		"heavy_sword" => "沉重有力，攻击伤害 70。",
		"healing_potion" => "使用后恢复 2 点血量。",
		_ => ""
	};

	private void HandleInventoryMouse(InputEventMouseButton mouseEvent)
	{
		var point = mouseEvent.Position;
		if (!mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && _inventoryDragSlot >= 0)
			{
				var destinationLoot = _session.LootOpen && InventoryPanel(GetViewportRect().Size, true).HasPoint(point);
				var destination = destinationLoot ? InventoryPanel(GetViewportRect().Size, true) : InventoryPanel(GetViewportRect().Size, false);
				var target = -1;
				for (var i = 0; i < _session.Bag.Capacity; i++)
					if (InventorySlot(destination, i).HasPoint(point)) { target = i; break; }
				if (target >= 0 && !_inventoryDragFromLoot && !destinationLoot) _session.Bag.Move(_inventoryDragSlot, target);
				else if (target >= 0 && _inventoryDragFromLoot && !destinationLoot) _session.TransferLoot(_inventoryDragSlot, target);
				_inventoryDragSlot = -1;
				_inventoryDragStack = null;
				QueueRedraw();
			}
			return;
		}
		if (mouseEvent.ButtonIndex != MouseButton.Left) return;
		var size = GetViewportRect().Size;
		var bagPanel = InventoryPanel(size, false);
		var lootPanel = InventoryPanel(size, true);
		var inLoot = _session.LootOpen && lootPanel.HasPoint(point);
		var panel = inLoot ? lootPanel : bagPanel;
		if (!panel.HasPoint(point)) return;
		for (var i = 0; i < (inLoot ? 3 : 2); i++)
		{
			if (!InventoryButton(panel, i, inLoot).HasPoint(point)) continue;
			if (_session.LootOpen && i == 2 && inLoot) _session.TakeAllLoot();
			else if (i == 0) { _inventorySplitMode = !_inventorySplitMode; _inventorySplitSource = -1; }
			else if (i == 1 && !inLoot) _session.Bag.Sort();
			QueueRedraw();
			return;
		}
		for (var i = 0; i < _session.Bag.Capacity; i++)
		{
			if (!InventorySlot(panel, i).HasPoint(point)) continue;
			if (_inventorySplitMode && !inLoot)
			{
				if (_inventorySplitSource < 0) _inventorySplitSource = i;
				else { _session.Bag.Split(_inventorySplitSource, i); _inventorySplitSource = -1; _inventorySplitMode = false; }
			}
			else
			{
				_inventoryDragFromLoot = inLoot;
				_inventoryDragSlot = i;
				var stack = inLoot ? _session.LootItems.ElementAtOrDefault(i) : _session.Bag.Slots[i];
				_inventoryDragStack = stack == null ? null : new Inventory.Stack(stack.Id, stack.Count);
			}
			QueueRedraw();
			return;
		}
	}

	private void DrawInventoryIcon(string itemId, Rect2 slot)
	{
		var center = slot.GetCenter();
		switch (itemId)
		{
			case "wood":
				DrawCircle(center, 18, new Color("9a7650"));
				DrawCircle(center + new Vector2(-5, -4), 8, new Color("c9a878"));
				DrawLine(center + new Vector2(-4, 9), center + new Vector2(11, -10), new Color("e6d2a4"), 2, true);
				break;
			case "light_sword":
			case "heavy_sword":
				var length = itemId == "light_sword" ? 27 : 23;
				var angle = itemId == "light_sword" ? -Mathf.Pi / 4 : -Mathf.Pi / 3.5f;
				var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				DrawLine(center - direction * 9, center + direction * length, new Color("d9e4e8"), itemId == "light_sword" ? 3 : 6, true);
				DrawLine(center - direction * 12 + direction.Orthogonal() * 6, center - direction * 12 - direction.Orthogonal() * 6, new Color("c99a58"), 3, true);
				break;
			case "healing_potion":
				DrawCircle(center, 16, new Color("a54c48"));
				DrawCircle(center + new Vector2(-5, -6), 5, new Color("e99175"));
				DrawLine(center + new Vector2(0, -14), center + new Vector2(8, -23), new Color("dbbd79"), 3, true);
				break;
		}
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
