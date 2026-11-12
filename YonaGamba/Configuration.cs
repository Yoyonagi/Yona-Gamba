using System;
using System.Collections.Generic;
using System.Text.Json;
using Dalamud.Configuration;

namespace YonaGamba;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    // Appearance
    public bool DarkMode { get; set; } = true;

    // Main window
    public bool LockWindow { get; set; }
    public string LastPage { get; set; } = "minigames";

    // Modules: on/off state and each module's own settings (stored as JSON).
    public Dictionary<string, bool> EnabledModules { get; set; } = new();
    public Dictionary<string, string> ModuleData { get; set; } = new();

    public T LoadModuleData<T>(string moduleId) where T : class, new()
    {
        if (ModuleData.TryGetValue(moduleId, out var json))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, $"Could not read settings for module '{moduleId}', using defaults.");
            }
        }

        return new T();
    }

    public void SaveModuleData<T>(string moduleId, T data)
    {
        ModuleData[moduleId] = JsonSerializer.Serialize(data);
        Save();
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
