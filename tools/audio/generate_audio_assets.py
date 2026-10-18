"""Generate the original procedural audio shipped with Sword Tomb.

Uses only Python's standard library so the assets are reproducible without a DAW.
"""

from array import array
import math
from pathlib import Path
import random
import wave


ROOT = Path(__file__).resolve().parents[2]
SAMPLE_RATE = 22_050
TAU = math.tau


def clamp(value: float) -> float:
    return max(-1.0, min(1.0, value))


def write_wav(path: Path, channels: list[list[float]], target_peak: float = 0.92) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    peak = max(abs(sample) for channel in channels for sample in channel) or 1.0
    gain = target_peak / peak
    frames = array("h")
    for samples in zip(*channels):
        for sample in samples:
            frames.append(round(clamp(sample * gain) * 32_767))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(len(channels))
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(frames.tobytes())
    print(f"wrote {path.relative_to(ROOT)} ({len(channels[0]) / SAMPLE_RATE:.2f}s, peak gain {gain:.3f})")


def periodic_frequency(frequency: float, duration: float) -> float:
    """Quantize to a whole number of cycles so sustained tones loop cleanly."""
    return round(frequency * duration) / duration


def make_music() -> None:
    duration = 36.0
    count = round(duration * SAMPLE_RATE)
    left = [0.0] * count
    right = [0.0] * count
    drones = [
        (55.00, 82.41, 110.00),   # A / E: the tomb array breathing
        (49.00, 73.42, 98.00),    # G / D: rust mist gathering
        (55.00, 82.41, 123.47),   # A / E / B: open, unresolved
        (49.00, 73.42, 110.00),   # G / D / A: returns without resolving
    ]
    # D-minor pentatonic fragments: remembered sword names, never a full melody.
    plucks = [(2.2, 293.66), (5.1, 349.23), (8.0, 440.00),
              (11.4, 392.00), (14.7, 293.66), (18.8, 261.63),
              (22.2, 220.00), (25.0, 261.63), (29.1, 349.23), (32.2, 293.66)]
    sword_chimes = [(7.1, 587.33), (16.5, 440.00), (27.0, 523.25)]

    for index in range(count):
        t = index / SAMPLE_RATE
        drone = drones[min(int(t // 9), 3)]
        local = t % 9
        section_fade = min(1.0, local / 1.4, (9.0 - local) / 1.4)
        slow_breath = 0.68 + 0.32 * math.sin(TAU * t / 18) ** 2
        drone_l = drone_r = 0.0
        for voice, base in enumerate(drone):
            frequency = periodic_frequency(base, duration)
            phase = voice * 0.81
            drone_l += math.sin(TAU * frequency * t + phase) + 0.13 * math.sin(TAU * frequency * 2 * t)
            drone_r += math.sin(TAU * frequency * t + phase + 0.04) + 0.13 * math.sin(TAU * frequency * 2 * t + 0.09)
        drone_l *= 0.043 * section_fade * slow_breath
        drone_r *= 0.043 * section_fade * slow_breath

        mist_l = sum(math.sin(TAU * periodic_frequency(freq, duration) * t + phase)
                     for freq, phase in ((0.22, 0.2), (0.42, 1.7), (0.69, 2.4), (1.08, 4.1))) * 0.008
        mist_r = sum(math.sin(TAU * periodic_frequency(freq, duration) * t + phase)
                     for freq, phase in ((0.25, 2.2), (0.47, 0.8), (0.78, 3.4), (1.14, 5.0))) * 0.008

        pluck_l = pluck_r = 0.0
        for start, frequency in plucks:
            age = t - start
            if 0 <= age < 2.8:
                envelope = (1 - math.exp(-age * 42)) * math.exp(-age * 2.25)
                bend = age * (1 - math.exp(-age * 9)) * -0.9
                tone = math.sin(TAU * frequency * age + bend) + 0.32 * math.sin(TAU * frequency * 2.01 * age)
                pluck_l += tone * envelope * 0.047
                pluck_r += tone * envelope * 0.041

        metal_l = metal_r = 0.0
        for start, frequency in sword_chimes:
            age = t - start
            if 0 <= age < 4.2:
                envelope = (1 - math.exp(-age * 80)) * math.exp(-age * 1.35)
                tone = (math.sin(TAU * frequency * age) +
                        0.42 * math.sin(TAU * frequency * 2.71 * age) +
                        0.24 * math.sin(TAU * frequency * 4.08 * age))
                metal_l += tone * envelope * 0.035
                metal_r += tone * envelope * 0.041

        # A quiet nine-second pulse suggests the damaged array without becoming percussion.
        pulse_age = local
        rust_pulse = math.sin(TAU * 47 * pulse_age) * math.exp(-pulse_age * 4.8) * 0.035
        loop_fade = min(1.0, t / 0.18, (duration - t) / 0.18)
        left[index] = (drone_l + mist_l + pluck_l + metal_l + rust_pulse) * loop_fade
        right[index] = (drone_r + mist_r + pluck_r + metal_r + rust_pulse * 0.72) * loop_fade

    assert abs(left[0] - left[-1]) < 0.001 and abs(right[0] - right[-1]) < 0.001
    write_wav(ROOT / "assets/audio/music/bgm_sword_tomb_rust_mist_loop.wav", [left, right], target_peak=0.72)


def make_sword_launch() -> None:
    duration = 0.38
    count = round(duration * SAMPLE_RATE)
    rng = random.Random(0x5A01)
    mono = []
    air = 0.0
    phase = 0.0
    for index in range(count):
        t = index / SAMPLE_RATE
        frequency = 310 + 1450 * math.exp(-t * 7)
        phase += TAU * frequency / SAMPLE_RATE
        air = air * 0.72 + (rng.random() * 2 - 1) * 0.28
        whoosh = air * math.sin(math.pi * min(t / 0.23, 1)) * math.exp(-t * 5.2)
        steel = (math.sin(phase) + 0.35 * math.sin(phase * 2.73)) * math.exp(-t * 11)
        mono.append((whoosh * 0.7 + steel * 0.34) * min(1.0, (duration - t) / 0.045))
    write_wav(ROOT / "assets/audio/sfx/combat/sfx_sword_launch.wav", [mono])


def make_sword_qi() -> None:
    duration = 0.52
    count = round(duration * SAMPLE_RATE)
    rng = random.Random(0x5101)
    mono = []
    noise = 0.0
    phase = 0.0
    for index in range(count):
        t = index / SAMPLE_RATE
        frequency = 240 + 960 * (t / duration) ** 1.35
        phase += TAU * frequency / SAMPLE_RATE
        noise = noise * 0.86 + (rng.random() * 2 - 1) * 0.14
        envelope = math.sin(math.pi * t / duration) ** 0.7
        shimmer = math.sin(phase) + 0.28 * math.sin(phase * 2.02)
        mono.append((shimmer * 0.46 + noise * 0.7) * envelope * min(1.0, (duration - t) / 0.035))
    write_wav(ROOT / "assets/audio/sfx/combat/sfx_sword_qi_cast.wav", [mono])


def make_sword_hit() -> None:
    duration = 0.3
    count = round(duration * SAMPLE_RATE)
    rng = random.Random(0x517)
    mono = []
    for index in range(count):
        t = index / SAMPLE_RATE
        clang = sum(weight * math.sin(TAU * 720 * ratio * t)
                    for weight, ratio in ((1, 1), (.55, 1.53), (.32, 2.71))) * math.exp(-t * 15)
        spark = (rng.random() * 2 - 1) * math.exp(-t * 42)
        mono.append((clang * 0.65 + spark * 0.35) * min(1.0, (duration - t) / 0.035))
    write_wav(ROOT / "assets/audio/sfx/combat/sfx_sword_hit.wav", [mono])


def make_sword_defeat() -> None:
    duration = 0.62
    count = round(duration * SAMPLE_RATE)
    rng = random.Random(0xD3F3A7)
    mono = []
    dust = 0.0
    for index in range(count):
        t = index / SAMPLE_RATE
        dust = dust * 0.8 + (rng.random() * 2 - 1) * 0.2
        break_tone = sum(weight * math.sin(TAU * 510 * ratio * t)
                         for weight, ratio in ((1, 1), (.48, 1.91), (.3, 3.17))) * math.exp(-t * 9)
        dissolve = dust * math.sin(TAU * (1300 - 900 * t) * t) * math.exp(-t * 4.5)
        mono.append((break_tone * 0.55 + dissolve * 0.65) * min(1.0, (duration - t) / 0.07))
    write_wav(ROOT / "assets/audio/sfx/combat/sfx_sword_defeat.wav", [mono])


def make_spirit_hurt() -> None:
    duration = 0.58
    count = round(duration * SAMPLE_RATE)
    rng = random.Random(0x5A17)
    mono = []
    fracture = 0.0
    for index in range(count):
        t = index / SAMPLE_RATE
        fracture = fracture * 0.88 + (rng.random() * 2 - 1) * 0.12
        body = math.sin(TAU * (118 - 55 * t) * t) * math.exp(-t * 11)
        spirit = math.sin(TAU * 610 * t + 10 * math.sin(TAU * 19 * t)) * math.exp(-t * 7.5)
        mono.append((body * 0.62 + fracture * math.exp(-t * 6) * 0.55 + spirit * 0.3) * min(1.0, (duration - t) / 0.07))
    write_wav(ROOT / "assets/audio/sfx/combat/sfx_spirit_hurt.wav", [mono])


def make_sword_state_cues() -> None:
    cues = {
        "sfx_spirit_enter_sword.wav": (0.58, 260, 940, 1),
        "sfx_sword_return.wav": (0.48, 980, 210, -1),
        "sfx_sword_frame_switch.wav": (0.32, 370, 555, 1),
    }
    for filename, (duration, start_frequency, end_frequency, direction) in cues.items():
        count = round(duration * SAMPLE_RATE)
        mono = []
        phase = 0.0
        for index in range(count):
            t = index / SAMPLE_RATE
            progress = t / duration
            frequency = start_frequency + (end_frequency - start_frequency) * progress
            phase += TAU * frequency / SAMPLE_RATE
            envelope = math.sin(math.pi * progress) ** 0.65
            chime_age = t - duration * 0.58
            chime = 0.0 if chime_age < 0 else (
                math.sin(TAU * (620 if direction > 0 else 410) * chime_age) +
                0.38 * math.sin(TAU * (1680 if direction > 0 else 1130) * chime_age)
            ) * math.exp(-chime_age * 15)
            mono.append((math.sin(phase) * envelope * 0.58 + chime * 0.42) * min(1.0, (duration - t) / 0.04))
        write_wav(ROOT / "assets/audio/sfx/sword" / filename, [mono])


if __name__ == "__main__":
    make_music()
    make_sword_launch()
    make_sword_qi()
    make_sword_hit()
    make_sword_defeat()
    make_spirit_hurt()
    make_sword_state_cues()
