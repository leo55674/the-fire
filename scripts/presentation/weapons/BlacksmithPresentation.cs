using Godot;

public partial class BlacksmithPresentation : Node2D
{
	[Export] private float _worldX = -260;
	[Export] private Vector2 _screenOffset = new(0, -100);

	public Vector2 WorldPosition(float groundY) => new(_worldX, groundY);
	public bool IsInRange(Vector2 player, float groundY, float range) => player.DistanceTo(WorldPosition(groundY)) < range;
	public void Present(Vector2 screenPosition) => Position = screenPosition + _screenOffset;
}
