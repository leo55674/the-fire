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

	public enum SwordMode { Following, Controlled, Attacking, Returning }
	public enum SwordArchetype { Light, Heavy }
	private readonly record struct SwordSample(float Time, Vector2 Offset);
}
