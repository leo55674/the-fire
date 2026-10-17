using Godot;
using System.Collections.Generic;

public partial class GameSession : Node
{
	public sealed class TreeState
	{
		public Vector2 Position;
		public TreeState(float x, float groundY) => Position = new Vector2(x, groundY);
	}

	public sealed class ChestState
	{
		public Vector2 Position;
		public bool Searched;
		public List<Inventory.Stack> Loot = new();
		public ChestState(Vector2 position) => Position = position;
	}

	public sealed class FireState
	{
		public Vector2 Position;
		public float Life;
		public float Radius;
		public FireState(Vector2 position, float life, float radius) => (Position, Life, Radius) = (position, life, radius);
	}

	public enum EnemyKind { Hunter, Extinguisher, Thief }
	public enum SwordMode { Following, Controlled, Attacking, Returning }
	public enum SwordArchetype { Light, Heavy }
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
		public void BeginDeath(float unclaimedCorpseLifetime) { IsDying = true; DeathTime = unclaimedCorpseLifetime; DeathAge = 0; }
	}
}