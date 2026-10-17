using Godot;
using System;

public partial class AudioDirector : Node
{
	[Export] private GameSession _session;
	[Export] private AudioStreamPlayer _music;
	[Export] private AudioStreamPlayer _swordLaunch;
	[Export] private AudioStreamPlayer _swordHit;
	[Export] private AudioStreamPlayer _swordDefeat;
	[Export] private AudioStreamPlayer _controlEnter;
	[Export] private AudioStreamPlayer _swordReturn;
	[Export] private AudioStreamPlayer _swordSwitch;
	[Export] private AudioStreamPlayer _spiritHurt;

	public override void _Ready()
	{
		if (_session == null || _music == null || _swordLaunch == null ||
			_swordHit == null || _swordDefeat == null || _controlEnter == null ||
			_swordReturn == null || _swordSwitch == null || _spiritHurt == null)
			throw new InvalidOperationException("AudioDirector requires a session and all audio players.");
		_session.SoundCueRequested += PlayCue;
		_music.Finished += () => _music.Play();
		_music.Play();
	}

	private void PlayCue(string cue) => (cue switch
	{
		"sword_launch" => _swordLaunch,
		"sword_hit" => _swordHit,
		"sword_defeat" => _swordDefeat,
		"control_enter" => _controlEnter,
		"sword_return" => _swordReturn,
		"sword_switch" => _swordSwitch,
		"spirit_hurt" => _spiritHurt,
		_ => null
	})?.Play();
}
