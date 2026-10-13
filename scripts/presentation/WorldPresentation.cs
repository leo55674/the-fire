using Godot;
using System;
using System.Collections.Generic;

public partial class WorldPresentation : Node2D
{
	private static readonly Rect2 RoadRegion = new(0, 320, 2172, 196);
	private static readonly Rect2[] TreeRegions =
	{
		new(16, 4, 354, 612), new(388, 44, 298, 574), new(714, 126, 288, 490),
		new(1024, 152, 246, 464), new(1302, 230, 208, 388)
	};
	private static readonly Rect2 TreeStumpRegion = new(882, 613, 218, 260);

	[Export] private Texture2D _roadAtlas;
	[Export] private Texture2D _treeAtlas;
	[Export] private Godot.Collections.Array<Texture2D> _mainFireTextures;
	[Export] private Godot.Collections.Array<Texture2D> _enemyTextures;
	[Export] private ActionBindings _input;
	[Export] private Node2D _road;
	[Export] private Polygon2D _ground;
	[Export] private Node2D _trees;
	[Export] private Sprite2D _mainFire;
	[Export] private Label _nameFragment;
	[Export] private Node2D _temporaryFires;
	[Export] private Node2D _enemies;
	[Export] private Node2D _projectiles;
	[Export] private Line2D _axeSwing;
	[Export] private PlayerPresentation _player;

	private readonly List<Sprite2D> _treeVisuals = new();
	private readonly List<Sprite2D> _fireVisuals = new();
	private readonly List<Sprite2D> _enemyVisuals = new();
	private readonly List<ColorRect> _projectileVisuals = new();

	public override void _Ready()
	{
		if (_enemyTextures == null || _enemyTextures.Count != 9)
			throw new InvalidOperationException("WorldPresentation requires three states for each of its three enemy types.");
		foreach (var child in _trees.GetChildren())
			if (child is Sprite2D tree) _treeVisuals.Add(tree);
	}

	public void Sync(GameSession session, bool menuOpen)
	{
		Visible = !menuOpen;
		if (menuOpen) return;

		var viewport = GetViewportRect().Size;
		UpdateGround(viewport, session.GroundY);
		UpdateRoad(viewport, session);
		UpdateTrees(session);
		UpdateMainFire(session);
		UpdateName(session);
		UpdateFires(session);
		UpdateEnemies(session);
		UpdateProjectiles(session);
		UpdatePlayer(session);
	}

	private Vector2 Screen(GameSession session, Vector2 world) =>
		new(GetViewportRect().Size.X / 2 + world.X - session.Player.X, world.Y);

	private void UpdateGround(Vector2 viewport, float groundY)
	{
		_ground.Polygon = new[]
		{
			new Vector2(0, groundY), new Vector2(viewport.X, groundY), viewport, new Vector2(0, viewport.Y)
		};
	}

	private void UpdateRoad(Vector2 viewport, GameSession session)
	{
		const float tileHeight = 118f;
		var tileWidth = RoadRegion.Size.X / RoadRegion.Size.Y * tileHeight;
		var firstTile = Mathf.FloorToInt((session.Player.X - viewport.X / 2) / tileWidth) - 1;
		var lastTile = Mathf.CeilToInt((session.Player.X + viewport.X / 2) / tileWidth) + 1;
		var required = lastTile - firstTile + 1;
		EnsureRoadCount(required);
		for (var i = 0; i < _road.GetChildCount(); i++)
		{
			var sprite = _road.GetChild<Sprite2D>(i);
			sprite.Visible = i < required;
			if (!sprite.Visible) continue;
			var tile = firstTile + i;
			sprite.Texture = _roadAtlas;
			sprite.RegionEnabled = true;
			sprite.RegionRect = RoadRegion;
			sprite.Centered = false;
			sprite.Position = new Vector2(viewport.X / 2 + tile * tileWidth - session.Player.X, session.GroundY - 25);
			sprite.Scale = Vector2.One * (tileHeight / RoadRegion.Size.Y);
		}
	}

	private void EnsureRoadCount(int count)
	{
		while (_road.GetChildCount() < count)
		{
			var sprite = new Sprite2D
			{
				Name = $"RoadTile{_road.GetChildCount() + 1:00}", Texture = _roadAtlas,
				RegionEnabled = true, RegionRect = RoadRegion, Centered = false
			};
			_road.AddChild(sprite);
		}
	}

	private void UpdateTrees(GameSession session)
	{
		while (_treeVisuals.Count < session.Trees.Count)
		{
			var sprite = new Sprite2D { Name = $"Tree{_treeVisuals.Count + 1:00}", Texture = _treeAtlas, RegionEnabled = true, Centered = false };
			_trees.AddChild(sprite);
			_treeVisuals.Add(sprite);
		}
		for (var i = 0; i < _treeVisuals.Count; i++)
		{
			var sprite = _treeVisuals[i];
			sprite.Visible = i < session.Trees.Count && session.IsVisible(session.Trees[i].Position);
			if (!sprite.Visible) continue;
			var tree = session.Trees[i];
			var source = tree.Cut ? TreeStumpRegion : TreeRegions[(int)MathF.Abs(tree.Position.X / 155) % TreeRegions.Length];
			var scale = tree.Cut ? .28f : .32f;
			sprite.Texture = _treeAtlas;
			sprite.RegionEnabled = true;
			sprite.Centered = false;
			sprite.RegionRect = source;
			sprite.Scale = Vector2.One * scale;
			var screen = Screen(session, tree.Position);
			sprite.Position = screen - new Vector2(source.Size.X * scale / 2, source.Size.Y * scale);
		}
	}

	private void UpdateMainFire(GameSession session)
	{
		var stage = session.Fuel > 75 ? 3 : session.Fuel > 50 ? 2 : session.Fuel > 25 ? 1 : 0;
		if (_mainFireTextures.Count > stage) _mainFire.Texture = _mainFireTextures[stage];
		_mainFire.Position = Screen(session, session.MainFire) + new Vector2(0, -98);
		_mainFire.Scale = Vector2.One * (240f / 512f);
		_mainFire.Modulate = session.Fuel <= 0 ? new Color("414652") : Colors.White;
	}

	private void UpdateName(GameSession session)
	{
		var position = new Vector2(430, session.GroundY);
		_nameFragment.Visible = session.NameFound && session.IsVisible(position);
		_nameFragment.Position = Screen(session, position) + new Vector2(-22, -55);
	}

	private void UpdateFires(GameSession session)
	{
		EnsureFireCount(session.Fires.Count);
		for (var i = 0; i < _fireVisuals.Count; i++)
		{
			var sprite = _fireVisuals[i];
			sprite.Visible = i < session.Fires.Count && session.IsVisible(session.Fires[i].Position);
			if (sprite.Visible) sprite.Position = Screen(session, session.Fires[i].Position) + new Vector2(0, -28);
		}
	}

	private void EnsureFireCount(int count)
	{
		while (_fireVisuals.Count < count)
		{
			var sprite = new Sprite2D
			{
				Name = $"TemporaryFire{_fireVisuals.Count + 1:00}",
				Texture = _mainFireTextures.Count > 0 ? _mainFireTextures[0] : null,
				Scale = Vector2.One * .14f
			};
			_temporaryFires.AddChild(sprite);
			_fireVisuals.Add(sprite);
		}
	}

	private void UpdateEnemies(GameSession session)
	{
		EnsureEnemyCount(session.Enemies.Count);
		for (var i = 0; i < _enemyVisuals.Count; i++)
		{
			var sprite = _enemyVisuals[i];
			sprite.Visible = i < session.Enemies.Count && session.IsVisible(session.Enemies[i].Position);
			if (!sprite.Visible) continue;
			var enemy = session.Enemies[i];
			sprite.Texture = _enemyTextures[(int)enemy.Kind * 3 + (int)enemy.VisualState];
			sprite.Position = Screen(session, enemy.Position) + new Vector2(0, -69);
			sprite.FlipH = enemy.Position.X > session.Player.X;
		}
	}

	private void EnsureEnemyCount(int count)
	{
		while (_enemyVisuals.Count < count)
		{
			var sprite = new Sprite2D { Name = $"Enemy{_enemyVisuals.Count + 1:00}", Scale = Vector2.One * .48f };
			_enemies.AddChild(sprite);
			_enemyVisuals.Add(sprite);
		}
	}

	private void UpdateProjectiles(GameSession session)
	{
		EnsureProjectileCount(session.Projectiles.Count);
		for (var i = 0; i < _projectileVisuals.Count; i++)
		{
			var projectile = _projectileVisuals[i];
			projectile.Visible = i < session.Projectiles.Count && session.IsVisible(session.Projectiles[i].Position);
			if (projectile.Visible) projectile.Position = Screen(session, session.Projectiles[i].Position) - projectile.Size / 2;
		}
	}

	private void EnsureProjectileCount(int count)
	{
		while (_projectileVisuals.Count < count)
		{
			var projectile = new ColorRect { Name = $"Projectile{_projectileVisuals.Count + 1:00}", Size = new Vector2(12, 12), Color = new Color("ffd05c"), Rotation = Mathf.Pi / 4 };
			_projectiles.AddChild(projectile);
			_projectileVisuals.Add(projectile);
		}
	}

	private void UpdatePlayer(GameSession session)
	{
		var moving = _input.Held("left") || _input.Held("right");
		_player.Present(Screen(session, session.Player), false, session.HurtTime > 0, moving, _input.Held("left"));
		_axeSwing.Visible = session.SwingTime > 0;
		if (!_axeSwing.Visible) return;
		var playerPosition = Screen(session, session.Player);
		var direction = MathF.Sign(GetViewport().GetMousePosition().X - playerPosition.X);
		_axeSwing.Points = new[] { playerPosition + new Vector2(0, -16), playerPosition + new Vector2(direction * 48, -28) };
	}
}
