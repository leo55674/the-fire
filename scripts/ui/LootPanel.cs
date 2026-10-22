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
	[Export] private Control _layout;
	[Export] private ColorRect _backdrop;
	[Export] private TextureRect _frame;
	[Export] private Control _gridArea;
	[Export] private Button _closeButton;
	[Export] private Button _splitButton;
	[Export] private Button _sortButton;
	[Export] private Label _swordRoster;
	[Export] private Label _backpackName;
	[Export] private Label _capacity;
	[Export] private Label _description;
	[Export] private Control _bagDragHandle;
	[Export] private Control _lootLayout;
	[Export] private Panel _lootFrame;
	[Export] private Control _lootGridArea;
	[Export] private Label _lootTitle;
	[Export] private Label _lootDescription;
	[Export] private Label _lootStatus;
	[Export] private Control _lootDragHandle;
	[Export] private float _gridGap = 5f;
	[Export] private float _lootGridGap = 7f;
	private Control _draggingLayout;
	private Vector2 _dragOffset;

	public override void _Ready()
	{
		_woodIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_sword_remnants.png");
		_ironIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_forge_ore.png");
		_potionIcon = GD.Load<Texture2D>("res://assets/art/runtime/ui/items/item_healing_potion.png");
		if (_layout == null || _backdrop == null || _frame == null || _gridArea == null || _closeButton == null ||
			_splitButton == null || _sortButton == null || _swordRoster == null ||
			_backpackName == null || _capacity == null || _description == null || _bagDragHandle == null ||
			_lootLayout == null || _lootFrame == null || _lootGridArea == null ||
			_lootTitle == null || _lootDescription == null || _lootStatus == null || _lootDragHandle == null)
			throw new InvalidOperationException("LootPanel requires its inventory and loot scene nodes.");
		_layout.Visible = false;
		_lootLayout.Visible = false;
		_backdrop.Visible = false;
	}

	public override void _Process(double delta)
	{
		var open = Session?.InventoryOpen == true;
		if (open && !_wasOpen) _equipFailed = false;
		_layout.Visible = open;
		_lootLayout.Visible = open && Session.LootOpen;
		_backdrop.Visible = open;
		if (!open) _draggingLayout = null;
		if (open)
		{
			var size = GetViewportRect().Size;
			_swordRoster.Text = Session.SwordRoster;
			_backpackName.Text = ItemName(Session.Bag.EquippedBackpack);
			_capacity.Text = $"{Session.Bag.Width}×{Session.Bag.Height} 格";
			_splitButton.Text = _splitMode ? "拆分：已开启" : "拆分";
			var hovered = FindItem(Session.Bag.Slots, Panel(size, false), GetViewport().GetMousePosition(), false);
			_description.Text = hovered >= 0 ? $"{ItemName(Session.Bag.Slots[hovered].Id)} · {ItemDescription(Session.Bag.Slots[hovered].Id)}" :
				_equipFailed ? "空间不足，无法换装" : "拖动摆放 · 双击背包换装 · E 使用疗伤丹";
			_description.AddThemeColorOverride("font_color", _equipFailed ? new Color("ff9e91") : new Color("8faac0"));
			if (Session.LootOpen)
			{
				_lootTitle.Text = $"搜刮物资 · {Session.LootSystem.TargetName}";
				_lootStatus.Visible = !Session.LootSystem.IsSearching;
				var lootItems = Session.LootSystem.Items;
				var lootHovered = FindItem(lootItems, Panel(size, true), GetViewport().GetMousePosition(), true);
				_lootDescription.Text = lootHovered >= 0 && lootItems[lootHovered].Searched
					? $"{ItemName(lootItems[lootHovered].Id)} · {ItemDescription(lootItems[lootHovered].Id)}"
					: "拖动物品摆放；双击搜出的物品可拾取";
			}
		}
		if (open || open != _wasOpen) QueueRedraw();
		_wasOpen = open;
	}

	public override void _Draw()
	{
		if (Session == null || !Session.InventoryOpen) return;
		var size = GetViewportRect().Size;
		var split = Session.LootOpen;
		var bag = Panel(size, false);
		DrawBag(bag);
		if (split) DrawLoot(Panel(size, true), Session.LootSystem.Items);
		if (_dragIndex >= 0)
		{
			var stack = _dragFromLoot ? Session.LootSystem.Items[_dragIndex] : Session.Bag.Slots[_dragIndex];
			if (stack != null) DrawStack(GetViewport().GetMousePosition() - new Vector2(38, 38), new Vector2(76, 76), stack, true);
		}
	}

	public void HandleMotion(InputEventMouseMotion e)
	{
		if (_draggingLayout == null || Session?.InventoryOpen != true) return;
		_draggingLayout.GlobalPosition = e.Position + _dragOffset;
		QueueRedraw();
	}

	private Rect2 Panel(Vector2 size, bool loot)
	{
		return loot ? _lootFrame.GetGlobalRect() : _frame.GetGlobalRect();
	}

	private Rect2 CellRect(Rect2 panel, int index, bool loot)
	{
		var columns = loot ? 6 : Session.Bag.Width;
		var rows = loot ? 4 : Session.Bag.Height;
		if (!loot)
		{
			var gridArea = _gridArea.GetGlobalRect();
			var gridGap = _gridGap * _layout.Scale.X;
			var gridCell = Mathf.Min((gridArea.Size.X - gridGap * (columns - 1)) / columns, (gridArea.Size.Y - gridGap * (rows - 1)) / rows);
			var gridUsed = new Vector2(gridCell * columns + gridGap * (columns - 1), gridCell * rows + gridGap * (rows - 1));
			var gridOrigin = gridArea.Position + (gridArea.Size - gridUsed) / 2;
			return new Rect2(gridOrigin + new Vector2(index % columns * (gridCell + gridGap), index / columns * (gridCell + gridGap)), new Vector2(gridCell, gridCell));
		}
		var lootArea = _lootGridArea.GetGlobalRect();
		var lootGap = _lootGridGap * _lootLayout.Scale.X;
		var lootCell = Mathf.Min((lootArea.Size.X - lootGap * (columns - 1)) / columns, (lootArea.Size.Y - lootGap * (rows - 1)) / rows);
		var lootUsed = new Vector2(lootCell * columns + lootGap * (columns - 1), lootCell * rows + lootGap * (rows - 1));
		var lootOrigin = lootArea.Position + (lootArea.Size - lootUsed) / 2;
		return new Rect2(lootOrigin + new Vector2(index % columns * (lootCell + lootGap), index / columns * (lootCell + lootGap)), new Vector2(lootCell, lootCell));
	}

	private void DrawBag(Rect2 frame)
	{
		for (var i = 0; i < Session.Bag.Capacity; i++)
		{
			var rect = CellRect(frame, i, false);
			DrawRect(rect, rect.HasPoint(GetViewport().GetMousePosition()) ? new Color("866137", .65f) : new Color("061424", .65f));
			DrawRect(rect, new Color("a68a68", .6f), false, 1);
		}
		var slots = Session.Bag.Slots;
		for (var i = 0; i < slots.Count; i++)
			if (slots[i] != null && Session.Bag.SlotFor(slots[i]) == i)
			{
				var rect = ItemRect(frame, i, slots[i], false);
				DrawStack(rect.Position, rect.Size, slots[i], false);
			}
	}

	private Rect2 ItemRect(Rect2 panel, int slot, Inventory.Stack stack, bool loot)
	{
		var first = CellRect(panel, slot, loot);
		var last = CellRect(panel, slot + (stack.Height - 1) * (loot ? 6 : Session.Bag.Width) + stack.Width - 1, loot);
		return new Rect2(first.Position, last.End - first.Position);
	}

	private void DrawLoot(Rect2 panel, System.Collections.Generic.IReadOnlyList<Inventory.Stack> slots)
	{
		for (var i = 0; i < 24; i++)
		{
			var rect = CellRect(panel, i, true);
			var cellHovered = rect.HasPoint(GetViewport().GetMousePosition());
			DrawRect(rect, cellHovered ? new Color("5d4930") : new Color("111923"));
			DrawRect(rect, new Color("46535e"), false, 1);
		}
		for (var i = 0; i < slots.Count; i++)
		{
			var stack = slots[i];
			if (stack == null || stack.LootSlot < 0) continue;
			var rect = ItemRect(panel, stack.LootSlot, stack, true);
			if (stack.Searched) DrawStack(rect.Position, rect.Size, stack, false);
			else { DrawRect(rect, new Color("17222e")); DrawRect(rect, new Color("46535e"), false, 1); }
			if (Session.LootSystem.IsSearching && i == Session.LootSystem.SearchedCount) DrawSearchSpinner(rect);
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
		if (e.Pressed && _closeButton.GetGlobalRect().HasPoint(point)) { Session.ToggleInventory(); _dragIndex = -1; QueueRedraw(); return; }
		if (!e.Pressed && _draggingLayout != null) { _draggingLayout = null; return; }
		if (e.Pressed)
		{
			var handle = Session.LootOpen && _lootDragHandle.GetGlobalRect().HasPoint(point) ? _lootLayout :
				_bagDragHandle.GetGlobalRect().HasPoint(point) ? _layout : null;
			if (handle != null)
			{
				_draggingLayout = handle;
				_dragOffset = handle.GlobalPosition - point;
				return;
			}
		}
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
			if (_splitButton.GetGlobalRect().HasPoint(point))
			{ _splitMode = !_splitMode; _splitSource = -1; QueueRedraw(); return; }
			if (_sortButton.GetGlobalRect().HasPoint(point))
			{ Session.Bag.Sort(); QueueRedraw(); return; }
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
