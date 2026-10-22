using Godot;
using System;

public partial class SwordCaseHud : Control
{
	[Export] private TextureRect _mainSword;
	[Export] private TextureRect _leftSword;
	[Export] private TextureRect _rightSword;
	[Export] private Texture2D _zhaoye;
	[Export] private Texture2D _shuanghua;
	[Export] private Texture2D _bilan;

	public override void _Ready()
	{
		if (_mainSword == null || _leftSword == null || _rightSword == null || _zhaoye == null || _shuanghua == null || _bilan == null)
			throw new InvalidOperationException("SwordCaseHud requires three sword nodes and textures.");
	}

	public void Sync(GameSession session)
	{
		Texture2D SwordTexture(int index) => index switch { 0 => _zhaoye, 1 => _shuanghua, 2 => _bilan, _ => null };
		_mainSword.Texture = SwordTexture(session.ActiveSwordIndex);
		_leftSword.Visible = session.SwordCount > 1;
		_rightSword.Visible = session.SwordCount > 2;
		if (_leftSword.Visible) _leftSword.Texture = SwordTexture(session.CompanionSwordIndex(0));
		if (_rightSword.Visible) _rightSword.Texture = SwordTexture(session.CompanionSwordIndex(1));
	}
}
