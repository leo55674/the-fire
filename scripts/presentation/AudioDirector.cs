using Godot;
using System;

public partial class AudioDirector : Node
{
	[Export] private GameSession _session;
	[Export] private AudioStreamPlayer _music;
	[Export] private AudioStreamPlayer _axe;
	[Export] private AudioStreamPlayer _firebolt;
	[Export] private AudioStreamPlayer _hurt;

	public override void _Ready()
	{
		if (_session == null || _music == null || _axe == null || _firebolt == null || _hurt == null)
			throw new InvalidOperationException("AudioDirector requires a session and four audio players.");
		_session.AttackPerformed += OnAttackPerformed;
		_session.PlayerDamaged += () => _hurt.Play();
		_music.Finished += () => _music.Play();
		_music.Play();
	}

	private void OnAttackPerformed(string kind) => (kind == "axe" ? _axe : _firebolt).Play();
}
