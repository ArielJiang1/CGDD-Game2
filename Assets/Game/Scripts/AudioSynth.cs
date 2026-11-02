// @role Every sound in the game, generated in code (hum, drones, chimes, footsteps, voices, the climax chord)
using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Every sound in the game is made here, in code - no audio files needed.
    /// Loops are built with a whole number of cycles (or a cross-fade), so they repeat without a click.
    /// Audio identity (from our style sheet): fluorescent buzzing, ventilation hum, footsteps in quiet halls,
    /// sparse repetitive drones, polite chimes, sudden silence when a rule is broken.
    /// </summary>
    public static class AudioSynth
    {
        const int SR = 22050;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static System.Random rng = new System.Random(1952);

        static AudioClip Make(string name, float[] data)
        {
            AudioClip c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            cache[name] = c;
            return c;
        }

        static bool Cached(string name, out AudioClip c) { return cache.TryGetValue(name, out c) && c != null; }
        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float TwoPi { get { return Mathf.PI * 2f; } }

        /// <summary>Joins the end of a longer buffer onto its start, so it loops seamlessly.</summary>
        static float[] CrossfadeLoop(float[] raw, int length, int fade)
        {
            float[] o = new float[length];
            for (int i = 0; i < length; i++) o[i] = raw[i];
            for (int i = 0; i < fade; i++)
            {
                float a = i / (float)fade;
                o[i] = raw[i] * a + raw[length + i] * (1f - a);
            }
            return o;
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        // ------------------------------------------------------------------ loops
        /// <summary>Fluorescent tube: 120 Hz mains hum with harmonics and a faint crackle. 2 s loop.</summary>
        public static AudioClip FluorescentHum()
        {
            AudioClip c; if (Cached("hum", out c)) return c;
            int n = SR * 2;
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float s = Mathf.Sin(TwoPi * 120f * t) * 0.5f + Mathf.Sin(TwoPi * 240f * t) * 0.3f + Mathf.Sin(TwoPi * 360f * t) * 0.12f
                        + Mathf.Sin(TwoPi * 1080f * t) * 0.04f;
                d[i] = s + Noise() * 0.035f;
            }
            Normalize(d, 0.5f);
            return Make("hum", d);
        }

        /// <summary>Ventilation: soft low-passed noise. 4 s loop.</summary>
        public static AudioClip Ventilation()
        {
            AudioClip c; if (Cached("vent", out c)) return c;
            int n = SR * 4, f = SR / 4;
            float[] raw = new float[n + f];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                lp += (Noise() - lp) * 0.06f;
                lp2 += (lp - lp2) * 0.08f;
                raw[i] = lp2;
            }
            float[] d = CrossfadeLoop(raw, n, f);
            Normalize(d, 0.6f);
            return Make("vent", d);
        }

        /// <summary>Sparse institutional drone (A1 + E2 + a slightly sour A2), slow swell. 8 s loop.</summary>
        public static AudioClip Drone()
        {
            AudioClip c; if (Cached("drone", out c)) return c;
            int n = SR * 8;
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float swell = 0.65f + 0.35f * Mathf.Sin(TwoPi * t / 8f);
                float s = Mathf.Sin(TwoPi * 55f * t) * 0.55f
                        + Mathf.Sin(TwoPi * 82.5f * t) * 0.3f * (0.5f + 0.5f * Mathf.Sin(TwoPi * t / 4f))
                        + Mathf.Sin(TwoPi * 110.375f * t) * 0.18f
                        + Mathf.Sin(TwoPi * 164.875f * t) * 0.07f;
                d[i] = s * swell;
            }
            Normalize(d, 0.55f);
            return Make("drone", d);
        }

        /// <summary>Dissonant beating layer that fades in with danger. 4 s loop.</summary>
        public static AudioClip DangerLayer()
        {
            AudioClip c; if (Cached("danger", out c)) return c;
            int n = SR * 4;
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float trem = 0.6f + 0.4f * Mathf.Sin(TwoPi * 6f * t);
                d[i] = (Mathf.Sin(TwoPi * 233.25f * t) + Mathf.Sin(TwoPi * 246.75f * t) * 0.9f + Mathf.Sin(TwoPi * 58.25f * t) * 0.7f) * trem;
            }
            Normalize(d, 0.55f);
            return Make("danger", d);
        }

        /// <summary>Someone humming an off-key anthem. 7.5 s loop.</summary>
        public static AudioClip HumMelody()
        {
            AudioClip c; if (Cached("melody", out c)) return c;
            float[] notes = { 261.6f, 329.6f, 392f, 329.6f, 349.2f, 293.7f, 261.6f, 0f, 293.7f, 311f, 0f };
            float noteLen = 0.68f;
            int n = (int)(SR * noteLen * notes.Length);
            float[] d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                int k = Mathf.Min(notes.Length - 1, (int)(t / noteLen));
                float local = t - k * noteLen;
                float f = notes[k];
                if (f <= 0f) { d[i] = 0f; continue; }
                f *= (k % 3 == 1) ? 0.985f : 1f;                                   // a little flat, sometimes
                f *= 1f + 0.012f * Mathf.Sin(TwoPi * 5.2f * t);                    // vibrato
                phase += TwoPi * f / SR;
                float env = Mathf.Clamp01(local / 0.08f) * Mathf.Clamp01((noteLen - local) / 0.12f);
                d[i] = (Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f) + 0.1f * Mathf.Sin(phase * 3f)) * env;
            }
            Normalize(d, 0.6f);
            return Make("melody", d);
        }

        /// <summary>Slow breathing of sleeping students. 4 s loop.</summary>
        public static AudioClip Breathing()
        {
            AudioClip c; if (Cached("breath", out c)) return c;
            int n = SR * 4;
            float[] d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                lp += (Noise() - lp) * 0.12f;
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * t / 4f), 2f);
                d[i] = lp * env;
            }
            Normalize(d, 0.5f);
            return Make("breath", d);
        }

        // ------------------------------------------------------------------ one-shots
        /// <summary>Two-tone PA chime ("ding-dong").</summary>
        public static AudioClip Chime()
        {
            AudioClip c; if (Cached("chime", out c)) return c;
            int n = (int)(SR * 1.8f);
            float[] d = new float[n];
            Bell(d, 659.25f, 0f, 1.2f);
            Bell(d, 523.25f, 0.55f, 1.25f);
            Normalize(d, 0.6f);
            return Make("chime", d);
        }

        /// <summary>Bright confirmation chime (message sent, door open...).</summary>
        public static AudioClip Confirm()
        {
            AudioClip c; if (Cached("confirm", out c)) return c;
            int n = (int)(SR * 0.9f);
            float[] d = new float[n];
            Bell(d, 1046.5f, 0f, 0.6f);
            Bell(d, 1568f, 0.12f, 0.7f);
            Normalize(d, 0.45f);
            return Make("confirm", d);
        }

        static void Bell(float[] d, float f, float start, float len)
        {
            int s0 = (int)(start * SR), s1 = Mathf.Min(d.Length, s0 + (int)(len * SR));
            for (int i = s0; i < s1; i++)
            {
                float t = (i - s0) / (float)SR;
                float env = Mathf.Exp(-3.2f * t) * Mathf.Clamp01(t / 0.004f);
                d[i] += (Mathf.Sin(TwoPi * f * t) + 0.35f * Mathf.Sin(TwoPi * f * 2.76f * t) * Mathf.Exp(-6f * t) + 0.2f * Mathf.Sin(TwoPi * f * 2f * t)) * env;
            }
        }

        static AudioClip[] steps;
        /// <summary>Three variations of a footstep on hard floor.</summary>
        public static AudioClip[] Steps()
        {
            if (steps != null && steps[0] != null) return steps;
            steps = new AudioClip[3];
            for (int v = 0; v < 3; v++)
            {
                int n = (int)(SR * 0.16f);
                float[] d = new float[n];
                float lp = 0f, cut = 0.18f + v * 0.05f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR;
                    lp += (Noise() - lp) * cut;
                    d[i] = lp * Mathf.Exp(-t * 38f) * 1.4f + Mathf.Sin(TwoPi * (68f + v * 9f) * t) * Mathf.Exp(-t * 28f) * 0.8f;
                }
                Normalize(d, 0.8f);
                steps[v] = Make("step" + v, d);
            }
            return steps;
        }

        /// <summary>A ring of keys.</summary>
        public static AudioClip Keys()
        {
            AudioClip c; if (Cached("keys", out c)) return c;
            int n = (int)(SR * 0.5f);
            float[] d = new float[n];
            float[] onsets = { 0f, 0.07f, 0.16f, 0.21f };
            for (int o = 0; o < onsets.Length; o++)
            {
                float f1 = 2600f + o * 530f, f2 = 4100f + o * 370f;
                int s0 = (int)(onsets[o] * SR);
                for (int i = s0; i < n; i++)
                {
                    float t = (i - s0) / (float)SR;
                    float env = Mathf.Exp(-t * 22f);
                    d[i] += (Mathf.Sin(TwoPi * f1 * t) + 0.6f * Mathf.Sin(TwoPi * f2 * t) + Noise() * 0.3f) * env;
                }
            }
            Normalize(d, 0.45f);
            return Make("keys", d);
        }

        /// <summary>A creaking door.</summary>
        /// <summary>A page turned in an old book: a dry, papery rustle (band-passed noise, two quick swells).</summary>
        public static AudioClip Rustle()
        {
            AudioClip c; if (Cached("rustle", out c)) return c;
            int n = (int)(SR * 0.32f);
            float[] d = new float[n];
            float lp = 0f, hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float x = Noise();
                lp += (x - lp) * 0.35f;          // low-pass
                hp = lp - prev; prev = lp;        // then high-pass = papery band
                float env = Mathf.Exp(-Mathf.Pow((t - 0.06f) / 0.04f, 2f)) + 0.7f * Mathf.Exp(-Mathf.Pow((t - 0.19f) / 0.06f, 2f));
                d[i] = hp * env * (0.7f + 0.3f * Noise());
            }
            Normalize(d, 0.55f);
            return Make("rustle", d);
        }

        public static AudioClip Creak()
        {
            AudioClip c; if (Cached("creak", out c)) return c;
            int n = (int)(SR * 1.0f);
            float[] d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 170f + 130f * t + 25f * Mathf.Sin(TwoPi * 7f * t) + Noise() * 20f;
                phase += f / SR;
                float saw = (phase % 1f) * 2f - 1f;
                lp += (saw - lp) * 0.25f;
                d[i] = lp * Mathf.Sin(Mathf.PI * t) * (0.6f + 0.4f * Mathf.Sin(TwoPi * 13f * t));
            }
            Normalize(d, 0.5f);
            return Make("creak", d);
        }

        /// <summary>A pigeon's soft "hoo-HOOoo" (low, breathy - about as present as a footstep, never startling).</summary>
        public static AudioClip Coo()
        {
            AudioClip c; if (Cached("coo", out c)) return c;
            int n = (int)(SR * 1.15f);
            float[] d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                // two notes: a short low one, then a longer one that rises and falls
                float env, f;
                if (t < 0.28f) { env = Mathf.Sin(Mathf.PI * t / 0.28f); f = 300f; }
                else if (t < 0.38f) { env = 0f; f = 300f; }
                else { float u = (t - 0.38f) / 0.77f; env = Mathf.Sin(Mathf.PI * u) * (1f - 0.3f * u); f = 310f + 70f * Mathf.Sin(Mathf.PI * u); }
                f += 9f * Mathf.Sin(TwoPi * 23f * t);   // the throaty warble
                phase += f / SR;
                float tone = Mathf.Sin(TwoPi * phase) + 0.25f * Mathf.Sin(TwoPi * phase * 2f);
                d[i] = (tone * 0.85f + Noise() * 0.12f) * env * env;
            }
            Normalize(d, 0.45f);
            return Make("coo", d);
        }

        /// <summary>A few wing beats (soft noise bursts).</summary>
        public static AudioClip Flap()
        {
            AudioClip c; if (Cached("flap", out c)) return c;
            int n = (int)(SR * 0.7f);
            float[] d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float beat = Mathf.Repeat(t, 0.11f) / 0.11f;
                float env = Mathf.Exp(-beat * 6f) * (1f - t / 0.7f);
                lp += (Noise() - lp) * 0.35f;
                d[i] = lp * env;
            }
            Normalize(d, 0.5f);
            return Make("flap", d);
        }

        /// <summary>Low hit used when a warning is given.</summary>
        public static AudioClip Stinger()
        {
            AudioClip c; if (Cached("stinger", out c)) return c;
            int n = (int)(SR * 1.2f);
            float[] d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = 70f * Mathf.Exp(-t * 1.5f) + 32f;
                phase += TwoPi * f / SR;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 2.2f) * 1.2f
                     + Noise() * Mathf.Exp(-t * 18f) * 0.5f
                     + (Mathf.Sin(TwoPi * 466f * t) + Mathf.Sin(TwoPi * 493f * t)) * 0.12f * Mathf.Exp(-t * 3f);
            }
            Normalize(d, 0.8f);
            return Make("stinger", d);
        }

        /// <summary>Phone vibrating in a drawer.</summary>
        public static AudioClip Vibrate()
        {
            AudioClip c; if (Cached("vibrate", out c)) return c;
            int n = (int)(SR * 0.35f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Sin(Mathf.PI * t / 0.35f);
                d[i] = Mathf.Sign(Mathf.Sin(TwoPi * 160f * t)) * 0.3f * env;
            }
            return Make("vibrate", d);
        }

        /// <summary>Typewriter tick.</summary>
        public static AudioClip Tick()
        {
            AudioClip c; if (Cached("tick", out c)) return c;
            int n = (int)(SR * 0.025f);
            float[] d = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float x = Noise();
                d[i] = (x - prev) * Mathf.Exp(-t * 260f);
                prev = x;
            }
            Normalize(d, 0.5f);
            return Make("tick", d);
        }

        /// <summary>A short "voice" syllable. Pitch it per speaker (Animal-Crossing style mumble).</summary>
        public static AudioClip Blip()
        {
            AudioClip c; if (Cached("blip", out c)) return c;
            int n = (int)(SR * 0.085f);
            float[] d = new float[n];
            float f0 = 150f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float s = 0f;
                for (int h = 1; h <= 12; h++)
                {
                    float fh = f0 * h;
                    float w = Mathf.Exp(-Mathf.Pow((fh - 700f) / 260f, 2f)) + 0.6f * Mathf.Exp(-Mathf.Pow((fh - 1200f) / 300f, 2f)) + 0.25f / h;
                    s += Mathf.Sin(TwoPi * fh * t) * w;
                }
                d[i] = s * Mathf.Sin(Mathf.PI * t / 0.085f);
            }
            Normalize(d, 0.5f);
            return Make("blip", d);
        }

        /// <summary>
        /// Many people talking at a normal volume behind a door (muffled). 6 s seamless loop.
        /// The one sound in the game that is not polite, not monitored, not quiet.
        /// </summary>
        public static AudioClip Murmur()
        {
            AudioClip c; if (Cached("murmur", out c)) return c;
            int n = SR * 6, fade = SR / 2;
            float[] raw = new float[n + fade];
            System.Random r = new System.Random(304);
            for (int v = 0; v < 7; v++)
            {
                float f0 = 105f + (float)r.NextDouble() * 170f;
                float pan = 0.6f + (float)r.NextDouble() * 0.6f;
                int i = (int)(r.NextDouble() * SR * 0.5);
                while (i < raw.Length)
                {
                    // one syllable, then a short gap (or a longer pause between phrases)
                    int len = (int)(SR * (0.09 + r.NextDouble() * 0.16));
                    float fa = 450f + (float)r.NextDouble() * 500f, fb = 1000f + (float)r.NextDouble() * 900f;
                    float bend = 1f + ((float)r.NextDouble() - 0.5f) * 0.25f;
                    for (int k = 0; k < len && i + k < raw.Length; k++)
                    {
                        float t = k / (float)SR;
                        float env = Mathf.Sin(Mathf.PI * k / len);
                        float f = f0 * Mathf.Lerp(1f, bend, k / (float)len);
                        float sum = 0f;
                        for (int h = 1; h <= 8; h++)
                        {
                            float fh = f * h;
                            float w = Mathf.Exp(-Mathf.Pow((fh - fa) / 220f, 2f)) + 0.5f * Mathf.Exp(-Mathf.Pow((fh - fb) / 280f, 2f)) + 0.15f / h;
                            sum += Mathf.Sin(TwoPi * fh * t) * w;
                        }
                        raw[i + k] += sum * env * 0.12f * pan;
                    }
                    i += len + (int)(SR * (r.NextDouble() < 0.18 ? 0.35 + r.NextDouble() * 0.6 : 0.03 + r.NextDouble() * 0.08));
                }
            }
            // muffled: through a door
            float y = 0f;
            for (int i = 0; i < raw.Length; i++) { y += (raw[i] - y) * 0.18f; raw[i] = y + Noise() * 0.004f; }
            float[] d = CrossfadeLoop(raw, n, fade);
            Normalize(d, 0.5f);
            return Make("murmur", d);
        }

        // ------------------------------------------------------------------ the climax (every phone lights up)
        static float Bellish(float f, float t)
        {
            // a clear bell: a few inharmonic partials with their own decays
            return Mathf.Sin(TwoPi * f * t) * Mathf.Exp(-t * 2.2f)
                 + 0.45f * Mathf.Sin(TwoPi * f * 2.76f * t) * Mathf.Exp(-t * 4.5f)
                 + 0.22f * Mathf.Sin(TwoPi * f * 5.4f * t) * Mathf.Exp(-t * 8f)
                 + 0.3f * Mathf.Sin(TwoPi * f * 2f * t) * Mathf.Exp(-t * 3f);
        }

        /// <summary>One phone waking up: a soft bright bell (play at different pitches, one per phone).</summary>
        public static AudioClip Ping()
        {
            AudioClip c; if (Cached("ping", out c)) return c;
            int n = (int)(SR * 1.6f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float atk = Mathf.Clamp01(t / 0.004f);
                d[i] = Bellish(660f, t) * atk;
            }
            Normalize(d, 0.6f);
            return Make("ping", d);
        }

        /// <summary>A rising swell under the lights coming on: a wide minor chord slowly opening up, with a cold shimmer on top.</summary>
        public static AudioClip Swell()
        {
            AudioClip c; if (Cached("swell", out c)) return c;
            float len = 6f;
            int n = (int)(SR * len);
            float[] d = new float[n];
            float[] chord = { 55f, 110f, 164.81f, 220f, 261.63f, 329.63f, 493.88f };   // A minor add9: open, a little sad
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float k = t / len;
                float env = k * k * (3f - 2f * k);               // smooth rise
                float s = 0f;
                for (int j = 0; j < chord.Length; j++)
                {
                    float f = chord[j] * (1f + 0.002f * Mathf.Sin(t * (0.7f + j * 0.13f)));   // slow chorus
                    float voice = Mathf.Sin(TwoPi * f * t) + 0.35f * Mathf.Sin(TwoPi * f * 2f * t) * k + 0.15f * Mathf.Sin(TwoPi * f * 3f * t) * k * k;
                    float enter = Mathf.Clamp01((k - j * 0.06f) * 3f);   // low notes first, then higher ones join
                    s += voice * enter / chord.Length;
                }
                // airy shimmer that opens up (filtered noise)
                lp += (Noise() - lp) * (0.02f + 0.25f * k);
                s += lp * 0.25f * k * k;
                s *= 1f + 0.08f * Mathf.Sin(TwoPi * 5.5f * t) * k;     // tremolo as it grows
                d[i] = s * env;
            }
            Normalize(d, 0.7f);
            return Make("swell", d);
        }

        /// <summary>
        /// The moment everyone is lit: freedom, but lonely. Open fifths and ninths with no third (neither happy nor sad),
        /// a slow swell instead of a hit, one distant bell answered a step lower, and wind in the long tail.
        /// </summary>
        public static AudioClip Bloom()
        {
            AudioClip c; if (Cached("bloom", out c)) return c;
            float lpw = 0f;
            float len = 9f;
            int n = (int)(SR * len);
            float[] d = new float[n];
            float[] chord = { 55f, 82.41f, 110f, 164.81f, 246.94f, 329.63f, 493.88f, 659.26f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float atk = Mathf.Clamp01(t / 0.9f);
                atk = atk * atk * (3f - 2f * atk);
                float s = 0f;
                for (int j = 0; j < chord.Length; j++)
                {
                    float f = chord[j];
                    float decay = Mathf.Exp(-t * (0.18f + j * 0.04f));
                    float beat = 1f + 0.003f * Mathf.Sin(t * (0.5f + j * 0.21f));     // slightly unsteady, like a room tone
                    s += (Mathf.Sin(TwoPi * f * beat * t) + 0.25f * Mathf.Sin(TwoPi * f * 2.003f * t)) * decay * (j < 2 ? 1.3f : 1f);
                }
                s /= chord.Length;
                // one far bell (E5)... answered a step lower (D5), quieter: the question nobody answers
                if (t > 0.6f) s += 0.32f * Bellish(659.26f, t - 0.6f);
                if (t > 2.4f) s += 0.2f * Bellish(587.33f, t - 2.4f);
                lpw += (Noise() - lpw) * 0.015f;                     // wind in the tail
                s += lpw * 0.5f * Mathf.Clamp01((t - 1.5f) / 3f) * Mathf.Exp(-Mathf.Max(0f, t - 6f) * 0.6f);
                d[i] = s * atk;
            }
            Normalize(d, 0.85f);
            return Make("bloom", d);
        }

        /// <summary>Screen flash click.</summary>
        public static AudioClip Click()
        {
            AudioClip c; if (Cached("click", out c)) return c;
            int n = (int)(SR * 0.06f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                d[i] = (Mathf.Sin(TwoPi * 2200f * t) * 0.6f + Noise() * 0.4f) * Mathf.Exp(-t * 90f);
            }
            Normalize(d, 0.4f);
            return Make("click", d);
        }

        /// <summary>Glitchy PA - the system stuttering.</summary>
        public static AudioClip Glitch()
        {
            AudioClip c; if (Cached("glitch", out c)) return c;
            int n = (int)(SR * 0.8f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                bool on = ((int)(t * 23f)) % 3 != 0;
                d[i] = on ? (Mathf.Sign(Mathf.Sin(TwoPi * (300f + 900f * Mathf.Repeat(t * 7f, 1f)) * t)) * 0.4f + Noise() * 0.3f) * (1f - t / 0.8f) : 0f;
            }
            Normalize(d, 0.45f);
            return Make("glitch", d);
        }
    }
}
