using Godot;

public partial class FlyingSwordPresentation : Node2D
{
	public enum SwordVisual { Zhaoye, Shuanghua, Bilan, ZhaoyeReplay }

	private Sprite2D _zhaoye;
	private Sprite2D _shuanghua;
	private Sprite2D _bilan;
	private Sprite2D _zhaoyeReplay;

	public override void _Ready() => ResolveSprites();

	private void ResolveSprites()
	{
		_zhaoye ??= GetNode<Sprite2D>("Zhaoye");
		_shuanghua ??= GetNode<Sprite2D>("Shuanghua");
		_bilan ??= GetNode<Sprite2D>("Bilan");
		_zhaoyeReplay ??= GetNode<Sprite2D>("ZhaoyeReplay");
	}

	public void Configure(SwordVisual sword)
	{
		ResolveSprites();
		_zhaoye.Visible = sword == SwordVisual.Zhaoye;
		_shuanghua.Visible = sword == SwordVisual.Shuanghua;
		_bilan.Visible = sword == SwordVisual.Bilan;
		_zhaoyeReplay.Visible = sword == SwordVisual.ZhaoyeReplay;
	}

	public void Present(Vector2 position, float rotation, bool visible)
	{
		Position = position;
		Rotation = rotation;
		Visible = visible;
	}
}
