using Godot;

public partial class EnemyPresentation : Node2D
{
	[Export] private Sprite2D _sprite;
	[Export] private Label _damageLabel;
	[Export] private Godot.Collections.Array<Texture2D> _textures;

	public void Present(GameSession.EnemyState enemy, GameSession session, Vector2 screenPosition)
	{
		Visible = session.IsVisible(enemy.Position);
		if (!Visible) return;
		Position = screenPosition + new Vector2(0, -69);
		_sprite.Texture = _textures[(int)enemy.Kind * 3 + (int)enemy.VisualState];
		_sprite.FlipH = enemy.Position.X > session.Player.X;
		_damageLabel.Visible = enemy.DamageFeedbackTime > 0;
		if (!_damageLabel.Visible) return;
		var progress = 1 - enemy.DamageFeedbackTime / session.Balance.EnemyDamageFeedbackDuration;
		_damageLabel.Text = $"-{enemy.LastDamage:0}";
		_damageLabel.Position = new Vector2(-50, -73 - progress * 24);
		_damageLabel.Modulate = new Color(1, 1, 1, Mathf.Clamp(enemy.DamageFeedbackTime * 4, 0, 1));
	}
}
