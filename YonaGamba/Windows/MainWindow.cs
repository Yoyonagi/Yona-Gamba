using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using YonaGamba.Modules;
using YonaGamba.Theme;
using YonaGamba.UI;
using YonaGamba.Windows.Pages;

namespace YonaGamba.Windows;

/// <summary>
/// The main window. There is no native title bar: a navigation drawer sits on the left,
/// and the page on the right has its own header with an action bar (links, lock, minimize, close).
/// Minimizing shrinks the window to a small bar that can be parked anywhere.
/// </summary>
public sealed class MainWindow : Md3Window
{
    public const string SettingsPageId = "settings";

    private const ImGuiWindowFlags BaseFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    private static readonly Vector2 DefaultSize = new(1000, 680);
    private static readonly Vector2 MinimumSize = new(820, 520);
    private static readonly Vector2 MinimizedSize = new(300, 64);

    private readonly Plugin plugin;
    private readonly SettingsPage settings;
    private string page;
    private bool restartFade = true;

    private bool minimized;
    private bool restoreSize;
    private Vector2 expandedSize = DefaultSize;

    public MainWindow(Plugin plugin) : base("Yona Gamba###YonaGambaMain", BaseFlags)
    {
        this.plugin = plugin;
        settings = new SettingsPage(plugin);
        page = plugin.Configuration.LastPage;

        Size = DefaultSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = Constraints(MinimumSize);
    }

    public void Open(string pageId)
    {
        Navigate(pageId);
        if (minimized) Restore();
        IsOpen = true;
    }

    public void Navigate(string pageId)
    {
        if (page == pageId) return;
        page = pageId;
        restartFade = true;
        plugin.Configuration.LastPage = pageId;
        plugin.Configuration.Save();
    }

    public override void OnOpen() => restartFade = true;

    public override void PreDraw()
    {
        base.PreDraw();

        if (minimized)
        {
            Flags = BaseFlags | ImGuiWindowFlags.NoResize | (plugin.Configuration.LockWindow ? ImGuiWindowFlags.NoMove : ImGuiWindowFlags.None);
            Size = MinimizedSize;
            SizeCondition = ImGuiCond.Always;
            SizeConstraints = Constraints(MinimizedSize);
            return;
        }

        Flags = plugin.Configuration.LockWindow
            ? BaseFlags | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize
            : BaseFlags;
        SizeConstraints = Constraints(MinimumSize);

        if (restoreSize)
        {
            // One frame of "Always" puts the window back to the size it had before minimizing.
            Size = expandedSize;
            SizeCondition = ImGuiCond.Always;
            restoreSize = false;
        }
        else
        {
            SizeCondition = ImGuiCond.FirstUseEver;
        }
    }

    public override void Draw()
    {
        if (minimized)
        {
            DrawMinimized();
            return;
        }

        var s = Md3Theme.Scale;

        using (var drawer = ImRaii.Child("##yona-drawer", new Vector2(212 * s, 0), false, ImGuiWindowFlags.NoScrollbar))
        {
            if (drawer.Success) DrawDrawer();
        }

        ImGui.SameLine(0, 14 * s);

        // The page sits on a rounded, slightly darker pane with its own padding.
        using (var pane = Md3.Pane("##yona-content", Vector2.Zero, Md3Theme.Scheme.SurfaceContainerLow, 22, 18,
                                   ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (!pane.Visible) return;

            // Each page fades and slides in when it becomes active.
            if (restartFade)
            {
                Anim.Set("##page-fade", 0f);
                restartFade = false;
            }

            var fade = Anim.Ease(Anim.To("##page-fade", 1f, 9f));
            DrawHeader();

            using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, Math.Max(0.02f, fade)))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (1f - fade) * 14 * s);
                DrawPage();
            }

            Snackbar.Draw();
        }
    }

    // ───────────────────────────── Minimize ─────────────────────────────

    private static WindowSizeConstraints Constraints(Vector2 minimum) => new()
    {
        MinimumSize = minimum,
        MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
    };

    private void Minimize()
    {
        // Window.Size is in unscaled units; Dalamud applies the global scale itself.
        expandedSize = ImGui.GetWindowSize() / Md3Theme.Scale;
        minimized = true;
    }

    private void Restore()
    {
        minimized = false;
        restoreSize = true;
        restartFade = true;
    }

    private void DrawMinimized()
    {
        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;
        var start = ImGui.GetCursorPos();

        Md3.IconBadge(FontAwesomeIcon.Dice, 36, c.Primary, c.OnPrimary, 11);
        ImGui.SetCursorPos(start + new Vector2(46 * s, (36 * s - ImGui.GetTextLineHeight()) / 2));
        ImGui.Text("Yona Gamba");

        ImGui.SetCursorPos(start + new Vector2(0, -2 * s));
        Md3.BeginActionBar(2);
        if (Md3.IconButton("##min-restore", FontAwesomeIcon.WindowMaximize, "Restore the window"))
            Restore();
        ImGui.SameLine(0, 0);
        if (Md3.IconButton("##min-close", FontAwesomeIcon.Times, "Close"))
            IsOpen = false;
    }

    // ───────────────────────────── Drawer ─────────────────────────────

    private void DrawDrawer()
    {
        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;

        // Brand block
        using (Md3.Card("brand", 0, Md3.CardStyle.Filled))
        {
            var start = ImGui.GetCursorPos();
            Md3.IconBadge(FontAwesomeIcon.Dice, 40, c.Primary, c.OnPrimary, 12);
            ImGui.SetCursorPos(start + new Vector2(52 * s, 2 * s));
            ImGui.Text("Yona Gamba");
            ImGui.SetCursorPos(start + new Vector2(52 * s, 2 * s + ImGui.GetTextLineHeight() + 2 * s));
            Md3.Label($"v{Plugin.PluginInterface.Manifest.AssemblyVersion}");
            ImGui.SetCursorPos(start + new Vector2(0, 40 * s));
            ImGui.Dummy(Vector2.Zero);
        }

        Md3.Spacer(6);
        foreach (var module in plugin.Modules.Modules)
        {
            if (module is not { IsEnabled: true, HasPage: true }) continue;
            NavEntry(module.Id, module.Icon, module.Name, module.Description);
        }

        // Settings stays pinned to the bottom of the drawer.
        var bottom = ImGui.GetWindowHeight() - 40 * s - 2 * s;
        if (ImGui.GetCursorPosY() < bottom) ImGui.SetCursorPosY(bottom);
        NavEntry(SettingsPageId, FontAwesomeIcon.Cog, "Settings", "Appearance and module options");
    }

    private void NavEntry(string id, FontAwesomeIcon icon, string label, string tooltip)
    {
        if (Md3.NavItem($"{label}##nav-{id}", icon, page == id, tooltip))
            Navigate(id);
    }

    // ───────────────────────────── Header ─────────────────────────────

    private void DrawHeader()
    {
        const int actions = 5;
        var s = Md3Theme.Scale;
        var (title, subtitle) = PageInfo();
        var top = ImGui.GetCursorPos();

        // Title and subtitle never run under the action bar: they are shortened to fit.
        var textWidth = Math.Max(60 * s, ImGui.GetContentRegionAvail().X - (actions * 36 + 8) * s - 20 * s);
        using (Md3Type.Headline()) title = Md3.Fit(title, textWidth);
        Md3.Headline(title);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2 * s);
        var fitted = subtitle;
        using (Md3Type.Label()) fitted = Md3.Fit(subtitle, textWidth);
        Md3.Label(fitted);
        if (fitted.Length < subtitle.Length) Md3.Tooltip(subtitle);

        // Top-right action bar
        ImGui.SetCursorPos(top);
        Md3.BeginActionBar(actions);
        if (Md3.IconButton("##act-github", FontAwesomeIcon.Code, "Source code on GitHub"))
            Util.OpenLink("https://github.com/Yoyonagi/Yona-Gamba");
        ImGui.SameLine(0, 0);
        if (Md3.IconButton("##act-issue", FontAwesomeIcon.Bug, "Report a problem"))
            Util.OpenLink("https://github.com/Yoyonagi/Yona-Gamba/issues");
        ImGui.SameLine(0, 0);

        var locked = plugin.Configuration.LockWindow;
        if (Md3.IconButton("##act-lock", locked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
                           locked ? "Window is locked in place. Click to unlock." : "Lock the window's position and size", locked))
        {
            plugin.Configuration.LockWindow = !locked;
            plugin.Configuration.Save();
        }

        ImGui.SameLine(0, 0);
        if (Md3.IconButton("##act-minimize", FontAwesomeIcon.WindowMinimize, "Minimize to a small bar"))
            Minimize();
        ImGui.SameLine(0, 0);
        if (Md3.IconButton("##act-close", FontAwesomeIcon.Times, "Close"))
            IsOpen = false;

        ImGui.SetCursorPos(new Vector2(top.X, top.Y + 64 * s));
        ImGui.Dummy(Vector2.Zero);
    }

    private (string Title, string Subtitle) PageInfo()
    {
        if (page == SettingsPageId) return ("Settings", "Appearance, window and per-module options.");
        if (CurrentModule() is { } module) return (module.Name, module.Description);
        return ("Yona Gamba", string.Empty);
    }

    private IModule? CurrentModule() =>
        plugin.Modules.Get(page) is { IsEnabled: true, HasPage: true } module ? module : null;

    // ───────────────────────────── Page ─────────────────────────────

    private void DrawPage()
    {
        if (page == SettingsPageId)
        {
            settings.Draw();
            return;
        }

        // Anything that isn't settings shows the plugin's own page.
        var module = CurrentModule() ?? plugin.Modules.Modules.FirstOrDefault(m => m is { IsEnabled: true, HasPage: true });
        if (module == null)
        {
            Md3.Supporting("Nothing to show: the module failed to start. Check /xllog for details.");
            return;
        }

        page = module.Id;

        try
        {
            module.DrawPage();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Module {module.Id} threw while drawing");
            using (Md3.Card($"err-{module.Id}"))
            {
                Md3.Title("Something went wrong", Md3Theme.Scheme.Error);
                Md3.Body(ex.Message);
            }
        }
    }
}
