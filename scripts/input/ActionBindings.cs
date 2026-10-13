using Godot;
using System;
using System.Collections.Generic;

public partial class ActionBindings : Node
{
	public static readonly (string Action, string Label)[] Definitions =
	{
		("left", "向左移动"), ("right", "向右移动"), ("jump", "跳跃"), ("axe", "挥斧攻击"),
		("firebolt", "火种攻击"), ("chop", "砍伐树木"), ("feed", "添入主火"), ("upgrade", "升级主火"),
		("torch", "放置火把"), ("campfire", "放置野营火"), ("burn_name", "燃烧名字"),
		("return_name", "归还名字"), ("restart", "重燃循环")
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

	public bool Held(string action) => _bindings[action].Key is Key key && Input.IsKeyPressed(key);
	public bool Pressed(string action) => _pressedActions.Remove(action);
	public string LabelFor(string action) => _bindings[action].Label;

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
		_bindings["axe"] = new Binding(MouseButton.Left); _bindings["firebolt"] = new Binding(MouseButton.Right);
		_bindings["chop"] = new Binding(Key.F); _bindings["feed"] = new Binding(Key.E); _bindings["upgrade"] = new Binding(Key.U);
		_bindings["torch"] = new Binding(Key.Q); _bindings["campfire"] = new Binding(Key.C); _bindings["burn_name"] = new Binding(Key.B);
		_bindings["return_name"] = new Binding(Key.G); _bindings["restart"] = new Binding(Key.Enter);
	}

	private void Load()
	{
		var config = new ConfigFile();
		if (config.Load("user://controls.cfg") != Error.Ok) return;
		foreach (var (action, _) in Definitions)
		{
			var keyName = config.GetValue("controls", action + "_key", "").AsString();
			var mouseName = config.GetValue("controls", action + "_mouse", "").AsString();
			if (Enum.TryParse<Key>(keyName, out var key)) _bindings[action] = new Binding(key);
			if (Enum.TryParse<MouseButton>(mouseName, out var mouse)) _bindings[action] = new Binding(mouse);
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
