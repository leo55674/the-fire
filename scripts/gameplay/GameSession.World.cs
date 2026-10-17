using Godot;
using System;

public partial class GameSession : Node
{
	private void UpdateFire(float dt)
	{
		Fuel = MathF.Max(0, Fuel - (_balance.FuelBaseDecay + FireLevel * _balance.FuelDecayPerFireLevel) * dt);
		if (Fuel <= 0)
		{
			Message = "剑冢灵蕴耗尽。按 Enter 重启远征。";
			if (_input.Pressed("restart")) Reset();
			return;
		}
		_chestSpawnTimer -= dt;
		if (_chestSpawnTimer <= 0)
		{
			if (Chests.FindAll(chest => !chest.Searched).Count < _balance.ChestMaxUnsearchedCount) SpawnChestNearPlayer();
			_chestSpawnTimer = _random.Next((int)_balance.ChestInitialSpawnInterval, (int)_balance.ChestInitialSpawnInterval + 7);
		}
		_spawnTimer -= dt;
		if (_spawnTimer <= 0) { SpawnEnemy(); _spawnTimer = MathF.Max(_balance.EnemySpawnMinInterval, _balance.EnemySpawnBaseInterval - FireLevel * _balance.EnemySpawnFireLevelFactor - DreamAsh * _balance.EnemySpawnDreamAshFactor); }
		if (FinalNight > 0 && (FinalNight -= dt) <= 0) Message = "剑冢守住了最终剑潮。";
	}

	private void UpdateTemporaryFires(float dt)
	{
		for (var i = Fires.Count - 1; i >= 0; i--)
		{
			Fires[i].Life -= dt;
			if (Fires[i].Life <= 0) Fires.RemoveAt(i);
		}
	}

	private void UpdateEnemies(float dt)
	{
		foreach (var enemy in Enemies)
		{
			enemy.DamageFeedbackTime = MathF.Max(0, enemy.DamageFeedbackTime - dt);
			if (enemy.IsDying)
			{
				enemy.DeathAge += dt;
				if (enemy.Searched && enemy.Loot.Find(stack => stack != null) == null) enemy.DeathTime = MathF.Min(enemy.DeathTime, _balance.EnemyCorpseLifetime);
				enemy.DeathTime -= dt;
				continue;
			}
			enemy.HurtTime = MathF.Max(0, enemy.HurtTime - dt);
			enemy.AttackDelay = MathF.Max(0, enemy.AttackDelay - dt);
			var target = enemy.Kind == EnemyKind.Extinguisher && Player.DistanceTo(MainFire) > _balance.ExtinguisherTargetRange ? MainFire : Player;
			if (enemy.Kind == EnemyKind.Thief && enemy.StoleWood) target = new Vector2(MathF.Sign(enemy.Position.X) * 1600, GroundY);
			enemy.Position += new Vector2(MathF.Sign(target.X - enemy.Position.X) * (enemy.Kind == EnemyKind.Hunter ? _balance.HunterSpeed : _balance.OtherEnemySpeed) * dt, 0);
			if (enemy.Kind == EnemyKind.Extinguisher && enemy.Position.DistanceTo(MainFire) < _balance.ExtinguisherAttackRange && enemy.AttackDelay <= 0)
			{
				Fuel = MathF.Max(0, Fuel - _balance.ExtinguisherFuelDamage); enemy.AttackDelay = _balance.ExtinguisherAttackCooldown; Message = "啮阵兽正在侵蚀剑冢！";
			}
			if (enemy.Kind == EnemyKind.Thief && !enemy.StoleWood && enemy.Position.DistanceTo(Player) < _balance.ThiefAttackRange && enemy.AttackDelay <= 0)
			{
			if (Wood > 0) { _inventory.Remove("wood", 1); enemy.StoleWood = true; Message = "盗铭伥偷走了一块剑骸。"; }
				enemy.AttackDelay = _balance.ThiefAttackCooldown;
			}
			if (enemy.Kind == EnemyKind.Hunter && enemy.Position.DistanceTo(Player) < _balance.HunterAttackRange && enemy.AttackDelay <= 0)
			{
				DamagePlayer(); enemy.AttackDelay = _balance.HunterAttackCooldown;
			}
		}
		Enemies.RemoveAll(enemy => enemy.IsDying ? enemy.DeathTime <= 0 : MathF.Abs(enemy.Position.X) > _balance.EnemyDespawnDistance);
	}


	private void PlaceFire(float life, float radius, int cost, string message)
	{
		if (Fires.Exists(fire => Player.DistanceTo(fire.Position) < 40)) return;
		_inventory.Remove("wood", cost); Fires.Add(new FireState(Player, life, radius)); Message = message;
	}

	private void SpawnEnemy()
	{
		var side = _random.Next(2) == 0 ? -1 : 1;
		Enemies.Add(new EnemyState((EnemyKind)_random.Next(0, 3), side * _random.Next(650, 1050), GroundY, _balance.EnemyHealth, _balance.EnemyDeathAnimationDuration, _balance.EnemyUnclaimedCorpseLifetime));
	}

	private void SpawnChest(float x) => Chests.Add(new ChestState(new Vector2(x, GroundY)));
	private void SpawnChestNearPlayer() => SpawnChest(Player.X + (_random.Next(2) == 0 ? -1 : 1) * _random.Next(260, 701));

}
