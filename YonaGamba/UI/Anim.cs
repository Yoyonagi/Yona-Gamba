using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;

namespace YonaGamba.UI;

/// <summary>
/// Tiny animation helper: every animated value is keyed by an ImGui id and eased toward
/// its target a little each frame, which gives smooth hover, toggle and expand transitions.
/// </summary>
public static class Anim
{
    private static readonly Dictionary<uint, float> Values = new();

    /// <summary>Eases the value stored under <paramref name="id"/> toward <paramref name="target"/> and returns it.</summary>
    public static float To(uint id, float target, float speed = 14f)
    {
        if (!Values.TryGetValue(id, out var current))
        {
            Values[id] = target;
            return target;
        }

        var dt = Math.Clamp(ImGui.GetIO().DeltaTime, 0f, 0.1f);
        current += (target - current) * Math.Min(1f, dt * speed);
        if (Math.Abs(target - current) < 0.003f) current = target;
        Values[id] = current;
        return current;
    }

    public static float To(string id, float target, float speed = 14f) => To(ImGui.GetID(id), target, speed);

    /// <summary>Jumps the value to <paramref name="value"/>, e.g. to restart a fade-in.</summary>
    public static void Set(uint id, float value) => Values[id] = value;

    public static void Set(string id, float value) => Set(ImGui.GetID(id), value);

    /// <summary>Smooth ease-out curve for values in 0..1.</summary>
    public static float Ease(float t) => 1f - (1f - t) * (1f - t);
}
