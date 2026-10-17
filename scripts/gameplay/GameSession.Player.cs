using Godot;
using System;

public partial class GameSession : Node
{
	private void UpdatePlayer(float dt)
	{
		IsDashing = false;
		_dashCooldown = MathF.Max(0, _dashCooldown - dt);
		var dashPressed = _input.Pressed("dash");
		if (!IsSwordControlled)
		{
			var direction = (_input.Held("right") ? 1 : 0) - (_input.Held("left") ? 1 : 0);
			if (direction != 0) PlayerFacing = direction;
			if (dashPressed && _dashCooldown <= 0)
			{
				_dashRemaining = _balance.DashDistance;
				_dashCooldown = _balance.DashCooldown;
				Message = "身化剑影，避开锋芒。";
			}
			if (_dashRemaining > 0)
			{
				IsDashing = true;
				var step = MathF.Min(_balance.DashSpeed * dt, _dashRemaining);
				Player += new Vector2(PlayerFacing * step, 0);
				_dashRemaining -= step;
			}
			else Player += new Vector2(direction * _balance.PlayerMoveSpeed * dt, 0);
		}
		_verticalVelocity += _balance.Gravity * dt;
		if (!IsSwordControlled && _input.Held("jump") && Player.Y >= GroundY) _verticalVelocity = -_balance.JumpImpulse;
		Player = new Vector2(Player.X, MathF.Min(GroundY, Player.Y + _verticalVelocity * dt));
		if (Player.Y >= GroundY) _verticalVelocity = 0;
		var spiritPosition = IsSwordControlled ? SwordPosition : Player;
		var emberRecovery = IsInSpiritRecoveryRange(spiritPosition) ? _balance.EmberTombRecovery : IsSwordControlled ? 0 : _balance.EmberPassiveRecovery;
		Ember = Mathf.Clamp(Ember + emberRecovery * dt, 0, _balance.MaxEmber);
		_attackCooldown = MathF.Max(0, _attackCooldown - dt);
		SwingTime = MathF.Max(0, SwingTime - dt);
		HurtTime = MathF.Max(0, HurtTime - dt);
	}

	private void UpdateSword(float dt)
	{
		_swordHitCooldown = MathF.Max(0, _swordHitCooldown - dt);
		if (_input.Pressed("sword_control"))
		{
			if (SwordState == SwordMode.Controlled)
				BeginSwordReturn("本命剑循神识线归鞘。");
			else if (SwordState == SwordMode.Following && Fuel > 0 && Ember > 0)
			{
				SwordState = SwordMode.Controlled;
				_swordControlTarget = SwordPosition;
				Input.MouseMode = Input.MouseModeEnum.Captured;
				EmitSignal(SignalName.SoundCueRequested, "control_enter");
				Message = "神识入剑：视角随剑，移鼠御剑，再按右键归剑。";
			}
		}
		var controlBlendTarget = IsSwordControlled ? 1f : 0f;
		_swordControlBlend = Mathf.MoveToward(_swordControlBlend, controlBlendTarget, dt / _balance.SwordControlTransitionDuration);

		if (SwordState == SwordMode.Following)
		{
			var target = SwordFollowPosition();
			SwordPosition = SwordPosition.Lerp(target, 1 - MathF.Exp(-10 * dt));
			SwordVelocity = new Vector2(PlayerFacing, 0);
			return;
		}
		if (SwordState == SwordMode.Returning)
		{
			var target = SwordFollowPosition();
			var toTarget = target - SwordPosition;
			SwordVelocity = toTarget.Normalized() * _balance.SwordReturnSpeed;
			SwordPosition = SwordPosition.MoveToward(target, _balance.SwordReturnSpeed * dt);
			if (SwordPosition.DistanceTo(target) < 4)
			{
				SwordPosition = target;
				SwordState = SwordMode.Following;
			}
			return;
		}
		if (SwordState == SwordMode.Attacking)
		{
			SwordVelocity = (_swordAttackTarget - SwordPosition).Normalized() * _balance.SwordAttackSpeed;
			SwordPosition = SwordPosition.MoveToward(_swordAttackTarget, _balance.SwordAttackSpeed * dt);
			var attackHit = !_swordAttackHit ? Enemies.Find(enemy => !enemy.IsDying && enemy.Position.DistanceTo(SwordPosition) < _balance.EnemyAttackHitRadius) : null;
			if (attackHit != null)
			{
				_swordAttackHit = true;
					var killed = attackHit.TakeDamage(SwordDamage, _balance.EnemyDamageFeedbackDuration, _balance.EnemyHurtDuration, _balance.EnemyUnclaimedCorpseLifetime);
				EmitSignal(SignalName.SoundCueRequested, killed ? "sword_defeat" : "sword_hit");
				Message = killed ? "飞剑斩灭了一缕剑煞。" : $"飞剑造成 {SwordDamage:0} 点伤害。";
			}
			if (SwordPosition.DistanceTo(_swordAttackTarget) < 3) BeginSwordReturn(Message);
			return;
		}

		var targetOffset = (_swordControlTarget - SwordPosition).LimitLength(_balance.SwordControlMaxLead);
		_swordControlTarget = SwordPosition + targetOffset;
		var desiredMovement = targetOffset * (1 - MathF.Exp(-_balance.SwordControlSmoothness * dt));
		desiredMovement = desiredMovement.LimitLength(_balance.SwordControlSpeed * dt);
		var movement = (SwordVelocity * dt).Lerp(desiredMovement, _swordControlBlend);
		SwordVelocity = movement / MathF.Max(dt, .001f);
		SwordPosition += movement;

		var distance = SwordDistance;
		if (!IsInSpiritRecoveryRange(SwordPosition)) Ember = MathF.Max(0, Ember - (_balance.EmberControlBaseDrain + distance / _balance.EmberControlDistanceDrain) * dt);
		var hit = _swordHitCooldown <= 0 ? Enemies.Find(enemy => !enemy.IsDying && enemy.Position.DistanceTo(SwordPosition) < _balance.SwordHitRadius) : null;
		if (hit != null)
		{
			_swordHitCooldown = _balance.SwordHitCooldown;
			var killed = hit.TakeDamage(SwordDamage, _balance.EnemyDamageFeedbackDuration, _balance.EnemyHurtDuration, _balance.EnemyUnclaimedCorpseLifetime);
			EmitSignal(SignalName.SoundCueRequested, killed ? "sword_defeat" : "sword_hit");
			Message = killed ? "飞剑贯穿了一缕剑煞。" : $"本命剑造成 {SwordDamage:0} 点伤害。";
		}
		if (Ember <= 0) BeginSwordReturn("神识耗尽，本命剑强制归剑。");
	}

	private Vector2 SwordFollowPosition() => Player + new Vector2(-PlayerFacing * 64, -44);
	private bool IsInSpiritRecoveryRange(Vector2 position) =>
		position.DistanceTo(MainFire) < _balance.SwordTombRadius || Fires.Exists(fire => position.DistanceTo(fire.Position) < fire.Radius);

	private void BeginSwordReturn(string message)
	{
		SwordState = SwordMode.Returning;
		_swordControlTarget = SwordPosition;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		EmitSignal(SignalName.SoundCueRequested, "sword_return");
		Message = message;
	}


	private void HandleCombat()
	{
		if (IsSwordControlled) return;
		var axe = _input.Pressed("axe");
		if (_attackCooldown > 0) return;
		var mouse = GetViewport().GetMousePosition();
		var viewportCenter = GetViewport().GetVisibleRect().Size / 2;
		var mouseWorld = new Vector2(CameraFocus.X + mouse.X - viewportCenter.X, mouse.Y);
		if (axe && SwordState == SwordMode.Following)
		{
			_attackCooldown = _balance.AttackCooldown;
			SwordState = SwordMode.Attacking;
			_swordAttackTarget = mouseWorld;
			_swordAttackHit = false;
			EmitSignal(SignalName.SoundCueRequested, "sword_launch");
			Message = "飞剑应念而出。";
		}
	}

	private void DamagePlayer()
	{
		if (HurtTime > 0 || IsDashing) return;
		Health--; HurtTime = _balance.PlayerHurtDuration; Message = IsSwordControlled ? "忆身受袭，御剑被强制中断！" : "逐魄煞击中了忆身。";
		if (IsSwordControlled) BeginSwordReturn(Message);
		EmitSignal(SignalName.SoundCueRequested, "spirit_hurt");
		if (Health > 0) return;
		_inventory.Remove("wood", Wood / _balance.RespawnWoodLossDivisor); Fuel = MathF.Max(0, Fuel - _balance.RespawnFuelPenalty); Health = _balance.MaxHealth; Player = MainFire;
		Message = "忆身消散后在剑冢重凝，并遗失了部分剑骸。";
	}

}
