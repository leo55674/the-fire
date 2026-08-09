using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	private const float GroundY = 560f;
	private static readonly Vector2 MainFire = new(0, GroundY);
	private readonly Random _random = new();
	private readonly List<Tree> _trees = new();
	private readonly List<Enemy> _enemies = new();
	private readonly List<FireSource> _fires = new();
	private readonly List<Projectile> _projectiles = new();
	private readonly Dictionary<string, Binding> _bindings = new();
	private readonly HashSet<string> _pressedActions = new();

	private Vector2 _player = MainFire;
	private float _verticalVelocity;
	private float _health = 5;
	private float _fuel = 70;
	private float _ember = 100;
	private float _dreamAsh;
	private float _spawnTimer = 6;
	private float _attackCooldown;
	private float _swingTime;
	private float _hurtTime;
	private float _finalNight;
	private int _wood;
	private int _fireLevel = 1;
	private bool _nameFound = true;
	private bool _mainMenuOpen = true;
	private bool _settingsOpen;
	private string _editingAction;
	private string _message = "\u5b88\u4f4f\u5f15\u9b42\u706b\u3002";

	private sealed class Tree
	{
		public Vector2 Position;
		public bool Cut;
		public Tree(float x) => Position = new Vector2(x, GroundY);
	}

	private sealed class FireSource
	{
		public Vector2 Position;
		public float Life;
		public float Radius;
		public FireSource(Vector2 position, float life, float radius) => (Position, Life, Radius) = (position, life, radius);
	}

	private enum EnemyKind { Hunter, Extinguisher, Thief }

	private sealed class Enemy
	{
		public EnemyKind Kind;
		public Vector2 Position;
		public float AttackDelay;
		public bool StoleWood;
		public Enemy(EnemyKind kind, float x) => (Kind, Position) = (kind, new Vector2(x, GroundY));
	}

	private sealed class Projectile
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Life = 1.1f;
		public Projectile(Vector2 position, Vector2 velocity) => (Position, Velocity) = (position, velocity);
	}

	private sealed class Binding
	{
		public Key? Key;
		public MouseButton? Mouse;
		public Binding(Key key) => Key = key;
		public Binding(MouseButton mouse) => Mouse = mouse;
		public string Label => Key?.ToString() ?? Mouse?.ToString() ?? "\u672a\u7ed1\u5b9a";
	}

	private static readonly (string Action, string Label)[] Controls =
	{
		("left", "\u5411\u5de6\u79fb\u52a8"), ("right", "\u5411\u53f3\u79fb\u52a8"), ("jump", "\u8df3\u8dc3"), ("axe", "\u6325\u65a7\u653b\u51fb"),
		("firebolt", "\u706b\u79cd\u653b\u51fb"), ("chop", "\u780d\u4f10\u6811\u6728"), ("feed", "\u6dfb\u5165\u4e3b\u706b"), ("upgrade", "\u5347\u7ea7\u4e3b\u706b"),
		("torch", "\u653e\u7f6e\u706b\u70ac"), ("campfire", "\u653e\u7f6e\u91ce\u8425\u706b"), ("burn_name", "\u71c3\u70e7\u540d\u5b57"),
		("return_name", "\u5f52\u8fd8\u540d\u5b57"), ("restart", "\u91cd\u71c3\u5faa\u73af")
	};

	public override void _Ready()
	{
		SetDefaultBindings();
		LoadBindings();
		for (var i = -10; i <= 10; i++)
			if (i != 0) _trees.Add(new Tree(i * 155 + _random.Next(-35, 36)));
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (_mainMenuOpen || _settingsOpen) { QueueRedraw(); return; }
		var dt = (float)delta;
		UpdatePlayer(dt);
		UpdateFire(dt);
		UpdateEnemies(dt);
		UpdateProjectiles(dt);
		UpdateTemporaryFires(dt);
		HandleInteractions();
		HandleCombat();
		QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (_mainMenuOpen && !_settingsOpen)
		{
			if (@event is InputEventKey menuKey && menuKey.Pressed && !menuKey.Echo)
			{
				if (menuKey.Keycode == Key.F1) { _settingsOpen = true; return; }
				if (menuKey.Keycode == Key.Enter || menuKey.Keycode == Key.Space) { StartGame(); return; }
			}
			if (@event is InputEventMouseButton menuMouse && menuMouse.Pressed && menuMouse.ButtonIndex == MouseButton.Left)
			{
				var mouse = GetViewport().GetMousePosition();
				if (MenuStartButton().HasPoint(mouse)) StartGame();
				if (MenuSettingsButton().HasPoint(mouse)) _settingsOpen = true;
				if (MenuExitButton().HasPoint(mouse)) GetTree().Quit();
			}
			return;
		}
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.F1) { _editingAction = null; _settingsOpen = !_settingsOpen; return; }
			if (_settingsOpen && _editingAction != null) { SetBinding(_editingAction, keyEvent.Keycode); return; }
			if (!_settingsOpen) AddPressedAction(binding => binding.Key == keyEvent.Keycode);
		}
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (_settingsOpen)
			{
				if (_editingAction != null) SetBinding(_editingAction, mouseEvent.ButtonIndex);
				else SelectSettingRow(GetViewport().GetMousePosition());
				return;
			}
			if (mouseEvent.ButtonIndex == MouseButton.Left && GameExitButton().HasPoint(GetViewport().GetMousePosition()))
			{
				GetTree().Quit();
				return;
			}
			AddPressedAction(binding => binding.Mouse == mouseEvent.ButtonIndex);
		}
	}

	private void SetDefaultBindings()
	{
		_bindings["left"] = new Binding(Key.A); _bindings["right"] = new Binding(Key.D); _bindings["jump"] = new Binding(Key.W);
		_bindings["axe"] = new Binding(MouseButton.Left); _bindings["firebolt"] = new Binding(MouseButton.Right);
		_bindings["chop"] = new Binding(Key.F); _bindings["feed"] = new Binding(Key.E); _bindings["upgrade"] = new Binding(Key.U);
		_bindings["torch"] = new Binding(Key.Q); _bindings["campfire"] = new Binding(Key.C); _bindings["burn_name"] = new Binding(Key.B);
		_bindings["return_name"] = new Binding(Key.G); _bindings["restart"] = new Binding(Key.Enter);
	}

	private void LoadBindings()
	{
		var config = new ConfigFile();
		if (config.Load("user://controls.cfg") != Error.Ok) return;
		foreach (var (action, _) in Controls)
		{
			var keyName = config.GetValue("controls", action + "_key", "").AsString();
			var mouseName = config.GetValue("controls", action + "_mouse", "").AsString();
			if (Enum.TryParse<Key>(keyName, out var key)) _bindings[action] = new Binding(key);
			if (Enum.TryParse<MouseButton>(mouseName, out var mouse)) _bindings[action] = new Binding(mouse);
		}
	}

	private void SaveBindings()
	{
		var config = new ConfigFile();
		foreach (var (action, _) in Controls)
		{
			config.SetValue("controls", action + "_key", _bindings[action].Key?.ToString() ?? "");
			config.SetValue("controls", action + "_mouse", _bindings[action].Mouse?.ToString() ?? "");
		}
		config.Save("user://controls.cfg");
	}

	private void SetBinding(string action, Key key)
	{
		foreach (var pair in _bindings) if (pair.Key != action && pair.Value.Key == key) pair.Value.Key = null;
		_bindings[action] = new Binding(key);
		SaveBindings(); _editingAction = null;
	}

	private void SetBinding(string action, MouseButton mouse)
	{
		foreach (var pair in _bindings) if (pair.Key != action && pair.Value.Mouse == mouse) pair.Value.Mouse = null;
		_bindings[action] = new Binding(mouse);
		SaveBindings(); _editingAction = null;
	}

	private void AddPressedAction(Func<Binding, bool> match)
	{
		foreach (var pair in _bindings)
			if (match(pair.Value)) _pressedActions.Add(pair.Key);
	}

	private bool Held(string action) => _bindings[action].Key is Key key && Input.IsKeyPressed(key);
	private bool Pressed(string action) => _pressedActions.Remove(action);
	private Rect2 SettingRow(int index) => new(GetViewportRect().Size.X / 2 - 205, 106 + index * 27, 410, 24);

	private void SelectSettingRow(Vector2 mouse)
	{
		for (var i = 0; i < Controls.Length; i++)
			if (SettingRow(i).HasPoint(mouse)) { _editingAction = Controls[i].Action; return; }
	}

	private Rect2 MenuStartButton() => new(GetViewportRect().Size.X / 2 - 145, GetViewportRect().Size.Y / 2 + 28, 290, 48);
	private Rect2 MenuSettingsButton() => new(GetViewportRect().Size.X / 2 - 145, GetViewportRect().Size.Y / 2 + 88, 290, 42);
	private Rect2 MenuExitButton() => new(GetViewportRect().Size.X / 2 - 145, GetViewportRect().Size.Y / 2 + 140, 290, 42);
	private Rect2 GameExitButton() => new(GetViewportRect().Size.X - 128, GetViewportRect().Size.Y - 52, 110, 32);

	private void StartGame()
	{
		_mainMenuOpen = false;
		_message = "\u5b88\u4f4f\u5f15\u9b42\u706b\u3002";
	}

	private void UpdatePlayer(float dt)
	{
		_player.X += ((Held("right") ? 1 : 0) - (Held("left") ? 1 : 0)) * 250 * dt;
		_verticalVelocity += 900 * dt;
		if (Held("jump") && _player.Y >= GroundY) _verticalVelocity = -440;
		_player.Y = MathF.Min(GroundY, _player.Y + _verticalVelocity * dt);
		if (_player.Y >= GroundY) _verticalVelocity = 0;
		var nearLight = _player.DistanceTo(MainFire) < 100 || _fires.Exists(fire => _player.DistanceTo(fire.Position) < fire.Radius);
		_ember = Mathf.Clamp(_ember + (nearLight ? 45 : -7) * dt, 0, 100);
		_attackCooldown = MathF.Max(0, _attackCooldown - dt);
		_swingTime = MathF.Max(0, _swingTime - dt);
		_hurtTime = MathF.Max(0, _hurtTime - dt);
	}

	private void UpdateFire(float dt)
	{
		_fuel = MathF.Max(0, _fuel - (0.42f + _fireLevel * 0.08f) * dt);
		if (_fuel <= 0)
		{
			_message = "\u4e3b\u706b\u7184\u706d\u3002\u6309\u91cd\u71c3\u5faa\u73af\u5f00\u59cb\u65b0\u7684\u8fdc\u5f81\u3002";
			if (Pressed("restart")) ResetRun();
			return;
		}
		_spawnTimer -= dt;
		if (_spawnTimer <= 0) { SpawnEnemy(); _spawnTimer = MathF.Max(2.4f, 7.5f - _fireLevel - _dreamAsh * .12f); }
		if (_finalNight > 0 && (_finalNight -= dt) <= 0) _message = "\u70fd\u706b\u7a7f\u900f\u4e86\u957f\u591c\u3002";
	}

	private void UpdateTemporaryFires(float dt)
	{
		for (var i = _fires.Count - 1; i >= 0; i--)
		{
			_fires[i].Life -= dt;
			if (_fires[i].Life <= 0) _fires.RemoveAt(i);
		}
	}

	private void UpdateEnemies(float dt)
	{
		foreach (var enemy in _enemies)
		{
			enemy.AttackDelay = MathF.Max(0, enemy.AttackDelay - dt);
			var target = enemy.Kind == EnemyKind.Extinguisher && _player.DistanceTo(MainFire) > 190 ? MainFire : _player;
			if (enemy.Kind == EnemyKind.Thief && enemy.StoleWood) target = new Vector2(MathF.Sign(enemy.Position.X) * 1600, GroundY);
			enemy.Position.X += MathF.Sign(target.X - enemy.Position.X) * (enemy.Kind == EnemyKind.Hunter ? 105 : 75) * dt;
			if (enemy.Kind == EnemyKind.Extinguisher && enemy.Position.DistanceTo(MainFire) < 38 && enemy.AttackDelay <= 0)
			{
				_fuel = MathF.Max(0, _fuel - 6); enemy.AttackDelay = 2.2f; _message = "\u706d\u706b\u8005\u6b63\u5728\u653b\u51fb\u4e3b\u706b\uff01";
			}
			if (enemy.Kind == EnemyKind.Thief && !enemy.StoleWood && enemy.Position.DistanceTo(_player) < 35 && enemy.AttackDelay <= 0)
			{
				if (_wood > 0) { _wood--; enemy.StoleWood = true; _message = "\u7a83\u6728\u8005\u5077\u8d70\u4e86\u8bb0\u5fc6\u6728\u3002"; }
				enemy.AttackDelay = 1.5f;
			}
			if (enemy.Kind == EnemyKind.Hunter && enemy.Position.DistanceTo(_player) < 34 && enemy.AttackDelay <= 0)
			{
				DamagePlayer(); enemy.AttackDelay = 1.3f;
			}
		}
		_enemies.RemoveAll(enemy => MathF.Abs(enemy.Position.X) > 1750);
	}

	private void UpdateProjectiles(float dt)
	{
		for (var i = _projectiles.Count - 1; i >= 0; i--)
		{
			var projectile = _projectiles[i];
			projectile.Position += projectile.Velocity * dt;
			projectile.Life -= dt;
			var hit = _enemies.Find(enemy => enemy.Position.DistanceTo(projectile.Position) < 25);
			if (hit != null) { _enemies.Remove(hit); projectile.Life = 0; _message = "\u706b\u79cd\u70e7\u5c3d\u4e86\u4e00\u7f15\u6267\u5ff5\u3002"; }
			if (projectile.Life <= 0) _projectiles.RemoveAt(i);
		}
	}

	private void HandleInteractions()
	{
		var chop = Pressed("chop"); var feed = Pressed("feed"); var upgrade = Pressed("upgrade");
		var burnName = Pressed("burn_name"); var returnName = Pressed("return_name");
		var torch = Pressed("torch"); var campfire = Pressed("campfire");
		if (_fuel <= 0) return;
		var tree = _trees.Find(candidate => !candidate.Cut && _player.DistanceTo(candidate.Position) < 58);
		if (tree != null && chop) { tree.Cut = true; _wood += 2; _message = "\u4f60\u780d\u4e0b\u4e86\u8bb0\u5fc6\u6728\u3002"; }
		var atFire = _player.DistanceTo(MainFire) < 75;
		if (atFire && feed && _wood > 0) { _wood--; _fuel = Mathf.Min(100, _fuel + 17); _message = "\u8bb0\u5fc6\u6728\u4e3a\u4e3b\u706b\u6dfb\u85aa\u3002"; }
		if (atFire && upgrade && _wood >= _fireLevel * 6 && _fireLevel < 3)
		{
			_wood -= _fireLevel * 6; _fireLevel++; _fuel = 100;
			_message = _fireLevel == 3 ? "\u70fd\u706b\u70b9\u71c3\uff0c\u957f\u591c\u5f00\u59cb\u4e86\u3002" : "\u5f15\u9b42\u706b\u66f4\u52a0\u65fa\u76db\u3002";
			if (_fireLevel == 3) { _finalNight = 30; for (var i = 0; i < 6; i++) SpawnEnemy(); }
		}
		if (_nameFound && _player.DistanceTo(new Vector2(430, GroundY)) < 60)
		{
			if (burnName) { _nameFound = false; _fuel = Mathf.Min(100, _fuel + 42); _dreamAsh += 2; _message = "\u540d\u5b57\u5728\u706b\u4e2d\u5316\u4e3a\u4e86\u68a6\u7070\u3002"; }
			if (returnName) { _nameFound = false; _dreamAsh = MathF.Max(0, _dreamAsh - 1); _message = "\u88ab\u8bb0\u4f4f\u7684\u4ea1\u9b42\u79bb\u5f00\u4e86\u8ff7\u96fe\u3002"; }
		}
		if (torch && _wood > 0) PlaceFire(22, 95, 1, "\u706b\u70ac\u7167\u4eae\u4e86\u5f52\u9014\u3002");
		if (campfire && _wood >= 3) PlaceFire(55, 170, 3, "\u91ce\u8425\u706b\u6682\u65f6\u963b\u6321\u4e86\u8ff7\u96fe\u3002");
	}

	private void HandleCombat()
	{
		var axe = Pressed("axe"); var firebolt = Pressed("firebolt");
		if (_attackCooldown > 0) return;
		var mouse = GetViewport().GetMousePosition();
		var aim = new Vector2(_player.X + mouse.X - GetViewportRect().Size.X / 2, mouse.Y) - _player;
		var direction = aim.Normalized();
		if (direction == Vector2.Zero) direction = Vector2.Right;
		if (axe)
		{
			_attackCooldown = .25f; _swingTime = .12f;
			var hit = _enemies.Find(enemy => enemy.Position.DistanceTo(_player) < 86 && (enemy.Position - _player).Normalized().Dot(direction) > .15f);
			if (hit != null) { _enemies.Remove(hit); _message = "\u65a7\u5203\u65a9\u65ad\u4e86\u4e00\u7f15\u6267\u5ff5\u3002"; }
		}
		else if (firebolt && _ember >= 15)
		{
			_attackCooldown = .32f; _ember -= 15;
			_projectiles.Add(new Projectile(_player + direction * 20, direction * 520)); _message = "\u4f60\u629b\u51fa\u4e86\u4e00\u679a\u706b\u79cd\u3002";
		}
	}

	private void PlaceFire(float life, float radius, int cost, string message)
	{
		if (_fires.Exists(fire => _player.DistanceTo(fire.Position) < 40)) return;
		_wood -= cost; _fires.Add(new FireSource(_player, life, radius)); _message = message;
	}

	private void SpawnEnemy()
	{
		var side = _random.Next(2) == 0 ? -1 : 1;
		_enemies.Add(new Enemy((EnemyKind)_random.Next(0, 3), side * _random.Next(650, 1050)));
	}

	private void DamagePlayer()
	{
		if (_hurtTime > 0) return;
		_health--; _hurtTime = 1; _message = "\u730e\u624b\u6495\u5f00\u4e86\u4f60\u7684\u4f59\u70ec\u3002";
		if (_health > 0) return;
		_wood /= 2; _fuel = MathF.Max(0, _fuel - 15); _health = 5; _player = MainFire;
		_message = "\u4f60\u5316\u4f5c\u4f59\u70ec\u56de\u5230\u4e86\u4e3b\u706b\u65c1\uff0c\u9057\u5931\u4e86\u90e8\u5206\u6728\u6750\u3002";
	}

	private void ResetRun()
	{
		_fuel = 70; _ember = 100; _health = 5; _wood = 0; _fireLevel = 1; _dreamAsh = 0; _finalNight = 0; _nameFound = true;
		_player = MainFire; _enemies.Clear(); _fires.Clear(); _projectiles.Clear();
		foreach (var tree in _trees) tree.Cut = false;
		_message = "\u4f59\u70ec\u91cd\u71c3\uff0c\u65b0\u7684\u5faa\u73af\u5f00\u59cb\u4e86\u3002";
	}

	private Vector2 Screen(Vector2 world) => new(GetViewportRect().Size.X / 2 + world.X - _player.X, world.Y);
	private bool IsVisibleToPlayer(Vector2 world) => world.DistanceTo(_player) < 155 + _ember * 2.1f - _dreamAsh * 10;

	public override void _Draw()
	{
		var size = GetViewportRect().Size;
		if (_mainMenuOpen)
		{
			DrawMainMenu(size);
			if (_settingsOpen) DrawSettings(size);
			return;
		}
		DrawRect(new Rect2(Vector2.Zero, size), new Color("10131d"));
		DrawRect(new Rect2(0, GroundY, size.X, size.Y - GroundY), new Color("17212a"));
		foreach (var tree in _trees) if (!tree.Cut && IsVisibleToPlayer(tree.Position)) DrawTree(Screen(tree.Position));
		if (_nameFound && IsVisibleToPlayer(new Vector2(430, GroundY))) DrawName(Screen(new Vector2(430, GroundY)));
		DrawFire(Screen(MainFire), 26 + _fireLevel * 10, new Color("ff8d42"), true);
		foreach (var fire in _fires) if (IsVisibleToPlayer(fire.Position)) DrawFire(Screen(fire.Position), 10, new Color("f4c266"), false);
		foreach (var enemy in _enemies) if (IsVisibleToPlayer(enemy.Position)) DrawEnemy(Screen(enemy.Position), enemy.Kind);
		foreach (var projectile in _projectiles) if (IsVisibleToPlayer(projectile.Position)) DrawCircle(Screen(projectile.Position), 7, new Color("ffd05c"));
		DrawPlayer(Screen(_player));
		DrawHud(size);
	}

	private void DrawTree(Vector2 p)
	{
		DrawRect(new Rect2(p.X - 6, p.Y - 64, 12, 64), new Color("5d3a36"));
		DrawCircle(p + new Vector2(0, -78), 28, new Color("29443c"));
		DrawCircle(p + new Vector2(-18, -62), 18, new Color("365748"));
		DrawCircle(p + new Vector2(18, -60), 18, new Color("365748"));
	}

	private void DrawFire(Vector2 p, float radius, Color color, bool main)
	{
		DrawCircle(p + new Vector2(0, -6), radius * 1.8f, new Color(color, .12f));
		DrawCircle(p + new Vector2(0, -5), radius, color);
		DrawCircle(p + new Vector2(0, -12), radius * .48f, new Color("fff2af"));
		DrawRect(new Rect2(p.X - radius, p.Y + radius * .5f, radius * 2, 5), new Color("503837"));
		if (main) DrawString(ThemeDB.FallbackFont, p + new Vector2(-24, -radius * 2), "\u4e3b\u706b", HorizontalAlignment.Left, -1, 16, new Color("ffe5a6"));
	}

	private void DrawName(Vector2 p)
	{
		DrawCircle(p + new Vector2(0, -30), 18, new Color("b6d1ef", .45f));
		DrawString(ThemeDB.FallbackFont, p + new Vector2(-20, -45), "NAME", HorizontalAlignment.Left, -1, 12, new Color("dcecff"));
	}

	private void DrawEnemy(Vector2 p, EnemyKind kind)
	{
		var color = kind switch { EnemyKind.Hunter => new Color("a64e67"), EnemyKind.Extinguisher => new Color("5a88a0"), _ => new Color("9b8060") };
		DrawCircle(p + new Vector2(0, -17), 15, color); DrawRect(new Rect2(p.X - 10, p.Y - 7, 20, 12), color);
		DrawString(ThemeDB.FallbackFont, p + new Vector2(-5, -13), kind == EnemyKind.Hunter ? "H" : kind == EnemyKind.Extinguisher ? "E" : "T", HorizontalAlignment.Left, -1, 13, Colors.White);
	}

	private void DrawPlayer(Vector2 p)
	{
		DrawCircle(p + new Vector2(0, -34), 13, _hurtTime > 0 ? new Color("ffdddd") : new Color("e9ded1"));
		DrawRect(new Rect2(p + new Vector2(-8, -21), new Vector2(16, 21)), new Color("784e50"));
		if (_swingTime <= 0) return;
		var swingDirection = MathF.Sign(GetViewport().GetMousePosition().X - p.X);
		DrawLine(p + new Vector2(0, -16), p + new Vector2(swingDirection * 48, -28), new Color("d9d5c5"), 7);
	}

	private void DrawHud(Vector2 size)
	{
		var font = ThemeDB.FallbackFont;
		DrawRect(new Rect2(18, 18, 380, 110), new Color("080b12", .78f));
		DrawString(font, new Vector2(32, 44), $"\u4e3b\u706b {MathF.Ceiling(_fuel),3}%   \u7b49\u7ea7 {_fireLevel}/3   \u68a6\u7070 {_dreamAsh:0}", HorizontalAlignment.Left, -1, 18, new Color("ffd797"));
		DrawString(font, new Vector2(32, 69), $"\u751f\u547d {_health:0}/5   \u4f59\u706b {_ember:0}%   \u8bb0\u5fc6\u6728 {_wood}", HorizontalAlignment.Left, -1, 17, Colors.White);
		DrawString(font, new Vector2(32, 97), _message, HorizontalAlignment.Left, 560, 14, new Color("b9c5d2"));
		DrawString(font, new Vector2(18, size.Y - 42), "WASD \u79fb\u52a8/\u8df3\u8dc3 | \u5de6\u952e\u6325\u65a7 | \u53f3\u952e\u706b\u79cd | F \u780d\u6811 | E \u6dfb\u706b | U \u5347\u7ea7 | Q \u706b\u70ac | C \u91ce\u8425\u706b", HorizontalAlignment.Left, -1, 14, new Color("d0d8df"));
		DrawString(font, new Vector2(size.X - 220, size.Y - 18), "F1 \u8bbe\u7f6e", HorizontalAlignment.Left, -1, 14, new Color("aebbc8"));
		DrawMenuButton(GameExitButton(), "\u9000\u51fa\u6e38\u620f", new Color("442939"));
		if (_nameFound && _player.DistanceTo(new Vector2(430, GroundY)) < 80)
			DrawString(font, new Vector2(size.X / 2 - 165, size.Y - 72), "\u540d\u5b57\u788e\u7247\uff1aB \u71c3\u70e7\uff08+\u706b\u52bf/+\u68a6\u7070\uff09  G \u5f52\u8fd8", HorizontalAlignment.Left, -1, 15, new Color("d9e8ff"));
		DrawMiniMap(size);
		if (_finalNight > 0) DrawString(font, new Vector2(size.X - 218, 142), $"\u70fd\u706b\u957f\u591c\uff1a{MathF.Ceiling(_finalNight)}", HorizontalAlignment.Left, -1, 17, new Color("ffb4a2"));
		if (_settingsOpen) DrawSettings(size);
	}

	private void DrawMiniMap(Vector2 size)
	{
		var panel = new Rect2(size.X - 222, 18, 204, 110);
		DrawRect(panel, new Color("080b12", .82f)); DrawRect(panel, new Color("607080"), false, 1);
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 19), "\u8ff7\u96fe\u5730\u56fe", HorizontalAlignment.Left, -1, 14, new Color("c9d7e6"));
		const float worldWidth = 3600;
		Vector2 Marker(Vector2 world) => new(panel.Position.X + 9 + (world.X + worldWidth / 2) / worldWidth * (panel.Size.X - 18), panel.Position.Y + 63);
		DrawRect(new Rect2(panel.Position + new Vector2(9, 62), new Vector2(panel.Size.X - 18, 2)), new Color("314150"));
		DrawCircle(Marker(MainFire), 5, new Color("ff9d4d"));
		foreach (var fire in _fires) DrawCircle(Marker(fire.Position), 3, new Color("ffd46b"));
		foreach (var enemy in _enemies) if (IsVisibleToPlayer(enemy.Position)) DrawCircle(Marker(enemy.Position), 3, new Color("c66a77"));
		var sight = (155 + _ember * 2.1f - _dreamAsh * 10) / worldWidth * (panel.Size.X - 18);
		DrawLine(Marker(_player) - Vector2.Right * sight, Marker(_player) + Vector2.Right * sight, new Color("dfeeff", .28f), 5);
		DrawCircle(Marker(_player), 4, new Color("ecf4ff"));
		DrawString(ThemeDB.FallbackFont, panel.Position + new Vector2(10, 97), "\u767d\u8272\uff1a\u4f60  \u6a59\u8272\uff1a\u4e3b\u706b", HorizontalAlignment.Left, -1, 11, new Color("9faebb"));
	}

	private void DrawMainMenu(Vector2 size)
	{
		DrawRect(new Rect2(Vector2.Zero, size), new Color("0c1019"));
		var center = new Vector2(size.X / 2, size.Y / 2);
		DrawCircle(center + new Vector2(0, -120), 92, new Color("ff8d42", .08f));
		DrawCircle(center + new Vector2(0, -120), 42, new Color("ff8d42"));
		DrawCircle(center + new Vector2(0, -132), 19, new Color("fff2af"));
		var font = ThemeDB.FallbackFont;
		DrawString(font, center + new Vector2(-68, -45), "\u5f15\u9b42\u706b", HorizontalAlignment.Left, -1, 42, new Color("ffe2a3"));
		DrawString(font, center + new Vector2(-182, -12), "\u6bcf\u4e00\u6b21\u71c3\u70e7\u7167\u4eae\u5f52\u9014\uff0c\u4e5f\u5582\u517b\u7740\u8ff7\u96fe\u3002", HorizontalAlignment.Left, -1, 17, new Color("bbc7d4"));
		DrawMenuButton(MenuStartButton(), "\u5f00\u59cb\u5b88\u706b", new Color("9a6537"));
		DrawMenuButton(MenuSettingsButton(), "\u5feb\u6377\u952e\u8bbe\u7f6e", new Color("233143"));
		DrawMenuButton(MenuExitButton(), "\u9000\u51fa\u6e38\u620f", new Color("442939"));
		DrawString(font, center + new Vector2(-154, 212), "Enter \u5f00\u59cb\u6e38\u620f  |  F1 \u6253\u5f00\u8bbe\u7f6e", HorizontalAlignment.Left, -1, 15, new Color("9dacba"));
	}

	private void DrawMenuButton(Rect2 rect, string text, Color color)
	{
		DrawRect(rect, color);
		DrawRect(rect, new Color("d5b476"), false, 1);
		DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(0, 31), text, HorizontalAlignment.Center, rect.Size.X, 20, Colors.White);
	}

	private void DrawSettings(Vector2 size)
	{
		var panel = new Rect2(size.X / 2 - 225, 48, 450, 430);
		DrawRect(panel, new Color("0b1019", .96f)); DrawRect(panel, new Color("d3a65a"), false, 2);
		var font = ThemeDB.FallbackFont;
		DrawString(font, panel.Position + new Vector2(22, 31), "\u5feb\u6377\u952e\u8bbe\u7f6e", HorizontalAlignment.Left, -1, 24, new Color("ffe0a0"));
		DrawString(font, panel.Position + new Vector2(22, 55), _editingAction == null ? "\u70b9\u51fb\u64cd\u4f5c\uff0c\u518d\u6309\u4e0b\u6309\u952e\u6216\u9f20\u6807\u952e" : "\u8bf7\u6309\u4e0b\u65b0\u7684\u6309\u952e\u6216\u9f20\u6807\u952e", HorizontalAlignment.Left, -1, 14, new Color("b9c8d5"));
		for (var i = 0; i < Controls.Length; i++)
		{
			var (action, label) = Controls[i]; var row = SettingRow(i); var selected = _editingAction == action;
			DrawRect(row, selected ? new Color("825e35") : new Color("182331"));
			DrawString(font, row.Position + new Vector2(12, 18), label, HorizontalAlignment.Left, -1, 16, Colors.White);
			DrawString(font, row.Position + new Vector2(260, 18), _bindings[action].Label, HorizontalAlignment.Left, 130, 16, new Color("ffd285"));
		}
		DrawString(font, panel.Position + new Vector2(22, 410), "F1 \u5173\u95ed\u8bbe\u7f6e\u3002\u4fee\u6539\u4f1a\u81ea\u52a8\u4fdd\u5b58\u3002", HorizontalAlignment.Left, -1, 14, new Color("9aabb9"));
	}
}
