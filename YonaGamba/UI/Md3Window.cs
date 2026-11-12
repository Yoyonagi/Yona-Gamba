using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using YonaGamba.Theme;

namespace YonaGamba.UI;

/// <summary>
/// Base class for every Yona window (including windows owned by modules).
/// Pushes the Material 3 style before ImGui.Begin and pops it after ImGui.End.
/// </summary>
public abstract class Md3Window : Window
{
    private IDisposable? theme;

    protected Md3Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
    }

    public override void PreDraw()
    {
        theme = Md3Theme.Push();
    }

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }
}
