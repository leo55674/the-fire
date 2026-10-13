using Godot;
using System;
using System.Collections.Generic;

public partial class GameSession : Node
{
	[Signal] public delegate void AttackPerformedEventHandler(string kind);
	[Signal] public delegate void PlayerDamagedEventHandler();

	[Export] private ActionBindings _input;
	private readonly Random _random = new();
	private float _verticalVelocity;
	private float _spawnTimer = 6;
	private float _attackCooldown;

	public readonly List<TreeState> Trees = new();
	public readonly List<EnemyState> Enemies = new();
	public readonly List<FireState> Fires = new();
	public readonly List<ProjectileState> Projectiles = new();
	public Vector2 Player { get; private set; }
	public float GroundY { get; private set; }
	public float Health { get; private set; } = 5;
	public float Fuel { get; private set; } = 70;
	public float Ember { get; private set; } = 100;
	public float DreamAsh { get; private set; }
	public float SwingTime { get; private set; }
	public float HurtTime { get; private set; }
	public float FinalNight { get; private set; }
	public int Wood { get; private set; }
	public int FireLevel { get; private set; } = 1;
	public bool NameFound { get; private set; } = true;
	public string Message { get; private set; } = "守住引魂火。";
	public Vector2 MainFire => new(0, GroundY);

	public void Initialize(float groundY)
	{
		GroundY = groundY;
		Player = MainFire;
		Trees.Clear();
		for (var i = -10; i <= 10; i++)
			if (i != 0) Trees.Add(new TreeState(i * 155 + _random.Next(-35, 36), GroundY));
	}

	public void Tick(float dt)
	{
		UpdatePlayer(dt);
		UpdateFire(dt);
		UpdateEnemies(dt);
		UpdateProjectiles(dt);
		UpdateTemporaryFires(dt);
		HandleInteractions();
		HandleCombat();
	}

	public void Start() => Message = "守住引魂火。";

	public void MoveToGround(float groundY)
	{
		var offset = new Vector2(0, groundY - GroundY);
		GroundY = groundY;
		Player += offset;
		foreach (var tree in Trees) tree.Position += offset;
		foreach (var enemy in Enemies) enemy.Position += offset;
		foreach (var fire in Fires) fire.Position += offset;
		foreach (var projectile in Projectiles) projectile.Position += offset;
	}

	public bool IsVisible(Vector2 world) => world.DistanceTo(Player) < 155 + Ember * 2.1f - DreamAsh * 10;

	private void UpdatePlayer(float dt)
	{
		Player += new Vector2(((_input.Held("right") ? 1 : 0) - (_input.Held("left") ? 1 : 0)) * 250 * dt, 0);
		_verticalVelocity += 900 * dt;
		if (_input.Held("jump") && Player.Y >= GroundY) _verticalVelocity = -440;
		Player = new Vector2(Player.X, MathF.Min(GroundY, Player.Y + _verticalVelocity * dt));
		if (Player.Y >= GroundY) _verticalVelocity = 0;
		var nearLight = Player.DistanceTo(MainFire) < 100 || Fires.Exists(fire => Player.DistanceTo(fire.Position) < fire.Radius);
		Ember = Mathf.Clamp(Ember + (nearLight ? 45 : -7) * dt, 0, 100);
		_attackCooldown = MathF.Max(0, _attackCooldown - dt);
		SwingTime = MathF.Max(0, SwingTime - dt);
		HurtTime = MathF.Max(0, HurtTime - dt);
	}

	private void UpdateFire(float dt)
	{
		Fuel = MathF.Max(0, Fuel - (0.42f + FireLevel * 0.08f) * dt);
		if (Fuel <= 0)
		{
			Message = "主火熄灭。按重燃循环开始新的远征。";
			if (_input.Pressed("restart")) Reset();
			return;
		}
		_spawnTimer -= dt;
		if (_spawnTimer <= 0) { SpawnEnemy(); _spawnTimer = MathF.Max(2.4f, 7.5f - FireLevel - DreamAsh * .12f); }
		if (FinalNight > 0 && (FinalNight -= dt) <= 0) Message = "烽火穿透了长夜。";
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
			if (enemy.IsDying) { enemy.DeathTime -= dt; continue; }
			enemy.AttackDelay = MathF.Max(0, enemy.AttackDelay - dt);
			var target = enemy.Kind == EnemyKind.Extinguisher && Player.DistanceTo(MainFire) > 190 ? MainFire : Player;
			if (enemy.Kind == EnemyKind.Thief && enemy.StoleWood) target = new Vector2(MathF.Sign(enemy.Position.X) * 1600, GroundY);
			enemy.Position += new Vector2(MathF.Sign(target.X - enemy.Position.X) * (enemy.Kind == EnemyKind.Hunter ? 105 : 75) * dt, 0);
			if (enemy.Kind == EnemyKind.Extinguisher && enemy.Position.DistanceTo(MainFire) < 38 && enemy.AttackDelay <= 0)
			{
				Fuel = MathF.Max(0, Fuel - 6); enemy.AttackDelay = 2.2f; Message = "灭火者正在攻击主火！";
			}
			if (enemy.Kind == EnemyKind.Thief && !enemy.StoleWood && enemy.Position.DistanceTo(Player) < 35 && enemy.AttackDelay <= 0)
			{
				if (Wood > 0) { Wood--; enemy.StoleWood = true; Message = "窃木者偷走了记忆木。"; }
				enemy.AttackDelay = 1.5f;
			}
			if (enemy.Kind == EnemyKind.Hunter && enemy.Position.DistanceTo(Player) < 34 && enemy.AttackDelay <= 0)
			{
				DamagePlayer(); enemy.AttackDelay = 1.3f;
			}
		}
		Enemies.RemoveAll(enemy => enemy.IsDying && enemy.DeathTime <= 0 || MathF.Abs(enemy.Position.X) > 1750);
	}

	private void UpdateProjectiles(float dt)
	{
		for (var i = Projectiles.Count - 1; i >= 0; i--)
		{
			var projectile = Projectiles[i];
			projectile.Position += projectile.Velocity * dt;
			projectile.Life -= dt;
			var hit = Enemies.Find(enemy => !enemy.IsDying && enemy.Position.DistanceTo(projectile.Position) < 25);
			if (hit != null) { hit.BeginDeath(); projectile.Life = 0; Message = "火种烧尽了一缕执念。"; }
			if (projectile.Life <= 0) Projectiles.RemoveAt(i);
		}
	}

	private void HandleInteractions()
	{
		var chop = _input.Pressed("chop"); var feed = _input.Pressed("feed"); var upgrade = _input.Pressed("upgrade");
		var burnName = _input.Pressed("burn_name"); var returnName = _input.Pressed("return_name");
		var torch = _input.Pressed("torch"); var campfire = _input.Pressed("campfire");
		if (Fuel <= 0) return;
		var tree = Trees.Find(candidate => !candidate.Cut && Player.DistanceTo(candidate.Position) < 58);
		if (tree != null && chop) { tree.Cut = true; Wood += 2; Message = "你砍下了记忆木。"; }
		var atFire = Player.DistanceTo(MainFire) < 75;
		if (atFire && feed && Wood > 0) { Wood--; Fuel = Mathf.Min(100, Fuel + 17); Message = "记忆木为主火添薪。"; }
		if (atFire && upgrade && Wood >= FireLevel * 6 && FireLevel < 3)
		{
			Wood -= FireLevel * 6; FireLevel++; Fuel = 100;
			Message = FireLevel == 3 ? "烽火点燃，长夜开始了。" : "引魂火更加旺盛。";
			if (FireLevel == 3) { FinalNight = 30; for (var i = 0; i < 6; i++) SpawnEnemy(); }
		}
		if (NameFound && Player.DistanceTo(new Vector2(430, GroundY)) < 60)
		{
			if (burnName) { NameFound = false; Fuel = Mathf.Min(100, Fuel + 42); DreamAsh += 2; Message = "名字在火中化为了梦灰。"; }
			if (returnName) { NameFound = false; DreamAsh = MathF.Max(0, DreamAsh - 1); Message = "被记住的亡魂离开了迷雾。"; }
		}
		if (torch && Wood > 0) PlaceFire(22, 95, 1, "火把照亮了归途。");
		if (campfire && Wood >= 3) PlaceFire(55, 170, 3, "野营火暂时阻挡了迷雾。");
	}

	private void HandleCombat()
	{
		var axe = _input.Pressed("axe"); var firebolt = _input.Pressed("firebolt");
		if (_attackCooldown > 0) return;
		var mouse = GetViewport().GetMousePosition();
		var aim = new Vector2(Player.X + mouse.X - GetViewport().GetVisibleRect().Size.X / 2, mouse.Y) - Player;
		var direction = aim.Normalized();
		if (direction == Vector2.Zero) direction = Vector2.Right;
		if (axe)
		{
			_attackCooldown = .25f; SwingTime = .12f;
			EmitSignal(SignalName.AttackPerformed, "axe");
			var hit = Enemies.Find(enemy => !enemy.IsDying && enemy.Position.DistanceTo(Player) < 86 && (enemy.Position - Player).Normalized().Dot(direction) > .15f);
			if (hit != null) { hit.BeginDeath(); Message = "斧刃斩断了一缕执念。"; }
		}
		else if (firebolt && Ember >= 15)
		{
			_attackCooldown = .32f; Ember -= 15;
			EmitSignal(SignalName.AttackPerformed, "firebolt");
			Projectiles.Add(new ProjectileState(Player + direction * 20, direction * 520)); Message = "你抛出了一枚火种。";
		}
	}

	private void PlaceFire(float life, float radius, int cost, string message)
	{
		if (Fires.Exists(fire => Player.DistanceTo(fire.Position) < 40)) return;
		Wood -= cost; Fires.Add(new FireState(Player, life, radius)); Message = message;
	}

	private void SpawnEnemy()
	{
		var side = _random.Next(2) == 0 ? -1 : 1;
		Enemies.Add(new EnemyState((EnemyKind)_random.Next(0, 3), side * _random.Next(650, 1050), GroundY));
	}

	private void DamagePlayer()
	{
		if (HurtTime > 0) return;
		Health--; HurtTime = 1; Message = "猎手撕开了你的余烬。";
		EmitSignal(SignalName.PlayerDamaged);
		if (Health > 0) return;
		Wood /= 2; Fuel = MathF.Max(0, Fuel - 15); Health = 5; Player = MainFire;
		Message = "你化作余烬回到了主火旁，遗失了部分木材。";
	}

	private void Reset()
	{
		Fuel = 70; Ember = 100; Health = 5; Wood = 0; FireLevel = 1; DreamAsh = 0; FinalNight = 0; NameFound = true;
		Player = MainFire; Enemies.Clear(); Fires.Clear(); Projectiles.Clear();
		foreach (var tree in Trees) tree.Cut = false;
		Message = "余烬重燃，新的循环开始了。";
	}

	public sealed class TreeState
	{
		public Vector2 Position;
		public bool Cut;
		public TreeState(float x, float groundY) => Position = new Vector2(x, groundY);
	}

	public sealed class FireState
	{
		public Vector2 Position;
		public float Life;
		public float Radius;
		public FireState(Vector2 position, float life, float radius) => (Position, Life, Radius) = (position, life, radius);
	}

	public enum EnemyKind { Hunter, Extinguisher, Thief }
	public enum EnemyVisualState { Normal, Hurt, Dead }

	public sealed class EnemyState
	{
		public EnemyKind Kind;
		public Vector2 Position;
		public float AttackDelay;
		public float DeathTime = -1;
		public bool StoleWood;
		public bool IsDying { get; private set; }
		public EnemyVisualState VisualState => !IsDying ? EnemyVisualState.Normal : DeathTime > .45f ? EnemyVisualState.Hurt : EnemyVisualState.Dead;
		public EnemyState(EnemyKind kind, float x, float groundY) => (Kind, Position) = (kind, new Vector2(x, groundY));
		public void BeginDeath() { IsDying = true; DeathTime = .7f; }
	}

	public sealed class ProjectileState
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Life = 1.1f;
		public ProjectileState(Vector2 position, Vector2 velocity) => (Position, Velocity) = (position, velocity);
	}
}
