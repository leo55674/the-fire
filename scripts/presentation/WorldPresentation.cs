using Godot;
using System;
using System.Collections.Generic;

public partial class WorldPresentation : Node2D
{
	private const int SwordFrontZIndex = 13;
	private static readonly Rect2[] TreeRegions =
	{
		new(16, 4, 354, 612), new(388, 44, 298, 574), new(714, 126, 288, 490),
		new(1024, 152, 246, 464), new(1302, 230, 208, 388)
	};
	[Export] private Texture2D _roadTexture;
	[Export] private Texture2D _treeAtlas;
	[Export] private PackedScene _flyingSwordScene;
	[Export] private Godot.Collections.Array<Texture2D> _mainFireTextures;
	[Export] private Godot.Collections.Array<PackedScene> _enemyScenes;
	[Export] private Node2D _road;
	[Export] private Polygon2D _ground;
	[Export] private Node2D _trees;
	[Export] private Sprite2D _mainFire;
	[Export] private BlacksmithPresentation _blacksmith;
	[Export] private Label _nameFragment;
	[Export] private Node2D _temporaryFires;
	[Export] private Node2D _enemies;
	[Export] private Node2D _chests;
	[Export] private Node2D _projectiles;
	[Export] private Line2D _axeSwing;
	[Export] private PlayerPresentation _player;

	private readonly List<Sprite2D> _treeVisuals = new();
	private readonly List<Sprite2D> _fireVisuals = new();
	private readonly List<EnemyPresentation> _enemyVisuals = new();
	private readonly List<ColorRect> _chestBodies = new();
	private readonly List<ColorRect> _chestLids = new();
	private FlyingSwordPresentation _swordVisual;
	private FlyingSwordPresentation _replayVisual;
	private readonly FlyingSwordPresentation[] _swordVisuals = new FlyingSwordPresentation[3];
	private readonly Vector2[] _swordDisplayPositions = new Vector2[3];
	private readonly bool[] _swordDisplayInitialized = new bool[3];
	private readonly CpuParticles2D[] _attackParticles = new CpuParticles2D[3];

	public override void _Ready()
	{
		if (_enemyScenes == null || _enemyScenes.Count != 3 || _enemyScenes[0] == null || _enemyScenes[1] == null || _enemyScenes[2] == null || _flyingSwordScene == null || _blacksmith == null)
			throw new InvalidOperationException("WorldPresentation requires three enemy scenes, the flying sword scene, and the blacksmith.");
		foreach (var child in _trees.GetChildren())
			if (child is Sprite2D tree) _treeVisuals.Add(tree);
		var swordNames = new[] { "Zhaoye", "Shuanghua", "Bilan" };
		for (var i = 0; i < _swordVisuals.Length; i++)
		{
			var sword = _flyingSwordScene.Instantiate<FlyingSwordPresentation>();
			sword.Name = swordNames[i];
			sword.Configure(SwordVisualFor(i));
			_swordVisuals[i] = sword;
			_attackParticles[i] = new CpuParticles2D
			{
				Name = "AttackParticles", Amount = 14, Lifetime = .2, Emitting = false,
				LocalCoords = false, Direction = Vector2.Left, Spread = 35,
				InitialVelocityMin = 35, InitialVelocityMax = 75,
				Gravity = Vector2.Zero, Color = new Color("b7f4ff", .85f)
			};
			sword.AddChild(_attackParticles[i]);
		}
		_swordVisual = _swordVisuals[0];
		_replayVisual = _flyingSwordScene.Instantiate<FlyingSwordPresentation>();
		_replayVisual.Name = "ReplaySword";
		_replayVisual.Configure(FlyingSwordPresentation.SwordVisual.ZhaoyeReplay);
		foreach (var sword in _swordVisuals) _projectiles.AddChild(sword);
		_projectiles.AddChild(_replayVisual);
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
		UpdateBlacksmith(session);
		UpdateName(session);
		UpdateFires(session);
		UpdateEnemies(session);
		UpdateChests(session);
		UpdateSword(session);
		UpdateCompanionSwords(session);
		UpdateReplaySword(session);
		UpdatePlayer(session);
	}

	private void UpdateBlacksmith(GameSession session)
	{
		_blacksmith.Present(Screen(session, session.BlacksmithPosition));
	}

	private void UpdateCompanionSwords(GameSession session)
	{
		for (var i = 0; i < session.CompanionSwordPositions.Count; i++)
		{
			var swordIndex = session.CompanionSwordIndex(i);
			if (swordIndex < 0) continue;
			var sword = _swordVisuals[swordIndex];
			sword.ZIndex = SwordFrontZIndex - i - 1;
			sword.Present(FollowSword(session, swordIndex, session.CompanionSwordPositions[i]), -Mathf.Pi / 2, true);
		}
		for (var i = session.SwordCount; i < _swordVisuals.Length; i++) _swordVisuals[i].Visible = false;
	}

	private Vector2 FollowSword(GameSession session, int swordIndex, Vector2 target)
	{
		var blend = 1 - MathF.Exp(-10 * MathF.Min((float)GetProcessDeltaTime(), .05f));
		_swordDisplayPositions[swordIndex] = _swordDisplayInitialized[swordIndex]
			? _swordDisplayPositions[swordIndex].Lerp(target, blend) : target;
		_swordDisplayInitialized[swordIndex] = true;
		var time = (float)Time.GetTicksMsec() * .001f;
		var phase = swordIndex * 2.17f;
		var bob = Mathf.Sin(time * (2.1f + swordIndex * .23f) + phase) * 3f
			+ Mathf.Sin(time * (1.17f + swordIndex * .19f) + phase * .7f) * 1.2f;
		return Screen(session, _swordDisplayPositions[swordIndex]) + new Vector2(0, bob);
	}

	private static FlyingSwordPresentation.SwordVisual SwordVisualFor(int index) => index switch
	{
		1 => FlyingSwordPresentation.SwordVisual.Shuanghua,
		2 => FlyingSwordPresentation.SwordVisual.Bilan,
		_ => FlyingSwordPresentation.SwordVisual.Zhaoye
	};

	private Vector2 Screen(GameSession session, Vector2 world) =>
		new(GetViewportRect().Size.X / 2 + world.X - session.CameraFocus.X, world.Y);

	private void UpdateSword(GameSession session)
	{
		var swordIndex = session.ActiveSwordIndex;
		_swordVisual = _swordVisuals[swordIndex];
		_swordVisual.ZIndex = SwordFrontZIndex;
		var swordScreen = session.SwordState == GameSession.SwordMode.Following
			? FollowSword(session, swordIndex, session.SwordPosition)
			: Screen(session, session.SwordPosition);
		if (session.SwordState != GameSession.SwordMode.Following)
		{
			_swordDisplayPositions[swordIndex] = session.SwordPosition;
			_swordDisplayInitialized[swordIndex] = true;
		}
		var targetRotation = session.SwordState == GameSession.SwordMode.Following
			? -Mathf.Pi / 2
			: session.SwordVelocity.LengthSquared() > 1 ? session.SwordVelocity.Angle() : _swordVisual.Rotation;
		if (session.SwordState == GameSession.SwordMode.Returning)
		{
			var lift = 1 - Mathf.Clamp(session.SwordFollowDistance / 180, 0, 1);
			targetRotation = Mathf.LerpAngle(targetRotation, -Mathf.Pi / 2, lift);
		}
		_swordVisual.Present(swordScreen, Mathf.LerpAngle(_swordVisual.Rotation, targetRotation, .18f), true);
		for (var i = 0; i < _attackParticles.Length; i++)
			_attackParticles[i].Emitting = i == swordIndex && session.SwordState == GameSession.SwordMode.Attacking;
	}

	private void UpdateReplaySword(GameSession session)
	{
		_replayVisual.Visible = session.HasRecordedSword;
		if (!session.HasRecordedSword) return;
		_replayVisual.Position = Screen(session, session.ReplaySwordPosition);
		var rotation = session.IsReplaySwordActive && session.ReplaySwordVelocity.LengthSquared() > 1
			? session.ReplaySwordVelocity.Angle() : -Mathf.Pi / 2;
		_replayVisual.Rotation = Mathf.LerpAngle(_replayVisual.Rotation, rotation, .18f);
	}

	private void UpdateGround(Vector2 viewport, float groundY)
	{
		_ground.Polygon = new[]
		{
			new Vector2(0, groundY), new Vector2(viewport.X, groundY), viewport, new Vector2(0, viewport.Y)
		};
	}

	private void UpdateRoad(Vector2 viewport, GameSession session)
	{
		const float horizontalTrim = 256;
		var scale = viewport.X / _roadTexture.GetWidth();
		var tileWidth = (_roadTexture.GetWidth() - horizontalTrim * 2) * scale;
		var offset = session.CameraFocus.X % tileWidth;
		if (offset < 0) offset += tileWidth;
		var y = session.GroundY - 392 * scale;
		for (var i = 0; i < 3; i++)
		{
			var sprite = _road.GetChild<Sprite2D>(i);
			sprite.Position = new Vector2(i * tileWidth - offset, y);
			sprite.Scale = Vector2.One * scale;
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
			var source = TreeRegions[(int)MathF.Abs(tree.Position.X / 155) % TreeRegions.Length];
			const float scale = .32f;
			sprite.Texture = _treeAtlas;
			sprite.RegionEnabled = true;
			sprite.Centered = false;
			sprite.RegionRect = source;
			sprite.Scale = Vector2.One * scale;
			var screen = Screen(session, tree.Position);
			sprite.Position = screen - new Vector2(source.Size.X * scale / 2, source.Size.Y * scale);
		}
	}

	private void UpdateChests(GameSession session)
	{
		while (_chestBodies.Count < session.Chests.Count)
		{
			var body = new ColorRect { Name = $"ChestBody{_chestBodies.Count + 1:00}", Size = new Vector2(42, 22), Color = new Color("76502f"), ZIndex = 3, MouseFilter = Control.MouseFilterEnum.Ignore };
			var lid = new ColorRect { Name = $"ChestLid{_chestLids.Count + 1:00}", Size = new Vector2(46, 9), Color = new Color("d8a94f"), ZIndex = 4, MouseFilter = Control.MouseFilterEnum.Ignore };
			_chests.AddChild(body);
			_chests.AddChild(lid);
			_chestBodies.Add(body);
			_chestLids.Add(lid);
		}
		for (var i = 0; i < _chestBodies.Count; i++)
		{
			var visible = i < session.Chests.Count && session.IsVisible(session.Chests[i].Position);
			_chestBodies[i].Visible = visible;
			_chestLids[i].Visible = visible;
			if (!visible) continue;
			var screen = Screen(session, session.Chests[i].Position);
			_chestBodies[i].Position = screen + new Vector2(-21, -22);
			_chestLids[i].Position = screen + (session.Chests[i].Searched ? new Vector2(-23, -25) : new Vector2(-23, -31));
			_chestBodies[i].Color = session.Chests[i].Searched ? new Color("594532") : new Color("76502f");
			_chestLids[i].Color = session.Chests[i].Searched ? new Color("98734b") : new Color("d8a94f");
		}
	}

	private void UpdateMainFire(GameSession session)
	{
		var balance = session.Balance;
		var stage = session.Fuel > balance.FireStageThreshold3 ? 3 : session.Fuel > balance.FireStageThreshold2 ? 2 : session.Fuel > balance.FireStageThreshold1 ? 1 : 0;
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
		EnsureEnemyCount(session);
		for (var i = 0; i < _enemyVisuals.Count; i++)
		{
			var enemy = session.Enemies[i];
			_enemyVisuals[i].Present(enemy, session, Screen(session, enemy.Position));
		}
	}

	private void EnsureEnemyCount(GameSession session)
	{
		while (_enemyVisuals.Count > session.Enemies.Count)
		{
			var last = _enemyVisuals[^1];
			_enemies.RemoveChild(last);
			last.QueueFree();
			_enemyVisuals.RemoveAt(_enemyVisuals.Count - 1);
		}
		for (var i = 0; i < session.Enemies.Count; i++)
		{
			var kind = session.Enemies[i].Kind;
			if (i < _enemyVisuals.Count && _enemyVisuals[i].Kind == kind) continue;
			if (i < _enemyVisuals.Count)
			{
				var old = _enemyVisuals[i];
				_enemies.RemoveChild(old);
				old.QueueFree();
			}
			var sprite = _enemyScenes[(int)kind].Instantiate<EnemyPresentation>();
			sprite.Name = $"Enemy{i + 1:00}";
			_enemies.AddChild(sprite);
			if (i < _enemyVisuals.Count) _enemyVisuals[i] = sprite;
			else _enemyVisuals.Add(sprite);
		}
	}

	private void UpdatePlayer(GameSession session)
	{
		var moving = session.PlayerMoving;
		_player.Present(Screen(session, session.Player), false, session.HurtTime > 0, moving, session.IsDashing, session.PlayerFacing < 0);
		_axeSwing.Visible = session.SwingTime > 0;
		if (!_axeSwing.Visible) return;
		var playerPosition = Screen(session, session.Player);
		var direction = MathF.Sign(GetViewport().GetMousePosition().X - playerPosition.X);
		_axeSwing.Points = new[] { playerPosition + new Vector2(0, -16), playerPosition + new Vector2(direction * 48, -28) };
	}
}
