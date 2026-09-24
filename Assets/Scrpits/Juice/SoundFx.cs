using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Звуки игры без аудио-файлов: короткие клипы синтезируются в коде один раз (шум + синусы с огибающей).
/// Громкость - PlayerPrefs "Volume" (тот же ключ, что в настройках). Вибрация - только на телефоне.
/// Заменить звук своим файлом: положить AudioClip в Resources/Sounds/&lt;имя клипа&gt; (например Sounds/Hit) - он возьмётся вместо синтеза.
/// </summary>
public static class SoundFx
{
    public enum Clip { Hit, BossHit, PlayerHit, Draw, PlayCard, Tick, Coin, Reveal, RevealEpic, Win, Lose, Click, Item, Block }

    private const int Rate = 44100;
    private static readonly Dictionary<Clip, AudioClip> clips = new Dictionary<Clip, AudioClip>();
    private static AudioSource source;
    private static readonly Dictionary<Clip, float> lastPlay = new Dictionary<Clip, float>();

    public static bool Enabled = true;

    public static float Volume => PlayerPrefs.GetFloat("Volume", 0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        clips.Clear();
        lastPlay.Clear();
        source = null;
    }

    public static void Play(Clip clip, float volume = 1f, float pitchJitter = 0.06f)
    {
        if (!Enabled || !Application.isPlaying) return;
        // один и тот же звук не чаще раза в 40 мс (пачка ударов не превращается в треск)
        float now = Time.unscaledTime;
        if (lastPlay.TryGetValue(clip, out var t) && now - t < 0.04f) return;
        lastPlay[clip] = now;
        var src = Source();
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(Get(clip), Mathf.Clamp01(volume) * Volume);
    }

    public static void PlayDelayed(Clip clip, float delay, float volume = 1f)
    {
        if (delay <= 0f) { Play(clip, volume); return; }
        CombatFx.StartRoutine(Delayed(clip, delay, volume));
    }

    private static IEnumerator Delayed(Clip clip, float delay, float volume)
    {
        yield return new WaitForSecondsRealtime(delay);
        Play(clip, volume);
    }

    /// <summary>Короткая вибрация (Android / iOS).</summary>
    public static void Vibrate()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    private static AudioSource Source()
    {
        if (source != null) return source;
        var go = new GameObject("[SoundFx]");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static AudioClip Get(Clip c)
    {
        if (clips.TryGetValue(c, out var clip) && clip != null) return clip;
        clip = Resources.Load<AudioClip>("Sounds/" + c);
        if (clip == null) clip = Synth(c);
        clips[c] = clip;
        return clip;
    }

    // ---------------- synthesis ----------------

    private static AudioClip Synth(Clip c)
    {
        switch (c)
        {
            case Clip.Hit: return Make("Hit", 0.16f, (t, r) => Noise(r) * Env(t, 0.002f, 0.06f) * 0.5f + Sine(t, 140f - 300f * t) * Env(t, 0.002f, 0.1f) * 0.7f);
            case Clip.BossHit: return Make("BossHit", 0.35f, (t, r) => Noise(r) * Env(t, 0.003f, 0.12f) * 0.45f + Sine(t, 80f - 80f * t) * Env(t, 0.004f, 0.25f) * 0.9f);
            case Clip.PlayerHit: return Make("PlayerHit", 0.3f, (t, r) => Noise(r) * Env(t, 0.002f, 0.08f) * 0.4f + Square(t, 110f - 120f * t) * Env(t, 0.003f, 0.2f) * 0.35f);
            case Clip.Draw: return Make("Draw", 0.18f, (t, r) => Noise(r) * Env(t, 0.03f, 0.1f) * 0.35f * (0.3f + t * 4f));
            case Clip.PlayCard: return Make("PlayCard", 0.14f, (t, r) => Noise(r) * Env(t, 0.001f, 0.04f) * 0.4f + Sine(t, 220f) * Env(t, 0.001f, 0.08f) * 0.5f);
            case Clip.Tick: return Make("Tick", 0.03f, (t, r) => Square(t, 1900f) * Env(t, 0.0005f, 0.012f) * 0.35f);
            case Clip.Click: return Make("Click", 0.06f, (t, r) => Sine(t, 900f) * Env(t, 0.001f, 0.025f) * 0.45f);
            case Clip.Coin: return Make("Coin", 0.28f, (t, r) => Sine(t, t < 0.07f ? 1320f : 1760f) * Env(t, 0.002f, 0.18f) * 0.45f);
            case Clip.Block: return Make("Block", 0.22f, (t, r) => (Sine(t, 620f) + Sine(t, 930f) * 0.6f) * Env(t, 0.001f, 0.14f) * 0.35f + Noise(r) * Env(t, 0.001f, 0.02f) * 0.3f);
            case Clip.Item: return Make("Item", 0.45f, (t, r) => Sine(t, 500f + 900f * t) * Env(t, 0.01f, 0.3f) * 0.35f + Sine(t, 1000f + 1800f * t) * Env(t, 0.02f, 0.25f) * 0.15f);
            case Clip.Reveal: return Arpeggio("Reveal", new[] { 523.25f, 659.25f, 783.99f }, 0.09f, 0.5f);
            case Clip.RevealEpic: return Arpeggio("RevealEpic", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.08f, 0.9f);
            case Clip.Win: return Arpeggio("Win", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 1.0f);
            case Clip.Lose: return Arpeggio("Lose", new[] { 392f, 329.63f, 261.63f, 196f }, 0.18f, 1.1f);
        }
        return Make("Silence", 0.05f, (t, r) => 0f);
    }

    private static AudioClip Make(string name, float seconds, System.Func<float, System.Random, float> f)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        var rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, rng), -1f, 1f);
        var clip = AudioClip.Create("sfx_" + name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Arpeggio(string name, float[] notes, float step, float seconds)
    {
        return Make(name, seconds, (t, r) =>
        {
            float v = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                float lt = t - k * step;
                if (lt < 0f) continue;
                v += (Sine(lt, notes[k]) * 0.7f + Sine(lt, notes[k] * 2f) * 0.15f) * Env(lt, 0.005f, seconds * 0.6f);
            }
            return v * 0.3f;
        });
    }

    private static float Sine(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);
    private static float Square(float t, float hz) => Sine(t, hz) >= 0f ? 1f : -1f;
    private static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

    /// <summary>Огибающая: быстрая атака, экспоненциальный спад.</summary>
    private static float Env(float t, float attack, float decay)
    {
        if (t < attack) return t / attack;
        return Mathf.Exp(-(t - attack) / Mathf.Max(0.001f, decay));
    }
}
