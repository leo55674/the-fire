using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	[Signal] public delegate void SoundCueRequestedEventHandler(string cue);

	[Export] private Inventory _inventory;
	[Export] private EnemySystem _enemySystem;
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
	private float _searchProgress;
	private object _searchTarget;
	private bool _searching;
	private List<Inventory.Stack> _lootItems = new();
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
	public Vector2 SwordPosition { get; private set; }
	public Vector2 SwordVelocity { get; private set; }
	public Vector2 ReplaySwordPosition { get; private set; }
	public Vector2 ReplaySwordVelocity { get; private set; }
	public Vector2 CameraFocus { get; private set; }
	public SwordMode SwordState { get; private set; } = SwordMode.Following;
	public SwordArchetype SwordFrame { get; private set; } = SwordArchetype.Light;
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
	public float SwordDamage => SwordFrame == SwordArchetype.Light ? _balance.LightSwordDamage : _balance.HeavySwordDamage;
	public BalanceValues Balance => _balance;
	public int PlayerFacing { get; private set; } = 1;
	public bool PlayerMoving { get; private set; }
	public int Wood => _inventory?.Count("wood") ?? 0;
	public Inventory Bag => _inventory;
	public IReadOnlyList<Inventory.Stack> LootItems => _lootItems;
	public bool LootOpen { get; private set; }
	public float SearchProgress => Mathf.Clamp(_searchProgress / _balance.SearchDuration, 0, 1);
	public bool IsSearching => _searching;
	public string SearchPrompt { get; private set; } = "";
	public int FireLevel { get; private set; } = 1;
	public bool NameFound { get; private set; } = true;
	public bool InventoryOpen { get; private set; }
	public string Message { get; private set; } = "守住剑冢，右键切换御剑，Shift 突进闪避。";
	public Vector2 MainFire => new(0, GroundY);

	public override void _Ready()
	{
		if (_enemySystem == null) throw new InvalidOperationException("GameSession requires an EnemySystem child.");
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
		_chestSpawnTimer = _balance.ChestInitialSpawnInterval;
		Player = MainFire;
		PlayerMoving = false;
		SwordPosition = SwordFollowPosition();
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
		for (var i = -10; i <= 10; i++)
			if (i != 0) Trees.Add(new TreeState(i * 155 + _random.Next(-35, 36), GroundY));
		Chests.Clear();
		_lootItems = new List<Inventory.Stack>();
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
			if (_commands.Pressed("switch_sword"))
			{
				if (_inventory.Count("light_sword") + _inventory.Count("heavy_sword") == 2)
				{
					SwordFrame = SwordFrame == SwordArchetype.Light ? SwordArchetype.Heavy : SwordArchetype.Light;
					Message = SwordFrame == SwordArchetype.Light ? "已装备轻剑胚。" : "已装备重剑胚。";
				}
				else Message = "尚未获得另一种剑胚。";
			}
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
		var moved = Player.DistanceTo(previousPlayer) > .1f;
		PlayerMoving = MathF.Abs(Player.X - previousPlayer.X) > .1f || IsDashing;
		UpdateSword(dt);
		UpdateFire(dt);
		_enemySystem.Tick(dt, Player, MainFire, GroundY, _inventory);
		UpdateTemporaryFires(dt);
		HandleInteractions(dt, moved);
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
			LootOpen = false;
			_lootItems = new List<Inventory.Stack>();
			InventoryOpen = false;
		}
		else InventoryOpen = !InventoryOpen;
	}

	public bool TransferLoot(int from, int to, int amount = int.MaxValue) => _inventory.TransferFrom(_lootItems, from, to, amount) > 0;

	public void TakeAllLoot()
	{
		for (var i = 0; i < _lootItems.Count; i++)
		{
			while (_lootItems[i] != null)
			{
				var target = _inventory.FirstTransferSlot(_lootItems[i]);
				if (target < 0 || _inventory.TransferFrom(_lootItems, i, target) == 0) break;
			}
		}
	}

	public void Start() => Message = "守住剑冢，右键切换御剑，Shift 突进闪避。";

	public void MoveToGround(float groundY)
	{
		var offset = new Vector2(0, groundY - GroundY);
		GroundY = groundY;
		Player += offset;
		SwordPosition += offset;
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
