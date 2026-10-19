using Godot;
using System;

public partial class ExtinguisherBehavior : EnemyPresentation
{
	public static bool Tick(EnemySystem.EnemyState enemy, float dt, Vector2 player, Vector2 mainFire, BalanceValues balance)
	{
		var target = player.DistanceTo(mainFire) > balance.ExtinguisherTargetRange ? mainFire : player;
		enemy.Position += new Vector2(MathF.Sign(target.X - enemy.Position.X) * balance.OtherEnemySpeed * dt, 0);
		if (enemy.Position.DistanceTo(mainFire) >= balance.ExtinguisherAttackRange || enemy.AttackDelay > 0) return false;
		enemy.AttackDelay = balance.ExtinguisherAttackCooldown;
		return true;
	}
}
