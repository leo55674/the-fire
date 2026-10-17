using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Owns only the Keeper's scene-authored visual. Movement and combat state live
/// in GameSession and are presented here from the session state.
/// </summary>
public partial class PlayerPresentation : AnimatedSprite2D
{
	private const float AfterimageInterval = .04f;
	private const float AfterimageLifetime = .2f;
	private const float SpecialIdleMinDelay = 4;
	private const float SpecialIdleMaxDelay = 8;
	[Export] private Color _hurtTint = new("ffdddd");
	[Export] private Color _normalTint = Colors.White;
	private readonly List<Afterimage> _afterimages = new();
	private readonly List<StringName> _specialIdleAnimations = new();
	private readonly RandomNumberGenerator _random = new();
	private float _afterimageCooldown;
	private float _specialIdleDelay;
	private bool _specialIdlePlaying;
	private bool _wasStanding;

	public override void _Ready()
	{
		_random.Randomize();
		foreach (var animation in SpriteFrames.GetAnimationNames())
			if (animation.ToString().StartsWith("idle_", StringComparison.Ordinal))
				_specialIdleAnimations.Add(animation);
		AnimationFinished += OnAnimationFinished;
	}

	public void Present(Vector2 screenPosition, bool hidden, bool hurt, bool moving, bool dashing, bool facingLeft = false)
	{
		Visible = !hidden;
		Position = screenPosition;
		FlipH = facingLeft;
		Modulate = hurt ? _hurtTint : _normalTint;
		if (hurt || moving)
		{
			_specialIdlePlaying = false;
			_wasStanding = false;
			PlayIfChanged(hurt ? "hurt" : "walk");
		}
		else if (!_specialIdlePlaying)
		{
			if (!_wasStanding)
			{
				_wasStanding = true;
				ScheduleSpecialIdle();
				PlayIfChanged("idle");
			}
			else if ((_specialIdleDelay -= (float)GetProcessDeltaTime()) <= 0)
				PlayRandomSpecialIdle();
		}
		UpdateAfterimages(dashing);
	}

	private void PlayIfChanged(StringName animation)
	{
		if (Animation != animation) Play(animation);
	}

	private void PlayRandomSpecialIdle()
	{
		if (_specialIdleAnimations.Count == 0) { ScheduleSpecialIdle(); return; }
		_specialIdlePlaying = true;
		Play(_specialIdleAnimations[_random.RandiRange(0, _specialIdleAnimations.Count - 1)]);
	}

	private void OnAnimationFinished()
	{
		if (!_specialIdlePlaying) return;
		_specialIdlePlaying = false;
		ScheduleSpecialIdle();
		Play("idle");
	}

	private void ScheduleSpecialIdle() =>
		_specialIdleDelay = _random.RandfRange(SpecialIdleMinDelay, SpecialIdleMaxDelay);

	private void UpdateAfterimages(bool dashing)
	{
		var dt = (float)GetProcessDeltaTime();
		for (var i = _afterimages.Count - 1; i >= 0; i--)
		{
			var afterimage = _afterimages[i];
			afterimage.Life -= dt;
			if (afterimage.Life <= 0)
			{
				afterimage.Sprite.QueueFree();
				_afterimages.RemoveAt(i);
				continue;
			}
			afterimage.Sprite.Modulate = new Color(.35f, .85f, 1, .38f * afterimage.Life / AfterimageLifetime);
		}

		_afterimageCooldown -= dt;
		if (!dashing) { _afterimageCooldown = 0; return; }
		if (_afterimageCooldown > 0 || SpriteFrames == null) return;
		_afterimageCooldown = AfterimageInterval;
		var texture = SpriteFrames.GetFrameTexture(Animation, Frame);
		if (texture == null) return;
		var sprite = new Sprite2D
		{
			Texture = texture, Position = Position, Scale = Scale, Offset = Offset,
			FlipH = FlipH, Centered = Centered, ZIndex = ZIndex - 1,
			Modulate = new Color(.35f, .85f, 1, .38f)
		};
		GetParent().AddChild(sprite);
		_afterimages.Add(new Afterimage(sprite));
	}

	private sealed class Afterimage
	{
		public readonly Sprite2D Sprite;
		public float Life = AfterimageLifetime;
		public Afterimage(Sprite2D sprite) => Sprite = sprite;
	}
}
