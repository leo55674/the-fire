using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Inventory : Node
{
	public sealed class Stack
	{
		public string Id;
		public int Count;
		public Stack(string id, int count) { Id = id; Count = count; }
	}

	private readonly Stack[] _slots = new Stack[24];
	public IReadOnlyList<Stack> Slots => _slots;
	public IReadOnlyDictionary<string, int> Items => _slots.Where(slot => slot != null)
		.GroupBy(slot => slot.Id).ToDictionary(group => group.Key, group => group.Sum(slot => slot.Count));
	public int Capacity => _slots.Length;
	public int Count(string id) => _slots.Where(slot => slot?.Id == id).Sum(slot => slot.Count);

	public override void _Ready() => Reset();

	public static void AddTo(List<Stack> slots, string id, int count)
	{
		if (count <= 0) return;
		var existing = slots.Find(slot => slot?.Id == id && id is "wood" or "healing_potion");
		if (existing != null) existing.Count += count;
		else slots.Add(new Stack(id, count));
	}

	public bool Add(string id, int amount)
	{
		if (amount <= 0) return false;
		var existing = System.Array.Find(_slots, slot => slot?.Id == id && id is "wood" or "healing_potion");
		if (existing != null) { existing.Count += amount; return true; }
		var empty = System.Array.FindIndex(_slots, slot => slot == null);
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
		if (_slots[to]?.Id == _slots[from].Id && _slots[from].Id is "wood" or "healing_potion")
		{
			_slots[to].Count += _slots[from].Count;
			_slots[from] = null;
		}
		else (_slots[from], _slots[to]) = (_slots[to], _slots[from]);
		return true;
	}

	public bool Split(int from, int to)
	{
		if (!Valid(from) || !Valid(to) || _slots[from] == null || _slots[from].Count < 2 || _slots[to] != null) return false;
		var amount = _slots[from].Count / 2;
		_slots[from].Count -= amount;
		_slots[to] = new Stack(_slots[from].Id, amount);
		return true;
	}

	public void Sort()
	{
		var sorted = _slots.Where(slot => slot != null).OrderBy(slot => slot.Id).ToArray();
		System.Array.Clear(_slots);
		System.Array.Copy(sorted, _slots, sorted.Length);
	}

	public int TransferFrom(List<Stack> source, int from, int to, int amount = int.MaxValue)
	{
		if (from < 0 || from >= source.Count || source[from] == null || !Valid(to)) return 0;
		var stack = source[from];
		var target = _slots[to];
		if (target != null && (target.Id != stack.Id || stack.Id is not ("wood" or "healing_potion"))) return 0;
		if (target == null && amount < stack.Count && stack.Id is not ("wood" or "healing_potion")) return 0;
		var moved = System.Math.Min(stack.Count, amount);
		if (target == null) _slots[to] = new Stack(stack.Id, moved);
		else target.Count += moved;
		stack.Count -= moved;
		if (stack.Count == 0) source[from] = null;
		return moved;
	}

	public int FirstTransferSlot(Stack stack)
	{
		var matching = System.Array.FindIndex(_slots, slot => slot?.Id == stack?.Id && stack?.Id is "wood" or "healing_potion");
		return matching >= 0 ? matching : System.Array.FindIndex(_slots, slot => slot == null);
	}

	public void Reset()
	{
		System.Array.Clear(_slots);
		_slots[0] = new Stack("light_sword", 1);
		_slots[1] = new Stack("healing_potion", 2);
	}

	private bool Valid(int index) => index >= 0 && index < _slots.Length;
}
