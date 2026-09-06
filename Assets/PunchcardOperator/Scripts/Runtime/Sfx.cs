using System.Collections.Generic;
using UnityEngine;

namespace PunchcardOperator
{
    /// <summary>Procedurally synthesised interaction sounds - short tone / noise bursts
    /// generated at runtime. No external audio files, so nothing to licence.
    /// Volume is driven by the saved SFX setting.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        private AudioSource _src;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;

            // name -> (freq Hz, seconds, noiseMix 0..1, decay)
            Bake("punch", 140, 0.10f, 0.85f, 40f);
            Bake("chad", 2200, 0.05f, 0.6f, 60f);
            Bake("patch", 320, 0.09f, 0.4f, 30f);
            Bake("shift", 520, 0.07f, 0.2f, 26f);
            Bake("swap", 440, 0.10f, 0.25f, 20f);
            Bake("flip", 200, 0.16f, 0.3f, 12f);
            Bake("copy", 660, 0.08f, 0.15f, 24f);
            Bake("load", 90, 0.22f, 0.5f, 10f);
            Bake("reader", 1200, 0.035f, 0.3f, 70f);
            Bake("step", 700, 0.04f, 0.1f, 55f);
            Bake("turn", 900, 0.05f, 0.15f, 45f);
            Bake("execute", 110, 0.30f, 0.35f, 7f);
            Bake("ui", 1000, 0.03f, 0.05f, 60f);
            Bake("error", 120, 0.22f, 0.5f, 9f);
            Bake("success", 523.25f, 0.5f, 0.0f, 4f);
            Bake("certified", 261.63f, 0.9f, 0.0f, 2.5f);
        }

        private void Bake(string name, float freq, float seconds, float noiseMix, float decay)
        {
            int rate = 44100;
            int n = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-decay * t);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                // light second harmonic for a mechanical timbre
                tone += 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = Mathf.Clamp(Mathf.Lerp(tone, noise, noiseMix) * env * 0.6f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            _clips[name] = clip;
        }

        public static void Play(string name, float pitch = 1f)
        {
            if (Instance == null) return;
            Instance.PlayInternal(name, pitch);
        }

        private void PlayInternal(string name, float pitch)
        {
            if (!_clips.TryGetValue(name, out var clip)) return;
            _src.pitch = Mathf.Clamp(pitch, 0.4f, 2.5f);
            _src.PlayOneShot(clip, Mathf.Clamp01(SaveSystem.Data.sfxVolume) * Mathf.Clamp01(SaveSystem.Data.masterVolume));
        }
    }
}
