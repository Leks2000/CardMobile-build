using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Арт из Resources по пути (например "Art/Cards/Footman", "Art/UI/coin"). Если картинка импортирована
/// не как Sprite (Texture Type = Default), спрайт создаётся из текстуры на лету. Кешируется.
/// </summary>
public static class ArtLib
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (cache.TryGetValue(path, out var s) && s != null) return s;
        s = Resources.Load<Sprite>(path);
        if (s == null)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                s.name = tex.name;
            }
        }
        if (s == null) Debug.LogWarning($"[ART] Missing sprite Resources/{path}");
        cache[path] = s;
        return s;
    }

    /// <summary>UI-спрайт из Resources/Art/UI.</summary>
    public static Sprite UI(string name) => Get("Art/UI/" + name);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cache.Clear();
}
