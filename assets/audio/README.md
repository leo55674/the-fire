# Audio assets

Original procedural audio for **The Fire / 引魂火**. No external samples or copyrighted recordings are used.

## Runtime files

- `music/bgm_embers_in_snow_loop.wav` — 32-second stereo dark-fantasy ambience loop.
- `sfx/combat/sfx_axe_swing.wav` — axe swing with a short impact body.
- `sfx/combat/sfx_firebolt_cast.wav` — rising ember/fire projectile cast.
- `sfx/combat/sfx_player_hurt.wav` — low impact and ember crackle, without a vocal performance.

All files are 22.05 kHz, signed 16-bit PCM WAV. They are generated deterministically by
`tools/audio/generate_audio_assets.py`; rerun that script to reproduce them.

## Mix intent

The music deliberately leaves the upper-mid range sparse so combat cues remain readable. Runtime
gain is set in `Main.tscn`: music at -15 dB, attacks at -5 dB, and player hurt at -3 dB.
