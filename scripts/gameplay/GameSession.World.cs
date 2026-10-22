using Godot;
using System;

public partial class GameSession : Node
{
	private void UpdateFire(float dt)
	{
		PhaseRemaining -= dt;
		if (PhaseRemaining <= 0)
		{
			IsNight = !IsNight;
			PhaseRemaining += IsNight ? _balance.NightDuration : _balance.DayDuration;
			Message = IsNight ? "夜幕降临，啮阵兽开始袭击村庄。" : "天亮了，村庄暂时安全。";
		}
		Fuel = MathF.Max(0, Fuel - (_balance.FuelBaseDecay + FireLevel * _balance.FuelDecayPerFireLevel) * dt);
		if (Fuel <= 0)
		{
			Message = "剑冢灵蕴耗尽。按 Enter 重启远征。";
			if (_commands.Pressed("restart")) Reset();
			return;
		}
		_chestSpawnTimer -= dt;
		if (_chestSpawnTimer <= 0)
		{
			if (Chests.FindAll(chest => !chest.Searched).Count < _balance.ChestMaxUnsearchedCount) SpawnChestNearPlayer();
			_chestSpawnTimer = _random.Next((int)_balance.ChestInitialSpawnInterval, (int)_balance.ChestInitialSpawnInterval + 7);
		}
		_enemySystem.AdvanceSpawning(dt, FireLevel, DreamAsh, GroundY, Player.X, IsNight || FinalNight > 0);
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

	private void PlaceFire(float life, float radius, int cost, string message)
	{
		if (Fires.Exists(fire => Player.DistanceTo(fire.Position) < 40)) return;
		_inventory.Remove("wood", cost); Fires.Add(new FireState(Player, life, radius)); Message = message;
	}

	private void SpawnChest(float x) => Chests.Add(new ChestState(new Vector2(x, GroundY)));
	private void SpawnChestNearPlayer() => SpawnChest(Player.X + (_random.Next(2) == 0 ? -1 : 1) * _random.Next(260, 701));

}
