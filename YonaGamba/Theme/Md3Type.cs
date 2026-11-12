using System;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;

namespace YonaGamba.Theme;

/// <summary>
/// A small slice of the Material 3 type scale, backed by Dalamud's default font at extra sizes.
/// Body text uses the normal Dalamud font.
/// </summary>
public static class Md3Type
{
    private static IFontHandle? headline;
    private static IFontHandle? title;
    private static IFontHandle? label;

    public static void Initialize(IUiBuilder ui)
    {
        headline = ui.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(26f)));
        title = ui.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(20f)));
        label = ui.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(13f)));
    }

    /// <summary>Headline Small — page titles.</summary>
    public static IDisposable Headline() => Push(headline);

    /// <summary>Title Large — card and section titles.</summary>
    public static IDisposable Title() => Push(title);

    /// <summary>Label Medium — navigation labels, captions.</summary>
    public static IDisposable Label() => Push(label);

    public static void Dispose()
    {
        headline?.Dispose();
        title?.Dispose();
        label?.Dispose();
        headline = title = label = null;
    }

    private static IDisposable Push(IFontHandle? handle) => handle?.Push() ?? NoOp.Instance;

    private sealed class NoOp : IDisposable
    {
        public static readonly NoOp Instance = new();
        public void Dispose() { }
    }
}
