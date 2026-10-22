using Godot;
using System;

public partial class LootPanel : Node2D
{
	[Export] public GameSession Session { get; set; }
	private bool _dragFromLoot;
	private int _dragIndex = -1;
	private float _lastClickTime;
	private int _lastClickIndex = -1;
	private bool _splitMode;
	private int _splitSource = -1;
	private bool _wasOpen;
	private bool _equipFailed;
	private Texture2D _woodIcon;
	private Texture2D _ironIcon;
	private Texture2D _potionIcon;

	public override void _Ready()
	{
		_woodIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_sword_remnants.png");
		_ironIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_forge_ore.png");
		_potionIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_healing_potion.png");
	}

	public override void _Process(double delta)
	{
		var open = Session?.InventoryOpen == true;
		if (open && !_wasOpen) _equipFailed = false;
		if (open || open != _wasOpen) QueueRedraw();
		_wasOpen = open;
	}

	public override void _Draw()
	{
		if (Session == null || !Session.InventoryOpen) return;
		var size = GetViewportRect().Size;
		DrawRect(new Rect2(Vector2.Zero, size), new Color("03060b", .68f));
		var split = Session.LootOpen;
		var bag = Panel(size, false);
		DrawGrid(bag, Session.Bag.Slots, false);
		if (split) DrawGrid(Panel(size, true), Session.LootSystem.Items, true);
		else DrawString(ThemeDB.FallbackFont, bag.Position + new Vector2(26, bag.Size.Y - 18), "E 使用疗伤丹    I / Esc 关闭", HorizontalAlignment.Left, -1, 14, new Color("ffe0a0"));
		if (_dragIndex >= 0)
		{
			var stack = _dragFromLoot ? Session.LootSystem.Items[_dragIndex] : Session.Bag.Slots[_dragIndex];
			if (stack != null) DrawStack(GetViewport().GetMousePosition() - new Vector2(38, 38), new Vector2(76, 76), stack, true);
		}
	}

	private Rect2 Panel(Vector2 size, bool loot)
	{
		var panelSize = new Vector2(size.X * (Session.LootOpen ? .43f : .56f), size.Y * .78f);
		var x = Session.LootOpen ? size.X * (loot ? .04f : .53f) : (size.X - panelSize.X) / 2;
		return new Rect2(new Vector2(x, (size.Y - panelSize.Y) / 2), panelSize);
	}

	private Rect2 CellRect(Rect2 panel, int index, bool loot)
	{
		var columns = loot ? 6 : Session.Bag.Width;
		var rows = loot ? 4 : Session.Bag.Height;
		var area = new Vector2(panel.Size.X - 40, panel.Size.Y - (loot ? 195 : 275));
		const float gap = 7;
		var cell = Mathf.Floor(Mathf.Min((area.X - gap * (columns - 1)) / columns, (area.Y - gap * (rows - 1)) / rows));
		var used = new Vector2(cell * columns + gap * (columns - 1), cell * rows + gap * (rows - 1));
		var origin = panel.Position + new Vector2((panel.Size.X - used.X) / 2, (loot ? 56 : 136) + (area.Y - used.Y) / 2);
		return new Rect2(origin + new Vector2(index % columns * (cell + gap), index / columns * (cell + gap)), new Vector2(cell, cell));
	}

	private Rect2 ItemRect(Rect2 panel, int slot, Inventory.Stack stack, bool loot)
	{
		var first = CellRect(panel, slot, loot);
		var last = CellRect(panel, slot + (stack.Height - 1) * (loot ? 6 : Session.Bag.Width) + stack.Width - 1, loot);
		return new Rect2(first.Position, last.End - first.Position);
	}

	private void DrawGrid(Rect2 panel, System.Collections.Generic.IReadOnlyList<Inventory.Stack> slots, bool loot)
	{
		DrawRect(panel, new Color("0c121c", .97f)); DrawRect(panel, new Color("c69b5c"), false, 2);
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(20, 36), loot ? $"搜刮物资 · {Session.LootSystem.TargetName}" : "行囊", HorizontalAlignment.Left, -1, 24, new Color("ffe0a0"));
		if (!loot)
		{
			DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(panel.Size.X - 180, 34), $"{Session.Bag.Width}×{Session.Bag.Height} 格", HorizontalAlignment.Right, 160, 14, new Color("b8c2cc"));
			DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(20, 65), $"装备 · 武器：{Session.SwordRoster}", HorizontalAlignment.Left, panel.Size.X - 40, 14, new Color("d0d8df"));
			DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(20, 94), $"背包：{ItemName(Session.Bag.EquippedBackpack)}", HorizontalAlignment.Left, panel.Size.X - 40, 14, new Color("ffe0a0"));
			DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(20, 116), _equipFailed ? "空间不足，无法换装" : "双击背包物品可换装", HorizontalAlignment.Left, panel.Size.X - 40, 12, _equipFailed ? new Color("ff9e91") : new Color("9daab5"));
		}
		for (var i = 0; i < (loot ? 24 : Session.Bag.Capacity); i++)
		{
			var rect = CellRect(panel, i, loot);
			var cellHovered = rect.HasPoint(GetViewport().GetMousePosition());
			DrawRect(rect, cellHovered ? new Color("5d4930") : new Color("111923"));
			DrawRect(rect, new Color("46535e"), false, 1);
		}
		if (loot)
		{
			for (var i = 0; i < slots.Count; i++)
			{
				var stack = slots[i];
				if (stack == null || stack.LootSlot < 0) continue;
				var rect = ItemRect(panel, stack.LootSlot, stack, true);
				if (stack.Searched) DrawStack(rect.Position, rect.Size, stack, false);
				else { DrawRect(rect, new Color("17222e")); DrawRect(rect, new Color("46535e"), false, 1); }
				if (Session.LootSystem.IsSearching && i == Session.LootSystem.SearchedCount) DrawSearchSpinner(rect);
			}
			if (!Session.LootSystem.IsSearching)
				DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(18, panel.Size.Y - 71), "搜索完成，可拖动或双击拾取    Esc 关闭", HorizontalAlignment.Left, panel.Size.X - 36, 14, new Color("d0d8df"));
		}
		else
		{
			for (var i = 0; i < slots.Count; i++)
				if (slots[i] != null && Session.Bag.SlotFor(slots[i]) == i)
				{
					var rect = ItemRect(panel, i, slots[i], false);
					DrawStack(rect.Position, rect.Size, slots[i], false);
				}
		}
		var desc = new Rect2(panel.Position + new Vector2(18, panel.Size.Y - 106), new Vector2(panel.Size.X - 36, 30));
		DrawRect(desc, new Color("151e28"));
		var hovered = FindItem(slots, panel, GetViewport().GetMousePosition(), loot);
		if (hovered >= 0 && (slots[hovered].Searched || !loot))
		{
			DrawString(ThemeDB.FallbackFont, desc.Position + new Vector2(8, 13), ItemName(slots[hovered].Id), HorizontalAlignment.Left, -1, 13, new Color("ffe0a0"));
			DrawString(ThemeDB.FallbackFont, desc.Position + new Vector2(8, 26), ItemDescription(slots[hovered].Id), HorizontalAlignment.Left, desc.Size.X - 16, 11, new Color("d0d8df"));
		}
		else DrawString(ThemeDB.FallbackFont, desc.Position + new Vector2(8, 20), "拖动物品摆放；双击搜出的物品可拾取", HorizontalAlignment.Left, desc.Size.X - 16, 13, new Color("9daab5"));
		if (!loot)
		{
			DrawButton(panel, 0, _splitMode ? "拆分：已开启" : "拆分"); DrawButton(panel, 1, "整理");
		}
	}

	private void DrawStack(Vector2 position, Vector2 size, Inventory.Stack stack, bool ghost)
	{
		var rect = new Rect2(position, size);
		DrawRect(rect, new Color("202a34", ghost ? .65f : 1)); DrawRect(rect, new Color("9b8059"), false, 1);
		var icon = stack.Id switch { "wood" => _woodIcon, "sword_iron" => _ironIcon, "healing_potion" => _potionIcon, _ => null };
		if (icon != null)
		{
			var bounds = rect.Grow(-6);
			var scale = Mathf.Min(bounds.Size.X / icon.GetSize().X, bounds.Size.Y / icon.GetSize().Y);
			var iconSize = icon.GetSize() * scale;
			DrawTextureRect(icon, new Rect2(bounds.GetCenter() - iconSize / 2, iconSize), false);
		}
		if (icon == null) DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(4, rect.Size.Y / 2), ItemName(stack.Id), HorizontalAlignment.Center, rect.Size.X - 8, 12, Colors.White);
		if (stack.Count > 1) DrawString(ThemeDB.FallbackFont, rect.End - new Vector2(20, 4), stack.Count.ToString(), HorizontalAlignment.Right, 17, 13, Colors.White);
	}

	private void DrawSearchSpinner(Rect2 item)
	{
		const int segmentCount = 10;
		var center = item.GetCenter();
		var radius = Mathf.Min(item.Size.Y * .38f, 25);
		var head = (int)(Time.GetTicksMsec() / 1000f * 1.5f * segmentCount) % segmentCount;
		for (var i = 0; i < segmentCount; i++)
		{
			var angle = -Mathf.Pi / 2 + Mathf.Tau * i / segmentCount;
			var distance = (head - i + segmentCount) % segmentCount;
			var alpha = 1f - distance / (float)segmentCount * .78f;
			var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			DrawSetTransform(position, angle + Mathf.Pi / 2, Vector2.One);
			DrawRect(new Rect2(-3, -6, 6, 12), new Color("f7d88d", alpha));
		}
		DrawSetTransform(Vector2.Zero, 0, Vector2.One);
	}

	private void DrawButton(Rect2 panel, int index, string text)
	{
		var width = (panel.Size.X - 56) / 2;
		var rect = new Rect2(panel.Position + new Vector2(20 + index * (width + 8), panel.Size.Y - 55), new Vector2(width, 34));
		DrawRect(rect, new Color("26313c")); DrawRect(rect, new Color("9b8059"), false, 1);
		DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(0, 22), text, HorizontalAlignment.Center, rect.Size.X, 14, Colors.White);
	}

	private string ItemName(string id) => id switch { "wood" => "剑骸", "sword_iron" => "铸剑铁料", "healing_potion" => "疗伤丹", "starter_backpack" => "初始背包", "large_backpack" => "扩容背包", _ => id };
	private string ItemDescription(string id) => id switch
	{
		"wood" => "用于补充灵蕴、升阶及布置剑阵。",
		"sword_iron" => "铁匠铺打造飞剑的材料，占用 2×1 格。",
		"healing_potion" => "使用后恢复血量。",
		"starter_backpack" => "装备后提供 5×5 格；空间不足时无法换装。",
		"large_backpack" => "装备后提供 6×5 格；双击可换装。",
		_ => ""
	};

	public void HandleInput(InputEventMouseButton e)
	{
		if (Session == null || !Session.InventoryOpen || e.ButtonIndex != MouseButton.Left) return;
		var point = e.Position; var size = GetViewportRect().Size;
		if (!e.Pressed && _dragIndex >= 0)
		{
			var bag = Panel(size, false); var target = FindItem(Session.Bag.Slots, bag, point, false);
			if (_dragFromLoot && bag.HasPoint(point)) Session.TransferLoot(_dragIndex, target >= 0 ? target : FindCell(bag, point));
			else if (!_dragFromLoot && bag.HasPoint(point)) Session.Bag.Move(_dragIndex, target >= 0 ? target : FindCell(bag, point));
			_dragIndex = -1; QueueRedraw(); return;
		}
		if (!e.Pressed) return;
		var lootOpen = Session.LootOpen; var lootPanel = Panel(size, true); var inLoot = lootOpen && lootPanel.HasPoint(point);
		var panel = inLoot ? lootPanel : Panel(size, false);
		if (!panel.HasPoint(point)) return;
		if (!inLoot)
		{
			var width = (panel.Size.X - 56) / 2;
			for (var i = 0; i < 2; i++)
			{
				var button = new Rect2(panel.Position + new Vector2(20 + i * (width + 8), panel.Size.Y - 55), new Vector2(width, 34));
				if (!button.HasPoint(point)) continue;
				if (i == 0) { _splitMode = !_splitMode; _splitSource = -1; }
				else Session.Bag.Sort();
				QueueRedraw(); return;
			}
		}
		var slots = inLoot ? Session.LootSystem.Items : Session.Bag.Slots;
		var index = FindItem(slots, panel, point, inLoot);
		if (!inLoot && _splitMode)
		{
			var cell = index >= 0 ? Session.Bag.SlotFor(Session.Bag.Slots[index]) : FindCell(panel, point);
			if (_splitSource < 0) _splitSource = cell;
			else { Session.Bag.Split(_splitSource, cell); _splitSource = -1; _splitMode = false; }
			QueueRedraw(); return;
		}
		if (index < 0) return;
		if (inLoot && !slots[index].Searched) return;
		if (!inLoot && (e.DoubleClick || (_lastClickIndex == index && Time.GetTicksMsec() / 1000f - _lastClickTime < .35f)) && slots[index].Id is "starter_backpack" or "large_backpack")
		{
			_equipFailed = !Session.Bag.EquipBackpack(index);
			_lastClickIndex = -1; _dragIndex = -1; QueueRedraw(); return;
		}
		if (inLoot && (e.DoubleClick || (_lastClickIndex == index && Time.GetTicksMsec() / 1000f - _lastClickTime < .35f)))
		{
			Session.TransferLoot(index, Session.Bag.FirstTransferSlot(slots[index]));
			_lastClickIndex = -1; QueueRedraw(); return;
		}
		_lastClickIndex = index; _lastClickTime = Time.GetTicksMsec() / 1000f;
		_dragFromLoot = inLoot; _dragIndex = index; QueueRedraw();
	}

	private int FindItem(System.Collections.Generic.IReadOnlyList<Inventory.Stack> slots, Rect2 panel, Vector2 point, bool loot)
	{
		for (var i = 0; i < slots.Count; i++)
		{
			var stack = slots[i]; if (stack == null) continue;
			if (loot ? stack.LootSlot >= 0 && ItemRect(panel, stack.LootSlot, stack, true).HasPoint(point) : Session.Bag.SlotFor(stack) == i && ItemRect(panel, i, stack, false).HasPoint(point)) return i;
		}
		return -1;
	}

	private int FindCell(Rect2 panel, Vector2 point)
	{
		for (var i = 0; i < Session.Bag.Capacity; i++) if (CellRect(panel, i, false).HasPoint(point)) return i;
		return -1;
	}
}
