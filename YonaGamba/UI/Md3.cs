using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using YonaGamba.Theme;

namespace YonaGamba.UI;

/// <summary>
/// Material Design 3 components for ImGui. Every module should build its UI from these
/// so the whole toolbox looks and behaves consistently. Interactive components are drawn
/// by hand so their hover, press and toggle states can animate.
/// </summary>
public static class Md3
{
    private static float S => Md3Theme.Scale;
    private static Md3Scheme C => Md3Theme.Scheme;

    /// <summary>Packs a color for draw-list calls, honoring the current style alpha (used for fades).</summary>
    public static uint U(Vector4 v) => ColorMath.ToImU32(v with { W = v.W * ImGui.GetStyle().Alpha });

    /// <summary>The part of an ImGui label shown on screen (everything before "##").</summary>
    private static string Visible(string label)
    {
        var i = label.IndexOf("##", StringComparison.Ordinal);
        return i < 0 ? label : label[..i];
    }

    private static float Hover(uint id, bool hovered, bool active = false) =>
        Anim.To(id, active ? 1f : hovered ? 0.6f : 0f, 16f);

    // ───────────────────────────── Typography ─────────────────────────────

    public static void Headline(string text, Vector4? color = null)
    {
        using (Md3Type.Headline())
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? C.OnSurface))
            ImGui.Text(text);
    }

    public static void Title(string text, Vector4? color = null)
    {
        using (Md3Type.Title())
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? C.OnSurface))
            ImGui.Text(text);
    }

    public static void Body(string text, Vector4? color = null)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? C.OnSurface))
            ImGui.TextWrapped(text);
    }

    /// <summary>Secondary text (OnSurfaceVariant), wrapped.</summary>
    public static void Supporting(string text) => Body(text, C.OnSurfaceVariant);

    public static void Label(string text, Vector4? color = null)
    {
        using (Md3Type.Label())
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? C.OnSurfaceVariant))
            ImGui.Text(text);
    }

    /// <summary>Small accent-colored section header.</summary>
    public static void SectionLabel(string text)
    {
        Spacer(4);
        Label(text, C.Accent);
    }

    public static void Spacer(float dp) => ImGui.Dummy(new Vector2(0, dp * S));

    public static void Divider()
    {
        var pos = ImGui.GetCursorScreenPos();
        var w = ImGui.GetContentRegionAvail().X;
        ImGui.Dummy(new Vector2(w, 9 * S));
        ImGui.GetWindowDrawList().AddLine(pos + new Vector2(0, 4 * S), pos + new Vector2(w, 4 * S), U(C.OutlineVariant), 1 * S);
    }

    /// <summary>Material "plain tooltip" for the last item.</summary>
    public static void Tooltip(string text)
    {
        // Disabled buttons still explain themselves on hover.
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) return;
        using var color = ImRaii.PushColor(ImGuiCol.PopupBg, C.InverseSurface)
                                .Push(ImGuiCol.Text, C.InverseOnSurface)
                                .Push(ImGuiCol.Border, Vector4.Zero);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 6) * S)
                                .Push(ImGuiStyleVar.PopupRounding, 6 * S)
                                .Push(ImGuiStyleVar.PopupBorderSize, 0f);
        ImGui.SetTooltip(text);
    }

    // ───────────────────────────── Icons ─────────────────────────────

    public static Vector2 IconSize(FontAwesomeIcon icon)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
            return ImGui.CalcTextSize(icon.ToIconString());
    }

    /// <summary>Draws an icon centered on <paramref name="center"/> without affecting layout.</summary>
    public static void DrawIcon(ImDrawListPtr dl, FontAwesomeIcon icon, Vector2 center, Vector4 color)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var str = icon.ToIconString();
            dl.AddText(center - InkCenter(str), U(color), str);
        }
    }

    /// <summary>
    /// Where the visible part of an icon sits inside its text box. Icons are centered on what
    /// is actually drawn, not on the font's line box, so glyphs that hug the baseline or lean
    /// to one side (minimize, pointer, lock) line up with their neighbours.
    /// </summary>
    private static Vector2 InkCenter(string str)
    {
        var font = ImGui.GetFont();
        var glyph = font.FindGlyph(str[0]);
        var scale = font.FontSize > 0 ? ImGui.GetFontSize() / font.FontSize : 1f;
        var ink = new Vector2(glyph.X0 + glyph.X1, glyph.Y0 + glyph.Y1) / 2 * scale;
        return ink.X > 0 && ink.Y > 0 ? ink : ImGui.CalcTextSize(str) / 2;
    }

    /// <summary>An icon inside a filled, rounded container.</summary>
    public static void IconBadge(FontAwesomeIcon icon, float sizeDp = 40, Vector4? background = null, Vector4? foreground = null, float radiusDp = -1)
    {
        var size = sizeDp * S;
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(size, size));
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(pos, pos + new Vector2(size), U(background ?? C.SecondaryContainer), radiusDp < 0 ? size / 2 : radiusDp * S);
        DrawIcon(dl, icon, pos + new Vector2(size / 2), foreground ?? C.OnSecondaryContainer);
    }

    // ───────────────────────────── Buttons ─────────────────────────────

    public static bool FilledButton(string label, FontAwesomeIcon? icon = null) =>
        Button(label, C.Primary, C.OnPrimary, null, icon);

    public static bool TonalButton(string label, FontAwesomeIcon? icon = null) =>
        Button(label, C.SecondaryContainer, C.OnSecondaryContainer, null, icon);

    public static bool OutlinedButton(string label, FontAwesomeIcon? icon = null) =>
        Button(label, Vector4.Zero, C.Accent, C.Outline, icon);

    public static bool TextButton(string label, FontAwesomeIcon? icon = null) =>
        Button(label, Vector4.Zero, C.Accent, null, icon);

    public static bool ErrorButton(string label, FontAwesomeIcon? icon = null) =>
        Button(label, C.Error, C.OnError, null, icon);

    private static bool Button(string label, Vector4 bg, Vector4 fg, Vector4? border, FontAwesomeIcon? icon)
    {
        var text = Visible(label);
        var id = ImGui.GetID(label);
        var ts = ImGui.CalcTextSize(text);
        var iconW = icon is { } measured ? IconSize(measured).X + 8 * S : 0f;
        var h = 36 * S;
        var w = ts.X + iconW + (bg.W <= 0 && border == null ? 28 : 44) * S;

        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, new Vector2(w, h));
        var hovered = ImGui.IsItemHovered();
        var t = Hover(id, hovered, ImGui.IsItemActive());
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var dl = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(w, h);
        if (bg.W > 0) dl.AddRectFilled(pos, max, U(bg), h / 2);
        if (t > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(fg, 0.16f * t)), h / 2);
        if (border is { } stroke) dl.AddRect(pos, max, U(stroke), h / 2, ImDrawFlags.None, 1 * S);

        var x = pos.X + (w - ts.X - iconW) / 2;
        if (icon is { } glyph)
        {
            DrawIcon(dl, glyph, new Vector2(x + (iconW - 8 * S) / 2, pos.Y + h / 2), fg);
            x += iconW;
        }

        dl.AddText(new Vector2(x, pos.Y + (h - ts.Y) / 2), U(fg), text);
        return clicked;
    }

    /// <summary>Circular icon button. <paramref name="selected"/> gives it a tonal container (toggle buttons).</summary>
    public static bool IconButton(string id, FontAwesomeIcon icon, string? tooltip = null, bool selected = false, bool filled = false, float sizeDp = 36, Vector4? color = null)
    {
        var size = sizeDp * S;
        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(id, new Vector2(size, size));
        var hovered = ImGui.IsItemHovered();
        var t = Hover(ImGui.GetID(id), hovered, ImGui.IsItemActive());
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var dl = ImGui.GetWindowDrawList();
        var center = pos + new Vector2(size / 2);
        var fg = color ?? (filled ? C.OnPrimary : selected ? C.OnSecondaryContainer : C.OnSurfaceVariant);

        if (filled) dl.AddCircleFilled(center, size / 2, U(C.Primary));
        else if (selected) dl.AddCircleFilled(center, size / 2, U(C.SecondaryContainer));
        if (t > 0.01f) dl.AddCircleFilled(center, size / 2, U(ColorMath.WithAlpha(fg, 0.16f * t)));

        DrawIcon(dl, icon, center, fg);
        if (tooltip != null) Tooltip(tooltip);
        return clicked;
    }

    /// <summary>
    /// Starts a right-aligned pill that holds <paramref name="count"/> icon buttons (36dp each).
    /// Follow with <see cref="IconButton"/> calls separated by <c>ImGui.SameLine(0, 0)</c>.
    /// </summary>
    public static void BeginActionBar(int count)
    {
        var w = count * 36 * S + 8 * S;
        var h = 40 * S;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - w);
        var pos = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(pos, pos + new Vector2(w, h), U(C.SurfaceContainerHighest), h / 2);
        dl.AddRect(pos, pos + new Vector2(w, h), U(C.OutlineVariant), h / 2, ImDrawFlags.None, 1 * S);
        ImGui.SetCursorScreenPos(pos + new Vector2(4 * S, 2 * S));
    }

    // ───────────────────────────── Selection ─────────────────────────────

    /// <summary>Material 3 switch with a sliding thumb. Returns true when toggled.</summary>
    public static bool Switch(string id, ref bool value)
    {
        var w = 48 * S;
        var h = 28 * S;
        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(id, new Vector2(w, h));
        if (clicked) value = !value;

        var hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        var key = ImGui.GetID(id);
        var t = Anim.Ease(Anim.To(key, value ? 1f : 0f, 16f));
        var hover = Anim.To(key + 1, hovered ? 1f : 0f, 16f);

        var dl = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(w, h);
        dl.AddRectFilled(pos, max, U(Vector4.Lerp(C.SurfaceContainerHighest, C.Primary, t)), h / 2);
        if (t < 0.99f)
            dl.AddRect(pos, max, U(ColorMath.WithAlpha(C.Outline, 1f - t)), h / 2, ImDrawFlags.None, 2 * S);

        var center = new Vector2(pos.X + h / 2 + (w - h) * t, pos.Y + h / 2);
        if (hover > 0.01f)
            dl.AddCircleFilled(center, h / 2 + 5 * S, U(ColorMath.WithAlpha(value ? C.Accent : C.OnSurface, 0.12f * hover)));

        var radius = (7f + 4f * t + 1f * hover) * S;
        dl.AddCircleFilled(center, radius, U(Vector4.Lerp(C.Outline, C.OnPrimary, t)));
        return clicked;
    }

    /// <summary>A list row: label + optional supporting text, with a switch on the trailing edge.</summary>
    public static bool SwitchRow(string label, string? supporting, ref bool value)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var start = ImGui.GetCursorPos();
        var switchW = 48 * S;
        var switchH = 28 * S;

        ImGui.BeginGroup();
        ImGui.PushTextWrapPos(start.X + avail - switchW - 16 * S);
        ImGui.Text(Visible(label));
        if (!string.IsNullOrEmpty(supporting))
            using (ImRaii.PushColor(ImGuiCol.Text, C.OnSurfaceVariant))
                ImGui.TextWrapped(supporting);
        ImGui.PopTextWrapPos();
        ImGui.EndGroup();
        var rowH = Math.Max(ImGui.GetItemRectSize().Y, switchH);

        ImGui.SetCursorPos(new Vector2(start.X + avail - switchW, start.Y + (rowH - switchH) / 2));
        var changed = Switch($"##switch-{label}", ref value);

        ImGui.SetCursorPos(new Vector2(start.X, start.Y + rowH));
        ImGui.Dummy(new Vector2(0, 0));
        return changed;
    }

    /// <summary>Filter chip. Returns true when toggled.</summary>
    public static bool FilterChip(string label, ref bool selected)
    {
        var text = Visible(label);
        var h = 32 * S;
        var ts = ImGui.CalcTextSize(text);
        var checkW = IconSize(FontAwesomeIcon.Check).X;
        var key = ImGui.GetID(label);
        var sel = Anim.To(key + 1, selected ? 1f : 0f, 16f);
        var w = 16 * S + (checkW + 8 * S) * sel + ts.X + 16 * S;

        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, new Vector2(w, h));
        if (clicked) selected = !selected;
        var hovered = ImGui.IsItemHovered();
        var t = Hover(key, hovered, ImGui.IsItemActive());

        var dl = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(w, h);
        var fg = selected ? C.OnSecondaryContainer : C.OnSurfaceVariant;

        if (sel > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(C.SecondaryContainer, sel)), 8 * S);
        if (sel < 0.99f) dl.AddRect(pos, max, U(ColorMath.WithAlpha(C.Outline, 1f - sel)), 8 * S, ImDrawFlags.None, 1 * S);
        if (t > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(fg, 0.12f * t)), 8 * S);

        var x = pos.X + 16 * S;
        if (sel > 0.5f)
            DrawIcon(dl, FontAwesomeIcon.Check, new Vector2(x + checkW / 2, pos.Y + h / 2), ColorMath.WithAlpha(fg, sel));
        x += (checkW + 8 * S) * sel;
        dl.AddText(new Vector2(x, pos.Y + (h - ts.Y) / 2), U(fg), text);
        return clicked;
    }

    /// <summary>Single-select segmented button. Returns true when the selection changed.</summary>
    public static bool SegmentedButtons(string id, IReadOnlyList<string> options, ref int selected, float width = 0)
    {
        var n = options.Count;
        if (n == 0) return false;

        var h = 36 * S;
        float segW;
        if (width > 0)
        {
            segW = width / n;
        }
        else
        {
            var maxText = 0f;
            foreach (var o in options) maxText = Math.Max(maxText, ImGui.CalcTextSize(o).X);
            segW = maxText + 36 * S;
        }

        var pos = ImGui.GetCursorScreenPos();
        var total = new Vector2(segW * n, h);
        var dl = ImGui.GetWindowDrawList();
        var changed = false;

        ImGui.PushID(id);

        // The selection indicator slides between segments.
        var slide = Anim.To("##slide", selected, 18f);
        var indMin = pos + new Vector2(segW * slide + 3 * S, 3 * S);
        var indMax = pos + new Vector2(segW * (slide + 1) - 3 * S, h - 3 * S);
        dl.AddRectFilled(pos, pos + total, U(C.SurfaceContainerHighest), h / 2);
        dl.AddRectFilled(indMin, indMax, U(C.SecondaryContainer), h / 2);

        for (var i = 0; i < n; i++)
        {
            var min = pos + new Vector2(segW * i, 0);
            ImGui.SetCursorScreenPos(min);
            if (ImGui.InvisibleButton($"##seg{i}", new Vector2(segW, h)) && selected != i)
            {
                selected = i;
                changed = true;
            }

            var hovered = ImGui.IsItemHovered();
            if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            var t = Hover(ImGui.GetID($"##seg{i}"), hovered && i != selected);
            var fg = i == selected ? C.OnSecondaryContainer : C.OnSurfaceVariant;
            if (t > 0.01f)
                dl.AddRectFilled(min + new Vector2(3 * S), min + new Vector2(segW - 3 * S, h - 3 * S), U(ColorMath.WithAlpha(C.OnSurface, 0.10f * t)), h / 2);

            var ts = ImGui.CalcTextSize(options[i]);
            dl.AddText(new Vector2(min.X + (segW - ts.X) / 2, min.Y + (h - ts.Y) / 2), U(fg), options[i]);
        }

        ImGui.PopID();
        ImGui.SetCursorScreenPos(pos);
        ImGui.Dummy(total);
        return changed;
    }

    // ───────────────────────────── Text input ─────────────────────────────

    /// <summary>Single-line text field with a hint and an optional leading icon.</summary>
    public static bool TextField(string id, string hint, ref string value, float width = 0, int maxLength = 256, FontAwesomeIcon? icon = null)
    {
        var w = width > 0 ? width : ImGui.GetContentRegionAvail().X;
        var pos = ImGui.GetCursorScreenPos();
        var leading = icon != null ? 36 * S : 16 * S;

        ImGui.SetNextItemWidth(w);
        bool changed;
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(leading, 9 * S)).Push(ImGuiStyleVar.FrameRounding, 999f))
            changed = ImGui.InputTextWithHint(id, hint, ref value, maxLength);

        if (icon is { } glyph)
        {
            var h = ImGui.GetItemRectSize().Y;
            DrawIcon(ImGui.GetWindowDrawList(), glyph, pos + new Vector2(20 * S, h / 2), C.OnSurfaceVariant);
        }

        return changed;
    }

    // ───────────────────────────── Lists and navigation ─────────────────────────────

    /// <summary>A selectable row for lists and pickers, with optional trailing caption.</summary>
    public static bool ListItem(string label, bool selected, string? trailing = null, FontAwesomeIcon? icon = null)
    {
        var visible = Visible(label);
        var w = ImGui.GetContentRegionAvail().X;
        var h = 36 * S;
        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, new Vector2(w, h));
        var hovered = ImGui.IsItemHovered();
        var key = ImGui.GetID(label);
        var t = Hover(key, hovered, ImGui.IsItemActive());
        var sel = Anim.To(key + 1, selected ? 1f : 0f, 16f);
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var dl = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(w, h);
        var fg = Vector4.Lerp(C.OnSurfaceVariant, C.OnSecondaryContainer, sel);

        if (sel > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(C.SecondaryContainer, sel)), h / 2);
        if (t > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(C.OnSurface, 0.08f * t)), h / 2);

        var x = pos.X + 14 * S;
        if (icon is { } glyph)
        {
            DrawIcon(dl, glyph, new Vector2(x + 9 * S, pos.Y + h / 2), fg);
            x += 30 * S;
        }

        var trailingW = 0f;
        if (!string.IsNullOrEmpty(trailing))
        {
            using (Md3Type.Label())
            {
                var ts = ImGui.CalcTextSize(trailing);
                trailingW = ts.X + 10 * S;
                dl.AddText(new Vector2(max.X - ts.X - 14 * S, pos.Y + (h - ts.Y) / 2), U(C.Accent), trailing);
            }
        }

        var text = Truncate(visible, max.X - x - 14 * S - trailingW);
        dl.AddText(new Vector2(x, pos.Y + (h - ImGui.GetTextLineHeight()) / 2), U(fg), text);
        if (text.Length < visible.Length) Tooltip(visible);
        return clicked;
    }

    /// <summary>A navigation drawer destination: icon and label in a pill that fills in when selected.</summary>
    public static bool NavItem(string label, FontAwesomeIcon icon, bool selected, string? tooltip = null)
    {
        var visible = Visible(label);
        var w = ImGui.GetContentRegionAvail().X;
        var h = 40 * S;
        var pos = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, new Vector2(w, h));
        var hovered = ImGui.IsItemHovered();
        var key = ImGui.GetID(label);
        var t = Hover(key, hovered, ImGui.IsItemActive());
        var sel = Anim.Ease(Anim.To(key + 1, selected ? 1f : 0f, 14f));
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var dl = ImGui.GetWindowDrawList();
        var max = pos + new Vector2(w, h);
        var fg = Vector4.Lerp(C.OnSurfaceVariant, C.OnSecondaryContainer, sel);

        if (sel > 0.01f)
        {
            // The pill grows out from the left as it fades in.
            var grow = new Vector2(pos.X + w * (0.55f + 0.45f * sel), max.Y);
            dl.AddRectFilled(pos, grow, U(ColorMath.WithAlpha(C.SecondaryContainer, sel)), h / 2);
        }

        if (t > 0.01f) dl.AddRectFilled(pos, max, U(ColorMath.WithAlpha(C.OnSurface, 0.08f * t)), h / 2);

        DrawIcon(dl, icon, new Vector2(pos.X + 24 * S, pos.Y + h / 2), fg);
        var text = Truncate(visible, w - 60 * S);
        dl.AddText(new Vector2(pos.X + 46 * S, pos.Y + (h - ImGui.GetTextLineHeight()) / 2), U(fg), text);

        if (tooltip != null) Tooltip(tooltip);
        else if (text.Length < visible.Length) Tooltip(visible);
        return clicked;
    }

    // ───────────────────────────── Progress ─────────────────────────────

    public static void LinearProgress(float fraction, float width = 0)
    {
        var w = width > 0 ? width : ImGui.GetContentRegionAvail().X;
        var h = 4 * S;
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(w, h));
        var dl = ImGui.GetWindowDrawList();
        fraction = Math.Clamp(fraction, 0f, 1f);
        dl.AddRectFilled(pos, pos + new Vector2(w, h), U(C.SecondaryContainer), h / 2);
        if (fraction > 0) dl.AddRectFilled(pos, new Vector2(pos.X + w * fraction, pos.Y + h), U(C.Accent), h / 2);
    }

    // ───────────────────────────── Containers ─────────────────────────────

    public enum CardStyle { Filled, Elevated, Outlined, Tonal }

    private static readonly Dictionary<uint, float> CardHeights = new();

    /// <summary>
    /// Begins a Material card. Use with <c>using (Md3.Card("id")) { ... }</c>.
    /// The background is drawn with last frame's height, so cards size themselves to their content.
    /// </summary>
    public static IDisposable Card(string id, float width = 0, CardStyle style = CardStyle.Filled, float minHeight = 0)
        => new CardScope(id, width, style, minHeight);

    /// <summary>An informational banner with a leading icon, like a tip or notice.</summary>
    public static void Banner(string id, FontAwesomeIcon icon, string text)
    {
        using (Card(id, 0, CardStyle.Tonal))
        {
            var start = ImGui.GetCursorPos();
            var iconSize = IconSize(icon);
            DrawIcon(ImGui.GetWindowDrawList(), icon, ImGui.GetCursorScreenPos() + new Vector2(iconSize.X / 2, ImGui.GetTextLineHeight() / 2), C.Accent);
            ImGui.SetCursorPos(start + new Vector2(iconSize.X + 12 * S, 0));
            Body(text, C.OnSecondaryContainer);
        }
    }

    private sealed class CardScope : IDisposable
    {
        private readonly uint id;
        private readonly Vector2 start;
        private readonly float width;
        private readonly float pad;
        private readonly float minHeight;

        public CardScope(string strId, float width, CardStyle style, float minHeight)
        {
            id = ImGui.GetID(strId);
            pad = 16 * S;
            this.minHeight = minHeight;
            start = ImGui.GetCursorScreenPos();
            this.width = width > 0 ? width : ImGui.GetContentRegionAvail().X;

            var dl = ImGui.GetWindowDrawList();
            var height = Math.Max(minHeight, CardHeights.TryGetValue(id, out var h) ? h : 0);
            if (height > 0)
            {
                var max = start + new Vector2(this.width, height);
                var r = 14 * S;
                switch (style)
                {
                    case CardStyle.Filled:
                        dl.AddRectFilled(start, max, U(C.SurfaceContainerHigh), r);
                        dl.AddRect(start, max, U(C.OutlineVariant), r, ImDrawFlags.None, 1 * S);
                        break;
                    case CardStyle.Elevated:
                        dl.AddRectFilled(start + new Vector2(0, 2 * S), max + new Vector2(0, 3 * S), U(ColorMath.WithAlpha(C.Scrim, 0.25f)), r);
                        dl.AddRectFilled(start, max, U(C.SurfaceContainerHigh), r);
                        break;
                    case CardStyle.Outlined:
                        dl.AddRect(start, max, U(C.OutlineVariant), r, ImDrawFlags.None, 1 * S);
                        break;
                    case CardStyle.Tonal:
                        dl.AddRectFilled(start, max, U(C.SecondaryContainer), r);
                        dl.AddRectFilled(start + new Vector2(0, r), new Vector2(start.X + 3 * S, max.Y - r), U(C.Accent), 2 * S);
                        break;
                }
            }

            ImGui.SetCursorScreenPos(start + new Vector2(pad, pad));
            ImGui.BeginGroup();
            ImGui.PushID(strId);
            ImGui.PushItemWidth(this.width - pad * 2);
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + this.width - pad * 2);
        }

        public void Dispose()
        {
            ImGui.PopTextWrapPos();
            ImGui.PopItemWidth();
            ImGui.PopID();
            ImGui.EndGroup();

            var height = Math.Max(minHeight, ImGui.GetItemRectMax().Y + pad - start.Y);
            CardHeights[id] = height;
            ImGui.SetCursorScreenPos(start);
            ImGui.Dummy(new Vector2(width, height));
        }
    }

    /// <summary>
    /// A rounded surface with real inner padding that hosts a scrollable child region.
    /// <code>using (var pane = Md3.Pane("id", size, color)) { if (pane.Visible) { ... } }</code>
    /// A size component of 0 fills the remaining space.
    /// </summary>
    public static PaneScope Pane(string id, Vector2 size, Vector4 background, float padDp = 16, float radiusDp = 16, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id, size, background, padDp, radiusDp, flags);

    public sealed class PaneScope : IDisposable
    {
        private readonly Vector2 start;
        private readonly Vector2 size;

        /// <summary>True when the pane is on screen and its content should be drawn.</summary>
        public bool Visible { get; }

        internal PaneScope(string id, Vector2 requested, Vector4 background, float padDp, float radiusDp, ImGuiWindowFlags flags)
        {
            var avail = ImGui.GetContentRegionAvail();
            size = new Vector2(requested.X > 0 ? requested.X : avail.X, requested.Y > 0 ? requested.Y : avail.Y);
            size = Vector2.Max(size, new Vector2(padDp * 2 * S + 8 * S));
            start = ImGui.GetCursorScreenPos();

            if (background.W > 0)
                ImGui.GetWindowDrawList().AddRectFilled(start, start + size, U(background), radiusDp * S);

            var pad = new Vector2(padDp * S);
            ImGui.SetCursorScreenPos(start + pad);
            // Raw Begin/End: the child stays open until Dispose, across the caller's block.
            Visible = ImGui.BeginChild(id, size - pad * 2, false, flags);
        }

        public void Dispose()
        {
            ImGui.EndChild();
            ImGui.SetCursorScreenPos(start);
            ImGui.Dummy(size);
        }
    }

    private static readonly Dictionary<uint, bool> ExpanderOpen = new();
    private static readonly Dictionary<uint, float> ExpanderHeights = new();

    /// <summary>
    /// A collapsible section with an icon, title and chevron; the body slides open.
    /// <code>using (var section = Md3.Expander("id", icon, "Title")) { if (section.Open) { ... } }</code>
    /// </summary>
    public static ExpanderScope Expander(string id, FontAwesomeIcon icon, string title, string? caption = null, bool defaultOpen = false)
        => new(id, icon, title, caption, defaultOpen);

    public sealed class ExpanderScope : IDisposable
    {
        private readonly uint id;
        private readonly Vector2 start;
        private readonly float width;
        private readonly float headerH;
        private readonly float bodyH;
        private readonly bool childBegun;

        /// <summary>True when the body is visible and should be drawn.</summary>
        public bool Open { get; }

        internal ExpanderScope(string strId, FontAwesomeIcon icon, string title, string? caption, bool defaultOpen)
        {
            id = ImGui.GetID(strId);
            if (!ExpanderOpen.TryGetValue(id, out var open)) open = defaultOpen;

            headerH = 48 * S;
            width = ImGui.GetContentRegionAvail().X;
            start = ImGui.GetCursorScreenPos();

            var clicked = ImGui.InvisibleButton(strId, new Vector2(width, headerH));
            if (clicked) open = !open;
            ExpanderOpen[id] = open;
            var hovered = ImGui.IsItemHovered();
            if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            var t = Anim.Ease(Anim.To(id, open ? 1f : 0f, 12f));
            var hover = Hover(id + 1, hovered, ImGui.IsItemActive());
            bodyH = ExpanderHeights.GetValueOrDefault(id, 0f) * t;

            var dl = ImGui.GetWindowDrawList();
            var max = start + new Vector2(width, headerH + bodyH);
            var r = 14 * S;
            dl.AddRectFilled(start, max, U(C.SurfaceContainerHigh), r);
            dl.AddRect(start, max, U(C.OutlineVariant), r, ImDrawFlags.None, 1 * S);
            if (hover > 0.01f)
                dl.AddRectFilled(start, start + new Vector2(width, headerH), U(ColorMath.WithAlpha(C.OnSurface, 0.06f * hover)), r);

            DrawIcon(dl, icon, start + new Vector2(26 * S, headerH / 2), C.Accent);
            var lineH = ImGui.GetTextLineHeight();
            dl.AddText(start + new Vector2(50 * S, (headerH - lineH) / 2), U(C.OnSurface), title);
            if (!string.IsNullOrEmpty(caption))
            {
                var titleW = ImGui.CalcTextSize(title).X;
                using (Md3Type.Label())
                    dl.AddText(start + new Vector2(50 * S + titleW + 12 * S, (headerH - ImGui.GetTextLineHeight()) / 2), U(C.OnSurfaceVariant), caption);
            }

            DrawIcon(dl, t > 0.5f ? FontAwesomeIcon.ChevronUp : FontAwesomeIcon.ChevronDown,
                     start + new Vector2(width - 24 * S, headerH / 2), C.OnSurfaceVariant);

            if (open || t > 0.01f)
            {
                ImGui.SetCursorScreenPos(start + new Vector2(18 * S, headerH));
                // Raw Begin/End: the child has to stay open until Dispose, across the caller's block.
                Open = ImGui.BeginChild($"##expander-body-{strId}", new Vector2(width - 36 * S, Math.Max(2 * S, bodyH)), false,
                                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                childBegun = true;
            }
        }

        public void Dispose()
        {
            if (childBegun)
            {
                if (Open) ExpanderHeights[id] = ImGui.GetCursorPosY() + 14 * S;
                ImGui.EndChild();
            }

            ImGui.SetCursorScreenPos(start);
            ImGui.Dummy(new Vector2(width, headerH + bodyH));
        }
    }

    /// <summary>Shortens text with an ellipsis so it fits <paramref name="maxWidth"/> in the current font.</summary>
    public static string Fit(string text, float maxWidth) => Truncate(text, maxWidth);

    private static string Truncate(string text, float maxWidth)
    {
        if (ImGui.CalcTextSize(text).X <= maxWidth) return text;
        for (var len = text.Length - 1; len > 1; len--)
        {
            var candidate = text[..len] + "…";
            if (ImGui.CalcTextSize(candidate).X <= maxWidth) return candidate;
        }

        return text.Length > 0 ? text[..1] : text;
    }
}
