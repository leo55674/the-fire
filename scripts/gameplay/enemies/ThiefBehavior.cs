using Godot;
using System;

public partial class ThiefBehavior : EnemyPresentation
{
	public static bool Tick(EnemySystem.EnemyState enemy, float dt, Vector2 player, float groundY, Inventory inventory, BalanceValues balance)
	{
		var target = enemy.StoleWood ? new Vector2(MathF.Sign(enemy.Position.X) * 1600, groundY) : player;
		enemy.Position += new Vector2(MathF.Sign(target.X - enemy.Position.X) * balance.OtherEnemySpeed * dt, 0);
		if (enemy.StoleWood || enemy.Position.DistanceTo(player) >= balance.ThiefAttackRange || enemy.AttackDelay > 0) return false;
		var stoleWood = inventory.Count("wood") > 0;
		if (stoleWood)
		{
			inventory.Remove("wood", 1);
			enemy.StoleWood = true;
		}
		enemy.AttackDelay = balance.ThiefAttackCooldown;
		return stoleWood;
	}
}
