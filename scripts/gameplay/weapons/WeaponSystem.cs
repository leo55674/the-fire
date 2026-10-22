using Godot;
using System;
using System.Collections.Generic;

public partial class WeaponSystem : Node
{
	private static readonly string[] SwordNames = { "照夜", "霜华", "碧澜" };
	private readonly int[] _swordOrder = { 0, 1, 2 };
	private readonly Vector2[] _companionSwordPositions = new Vector2[2];
	private bool _hasShuanghua;
	private bool _hasBilan;
	private float _formationFacing = 1;

	public int EquippedCount => 1 + (_hasShuanghua ? 1 : 0) + (_hasBilan ? 1 : 0);
	public int ActiveSwordIndex => _swordOrder[0];
	public string ActiveSwordName => SwordNames[ActiveSwordIndex];
	public float FormationFacing => _formationFacing;
	public string EquippedNames
	{
		get
		{
			var names = new string[EquippedCount];
			for (var i = 0; i < names.Length; i++) names[i] = i == 0 ? $"【{SwordNames[_swordOrder[i]]}】" : SwordNames[_swordOrder[i]];
			return string.Join(" · ", names);
		}
	}
	public IReadOnlyList<Vector2> CompanionSwordPositions => _companionSwordPositions;

	public void ResetEquipment(int facing)
	{
		_hasShuanghua = false;
		_hasBilan = false;
		for (var i = 0; i < _swordOrder.Length; i++) _swordOrder[i] = i;
		_formationFacing = facing;
		System.Array.Fill(_companionSwordPositions, Vector2.Zero);
	}

	public bool CycleActiveSword(int steps)
	{
		if (EquippedCount < 2 || steps == 0) return false;
		var shift = ((steps % EquippedCount) + EquippedCount) % EquippedCount;
		if (shift == 0) return false;
		var order = new int[EquippedCount];
		for (var i = 0; i < order.Length; i++) order[i] = _swordOrder[(i + shift) % order.Length];
		Array.Copy(order, _swordOrder, order.Length);
		return true;
	}

	public int CompanionSwordIndex(int slot)
	{
		return slot >= 0 && slot + 1 < EquippedCount ? _swordOrder[slot + 1] : -1;
	}

	public void UpdateFollowers(Vector2 player, int facing, float dt)
	{
		var blend = 1 - MathF.Exp(-8 * dt);
		_formationFacing = Mathf.Lerp(_formationFacing, facing, blend);
		for (var i = 0; i < _companionSwordPositions.Length; i++)
		{
			var target = player + new Vector2(-_formationFacing * (112 + i * 54), -44);
			_companionSwordPositions[i] = _companionSwordPositions[i].Lerp(target, 1 - MathF.Exp(-10 * dt));
		}
	}

	public string CraftNext(Inventory inventory, int materialCost)
	{
		if (EquippedCount >= 3) return "三剑已齐，铁匠铺暂无新配方。";
		if (inventory.Count("sword_iron") < materialCost) return $"铸剑铁料不足：打造还需要 {materialCost} 份。";
		inventory.Remove("sword_iron", materialCost);
		if (!_hasShuanghua)
		{
			_hasShuanghua = true;
			return "铁匠打造完成：霜华已加入剑列。";
		}
		_hasBilan = true;
		return "铁匠打造完成：碧澜已加入剑列。";
	}
}
