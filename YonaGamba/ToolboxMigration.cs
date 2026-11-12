using System;
using System.IO;
using System.Text.Json;

namespace YonaGamba;

/// <summary>
/// One-time import from Yona Toolbox, the plugin this one was split out of: the first time
/// Yona Gamba runs it takes over the theme choice, its module's settings and (for notes) the files.
/// </summary>
public static class ToolboxMigration
{
    public static void Run(Configuration config, string moduleId, bool copyNotes)
    {
        try
        {
            var configs = Plugin.PluginInterface.ConfigDirectory.Parent;
            if (configs == null) return;

            var oldConfig = Path.Combine(configs.FullName, "YonaToolbox.json");
            if (File.Exists(oldConfig))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(oldConfig));
                var old = doc.RootElement;
                if (old.TryGetProperty("DarkMode", out var dark) && dark.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    config.DarkMode = dark.GetBoolean();
                if (old.TryGetProperty("ModuleData", out var data) && data.ValueKind == JsonValueKind.Object &&
                    data.TryGetProperty(moduleId, out var module) && module.ValueKind == JsonValueKind.String)
                    config.ModuleData[moduleId] = module.GetString()!;
                Plugin.Log.Information("Imported settings from Yona Toolbox.");
            }

            var oldNotes = Path.Combine(configs.FullName, "YonaToolbox", "InstanceNotes");
            if (copyNotes && Directory.Exists(oldNotes))
            {
                var target = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "InstanceNotes");
                Directory.CreateDirectory(target);
                var copied = 0;
                foreach (var file in Directory.GetFiles(oldNotes, "*.md"))
                {
                    var destination = Path.Combine(target, Path.GetFileName(file));
                    if (File.Exists(destination)) continue;
                    File.Copy(file, destination);
                    copied++;
                }

                Plugin.Log.Information($"Copied {copied} note(s) from Yona Toolbox.");
            }

            config.Save();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Could not import data from Yona Toolbox; starting fresh.");
        }
    }
}
