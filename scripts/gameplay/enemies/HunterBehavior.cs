using Godot;
using System;

public partial class HunterBehavior : EnemyPresentation
{
	public static bool Tick(EnemySystem.EnemyState enemy, float dt, Vector2 player, BalanceValues balance)
	{
		enemy.Position += new Vector2(MathF.Sign(player.X - enemy.Position.X) * balance.HunterSpeed * dt, 0);
		if (enemy.Position.DistanceTo(player) >= balance.HunterAttackRange || enemy.AttackDelay > 0) return false;
		enemy.AttackDelay = balance.HunterAttackCooldown;
		return true;
	}
}
