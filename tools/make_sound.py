"""Synthesises the Laser Gun's firing sound (original, made from scratch): a sharp electric crack,
then a fast falling 'zap' with a buzzy overtone and a short echo. Writes sound/fire-original.wav (44.1 kHz, 16-bit mono).
The mod plays fire.wav from its folder, so any other WAV with that name can replace it.
Run:  python tools/make_sound.py"""
import math, os, random, struct, wave

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "sound", "fire-original.wav")
SR = 44100

def synth(dur=0.42, seed=3):
    rnd = random.Random(seed)
    n = int(SR * dur)
    out = [0.0] * n
    ph = ph2 = ph3 = 0.0
    lp = 0.0
    for i in range(n):
        t = i / SR
        # falling pitch: 3200 Hz -> ~280 Hz, fast at first
        f = 280 + 2920 * math.exp(-t * 22)
        ph += 2 * math.pi * f / SR
        ph2 += 2 * math.pi * (f * 1.503) / SR
        ph3 += 2 * math.pi * (f * 0.5) / SR
        fm = math.sin(ph + 2.2 * math.sin(ph2))          # buzzy FM body
        sq = 1.0 if math.sin(ph3) > 0 else -1.0           # sub 'thump'
        body = 0.62 * fm + 0.18 * sq * math.exp(-t * 30)
        env = (1 - math.exp(-t * 2500)) * math.exp(-t * 9.5)
        # crack: white noise burst, high-passed by differencing
        nz = rnd.uniform(-1, 1)
        crack = (nz - lp) * math.exp(-t * 140)
        lp = nz
        out[i] = body * env + 0.55 * crack
    # short slap-back echo
    d = int(SR * 0.055)
    for i in range(n - 1, d - 1, -1):
        out[i] += 0.28 * out[i - d]
    peak = max(abs(v) for v in out) or 1
    return [v / peak * 0.92 for v in out]

def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    s = synth()
    with wave.open(OUT, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(v * 32767)) for v in s))
    print("wrote", OUT)

if __name__ == "__main__":
    main()
