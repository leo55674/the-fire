using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	public const int CustomSwordMoveId = 10;
	public const int MaxSwordComboSteps = 10;

	private sealed record SwordMoveDefinition(string Name, SwordSample[] Path);
	private static readonly SwordMoveDefinition[] PresetSwordMoves =
	{
		new("上挑", Path(.8f, new(0, 0), new(25, 45), new(75, 15), new(135, -105), new(65, -35), new(0, 0))),
		new("下劈", Path(.8f, new(0, 0), new(35, -80), new(95, -110), new(150, 85), new(60, 25), new(0, 0))),
		new("直刺", Path(.7f, new(0, 0), new(85, 0), new(205, 0), new(80, 0), new(0, 0))),
		new("横扫", Path(.8f, new(0, 0), new(40, -35), new(145, -35), new(185, 20), new(70, 20), new(0, 0))),
		new("回旋", Path(1.1f, new(0, 0), new(50, -55), new(110, -15), new(90, 55), new(20, 70), new(-40, 20), new(0, 0))),
		new("斜上斩", Path(.75f, new(0, 0), new(45, 45), new(115, -35), new(175, -105), new(65, -30), new(0, 0))),
		new("斜下斩", Path(.75f, new(0, 0), new(40, -55), new(110, 20), new(175, 105), new(65, 30), new(0, 0))),
		new("折返刺", Path(1f, new(0, 0), new(155, 0), new(45, 0), new(200, -20), new(65, -5), new(0, 0))),
		new("游龙", Path(1.2f, new(0, 0), new(45, -55), new(90, 55), new(140, -60), new(200, 45), new(90, 10), new(0, 0))),
		new("环绕斩", Path(1.2f, new(0, 0), new(65, -75), new(155, -85), new(220, -20), new(185, 75), new(90, 85), new(0, 0)))
	};

	public static int PresetSwordMoveCount => PresetSwordMoves.Length;
	public static string SwordMoveName(int id) => id == CustomSwordMoveId ? "自录剑式" : PresetSwordMoves[id].Name;

	public bool AddComboMove(int id)
	{
		if (_swordCombo.Count >= MaxSwordComboSteps || id < 0 || id > CustomSwordMoveId || id == CustomSwordMoveId && !HasRecordedSword) return false;
		_swordCombo.Add(id);
		return true;
	}

	public bool MoveComboStep(int index, int direction)
	{
		var next = index + direction;
		if (index < 0 || next < 0 || index >= _swordCombo.Count || next >= _swordCombo.Count) return false;
		(_swordCombo[index], _swordCombo[next]) = (_swordCombo[next], _swordCombo[index]);
		return true;
	}

	public bool RemoveComboStep(int index)
	{
		if (index < 0 || index >= _swordCombo.Count) return false;
		_swordCombo.RemoveAt(index);
		return true;
	}

	public void PlaySwordCombo()
	{
		SetComboOpen(false);
		StartSwordReplay();
	}

	private static SwordSample[] Path(float duration, params Vector2[] offsets)
	{
		var path = new SwordSample[offsets.Length];
		for (var i = 0; i < offsets.Length; i++)
			path[i] = new SwordSample(duration * i / (offsets.Length - 1), offsets[i]);
		return path;
	}

	private SwordSample[][] BuildReplaySequence()
	{
		var sequence = new List<SwordSample[]>();
		foreach (var id in _swordCombo)
		{
			if (id == CustomSwordMoveId)
			{
				if (HasRecordedSword) sequence.Add(_recordedSword.ToArray());
			}
			else sequence.Add(PresetSwordMoves[id].Path);
		}
		if (sequence.Count == 0 && HasRecordedSword) sequence.Add(_recordedSword.ToArray());
		return sequence.ToArray();
	}
}
