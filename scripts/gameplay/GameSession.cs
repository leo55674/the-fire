using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	[Signal] public delegate void SoundCueRequestedEventHandler(string cue);

	[Export] private Inventory _inventory;
	[Export] private WeaponSystem _weaponSystem;
	[Export] private EnemySystem _enemySystem;
	[Export] private LootSystem _lootSystem;
	[Export] private BlacksmithPresentation _blacksmith;
	[Export] private Resource _balanceResource;
	private BalanceValues _balance;
	private GameCommands _commands;
	private readonly Random _random = new();
	private float _verticalVelocity;
	private float _attackCooldown;
	private float _swordHitCooldown;
	private float _swordControlBlend;
	private float _dashRemaining;
	private float _dashCooldown;
	private float _chestSpawnTimer;
	private Vector2 _swordControlTarget;
	private Vector2 _swordAttackTarget;
	private bool _swordAttackHit;
	private readonly List<SwordSample> _recordingSamples = new();
	private readonly List<SwordSample> _recordedSword = new();
	private SwordSample[] _playingSword = Array.Empty<SwordSample>();
	private SwordSample[][] _replaySequence = Array.Empty<SwordSample[]>();
	private int _replayMoveIndex;
	private readonly List<int> _swordCombo = new() { 2, 0, 4 };
	private Vector2 _recordingOrigin;
	private int _recordingFacing;
	private float _recordingTime;
	private float _replayTime;
	private int _replaySegment;
	private int _replayFacing;
	private Vector2 _replayOrigin;
	private float _replayDamage;
	private float _replayHitCooldown;
	private bool _replayActive;
	private bool _replayReturning;

	public readonly List<TreeState> Trees = new();
	public IReadOnlyList<EnemySystem.EnemyState> Enemies => _enemySystem.Enemies;
	public readonly List<ChestState> Chests = new();
	public readonly List<FireState> Fires = new();
	public Vector2 Player { get; private set; }
	public float GroundY { get; private set; }
	public float Health { get; private set; }
	public float Fuel { get; private set; }
	public float Ember { get; private set; }
	public float DreamAsh { get; private set; }
	public float SwingTime { get; private set; }
	public float HurtTime { get; private set; }
	public float FinalNight { get; private set; }
	public bool IsNight { get; private set; }
	public float PhaseRemaining { get; private set; }
	public float WorldHalfWidth => _balance.WorldHalfWidth;
	public float VillageRadius => _balance.VillageRadius;
	public Vector2 SwordPosition { get; private set; }
	public Vector2 SwordVelocity { get; private set; }
	public Vector2 ReplaySwordPosition { get; private set; }
	public Vector2 ReplaySwordVelocity { get; private set; }
	public Vector2 CameraFocus { get; private set; }
	public SwordMode SwordState { get; private set; } = SwordMode.Following;
	public bool IsSwordControlled => SwordState == SwordMode.Controlled;
	public bool IsDashing { get; private set; }
	public bool IsSwordInFlight => SwordState != SwordMode.Following;
	public float SwordControlBlend => _swordControlBlend;
	public float SwordDistance => SwordPosition.DistanceTo(Player);
	public float SwordFollowDistance => SwordPosition.DistanceTo(SwordFollowPosition());
	public bool HasRecordedSword => _recordedSword.Count > 1;
	public bool IsReplaySwordActive => _replayActive || _replayReturning;
	public IReadOnlyList<int> SwordCombo => _swordCombo;
	public bool ComboOpen { get; private set; }
	public bool WantsMouseCapture => IsSwordControlled && !ComboOpen && !InventoryOpen;
	public float RecordedSwordDuration => HasRecordedSword ? _recordedSword[^1].Time : 0;
	public float SwordRecordingDuration => IsSwordControlled ? MathF.Min(_recordingTime, _balance.SwordRecordMaxDuration) : 0;
	public float SwordDamage => _balance.SwordDamage;
	public int SwordCount => _weaponSystem.EquippedCount;
	public string SwordRoster => _weaponSystem.EquippedNames;
	public int ActiveSwordIndex => _weaponSystem.ActiveSwordIndex;
	public string ActiveSwordName => _weaponSystem.ActiveSwordName;
	public int CompanionSwordIndex(int slot) => _weaponSystem.CompanionSwordIndex(slot);
	public IReadOnlyList<Vector2> CompanionSwordPositions => _weaponSystem.CompanionSwordPositions;
	public Vector2 BlacksmithPosition => _blacksmith.WorldPosition(GroundY);
	public int SwordIron => _inventory?.Count("sword_iron") ?? 0;
	public BalanceValues Balance => _balance;
	public int PlayerFacing { get; private set; } = 1;
	public bool PlayerMoving { get; private set; }
	public int Wood => _inventory?.Count("wood") ?? 0;
	public Inventory Bag => _inventory;
	public IReadOnlyList<Inventory.Stack> LootItems => _lootSystem?.Items ?? Array.Empty<Inventory.Stack>();
	public LootSystem LootSystem => _lootSystem;
	public bool LootOpen => _lootSystem?.IsOpen ?? false;
	public float SearchProgress => _lootSystem?.Progress ?? 0;
	public bool IsSearching => _lootSystem?.IsSearching ?? false;
	public string SearchPrompt { get; private set; } = "";
	public int FireLevel { get; private set; } = 1;
	public bool NameFound { get; private set; } = true;
	public bool InventoryOpen { get; private set; }
	public string Message { get; private set; } = "守住剑冢，右键切换御剑，Shift 突进闪避。";
	public Vector2 MainFire => new(0, GroundY);

	public override void _Ready()
	{
		if (_enemySystem == null || _weaponSystem == null || _lootSystem == null || _blacksmith == null) throw new InvalidOperationException("GameSession requires EnemySystem, WeaponSystem, LootSystem and Blacksmith references.");
		_enemySystem.FireDamaged += damage => { Fuel = MathF.Max(0, Fuel - damage); Message = "啮阵兽正在侵蚀剑冢！"; };
		_enemySystem.WoodStolen += () => Message = "盗铭伥偷走了一块剑骸。";
		_enemySystem.PlayerHit += DamagePlayer;
	}

	public void Initialize(float groundY)
	{
		if (_balanceResource == null) throw new InvalidOperationException("GameSession requires a BalanceConfig resource.");
		_balance = new BalanceValues(_balanceResource);
		GroundY = groundY;
		Health = _balance.StartingHealth;
		Fuel = _balance.StartingFuel;
		Ember = _balance.StartingEmber;
		FireLevel = _balance.StartingFireLevel;
		_enemySystem.Initialize(_balance, _random);
		IsNight = false;
		PhaseRemaining = _balance.DayDuration;
		_chestSpawnTimer = _balance.ChestInitialSpawnInterval;
		Player = MainFire;
		PlayerMoving = false;
		_weaponSystem.ResetEquipment(PlayerFacing);
		SwordPosition = SwordFollowPosition();
		_weaponSystem.UpdateFollowers(Player, PlayerFacing, 1);
		ReplaySwordPosition = ReplaySwordFollowPosition();
		_recordingSamples.Clear();
		_recordedSword.Clear();
		_playingSword = Array.Empty<SwordSample>();
		_replaySequence = Array.Empty<SwordSample[]>();
		_replayMoveIndex = 0;
		ComboOpen = false;
		_replayActive = false;
		_replayReturning = false;
		CameraFocus = Player;
		Trees.Clear();
		for (var i = -(int)(_balance.WorldHalfWidth / 155); i <= (int)(_balance.WorldHalfWidth / 155); i++)
			if (i != 0 && MathF.Abs(i * 155) > _balance.VillageRadius) Trees.Add(new TreeState(i * 155 + _random.Next(-35, 36), GroundY));
		Chests.Clear();
		SpawnChestNearPlayer();
		SpawnChestNearPlayer();
	}

	public void Tick(float dt, GameCommands commands)
	{
		_commands = commands;
		PlayerMoving = false;
		if (_commands.Pressed("inventory")) ToggleInventory();
		if (InventoryOpen)
		{
			_lootSystem.Tick(dt);
			if (_commands.Pressed("feed"))
			{
				if (_inventory.Remove("healing_potion", 1))
				{
					var before = Health;
					Health = Mathf.Min(_balance.MaxHealth, Health + _balance.HealingPotionRestore);
					if (Health == before) _inventory.Add("healing_potion", 1);
					else Message = $"使用疗伤丹，恢复 {Health - before:0} 点血量。";
				}
				else Message = "没有疗伤丹了。";
			}
			return;
		}
		if (_commands.Pressed("combo_menu")) SetComboOpen(!ComboOpen);
		if (ComboOpen) return;
		var previousPlayer = Player;
		UpdatePlayer(dt);
		PlayerMoving = MathF.Abs(Player.X - previousPlayer.X) > .1f || IsDashing;
		_weaponSystem.UpdateFollowers(Player, PlayerFacing, dt);
		UpdateSword(dt);
		UpdateFire(dt);
		_enemySystem.Tick(dt, Player, MainFire, GroundY, _inventory, IsNight || FinalNight > 0);
		UpdateTemporaryFires(dt);
		HandleInteractions();
		HandleCombat();
		var cameraTarget = Player.Lerp(SwordPosition, Mathf.SmoothStep(0, 1, _swordControlBlend));
		CameraFocus = CameraFocus.Lerp(cameraTarget, 1 - MathF.Exp(-8 * dt));
	}

	public void CaptureSwordMouseMotion(Vector2 relative)
	{
		if (IsSwordControlled) _swordControlTarget += relative;
	}

	public void SetComboOpen(bool open)
	{
		ComboOpen = open;
	}

	public void ToggleInventory()
	{
		if (LootOpen)
		{
			_lootSystem.Close();
			InventoryOpen = false;
		}
		else InventoryOpen = !InventoryOpen;
	}

	public bool TransferLoot(int from, int to, int amount = int.MaxValue) => _lootSystem.TransferTo(_inventory, from, to, amount);

	public void Start() => Message = "守住剑冢，右键切换御剑，Shift 突进闪避。";

	public void MoveToGround(float groundY)
	{
		var offset = new Vector2(0, groundY - GroundY);
		GroundY = groundY;
		Player += offset;
		SwordPosition += offset;
		_weaponSystem.UpdateFollowers(Player, PlayerFacing, 1);
		ReplaySwordPosition += offset;
		_recordingOrigin += offset;
		_replayOrigin += offset;
		_swordControlTarget += offset;
		CameraFocus += offset;
		foreach (var tree in Trees) tree.Position += offset;
		_enemySystem.MoveToGround(offset);
		foreach (var fire in Fires) fire.Position += offset;
	}

	public bool IsVisible(Vector2 _) => true;

}
