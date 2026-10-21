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
		public bool Searched;
		public List<Inventory.Stack> Loot = new();
		public bool IsDying { get; private set; }
		public float DeathAnimationDuration;
		public EnemyVisualState VisualState => IsDying ? DeathAge < DeathAnimationDuration ? EnemyVisualState.Hurt : EnemyVisualState.Dead : HurtTime > 0 ? EnemyVisualState.Hurt : EnemyVisualState.Normal;

		public EnemyState(EnemyKind kind, float x, float groundY, float health, float deathAnimationDuration, float unclaimedCorpseLifetime)
		{
			Kind = kind;
			Position = new Vector2(x, groundY);
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

	public void AdvanceSpawning(float dt, int fireLevel, float dreamAsh, float groundY)
	{
		_spawnTimer -= dt;
		if (_spawnTimer > 0) return;
		Spawn(groundY);
		_spawnTimer = MathF.Max(_balance.EnemySpawnMinInterval,
			_balance.EnemySpawnBaseInterval - fireLevel * _balance.EnemySpawnFireLevelFactor - dreamAsh * _balance.EnemySpawnDreamAshFactor);
	}

	public void Spawn(float groundY)
	{
		var side = _random.Next(2) == 0 ? -1 : 1;
		_enemies.Add(new EnemyState((EnemyKind)_random.Next(0, 3), side * _random.Next(650, 1050), groundY,
			_balance.EnemyHealth, _balance.EnemyDeathAnimationDuration, _balance.EnemyUnclaimedCorpseLifetime));
	}

	public void Tick(float dt, Vector2 player, Vector2 mainFire, float groundY, Inventory inventory)
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
			switch (enemy.Kind)
			{
				case EnemyKind.Hunter:
					if (HunterBehavior.Tick(enemy, dt, player, _balance)) EmitSignal(SignalName.PlayerHit);
					break;
				case EnemyKind.Extinguisher:
					if (ExtinguisherBehavior.Tick(enemy, dt, player, mainFire, _balance))
						EmitSignal(SignalName.FireDamaged, _balance.ExtinguisherFuelDamage);
					break;
				case EnemyKind.Thief:
					if (ThiefBehavior.Tick(enemy, dt, player, groundY, inventory, _balance)) EmitSignal(SignalName.WoodStolen);
					break;
			}
		}
		_enemies.RemoveAll(enemy => enemy.IsDying ? enemy.DeathTime <= 0 : MathF.Abs(enemy.Position.X) > _balance.EnemyDespawnDistance);
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
