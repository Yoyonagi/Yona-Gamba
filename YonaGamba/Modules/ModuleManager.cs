using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Windowing;

namespace YonaGamba.Modules;

/// <summary>
/// Discovers every <see cref="IModule"/> in this assembly, restores its on/off state
/// and isolates failures so one broken module never takes the toolbox down.
/// </summary>
public sealed class ModuleManager : IDisposable
{
    private readonly Configuration configuration;
    private readonly List<IModule> modules = new();
    private readonly Dictionary<string, string> errors = new();

    public IReadOnlyList<IModule> Modules => modules;

    public ModuleManager(Configuration configuration, WindowSystem windowSystem)
    {
        this.configuration = configuration;
        var context = new ModuleContext(configuration, windowSystem);

        var types = typeof(ModuleManager).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        typeof(IModule).IsAssignableFrom(t) &&
                        t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in types)
        {
            try
            {
                var module = (IModule)Activator.CreateInstance(type)!;
                module.Initialize(context);
                modules.Add(module);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Failed to load module {type.Name}");
            }
        }

        modules.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Name, b.Name));

        foreach (var module in modules)
        {
            var enabled = configuration.EnabledModules.TryGetValue(module.Id, out var saved) ? saved : module.EnabledByDefault;
            if (enabled) TryEnable(module);
        }

        Plugin.Log.Information($"Loaded {modules.Count} module(s), {modules.Count(m => m.IsEnabled)} enabled.");
    }

    public IModule? Get(string id) => modules.FirstOrDefault(m => m.Id == id);

    public string? GetError(IModule module) => errors.GetValueOrDefault(module.Id);

    public void SetEnabled(IModule module, bool enabled)
    {
        if (enabled) TryEnable(module);
        else TryDisable(module);

        configuration.EnabledModules[module.Id] = enabled;
        configuration.Save();
    }

    private void TryEnable(IModule module)
    {
        try
        {
            module.Enable();
            errors.Remove(module.Id);
        }
        catch (Exception ex)
        {
            errors[module.Id] = ex.Message;
            Plugin.Log.Error(ex, $"Failed to enable module {module.Id}");
            TryDisable(module);
        }
    }

    private void TryDisable(IModule module)
    {
        try
        {
            module.Disable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"Failed to disable module {module.Id}");
        }
    }

    public void Dispose()
    {
        for (var i = modules.Count - 1; i >= 0; i--)
        {
            try
            {
                modules[i].Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"Failed to dispose module {modules[i].Id}");
            }
        }

        modules.Clear();
    }
}
