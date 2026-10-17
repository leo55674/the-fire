using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	private void HandleInteractions(float dt, bool moved)
	{
		var searchPressed = _input.Pressed("search");
		var feed = _input.Pressed("feed"); var upgrade = _input.Pressed("upgrade");
		var burnName = _input.Pressed("burn_name"); var returnName = _input.Pressed("return_name");
		var torch = _input.Pressed("torch"); var campfire = _input.Pressed("campfire");
		var switchSword = _input.Pressed("switch_sword");
		if (Fuel <= 0) return;
		if (switchSword && SwordState == SwordMode.Following)
		{
			SwordFrame = SwordFrame == SwordArchetype.Light ? SwordArchetype.Heavy : SwordArchetype.Light;
			EmitSignal(SignalName.SoundCueRequested, "sword_switch");
			Message = SwordFrame == SwordArchetype.Light
				? $"切换轻剑胚：转向灵敏，伤害 {_balance.LightSwordDamage:0}。"
				: $"切换重剑胚：惯性明显，伤害 {_balance.HeavySwordDamage:0}。";
		}
		UpdateSearch(searchPressed, moved, dt);
		var atFire = Player.DistanceTo(MainFire) < 75;
		if (atFire && feed && Wood > 0) { _inventory.Remove("wood", 1); Fuel = Mathf.Min(_balance.MaxFuel, Fuel + _balance.FuelFeedAmount); Message = "剑骸化入阵眼，剑冢灵蕴恢复。"; }
		if (atFire && upgrade && Wood >= FireLevel * _balance.FireUpgradeWoodPerLevel && FireLevel < _balance.MaxFireLevel)
		{
			_inventory.Remove("wood", FireLevel * _balance.FireUpgradeWoodPerLevel); FireLevel++; Fuel = _balance.MaxFuel;
			Message = FireLevel == _balance.MaxFireLevel ? "剑冢终阶开启，最终剑潮开始。" : "剑冢阵眼升阶。";
			if (FireLevel == _balance.MaxFireLevel) { FinalNight = _balance.FinalNightDuration; for (var i = 0; i < _balance.FinalNightInitialEnemyCount; i++) SpawnEnemy(); }
		}
		if (NameFound && Player.DistanceTo(new Vector2(430, GroundY)) < 60)
		{
			if (burnName) { NameFound = false; Fuel = Mathf.Min(_balance.MaxFuel, Fuel + _balance.NameBurnFuelAmount); DreamAsh += _balance.NameBurnDreamAsh; Message = "剑铭被强炼：灵蕴暴涨，剑煞加深。"; }
			if (returnName) { NameFound = false; DreamAsh = MathF.Max(0, DreamAsh - _balance.NameReturnDreamAshReduction); Message = "有名之剑完成归葬，剑煞稍退。"; }
		}
		if (torch && Wood >= _balance.TorchWoodCost) PlaceFire(_balance.TorchLifetime, _balance.TorchRadius, _balance.TorchWoodCost, "剑标在锈雾中建立了神识锚点。");
		if (campfire && Wood >= _balance.CampfireWoodCost) PlaceFire(_balance.CampfireLifetime, _balance.CampfireRadius, _balance.CampfireWoodCost, "临时剑阵暂时压制了附近剑煞。");
	}

	private void UpdateSearch(bool pressed, bool moved, float dt)
	{
		var chest = Chests.Find(candidate => Player.DistanceTo(candidate.Position) < _balance.SearchRange);
		var corpse = Enemies.Find(candidate => candidate.IsDying && Player.DistanceTo(candidate.Position) < _balance.SearchRange);
		object target = chest;
		if (corpse != null && (chest == null || Player.DistanceTo(corpse.Position) < Player.DistanceTo(chest.Position))) target = corpse;
		if (target == null)
		{
			_searchTarget = null;
			_searchProgress = 0;
			_searching = false;
			SearchPrompt = "";
			if (pressed) Message = "附近没有可搜刮的宝箱或尸体。";
			return;
		}
		if (!ReferenceEquals(_searchTarget, target))
		{
			_searchProgress = 0;
			_searching = false;
		}
		_searchTarget = target;
		var targetName = target is ChestState ? "宝箱" : "剑煞残骸";
		var searchKey = _input.LabelFor("search");
		var alreadySearched = target is ChestState searchedChest ? searchedChest.Searched : ((EnemyState)target).Searched;
		if (alreadySearched)
		{
			_searching = false;
			_searchProgress = 0;
			SearchPrompt = $"按 {searchKey} 再次查看{targetName}";
			if (pressed)
			{
				var contents = target is ChestState chestTarget ? chestTarget.Loot : ((EnemyState)target).Loot;
				OpenLoot(contents);
				Message = contents.Exists(stack => stack != null) ? $"重新打开{targetName}。" : $"{targetName}已经搜空。";
			}
			return;
		}
		if (moved && _searching)
		{
			_searching = false;
			_searchProgress = 0;
			Message = "移动打断了搜刮。";
		}
		if (pressed && !moved) _searching = true;
		SearchPrompt = _searching ? $"正在搜刮{targetName}（移动将中断）" : $"按一下 {searchKey} 搜刮{targetName}";
		if (!_searching) return;
		_searchProgress += dt;
		if (_searchProgress < _balance.SearchDuration) return;
		if (target is ChestState foundChest)
		{
			var amount = _random.Next(_balance.ChestWoodMin, _balance.ChestWoodMaxExclusive);
			Inventory.AddTo(foundChest.Loot, "wood", amount);
			var bonus = _random.Next(_balance.ChestBonusRollCount);
			if (bonus == 0) Inventory.AddTo(foundChest.Loot, "healing_potion", 1);
			var foundHeavySword = bonus == 1 && _inventory.Count("heavy_sword") == 0;
			if (foundHeavySword) Inventory.AddTo(foundChest.Loot, "heavy_sword", 1);
			foundChest.Searched = true;
			Message = bonus == 0 ? $"搜刮宝箱，获得 {amount} 块剑骸和一枚疗伤丹。"
				: foundHeavySword ? $"搜刮宝箱，获得 {amount} 块剑骸和重剑胚。"
				: $"搜刮宝箱，获得 {amount} 块剑骸。";
		}
		else if (target is EnemyState foundCorpse)
		{
			var amount = _random.Next(_balance.CorpseWoodMin, _balance.CorpseWoodMaxExclusive);
			Inventory.AddTo(foundCorpse.Loot, "wood", amount);
			foundCorpse.Searched = true;
			Message = $"搜刮剑煞残骸，获得 {amount} 块剑骸。";
		}
		_searchTarget = null;
		_searchProgress = 0;
		_searching = false;
		SearchPrompt = "";
		OpenLoot(target is ChestState chestContents ? chestContents.Loot : ((EnemyState)target).Loot);
	}

	private void OpenLoot(List<Inventory.Stack> contents)
	{
		_lootItems = contents;
		LootOpen = true;
		InventoryOpen = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}


	private void Reset()
	{
		Fuel = _balance.StartingFuel; Ember = _balance.StartingEmber; Health = _balance.StartingHealth; _inventory.Reset(); FireLevel = _balance.StartingFireLevel; DreamAsh = 0; FinalNight = 0; NameFound = true;
		_lootItems = new List<Inventory.Stack>(); LootOpen = false; InventoryOpen = false;
		Player = MainFire; Enemies.Clear(); Fires.Clear();
		_dashRemaining = 0; _dashCooldown = 0; IsDashing = false;
		SwordState = SwordMode.Following; SwordPosition = SwordFollowPosition(); CameraFocus = Player; _swordControlBlend = 0;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Chests.Clear();
		SpawnChestNearPlayer();
		SpawnChestNearPlayer();
		_spawnTimer = _balance.EnemyInitialSpawnDelay;
		_chestSpawnTimer = _balance.ChestInitialSpawnInterval;
		_searchTarget = null;
		_searchProgress = 0;
		_searching = false;
		SearchPrompt = "";
		Message = "神识在剑冢苏醒，新的远征开始了。";
	}

}
