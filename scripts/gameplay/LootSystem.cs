using Godot;
using System.Collections.Generic;
using System;

public partial class LootSystem : Node
{
	private float _searchDuration = 1;
	private float _progress;
	private int _nextItem;
	private List<Inventory.Stack> _items = new();

	public IReadOnlyList<Inventory.Stack> Items => _items;
	public bool IsOpen { get; private set; }
	public int SearchedCount => _nextItem;
	public string TargetName { get; private set; } = "物资";
	public bool IsSearching => IsOpen && _nextItem < _items.Count;
	public float Progress => IsSearching ? Mathf.Clamp(_progress / _searchDuration, 0, 1) : 0;

	public void Open(List<Inventory.Stack> items, string targetName, float duration)
	{
		_items = items;
		TargetName = targetName;
		_searchDuration = Mathf.Max(.1f, duration);
		_progress = 0;
		_nextItem = 0;
		var occupied = new bool[24];
		foreach (var item in _items)
		{
			item.Searched = false;
			for (var slot = 0; slot < occupied.Length; slot++)
			{
				if (slot % 6 + item.Width > 6 || slot / 6 + item.Height > 4) continue;
				var fits = true;
				for (var y = 0; y < item.Height; y++)
				for (var x = 0; x < item.Width; x++)
					if (occupied[(slot / 6 + y) * 6 + slot % 6 + x]) fits = false;
				if (!fits) continue;
				item.LootSlot = slot;
				for (var y = 0; y < item.Height; y++)
				for (var x = 0; x < item.Width; x++) occupied[(slot / 6 + y) * 6 + slot % 6 + x] = true;
				break;
			}
		}
		IsOpen = true;
	}

	public void Reopen(List<Inventory.Stack> items, string targetName, float duration)
	{
		_items = items;
		TargetName = targetName;
		_searchDuration = Mathf.Max(.1f, duration);
		_progress = 0;
		_nextItem = 0;
		while (_nextItem < _items.Count && (_items[_nextItem] == null || _items[_nextItem].Searched)) _nextItem++;
		IsOpen = true;
	}

	public void Tick(float dt)
	{
		if (!IsSearching) return;
		_progress += dt;
		if (_progress < _searchDuration) return;
		_progress -= _searchDuration;
		RevealNext();
	}

	public void Close()
	{
		IsOpen = false;
		_items = new List<Inventory.Stack>();
		_progress = 0;
		_nextItem = 0;
	}

	public bool TransferTo(Inventory inventory, int from, int to, int amount = int.MaxValue) => inventory.TransferFrom(_items, from, to, amount) > 0;

	public List<Inventory.Stack> CreateChestLoot(BalanceValues balance, Random random)
	{
		var items = new List<Inventory.Stack>();
		Inventory.AddTo(items, "wood", random.Next(balance.ChestWoodMin, balance.ChestWoodMaxExclusive));
		Inventory.AddTo(items, "sword_iron", random.Next(1, 3));
		if (random.Next(balance.ChestBonusRollCount) == 0) Inventory.AddTo(items, "healing_potion", 1);
		return items;
	}

	public List<Inventory.Stack> CreateCorpseLoot(int wood)
	{
		var items = new List<Inventory.Stack>();
		Inventory.AddTo(items, "wood", wood);
		Inventory.AddTo(items, "sword_iron", 1);
		return items;
	}

	private void RevealNext()
	{
		while (_nextItem < _items.Count && _items[_nextItem] == null) _nextItem++;
		if (_nextItem >= _items.Count) return;
		_items[_nextItem++].Searched = true;
	}
}
