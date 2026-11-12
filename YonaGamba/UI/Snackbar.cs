using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using YonaGamba.Theme;

namespace YonaGamba.UI;

/// <summary>Brief message shown at the bottom of the main window. Call <see cref="Show"/> from anywhere.</summary>
public static class Snackbar
{
    private static string? message;
    private static DateTime until;

    public static void Show(string text, double seconds = 3)
    {
        message = text;
        until = DateTime.UtcNow.AddSeconds(seconds);
    }

    /// <summary>Draws the snackbar over the current window's bottom edge.</summary>
    internal static void Draw()
    {
        if (message == null) return;
        if (DateTime.UtcNow > until)
        {
            message = null;
            return;
        }

        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        var text = ImGui.CalcTextSize(message);
        var size = new Vector2(Math.Min(text.X + 32 * s, winSize.X - 32 * s), text.Y + 28 * s);
        var min = new Vector2(winPos.X + (winSize.X - size.X) / 2, winPos.Y + winSize.Y - size.Y - 16 * s);

        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(min, min + size, Md3.U(c.InverseSurface), 4 * s);
        dl.AddText(min + new Vector2(16 * s, 14 * s), Md3.U(c.InverseOnSurface), message);
    }
}
