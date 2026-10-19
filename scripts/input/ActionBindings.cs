using Godot;
using System;
using System.Collections.Generic;

public partial class ActionBindings : Node
{
	public static readonly (string Action, string Label)[] Definitions =
	{
		("left", "向左移动"), ("right", "向右移动"), ("jump", "跳跃"),
		("dash", "突进（闪避）"), ("sword_control", "切换御剑/归剑"), ("switch_sword", "切换剑胚"),
		("replay_sword", "执行剑式连招"), ("combo_menu", "打开招式搭配"),
		("axe", "飞剑攻击"), ("search", "搜刮宝箱/尸体"), ("feed", "补充剑冢灵蕴"),
		("upgrade", "升级剑冢"), ("torch", "放置剑标"), ("campfire", "布置临时剑阵"),
		("burn_name", "强炼剑铭"), ("return_name", "归葬剑铭"), ("restart", "重启远征"), ("inventory", "打开背包")
	};

	private readonly Dictionary<string, Binding> _bindings = new();
	private readonly HashSet<string> _pressedActions = new();

	public override void _Ready()
	{
		SetDefaults();
		Load();
	}

	public void Capture(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey key && key.Pressed && !key.Echo)
			AddPressedAction(binding => binding.Key == key.Keycode);
		else if (inputEvent is InputEventMouseButton mouse && mouse.Pressed)
			AddPressedAction(binding => binding.Mouse == mouse.ButtonIndex);
	}

	public bool Held(string action)
	{
		var binding = _bindings[action];
		return binding.Key is Key key && Input.IsKeyPressed(key)
			|| binding.Mouse is MouseButton mouse && Input.IsMouseButtonPressed(mouse);
	}
	public bool Pressed(string action) => _pressedActions.Remove(action);
	public void ClearPressed() => _pressedActions.Clear();
	public string LabelFor(string action) => _bindings[action].Label;
	public GameCommands ConsumeGameplay(Vector2 attackTarget)
	{
		var held = new HashSet<string>();
		foreach (var (action, _) in Definitions)
			if (Held(action)) held.Add(action);
		var commands = new GameCommands(held, new HashSet<string>(_pressedActions), attackTarget, LabelFor("search"));
		_pressedActions.Clear();
		return commands;
	}

	public void Rebind(string action, Key key)
	{
		foreach (var pair in _bindings)
			if (pair.Key != action && pair.Value.Key == key) pair.Value.Key = null;
		_bindings[action] = new Binding(key);
		Save();
	}

	public void Rebind(string action, MouseButton mouse)
	{
		foreach (var pair in _bindings)
			if (pair.Key != action && pair.Value.Mouse == mouse) pair.Value.Mouse = null;
		_bindings[action] = new Binding(mouse);
		Save();
	}

	private void SetDefaults()
	{
		_bindings["left"] = new Binding(Key.A); _bindings["right"] = new Binding(Key.D); _bindings["jump"] = new Binding(Key.W);
		_bindings["dash"] = new Binding(Key.Shift); _bindings["sword_control"] = new Binding(MouseButton.Right);
		_bindings["switch_sword"] = new Binding(Key.Tab);
		_bindings["replay_sword"] = new Binding(Key.R);
		_bindings["combo_menu"] = new Binding(Key.J);
		_bindings["axe"] = new Binding(MouseButton.Left);
		_bindings["search"] = new Binding(Key.F); _bindings["feed"] = new Binding(Key.E); _bindings["upgrade"] = new Binding(Key.U);
		_bindings["torch"] = new Binding(Key.Q); _bindings["campfire"] = new Binding(Key.C); _bindings["burn_name"] = new Binding(Key.B);
		_bindings["return_name"] = new Binding(Key.G); _bindings["restart"] = new Binding(Key.Enter); _bindings["inventory"] = new Binding(Key.I);
	}

	private void Load()
	{
		var config = new ConfigFile();
		if (config.Load("user://controls.cfg") != Error.Ok) return;
		foreach (var (action, _) in Definitions)
		{
			var legacyAction = action == "search" ? "chop" : action;
			var keyName = config.GetValue("controls", action + "_key", config.GetValue("controls", legacyAction + "_key", "")).AsString();
			var mouseName = config.GetValue("controls", action + "_mouse", config.GetValue("controls", legacyAction + "_mouse", "")).AsString();
			if (Enum.TryParse<Key>(keyName, out var key)) _bindings[action] = new Binding(key);
			if (Enum.TryParse<MouseButton>(mouseName, out var mouse)) _bindings[action] = new Binding(mouse);
		}
		if (_bindings["sword_control"].Key == Key.Shift)
		{
			_bindings["sword_control"] = new Binding(MouseButton.Right);
			Save();
		}
	}

	private void Save()
	{
		var config = new ConfigFile();
		config.Load("user://controls.cfg");
		foreach (var (action, _) in Definitions)
		{
			config.SetValue("controls", action + "_key", _bindings[action].Key?.ToString() ?? "");
			config.SetValue("controls", action + "_mouse", _bindings[action].Mouse?.ToString() ?? "");
		}
		config.Save("user://controls.cfg");
	}

	private void AddPressedAction(Func<Binding, bool> match)
	{
		foreach (var pair in _bindings)
			if (match(pair.Value)) _pressedActions.Add(pair.Key);
	}

	private sealed class Binding
	{
		public Key? Key;
		public MouseButton? Mouse;
		public Binding(Key key) => Key = key;
		public Binding(MouseButton mouse) => Mouse = mouse;
		public string Label => Key?.ToString() ?? Mouse?.ToString() ?? "未绑定";
	}
}

public readonly struct GameCommands
{
	private readonly HashSet<string> _held;
	private readonly HashSet<string> _pressed;
	public Vector2 AttackTarget { get; }
	public string SearchLabel { get; }

	public GameCommands(HashSet<string> held, HashSet<string> pressed, Vector2 attackTarget, string searchLabel)
	{
		_held = held;
		_pressed = pressed;
		AttackTarget = attackTarget;
		SearchLabel = searchLabel;
	}

	public bool Held(string action) => _held.Contains(action);
	public bool Pressed(string action) => _pressed.Contains(action);
}
