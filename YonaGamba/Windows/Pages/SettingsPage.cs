using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using YonaGamba.Theme;
using YonaGamba.UI;

namespace YonaGamba.Windows.Pages;

/// <summary>Collapsible sections for the plugin itself and for its module that has options.</summary>
public sealed class SettingsPage
{
    private static readonly string[] ThemeModes = { "Dark", "Light" };
    private readonly Plugin plugin;

    public SettingsPage(Plugin plugin) => this.plugin = plugin;

    private Configuration Config => plugin.Configuration;

    public void Draw()
    {
        using var scroll = ImRaii.Child("##settings-scroll", Vector2.Zero, false);
        if (!scroll.Success) return;

        using (var section = Md3.Expander("appearance", FontAwesomeIcon.Palette, "Appearance", null, true))
        {
            if (section.Open)
            {
                Md3.Label("THEME");
                var mode = Config.DarkMode ? 0 : 1;
                if (Md3.SegmentedButtons("theme-mode", ThemeModes, ref mode, 220 * Md3Theme.Scale))
                {
                    Config.DarkMode = mode == 0;
                    Md3Theme.Apply(Config.DarkMode);
                    Config.Save();
                }
            }
        }

        using (var section = Md3.Expander("window", FontAwesomeIcon.WindowMaximize, "Window"))
        {
            if (section.Open)
            {
                var locked = Config.LockWindow;
                if (Md3.SwitchRow("Lock window", "Keep the main window where it is; it can't be moved or resized.", ref locked))
                {
                    Config.LockWindow = locked;
                    Config.Save();
                }
            }
        }

        foreach (var module in plugin.Modules.Modules)
        {
            if (module is not { IsEnabled: true, HasSettings: true }) continue;

            using var section = Md3.Expander($"module-{module.Id}", module.Icon, module.Name, "Module");
            if (!section.Open) continue;

            try
            {
                module.DrawSettings();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Module {module.Id} threw while drawing settings");
                Md3.Body(ex.Message, Md3Theme.Scheme.Error);
            }
        }

        using (var section = Md3.Expander("about", FontAwesomeIcon.InfoCircle, "About"))
        {
            if (section.Open)
            {
                Md3.Body($"Yona Gamba v{Plugin.PluginInterface.Manifest.AssemblyVersion}");
                Md3.Supporting("Commands: /ygamba, /ygamba settings.");
            }
        }
    }
}
