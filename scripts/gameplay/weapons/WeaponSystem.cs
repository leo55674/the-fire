using Godot;
using System;
using System.Collections.Generic;

public partial class WeaponSystem : Node
{
	private readonly Vector2[] _companionSwordPositions = new Vector2[2];
	private bool _hasShuanghua;
	private bool _hasBilan;

	public int EquippedCount => 1 + (_hasShuanghua ? 1 : 0) + (_hasBilan ? 1 : 0);
	public string EquippedNames => _hasBilan ? "照夜 · 霜华 · 碧澜" : _hasShuanghua ? "照夜 · 霜华" : "照夜";
	public IReadOnlyList<Vector2> CompanionSwordPositions => _companionSwordPositions;

	public void ResetEquipment()
	{
		_hasShuanghua = false;
		_hasBilan = false;
		System.Array.Fill(_companionSwordPositions, Vector2.Zero);
	}

	public void UpdateFollowers(Vector2 player, int facing, float dt)
	{
		for (var i = 0; i < _companionSwordPositions.Length; i++)
		{
			var target = player + new Vector2(-facing * (112 + i * 54), -32 - i * 12);
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
