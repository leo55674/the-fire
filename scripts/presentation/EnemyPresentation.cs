using Godot;
using System;

public partial class EnemyPresentation : Node2D
{
	[Export] private Sprite2D _sprite;
	[Export] private Label _damageLabel;
	[Export] private EnemySystem.EnemyKind _kind;
	[Export] private Texture2D _normalTexture;
	[Export] private Texture2D _hurtTexture;
	[Export] private Texture2D _deadTexture;
	public EnemySystem.EnemyKind Kind => _kind;

	public override void _Ready()
	{
		if (_sprite == null || _damageLabel == null || _normalTexture == null || _hurtTexture == null || _deadTexture == null)
			throw new InvalidOperationException("Enemy scene requires its sprite, damage label, and three state textures.");
	}

	public void Present(EnemySystem.EnemyState enemy, GameSession session, Vector2 screenPosition)
	{
		Visible = session.IsVisible(enemy.Position);
		if (!Visible) return;
		Position = screenPosition;
		_sprite.Texture = enemy.VisualState switch
		{
			EnemySystem.EnemyVisualState.Hurt => _hurtTexture,
			EnemySystem.EnemyVisualState.Dead => _deadTexture,
			_ => _normalTexture
		};
		if (!enemy.IsDying) _sprite.FlipH = enemy.Position.X > session.Player.X;
		_damageLabel.Visible = enemy.DamageFeedbackTime > 0;
		if (!_damageLabel.Visible) return;
		var progress = 1 - enemy.DamageFeedbackTime / session.Balance.EnemyDamageFeedbackDuration;
		_damageLabel.Text = $"-{enemy.LastDamage:0}";
		_damageLabel.Position = new Vector2(-50, -142 - progress * 24);
		_damageLabel.Modulate = new Color(1, 1, 1, Mathf.Clamp(enemy.DamageFeedbackTime * 4, 0, 1));
	}
}
