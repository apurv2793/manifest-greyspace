using UnityEngine;
using System.Collections.Generic;

// Real-time procedural audio synthesis - no audio asset files, following the same
// precedent already proven in the JS sibling engine (manifest-cod-experiment's
// src/audio/dsp.js). All 6 sounds ever requested via AudioManager.Play() are
// pre-generated once at bootstrap (AudioClip.Create + a hand-rolled ADSR-style
// envelope), then played via PlayOneShot on a small pooled AudioSource set so
// overlapping calls (e.g. rapid hits during a combo) don't cut each other off.
public class AudioManager : MonoBehaviour
{
    static AudioManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        var go = new GameObject("AudioManager");
        go.hideFlags = HideFlags.HideInHierarchy;
        instance = go.AddComponent<AudioManager>();

        instance.clips["dash"] = GenerateDash();
        instance.clips["player_hit"] = GeneratePlayerHit();
        instance.clips["sword_swing"] = GenerateSwordSwing();
        instance.clips["hit_enemy"] = GenerateHitEnemy();
        instance.clips["level_up"] = GenerateLevelUp();
        instance.clips["wave_clear"] = GenerateWaveClear();
        instance.clips["death"] = GenerateDeath();
        instance.clips["victory"] = GenerateVictory();
    }

    public static void Play(string clipName)
    {
        if (instance == null) Initialize();

        if (!instance.clips.TryGetValue(clipName, out var clip))
        {
            Debug.LogWarning($"AudioManager: unknown clip '{clipName}'");
            return;
        }

        // First idle source wins; if all are busy, steal the first (overlap is rare
        // enough with 6 pooled sources that this fallback almost never triggers).
        foreach (var source in instance.sources)
        {
            if (!source.isPlaying)
            {
                source.PlayOneShot(clip);
                return;
            }
        }
        instance.sources[0].PlayOneShot(clip);
    }

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly AudioSource[] sources = new AudioSource[6];

    void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].pitch = 1f;
            sources[i].volume = 1f;
            // Non-positional: this manager lives at a fixed world origin, not on the
            // player/camera, so any spatialBlend > 0 would make every hit/UI cue fade
            // and pan as the player moves away from (0,0,0) - these are gameplay-
            // feedback cues (hits, level-up, wave-clear), not world-positioned SFX.
            sources[i].spatialBlend = 0f;
        }
    }

    // ADSR-style envelope: linear attack -> lerp decay to sustain level -> linear
    // release to zero. No separate sustain hold phase - sustainLevel is just the
    // level decay settles to right before release begins, which is enough shape
    // for short percussive SFX without needing a fourth explicit stage.
    static float Envelope(int sampleIndex, int totalSamples, float attackTime, float decayTime,
                         float sustainLevel, float releaseTime, float sampleRate)
    {
        float t = (float)sampleIndex / sampleRate;
        float totalDuration = attackTime + decayTime + releaseTime;
        if (t > totalDuration) return 0f;

        float peak = 1f;

        if (t < attackTime)
            return t / attackTime * peak;

        float decayEnd = attackTime + decayTime;
        if (t < decayEnd)
        {
            float decayProgress = (t - attackTime) / decayTime;
            return Mathf.Lerp(peak, sustainLevel * peak, decayProgress);
        }

        float releaseStart = decayEnd;
        if (t < releaseStart + releaseTime)
        {
            float releaseProgress = (t - releaseStart) / releaseTime;
            return sustainLevel * peak * (1f - releaseProgress);
        }

        return 0f;
    }

    static AudioClip GenerateDash()
    {
        const int sampleRate = 44100;
        const float duration = 0.2f; // 200ms whoosh
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("dash", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = Random.Range(-1f, 1f);
            float envelope = Envelope(i, lengthSamples, 0.01f, 0.15f, 0.3f, 0.04f, sampleRate);
            // Amplitude-decays the noise burst over the clip (a cheap stand-in for a
            // true bandpass filter, which would need real per-sample IIR filtering).
            float decay = Mathf.Exp(-t * 15f);
            samples[i] = noise * envelope * decay;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GeneratePlayerHit()
    {
        const int sampleRate = 44100;
        const float duration = 0.1f; // 100ms dull thud
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("player_hit", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = 120f + t * 30f;
            float wave = Mathf.Sin(2 * Mathf.PI * freq * t);
            float envelope = Envelope(i, lengthSamples, 0.005f, 0.07f, 0.3f, 0.02f, sampleRate);
            samples[i] = wave * envelope;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateSwordSwing()
    {
        const int sampleRate = 44100;
        const float duration = 0.15f; // 150ms swish
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("sword_swing", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = Random.Range(-1f, 1f);
            float envelope = Envelope(i, lengthSamples, 0.008f, 0.12f, 0.3f, 0.02f, sampleRate);
            float sweepFreq = Mathf.Lerp(600f, 100f, t / duration);
            float modulator = Mathf.Sin(2 * Mathf.PI * sweepFreq * t) * 0.3f;
            samples[i] = (noise + modulator) * envelope;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateHitEnemy()
    {
        const int sampleRate = 44100;
        const float duration = 0.08f; // 80ms sharp impact - fires on every hit
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("hit_enemy", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = Random.Range(-1f, 1f) * 0.7f;
            float freqDrop = Mathf.Lerp(150f, 80f, t / duration);
            float tone = Mathf.Sin(2 * Mathf.PI * freqDrop * t) * 0.3f;
            float envelope = Envelope(i, lengthSamples, 0.0015f, 0.05f, 0.4f, 0.02f, sampleRate);
            samples[i] = (noise + tone) * envelope;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateLevelUp()
    {
        const int sampleRate = 44100;
        const float duration = 0.4f; // 400ms rising arpeggio
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("level_up", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        // C5, E5, G5, C6
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float noteDuration = duration / (notes.Length + 1);

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = n * noteDuration;
                float noteEnd = noteStart + noteDuration;
                if (t >= noteStart && t <= noteEnd)
                {
                    float phase = 2 * Mathf.PI * notes[n] * (t - noteStart);
                    float envelope = Envelope(
                        (int)((t - noteStart) * sampleRate),
                        (int)(noteDuration * sampleRate),
                        0.01f, 0.15f, 0.7f, 0.05f, sampleRate);
                    amplitude += Mathf.Sin(phase) * envelope;
                }
            }

            float shimmer = 0.03f * Random.Range(-1f, 1f);
            samples[i] = amplitude + shimmer;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateWaveClear()
    {
        const int sampleRate = 44100;
        const float duration = 0.5f; // 500ms warm two-note confirmation
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("wave_clear", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        // G4 + D5, staggered start
        float[] notes = { 392.00f, 587.33f };
        float[] noteStarts = { 0.1f, 0.3f };
        const float noteLen = 0.25f;

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = noteStarts[n];
                if (t >= noteStart && t <= noteStart + noteLen)
                {
                    float phase = 2 * Mathf.PI * notes[n] * (t - noteStart);
                    float envelope = Envelope(
                        (int)((t - noteStart) * sampleRate),
                        (int)(noteLen * sampleRate),
                        0.03f, 0.1f, 0.7f, 0.2f, sampleRate);
                    amplitude += Mathf.Sin(phase) * envelope;
                }
            }

            float shimmer = 0.05f * Random.Range(-1f, 1f);
            samples[i] = amplitude + shimmer;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateDeath()
    {
        const int sampleRate = 44100;
        const float duration = 2.2f; // Somber, descending - ~2.2 seconds
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("death", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        // Three-note descending minor chord progression: C3 → Bb2 → G2 (low register)
        float[] notes = { 130.81f, 116.54f, 98.00f };
        float[] noteStarts = { 0.2f, 0.7f, 1.2f }; // Staggered start with gaps for weight
        const float noteLen = 0.6f;

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = noteStarts[n];
                if (t >= noteStart && t <= noteStart + noteLen)
                {
                    float phase = 2 * Mathf.PI * notes[n] * (t - noteStart);
                    // Use low sustain level and long release for that "fade to black" effect
                    float envelope = Envelope(
                        (int)((t - noteStart) * sampleRate),
                        (int)(noteLen * sampleRate),
                        0.1f, 0.3f, 0.5f, 1.2f, sampleRate);
                    amplitude += Mathf.Sin(phase) * envelope;
                }
            }

            // Slight tremolo for extra melancholy weight
            float tremolo = 0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * 2.5f * t);
            samples[i] = amplitude * tremolo;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    static AudioClip GenerateVictory()
    {
        const int sampleRate = 44100;
        const float duration = 3.0f; // Grand flourish - ~3 seconds
        const int lengthSamples = (int)(sampleRate * duration);

        var clip = AudioClip.Create("victory", lengthSamples, 1, sampleRate, false);
        var samples = new float[lengthSamples];

        // Extended major arpeggio: C4 → E4 → G4 → C5 → E5 → G5 → C6
        float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 783.99f, 1046.50f };
        float noteDuration = duration / (notes.Length + 2); // Leave space for final held chord
        const float holdNoteStart = notes.Length * noteDuration;
        const float holdLen = 0.8f;

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = 0f;

            // Play the arpeggio
            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = n * noteDuration;
                float noteEnd = noteStart + noteDuration;
                if (t >= noteStart && t <= noteEnd)
                {
                    float phase = 2 * Mathf.PI * notes[n] * (t - noteStart);
                    float envelope = Envelope(
                        (int)((t - noteStart) * sampleRate),
                        (int)(noteDuration * sampleRate),
                        0.02f, 0.15f, 0.8f, 0.3f, sampleRate);
                    amplitude += Mathf.Sin(phase) * envelope;
                }
            }

            // Add a final sustained chord at the end
            if (t >= holdNoteStart && t <= holdNoteStart + holdLen)
            {
                float heldTime = t - holdNoteStart;
                for (int n = 0; n < notes.Length; n++)
                {
                    float phase = 2 * Mathf.PI * notes[n] * heldTime;
                    // Gentle fade out on the final chord
                    float envelope = Envelope(
                        (int)(heldTime * sampleRate),
                        (int)(holdLen * sampleRate),
                        0.05f, 0.4f, 1.0f, 0.35f, sampleRate);
                    amplitude += Mathf.Sin(phase) * envelope * 0.7f; // Slightly lower volume per note for final chord
                }
            }

            float shimmer = 0.02f * Random.Range(-1f, 1f);
            samples[i] = amplitude + shimmer;
        }

        clip.SetData(samples, 0);
        return clip;
    }
}
