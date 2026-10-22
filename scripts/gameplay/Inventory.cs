using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Inventory : Node
{
	public sealed class Stack
	{
		public string Id;
		public int Count;
		public int Width;
		public int Height;
		public int LootSlot = -1;
		public bool Searched = true;
		public Stack(string id, int count) { Id = id; Count = count; (Width, Height) = SizeOf(id); }
	}

	public const int StarterWidth = 5;
	public const int StarterHeight = 5;
	private Stack[] _slots = new Stack[StarterWidth * StarterHeight];
	public string EquippedBackpack { get; private set; } = "starter_backpack";
	public int Width => EquippedBackpack == "large_backpack" ? 6 : StarterWidth;
	public int Height => EquippedBackpack == "large_backpack" ? 5 : StarterHeight;
	public IReadOnlyList<Stack> Slots => _slots;
	public IReadOnlyDictionary<string, int> Items => _slots.Where(slot => slot != null)
		.GroupBy(slot => slot.Id).ToDictionary(group => group.Key, group => group.Sum(slot => slot.Count));
	public int Capacity => _slots.Length;
	public static (int Width, int Height) SizeOf(string id) => id switch
	{
		"sword_iron" => (2, 1),
		"starter_backpack" or "large_backpack" => (2, 2),
		"healing_potion" => (1, 1),
		_ => (1, 1)
	};
	public int Count(string id) => _slots.Where(slot => slot?.Id == id).Sum(slot => slot.Count);

	public override void _Ready() => Reset();

	public static void AddTo(List<Stack> slots, string id, int count)
	{
		if (count <= 0) return;
		var existing = slots.Find(slot => slot?.Id == id && id is "wood" or "healing_potion" or "sword_iron");
		if (existing != null) existing.Count += count;
		else slots.Add(new Stack(id, count));
	}

	public bool Add(string id, int amount)
	{
		if (amount <= 0) return false;
		var existing = System.Array.Find(_slots, slot => slot?.Id == id && id is "wood" or "healing_potion" or "sword_iron");
		if (existing != null) { existing.Count += amount; return true; }
		var empty = FirstFitSlot(_slots, id, Width, Height);
		if (empty < 0) return false;
		_slots[empty] = new Stack(id, amount);
		return true;
	}

	public bool Remove(string id, int amount)
	{
		if (amount <= 0 || Count(id) < amount) return false;
		for (var i = 0; i < _slots.Length && amount > 0; i++)
		{
			if (_slots[i]?.Id != id) continue;
			var removed = System.Math.Min(amount, _slots[i].Count);
			_slots[i].Count -= removed;
			amount -= removed;
			if (_slots[i].Count == 0) _slots[i] = null;
		}
		return true;
	}

	public bool Move(int from, int to)
	{
		if (!Valid(from) || !Valid(to) || from == to || _slots[from] == null) return false;
		var target = _slots[to];
		if (target?.Id == _slots[from].Id && _slots[from].Id is "wood" or "healing_potion" or "sword_iron")
		{
			_slots[to].Count += _slots[from].Count;
			_slots[from] = null;
		}
		else if (target == null && CanPlace(_slots, _slots[from], to, from, Width, Height)) { _slots[to] = _slots[from]; _slots[from] = null; }
		else if (target != null && _slots[from].Width == 1 && _slots[from].Height == 1 && target.Width == 1 && target.Height == 1)
		{
			var remaining = (Stack[])_slots.Clone(); remaining[from] = null; remaining[to] = null;
			if (!CanPlace(remaining, target, from, -1, Width, Height) || !CanPlace(remaining, _slots[from], to, -1, Width, Height)) return false;
			(_slots[from], _slots[to]) = (_slots[to], _slots[from]);
		}
		else return false;
		return true;
	}

	public bool Split(int from, int to)
	{
		if (!Valid(from) || !Valid(to) || from == to || _slots[from] == null || _slots[from].Count < 2 || _slots[from].Id is "starter_backpack" or "large_backpack" || !CanPlace(_slots, _slots[from], to, from, Width, Height)) return false;
		var amount = _slots[from].Count / 2;
		_slots[from].Count -= amount;
		_slots[to] = new Stack(_slots[from].Id, amount);
		return true;
	}

	public void Sort()
	{
		var sorted = _slots.Where(slot => slot != null).OrderBy(slot => slot.Id).ToArray();
		var packed = new Stack[_slots.Length];
		foreach (var stack in sorted)
		{
			var slot = FirstFitSlot(packed, stack.Id, Width, Height);
			if (slot < 0) return;
			packed[slot] = stack;
		}
		System.Array.Copy(packed, _slots, packed.Length);
	}

	public int TransferFrom(List<Stack> source, int from, int to, int amount = int.MaxValue)
	{
		if (from < 0 || from >= source.Count || source[from] == null || !source[from].Searched || !Valid(to)) return 0;
		var stack = source[from];
		var target = _slots[to];
		if (target != null && (target.Id != stack.Id || stack.Id is not ("wood" or "healing_potion" or "sword_iron"))) return 0;
		if (target == null && amount < stack.Count && stack.Id is not ("wood" or "healing_potion" or "sword_iron")) return 0;
		if (target == null && !CanPlace(_slots, stack, to, -1, Width, Height)) return 0;
		var moved = System.Math.Min(stack.Count, amount);
		if (target == null) _slots[to] = new Stack(stack.Id, moved);
		else target.Count += moved;
		stack.Count -= moved;
		if (stack.Count == 0) source[from] = null;
		return moved;
	}

	public int FirstTransferSlot(Stack stack)
	{
		var matching = System.Array.FindIndex(_slots, slot => slot?.Id == stack?.Id && stack?.Id is "wood" or "healing_potion" or "sword_iron");
		return matching >= 0 ? matching : FirstFitSlot(_slots, stack.Id, Width, Height);
	}

	public int SlotFor(Stack stack) => System.Array.IndexOf(_slots, stack);

	public bool CanPlace(Stack stack, int index, int ignored = -1) => CanPlace(_slots, stack, index, ignored, Width, Height);

	public bool EquipBackpack(int index)
	{
		if (!Valid(index) || _slots[index]?.Id is not ("starter_backpack" or "large_backpack")) return false;
		var next = _slots[index].Id;
		var (width, height) = next == "large_backpack" ? (6, 5) : (StarterWidth, StarterHeight);
		var packed = new Stack[width * height];
		var contents = _slots.Where((stack, slot) => stack != null && slot != index)
			.Append(new Stack(EquippedBackpack, 1)).OrderByDescending(stack => stack.Width * stack.Height);
		foreach (var stack in contents)
		{
			var slot = FirstFitSlot(packed, stack.Id, width, height);
			if (slot < 0) return false;
			packed[slot] = stack;
		}
		_slots = packed;
		EquippedBackpack = next;
		return true;
	}

	private static int FirstFitSlot(Stack[] slots, string id, int width, int height)
	{
		for (var i = 0; i < slots.Length; i++)
			if (CanPlace(slots, new Stack(id, 1), i, -1, width, height)) return i;
		return -1;
	}

	private static bool CanPlace(Stack[] slots, Stack stack, int index, int ignored, int width, int height)
	{
		if (index < 0 || index >= slots.Length || index % width + stack.Width > width || index / width + stack.Height > height) return false;
		for (var other = 0; other < slots.Length; other++)
		{
			if (other == ignored || slots[other] == null) continue;
			var ox = other % width; var oy = other / width;
			var x = index % width; var y = index / width;
			if (x < ox + slots[other].Width && x + stack.Width > ox && y < oy + slots[other].Height && y + stack.Height > oy) return false;
		}
		return true;
	}

	public void Reset()
	{
		EquippedBackpack = "starter_backpack";
		_slots = new Stack[StarterWidth * StarterHeight];
		_slots[0] = new Stack("healing_potion", 2);
	}

	private bool Valid(int index) => index >= 0 && index < _slots.Length;
}
