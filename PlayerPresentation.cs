using Godot;

/// <summary>
/// Owns only the Keeper's scene-authored visual. Movement and combat state stay
/// in Main until their respective gameplay components are extracted.
/// </summary>
public partial class PlayerPresentation : AnimatedSprite2D
{
	[Export] private Color _hurtTint = new("ffdddd");
	[Export] private Color _normalTint = Colors.White;

	public void Present(Vector2 screenPosition, bool hidden, bool hurt, bool moving, bool facingLeft = false)
	{
		Visible = !hidden;
		Position = screenPosition;
		FlipH = moving && facingLeft;
		Modulate = hurt ? _hurtTint : _normalTint;
		var animation = hurt ? "hurt" : moving ? "walk" : "idle";
		if (Animation != animation)
			Play(animation);
	}
}
