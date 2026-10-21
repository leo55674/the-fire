using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	private void HandleInteractions()
	{
		var searchPressed = _commands.Pressed("search");
		var feed = _commands.Pressed("feed"); var upgrade = _commands.Pressed("upgrade");
		var burnName = _commands.Pressed("burn_name"); var returnName = _commands.Pressed("return_name");
		var torch = _commands.Pressed("torch"); var campfire = _commands.Pressed("campfire");
		if (Fuel <= 0) return;
		if (_blacksmith.IsInRange(Player, GroundY, _balance.BlacksmithRange))
		{
			SearchPrompt = SwordCount switch
			{
				1 => $"按 {_commands.SearchLabel} 打造霜华（需要 {_balance.SwordCraftMaterialCost} 份铸剑铁料；现有 {SwordIron}）",
				2 => $"按 {_commands.SearchLabel} 打造碧澜（需要 {_balance.SwordCraftMaterialCost} 份铸剑铁料；现有 {SwordIron}）",
				_ => "铁匠铺：三剑已齐"
			};
		if (searchPressed) Message = _weaponSystem.CraftNext(_inventory, _balance.SwordCraftMaterialCost);
			return;
		}
		UpdateSearch(searchPressed);
		var atFire = Player.DistanceTo(MainFire) < 75;
		if (atFire && feed && Wood > 0) { _inventory.Remove("wood", 1); Fuel = Mathf.Min(_balance.MaxFuel, Fuel + _balance.FuelFeedAmount); Message = "剑骸化入阵眼，剑冢灵蕴恢复。"; }
		if (atFire && upgrade && Wood >= FireLevel * _balance.FireUpgradeWoodPerLevel && FireLevel < _balance.MaxFireLevel)
		{
			_inventory.Remove("wood", FireLevel * _balance.FireUpgradeWoodPerLevel); FireLevel++; Fuel = _balance.MaxFuel;
			Message = FireLevel == _balance.MaxFireLevel ? "剑冢终阶开启，最终剑潮开始。" : "剑冢阵眼升阶。";
			if (FireLevel == _balance.MaxFireLevel) { FinalNight = _balance.FinalNightDuration; for (var i = 0; i < _balance.FinalNightInitialEnemyCount; i++) _enemySystem.Spawn(GroundY); }
		}
		if (NameFound && Player.DistanceTo(new Vector2(430, GroundY)) < 60)
		{
			if (burnName) { NameFound = false; Fuel = Mathf.Min(_balance.MaxFuel, Fuel + _balance.NameBurnFuelAmount); DreamAsh += _balance.NameBurnDreamAsh; Message = "剑铭被强炼：灵蕴暴涨，剑煞加深。"; }
			if (returnName) { NameFound = false; DreamAsh = MathF.Max(0, DreamAsh - _balance.NameReturnDreamAshReduction); Message = "有名之剑完成归葬，剑煞稍退。"; }
		}
		if (torch && Wood >= _balance.TorchWoodCost) PlaceFire(_balance.TorchLifetime, _balance.TorchRadius, _balance.TorchWoodCost, "剑标在锈雾中建立了神识锚点。");
		if (campfire && Wood >= _balance.CampfireWoodCost) PlaceFire(_balance.CampfireLifetime, _balance.CampfireRadius, _balance.CampfireWoodCost, "临时剑阵暂时压制了附近剑煞。");
	}

	private void UpdateSearch(bool pressed)
	{
		var chest = Chests.Find(candidate => Player.DistanceTo(candidate.Position) < _balance.SearchRange);
		var corpse = _enemySystem.FindCorpseNear(Player, _balance.SearchRange);
		object target = chest;
		if (corpse != null && (chest == null || Player.DistanceTo(corpse.Position) < Player.DistanceTo(chest.Position))) target = corpse;
		if (target == null)
		{
			SearchPrompt = "";
			if (pressed) Message = "附近没有可搜刮的宝箱或尸体。";
			return;
		}
		var targetName = target is ChestState ? "宝箱" : "剑煞残骸";
		var searchKey = _commands.SearchLabel;
		var alreadySearched = target is ChestState searchedChest ? searchedChest.Searched : ((EnemySystem.EnemyState)target).Searched;
		SearchPrompt = alreadySearched ? $"按 {searchKey} 查看{targetName}" : $"按 {searchKey} 打开并搜索{targetName}";
		if (!pressed) return;
		var contents = target is ChestState chestTarget ? chestTarget.Loot : ((EnemySystem.EnemyState)target).Loot;
		if (alreadySearched)
		{
			_lootSystem.Reopen(contents, targetName, _balance.SearchDuration);
			Message = contents.Exists(stack => stack != null) ? $"重新打开{targetName}。" : $"{targetName}已经搜空。";
		}
		else if (target is ChestState foundChest)
		{
			foundChest.Loot = _lootSystem.CreateChestLoot(_balance, _random);
			foundChest.Searched = true;
			_lootSystem.Open(foundChest.Loot, targetName, _balance.SearchDuration);
			Message = "宝箱已打开，正在逐件搜索物资。";
		}
		else if (target is EnemySystem.EnemyState foundCorpse)
		{
			var amount = _random.Next(_balance.CorpseWoodMin, _balance.CorpseWoodMaxExclusive);
			foundCorpse.Loot = _lootSystem.CreateCorpseLoot(amount);
			foundCorpse.Searched = true;
			_lootSystem.Open(foundCorpse.Loot, targetName, _balance.SearchDuration);
			Message = "残骸已打开，正在逐件搜索物资。";
		}
		InventoryOpen = true;
		SearchPrompt = "";
	}


	private void Reset()
	{
		Fuel = _balance.StartingFuel; Ember = _balance.StartingEmber; Health = _balance.StartingHealth; _inventory.Reset(); FireLevel = _balance.StartingFireLevel; DreamAsh = 0; FinalNight = 0; NameFound = true;
		_weaponSystem.ResetEquipment();
		_lootSystem.Close(); InventoryOpen = false;
		Player = MainFire; _enemySystem.Reset(); Fires.Clear();
		_dashRemaining = 0; _dashCooldown = 0; IsDashing = false;
		PlayerMoving = false;
		SwordState = SwordMode.Following; SwordPosition = SwordFollowPosition(); CameraFocus = Player; _swordControlBlend = 0;
		_weaponSystem.UpdateFollowers(Player, PlayerFacing, 1);
		ReplaySwordPosition = ReplaySwordFollowPosition(); ReplaySwordVelocity = Vector2.Zero;
		_recordingSamples.Clear(); _recordedSword.Clear(); _playingSword = Array.Empty<SwordSample>(); _replaySequence = Array.Empty<SwordSample[]>();
		_swordCombo.RemoveAll(id => id == CustomSwordMoveId); ComboOpen = false;
		_replayActive = false; _replayReturning = false; _replayTime = 0; _replayHitCooldown = 0;
		Chests.Clear();
		SpawnChestNearPlayer();
		SpawnChestNearPlayer();
		_chestSpawnTimer = _balance.ChestInitialSpawnInterval;
		SearchPrompt = "";
		Message = "神识在剑冢苏醒，新的远征开始了。";
	}

}
