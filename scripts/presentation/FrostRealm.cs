using Godot;

public partial class FrostRealm : Node2D
{
	[Export] private Sprite2D _backdrop;
	[Export] private FrostWraithKnight _boss;
	[Export] private Label _exitLabel;

	public void Reset() => _boss.Reset();

	public void Tick(GameSession session, float dt)
	{
		Visible = session.IsInRealm;
		if (!Visible) return;
		var size = GetViewportRect().Size;
		_backdrop.Position = size / 2;
		_backdrop.Scale = new Vector2(size.X / _backdrop.Texture.GetWidth(), size.Y / _backdrop.Texture.GetHeight());
		_exitLabel.Position = new Vector2(size.X / 2 - 650 - session.CameraFocus.X, session.GroundY - 105);
		QueueRedraw();
		_boss.Tick(session, dt);
	}

	public override void _Draw()
	{
		var size = GetViewportRect().Size;
		DrawRect(new Rect2(Vector2.Zero, size), new Color("080f1b"));
		DrawRect(new Rect2(0, size.Y * 7 / 9, size.X, size.Y * 2 / 9), new Color("152034", .85f));
	}
}
