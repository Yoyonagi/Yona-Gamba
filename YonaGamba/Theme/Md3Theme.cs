using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace YonaGamba.Theme;

/// <summary>
/// Holds the active <see cref="Md3Scheme"/> and maps it onto ImGui's style.
/// The style is only pushed around Yona windows (see <see cref="UI.Md3Window"/>),
/// so other plugins and Dalamud itself are never affected.
/// </summary>
public static class Md3Theme
{
    public static Md3Scheme Scheme { get; private set; } = Md3Scheme.Create(true);

    public static float Scale => ImGuiHelpers.GlobalScale;

    public static void Apply(bool dark) => Scheme = Md3Scheme.Create(dark);

    /// <summary>Pushes the full Material 3 style. Dispose to pop it.</summary>
    public static IDisposable Push()
    {
        var c = Scheme;
        var s = Scale;
        var scope = new StyleScope();
        var transparent = Vector4.Zero;

        // Colors are resolved by name so the theme keeps compiling across ImGui versions
        // that renamed members (e.g. TabActive -> TabSelected, NavHighlight -> NavCursor).
        void Col(string name, Vector4 value)
        {
            if (!Enum.TryParse<ImGuiCol>(name, out var col)) return;
            ImGui.PushStyleColor(col, value);
            scope.Colors++;
        }

        Col("Text", c.OnSurface);
        Col("TextDisabled", ColorMath.WithAlpha(c.OnSurface, 0.38f));
        Col("WindowBg", c.Surface);
        Col("ChildBg", transparent);
        Col("PopupBg", c.SurfaceContainerHigh);
        Col("Border", c.OutlineVariant);
        Col("BorderShadow", transparent);
        Col("FrameBg", c.SurfaceContainerHighest);
        Col("FrameBgHovered", ColorMath.StateLayer(c.SurfaceContainerHighest, c.OnSurface, 0.08f));
        Col("FrameBgActive", ColorMath.StateLayer(c.SurfaceContainerHighest, c.OnSurface, 0.12f));
        Col("TitleBg", c.SurfaceContainer);
        Col("TitleBgActive", c.SurfaceContainer);
        Col("TitleBgCollapsed", c.SurfaceContainer);
        Col("MenuBarBg", c.SurfaceContainer);
        Col("ScrollbarBg", transparent);
        Col("ScrollbarGrab", c.OutlineVariant);
        Col("ScrollbarGrabHovered", c.Outline);
        Col("ScrollbarGrabActive", c.OnSurfaceVariant);
        Col("CheckMark", c.Accent);
        Col("SliderGrab", c.Accent);
        Col("SliderGrabActive", c.Accent);
        Col("Button", c.SecondaryContainer);
        Col("ButtonHovered", ColorMath.StateLayer(c.SecondaryContainer, c.OnSecondaryContainer, 0.08f));
        Col("ButtonActive", ColorMath.StateLayer(c.SecondaryContainer, c.OnSecondaryContainer, 0.12f));
        Col("Header", c.SecondaryContainer);
        Col("HeaderHovered", ColorMath.StateLayer(c.SecondaryContainer, c.OnSecondaryContainer, 0.08f));
        Col("HeaderActive", ColorMath.StateLayer(c.SecondaryContainer, c.OnSecondaryContainer, 0.12f));
        Col("Separator", c.OutlineVariant);
        Col("SeparatorHovered", c.Accent);
        Col("SeparatorActive", c.Accent);
        Col("ResizeGrip", transparent);
        Col("ResizeGripHovered", ColorMath.WithAlpha(c.Accent, 0.50f));
        Col("ResizeGripActive", ColorMath.WithAlpha(c.Accent, 0.80f));
        Col("Tab", c.SurfaceContainer);
        Col("TabHovered", ColorMath.StateLayer(c.SurfaceContainer, c.OnSurface, 0.08f));
        Col("TabActive", c.SecondaryContainer);
        Col("TabSelected", c.SecondaryContainer);
        Col("TabSelectedOverline", c.Accent);
        Col("TabUnfocused", c.SurfaceContainer);
        Col("TabDimmed", c.SurfaceContainer);
        Col("TabUnfocusedActive", c.SecondaryContainer);
        Col("TabDimmedSelected", c.SecondaryContainer);
        Col("PlotLines", c.Accent);
        Col("PlotLinesHovered", c.Tertiary);
        Col("PlotHistogram", c.Accent);
        Col("PlotHistogramHovered", c.Tertiary);
        Col("TableHeaderBg", c.SurfaceContainerHigh);
        Col("TableBorderStrong", c.OutlineVariant);
        Col("TableBorderLight", ColorMath.WithAlpha(c.OutlineVariant, 0.5f));
        Col("TableRowBg", transparent);
        Col("TableRowBgAlt", ColorMath.WithAlpha(c.OnSurface, 0.03f));
        Col("TextSelectedBg", ColorMath.WithAlpha(c.Accent, 0.35f));
        Col("DragDropTarget", c.Accent);
        Col("NavHighlight", c.Accent);
        Col("NavCursor", c.Accent);
        Col("NavWindowingHighlight", c.Accent);
        Col("NavWindowingDimBg", ColorMath.WithAlpha(c.Scrim, 0.3f));
        Col("ModalWindowDimBg", ColorMath.WithAlpha(c.Scrim, 0.4f));

        void Var(ImGuiStyleVar v, float value) { ImGui.PushStyleVar(v, value); scope.Vars++; }
        void Var2(ImGuiStyleVar v, Vector2 value) { ImGui.PushStyleVar(v, value); scope.Vars++; }

        Var2(ImGuiStyleVar.WindowPadding, new Vector2(14, 14) * s);
        Var2(ImGuiStyleVar.FramePadding, new Vector2(14, 8) * s);
        Var2(ImGuiStyleVar.ItemSpacing, new Vector2(8, 8) * s);
        Var2(ImGuiStyleVar.ItemInnerSpacing, new Vector2(8, 6) * s);
        Var(ImGuiStyleVar.WindowRounding, 20 * s);
        Var(ImGuiStyleVar.ChildRounding, 16 * s);
        Var(ImGuiStyleVar.FrameRounding, 10 * s);
        Var(ImGuiStyleVar.PopupRounding, 12 * s);
        Var(ImGuiStyleVar.ScrollbarRounding, 12 * s);
        Var(ImGuiStyleVar.GrabRounding, 12 * s);
        Var(ImGuiStyleVar.TabRounding, 10 * s);
        Var(ImGuiStyleVar.WindowBorderSize, 1 * s);
        Var(ImGuiStyleVar.ChildBorderSize, 0);
        Var(ImGuiStyleVar.FrameBorderSize, 0);
        Var(ImGuiStyleVar.PopupBorderSize, 1 * s);
        Var(ImGuiStyleVar.ScrollbarSize, 8 * s);
        Var(ImGuiStyleVar.GrabMinSize, 12 * s);

        return scope;
    }

    private sealed class StyleScope : IDisposable
    {
        public int Colors;
        public int Vars;
        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (Vars > 0) ImGui.PopStyleVar(Vars);
            if (Colors > 0) ImGui.PopStyleColor(Colors);
        }
    }
}
