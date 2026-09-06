using System.Collections.Generic;
using UnityEngine;

namespace PunchcardOperator
{
    /// <summary>Procedurally synthesised interaction sounds - mechanical transients
    /// generated at startup. No external audio files, so nothing to licence.
    /// A small AudioSource pool lets reader ticks, steps and turns overlap without
    /// cutting each other; each cue gets a little random pitch drift so repeated
    /// hits don't sound identical. Volume follows the saved SFX + master settings.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        private struct Def
        {
            public float freq, seconds, noiseMix, decay, attack, sweep, jitter, gain;
        }

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _jitter = new Dictionary<string, float>();
        private readonly Dictionary<string, (string cue, float delay, float pitch)> _layer = new Dictionary<string, (string, float, float)>();
        private AudioSource[] _pool;
        private int _next;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _pool = new AudioSource[6];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = gameObject.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
                _pool[i].spatialBlend = 0f;
            }

            //   name       freq   secs  noise decay attack sweep jitter gain
            Add("punch",    150f, 0.11f, 0.80f, 34f, 0.002f, -0.35f, 0.06f, 0.95f);
            Add("chad",    2600f, 0.045f,0.75f, 70f, 0.000f,  0.20f, 0.16f, 0.5f);
            Add("patch",    300f, 0.10f, 0.45f, 26f, 0.010f, -0.10f, 0.06f, 0.8f);
            Add("shift",    500f, 0.08f, 0.30f, 24f, 0.004f,  0.30f, 0.08f, 0.7f);
            Add("swap",     420f, 0.12f, 0.28f, 18f, 0.004f, -0.25f, 0.05f, 0.7f);
            Add("flip",     180f, 0.17f, 0.35f, 12f, 0.002f,  0.60f, 0.05f, 0.85f);
            Add("copy",     620f, 0.09f, 0.18f, 22f, 0.004f,  0.15f, 0.08f, 0.7f);
            Add("load",      85f, 0.24f, 0.55f,  9f, 0.020f,  0.08f, 0.03f, 0.95f);
            Add("reader",  1150f, 0.032f,0.35f, 78f, 0.000f,  0.00f, 0.10f, 0.45f);
            Add("step",     640f, 0.045f,0.15f, 48f, 0.001f, -0.15f, 0.10f, 0.55f);
            Add("turn",     880f, 0.055f,0.20f, 40f, 0.001f,  0.35f, 0.10f, 0.55f);
            Add("execute",  100f, 0.34f, 0.30f,  6f, 0.030f,  0.25f, 0.02f, 0.9f);
            Add("ui",       960f, 0.030f,0.06f, 55f, 0.000f,  0.00f, 0.06f, 0.4f);
            Add("error",    110f, 0.26f, 0.45f,  8f, 0.006f, -0.30f, 0.03f, 0.85f);

            // chords (root+major third+fifth), noiseMix 0 -> BakeChord path
            BakeChord("success", 523.25f, 0.55f, 3.5f);
            BakeChord("certified", 261.63f, 1.1f, 2.2f);
            _jitter["success"] = 0.01f;
            _jitter["certified"] = 0.01f;

            // auto-layered cues: a punch flings a chad; a flip clacks twice
            _layer["punch"] = ("chad", 0.045f, 1.3f);
            _layer["flip"]  = ("swap", 0.10f, 0.8f);
        }

        private void Add(string name, float freq, float seconds, float noiseMix, float decay,
                         float attack, float sweep, float jitter, float gain)
        {
            _clips[name] = Bake(name, new Def
            {
                freq = freq, seconds = seconds, noiseMix = noiseMix, decay = decay,
                attack = attack, sweep = sweep, jitter = jitter, gain = gain
            });
            _jitter[name] = jitter;
        }

        private static AudioClip Bake(string name, Def d)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, Mathf.RoundToInt(rate * d.seconds));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            int atkSamples = Mathf.Max(1, Mathf.RoundToInt(rate * d.attack));

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float prog = (float)i / n;
                float env = Mathf.Exp(-d.decay * t);
                if (i < atkSamples) env *= (float)i / atkSamples;             // soft attack
                float f = d.freq * (1f + d.sweep * prog);                     // pitch sweep
                float tone = Mathf.Sin(2f * Mathf.PI * f * t)
                           + 0.30f * Mathf.Sin(2f * Mathf.PI * f * 2f * t)
                           + 0.12f * Mathf.Sin(2f * Mathf.PI * f * 3.01f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = Mathf.Clamp(Mathf.Lerp(tone, noise, d.noiseMix) * env * d.gain * 0.6f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip BakeChord(string name, float root, float seconds, float decay)
        {
            const int rate = 44100;
            int n = Mathf.RoundToInt(rate * seconds);
            var data = new float[n];
            float[] partials = { root, root * 1.25f, root * 1.5f, root * 2f };
            int atk = Mathf.RoundToInt(rate * 0.01f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-decay * t);
                if (i < atk) env *= (float)i / atk;
                float s = 0f;
                for (int p = 0; p < partials.Length; p++)
                    s += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * (1f / (p + 1.5f));
                data[i] = Mathf.Clamp(s * env * 0.4f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            _clips[name] = clip;
            return clip;
        }

        public static void Play(string name, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayInternal(name, pitch, true);
        }

        private void PlayInternal(string name, float pitch, bool allowLayer)
        {
            if (!_clips.TryGetValue(name, out var clip)) return;
            float j = _jitter.TryGetValue(name, out var jj) ? jj : 0f;
            float drift = 1f + Random.Range(-j, j);

            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.pitch = Mathf.Clamp(pitch * drift, 0.35f, 3f);
            float vol = Mathf.Clamp01(SaveSystem.Data.sfxVolume) * Mathf.Clamp01(SaveSystem.Data.masterVolume);
            src.PlayOneShot(clip, vol);

            if (allowLayer && _layer.TryGetValue(name, out var l))
                StartCoroutine(DelayedLayer(l.cue, l.delay, l.pitch));
        }

        private System.Collections.IEnumerator DelayedLayer(string cue, float delay, float pitch)
        {
            yield return new WaitForSecondsRealtime(delay);
            PlayInternal(cue, pitch, false);
        }
    }
}
