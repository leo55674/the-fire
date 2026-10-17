# Audio assets

Original procedural audio for **Sword Tomb / 剑冢**. No external samples or copyrighted recordings are used.

## Runtime files

- `music/bgm_sword_tomb_rust_mist_loop.wav` — 36-second stereo sword-tomb ambience loop. Low array drones, sparse pentatonic memory fragments, distant blade chimes, and a restrained rust pulse bridge the current cold-night landscape with the new flying-sword setting.
- `sfx/combat/sfx_sword_launch.wav` — fast blade launch with a restrained steel edge.
- `sfx/combat/sfx_sword_qi_cast.wav` — airy rising sword-qi cast.
- `sfx/combat/sfx_sword_hit.wav` — compact blade impact that stays readable during rapid control.
- `sfx/combat/sfx_sword_defeat.wav` — metallic break followed by dissipating sword-wraith noise.
- `sfx/combat/sfx_spirit_hurt.wav` — low body impact and fractured spirit resonance, without a vocal performance.
- `sfx/sword/sfx_spirit_enter_sword.wav` — rising consciousness transfer cue.
- `sfx/sword/sfx_sword_return.wav` — descending return sweep and sheath chime.
- `sfx/sword/sfx_sword_frame_switch.wav` — short dual-tone sword-frame switch confirmation.

All files are 22.05 kHz, signed 16-bit PCM WAV. They are generated deterministically by
`tools/audio/generate_audio_assets.py`; rerun that script to reproduce them.

## Mix intent

The music deliberately leaves the upper-mid range sparse so sword-control and combat cues remain readable. Runtime
gain is set in `scenes/Main.tscn`: music at -15 dB, combat cues at -5 to -1 dB, and sword-state cues at -6 to -4 dB.
