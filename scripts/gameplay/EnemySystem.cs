using Godot;
using System;
using System.Collections.Generic;

public partial class EnemySystem : Node
{
	[Signal] public delegate void FireDamagedEventHandler(float damage);
	[Signal] public delegate void WoodStolenEventHandler();
	[Signal] public delegate void PlayerHitEventHandler();

	public enum EnemyKind { Hunter, Extinguisher, Thief }
	public enum EnemyVisualState { Normal, Hurt, Dead }

	public sealed class EnemyState
	{
		public EnemyKind Kind;
		public Vector2 Position;
		public float AttackDelay;
		public float Health;
		public float HurtTime;
		public float DamageFeedbackTime;
		public float LastDamage;
		public float DeathTime = -1;
		public float DeathAge;
		public bool StoleWood;
		public float WanderCenter;
		public float WanderTime;
		public int WanderDirection;
		public bool WasActive;
		public bool Searched;
		public List<Inventory.Stack> Loot = new();
		public bool IsDying { get; private set; }
		public float DeathAnimationDuration;
		public EnemyVisualState VisualState => IsDying ? DeathAge < DeathAnimationDuration ? EnemyVisualState.Hurt : EnemyVisualState.Dead : HurtTime > 0 ? EnemyVisualState.Hurt : EnemyVisualState.Normal;

		public EnemyState(EnemyKind kind, float x, float groundY, float health, float deathAnimationDuration, float unclaimedCorpseLifetime)
		{
			Kind = kind;
			Position = new Vector2(x, groundY);
			WanderCenter = x;
			Health = health;
			DeathAnimationDuration = deathAnimationDuration;
			DeathTime = unclaimedCorpseLifetime;
		}

		public bool TakeDamage(float damage, float damageFeedbackDuration, float hurtDuration, float unclaimedCorpseLifetime)
		{
			Health -= damage;
			LastDamage = damage;
			DamageFeedbackTime = damageFeedbackDuration;
			if (Health <= 0) { BeginDeath(unclaimedCorpseLifetime); return true; }
			HurtTime = hurtDuration;
			return false;
		}

		private void BeginDeath(float unclaimedCorpseLifetime) { IsDying = true; DeathTime = unclaimedCorpseLifetime; DeathAge = 0; }
	}

	private readonly List<EnemyState> _enemies = new();
	private BalanceValues _balance;
	private Random _random;
	private float _spawnTimer;
	public IReadOnlyList<EnemyState> Enemies => _enemies;

	public void Initialize(BalanceValues balance, Random random)
	{
		_balance = balance;
		_random = random;
		Reset();
	}

	public void Reset()
	{
		_enemies.Clear();
		_spawnTimer = _balance.EnemyInitialSpawnDelay;
	}

	public void AdvanceSpawning(float dt, int fireLevel, float dreamAsh, float groundY, float playerX, bool isNight)
	{
		_spawnTimer -= dt;
		if (_spawnTimer > 0) return;
		Spawn(groundY, playerX, isNight);
		_spawnTimer = MathF.Max(_balance.EnemySpawnMinInterval,
			_balance.EnemySpawnBaseInterval - fireLevel * _balance.EnemySpawnFireLevelFactor - dreamAsh * _balance.EnemySpawnDreamAshFactor);
	}

	public void Spawn(float groundY, float playerX, bool isNight)
	{
		var side = _random.Next(2) == 0 ? -1 : 1;
		var anchor = isNight ? 0 : playerX;
		var x = anchor + side * _random.Next(650, 1050);
		if (MathF.Abs(x) <= _balance.VillageRadius) x = side * (_balance.VillageRadius + 100);
		x = Mathf.Clamp(x, -_balance.WorldHalfWidth + 40, _balance.WorldHalfWidth - 40);
		if (MathF.Abs(x) <= _balance.VillageRadius) return;
		_enemies.Add(new EnemyState((EnemyKind)_random.Next(0, 3), x, groundY,
			_balance.EnemyHealth, _balance.EnemyDeathAnimationDuration, _balance.EnemyUnclaimedCorpseLifetime));
	}

	public void Tick(float dt, Vector2 player, Vector2 mainFire, float groundY, Inventory inventory, bool isNight)
	{
		foreach (var enemy in _enemies)
		{
			enemy.DamageFeedbackTime = MathF.Max(0, enemy.DamageFeedbackTime - dt);
			if (enemy.IsDying)
			{
				enemy.DeathAge += dt;
				if (enemy.Searched && enemy.Loot.Find(stack => stack != null) == null)
					enemy.DeathTime = MathF.Min(enemy.DeathTime, _balance.EnemyCorpseLifetime);
				enemy.DeathTime -= dt;
				continue;
			}
			enemy.HurtTime = MathF.Max(0, enemy.HurtTime - dt);
			enemy.AttackDelay = MathF.Max(0, enemy.AttackDelay - dt);
			var nearPlayer = enemy.Position.DistanceTo(player) <= _balance.EnemyAggroRange;
			var active = nearPlayer || enemy.StoleWood || isNight && enemy.Kind == EnemyKind.Extinguisher;
			if (!active)
			{
				if (enemy.WasActive) { enemy.WanderCenter = enemy.Position.X; enemy.WanderTime = 0; }
				enemy.WasActive = false;
				Wander(enemy, dt);
				continue;
			}
			enemy.WasActive = true;
			switch (enemy.Kind)
			{
				case EnemyKind.Hunter:
					if (HunterBehavior.Tick(enemy, dt, player, _balance)) EmitSignal(SignalName.PlayerHit);
					break;
				case EnemyKind.Extinguisher:
					if (ExtinguisherBehavior.Tick(enemy, dt, player, mainFire, _balance, isNight))
						EmitSignal(SignalName.FireDamaged, _balance.ExtinguisherFuelDamage);
					break;
				case EnemyKind.Thief:
					if (ThiefBehavior.Tick(enemy, dt, player, groundY, inventory, _balance)) EmitSignal(SignalName.WoodStolen);
					break;
			}
		}
		_enemies.RemoveAll(enemy => enemy.IsDying ? enemy.DeathTime <= 0 : MathF.Abs(enemy.Position.X) > _balance.WorldHalfWidth);
	}

	private void Wander(EnemyState enemy, float dt)
	{
		var side = enemy.Position.X < 0 ? -1 : 1;
		if (MathF.Abs(enemy.Position.X) < _balance.VillageRadius)
		{
			enemy.Position += new Vector2(side * _balance.EnemyWanderSpeed * dt, 0);
			return;
		}
		if ((enemy.WanderTime -= dt) <= 0)
		{
			enemy.WanderTime = _random.Next(1, 4);
			enemy.WanderDirection = _random.Next(-1, 2);
		}
		var minX = MathF.Max(-_balance.WorldHalfWidth, enemy.WanderCenter - _balance.EnemyWanderRadius);
		var maxX = MathF.Min(_balance.WorldHalfWidth, enemy.WanderCenter + _balance.EnemyWanderRadius);
		if (side < 0) maxX = MathF.Min(maxX, -_balance.VillageRadius);
		else minX = MathF.Max(minX, _balance.VillageRadius);
		if (minX > maxX) { enemy.WanderCenter = enemy.Position.X; return; }
		var nextX = Mathf.Clamp(enemy.Position.X + enemy.WanderDirection * _balance.EnemyWanderSpeed * dt, minX, maxX);
		if (nextX == minX || nextX == maxX) enemy.WanderTime = 0;
		enemy.Position = new Vector2(nextX, enemy.Position.Y);
	}

	public EnemyState FindLivingNear(Vector2 position, float radius) =>
		_enemies.Find(enemy => !enemy.IsDying && enemy.Position.DistanceTo(position) < radius);

	public EnemyState FindLivingAlong(Vector2 from, Vector2 to, float radius) =>
		_enemies.Find(enemy => !enemy.IsDying &&
			enemy.Position.DistanceTo(Geometry2D.GetClosestPointToSegment(enemy.Position, from, to)) < radius);

	public EnemyState FindCorpseNear(Vector2 position, float radius) =>
		_enemies.Find(enemy => enemy.IsDying && position.DistanceTo(enemy.Position) < radius);

	public bool Damage(EnemyState enemy, float amount) =>
		enemy.TakeDamage(amount, _balance.EnemyDamageFeedbackDuration, _balance.EnemyHurtDuration, _balance.EnemyUnclaimedCorpseLifetime);

	public void MoveToGround(Vector2 offset)
	{
		foreach (var enemy in _enemies) enemy.Position += offset;
	}
}
