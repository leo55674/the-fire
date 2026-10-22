using Godot;
using System;

public partial class FrostWraithKnight : Node2D
{
	[Export] private Sprite2D _sprite;
	[Export] private Label _healthLabel;
	private const float MaxHealth = 600;
	private float _health = MaxHealth;
	private float _worldX = 350;
	private float _hitCooldown;
	private float _attackCooldown = 1.2f;
	private float _windup;
	private Vector2 _lastSword;
	private Vector2 _lastReplay;
	private bool _tracking;
	public bool Defeated => _health <= 0;

	public void Reset()
	{
		_health = MaxHealth;
		_worldX = 350;
		_hitCooldown = 0;
		_attackCooldown = 1.2f;
		_windup = 0;
		_tracking = false;
		Visible = true;
	}

	public void Tick(GameSession session, float dt)
	{
		Position = new Vector2(GetViewportRect().Size.X / 2 + _worldX - session.CameraFocus.X, session.GroundY);
		if (Defeated) { _healthLabel.Text = "霜冥骑士已陨落"; return; }
		if (dt <= 0) return;
		_hitCooldown = MathF.Max(0, _hitCooldown - dt);
		_attackCooldown = MathF.Max(0, _attackCooldown - dt);
		var body = new Vector2(_worldX, session.GroundY - 230);
		if (_tracking && _hitCooldown <= 0)
		{
			var activeSword = session.SwordState is GameSession.SwordMode.Attacking or GameSession.SwordMode.Controlled;
			var swordHit = activeSword && body.DistanceTo(Geometry2D.GetClosestPointToSegment(body, _lastSword, session.SwordPosition)) < 155;
			var replayHit = session.IsReplaySwordActive && body.DistanceTo(Geometry2D.GetClosestPointToSegment(body, _lastReplay, session.ReplaySwordPosition)) < 155;
			if (swordHit || replayHit)
			{
				_health = MathF.Max(0, _health - (replayHit ? session.SwordDamage * .5f : session.SwordDamage));
				_hitCooldown = .38f;
				if (Defeated) session.ReportRealmEvent("霜冥骑士倒下，秘境的寒意开始消散。");
			}
		}
		_lastSword = session.SwordPosition;
		_lastReplay = session.ReplaySwordPosition;
		_tracking = true;
		var distance = MathF.Abs(session.Player.X - _worldX);
		if (_windup > 0)
		{
			_windup -= dt;
			if (_windup <= 0 && distance < 175 && !Defeated) session.DamageFromBoss();
		}
		else if (distance > 190 && distance < 650)
			_worldX = Mathf.Clamp(_worldX + MathF.Sign(session.Player.X - _worldX) * 62 * dt, -170, 540);
		else if (distance <= 175 && _attackCooldown <= 0)
		{
			_windup = .55f;
			_attackCooldown = 2f;
			session.ReportRealmEvent("霜冥骑士正在蓄力挥剑，快突进闪避！");
		}
		_sprite.Modulate = Defeated ? new Color(.35f, .45f, .55f, .6f) : _windup > 0 ? new Color(1, .55f, .55f) : _hitCooldown > .22f ? new Color(.55f, .9f, 1) : Colors.White;
		_healthLabel.Text = Defeated ? "霜冥骑士已陨落" : $"霜冥骑士  {_health:0}/{MaxHealth:0}";
	}
}
