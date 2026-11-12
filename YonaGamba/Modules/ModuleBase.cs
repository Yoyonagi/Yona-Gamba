using System.Collections.Generic;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using YonaGamba.UI;

namespace YonaGamba.Modules;

/// <summary>Shared state handed to every module.</summary>
public sealed record ModuleContext(Configuration Configuration, WindowSystem WindowSystem);

/// <summary>
/// Convenience base class for modules. Override <see cref="OnEnable"/> / <see cref="OnDisable"/>
/// to hook and unhook game events; windows registered with <see cref="AddWindow"/> are removed
/// automatically when the module is disabled.
/// </summary>
public abstract class ModuleBase : IModule
{
    private readonly List<Window> windows = new();

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual FontAwesomeIcon Icon => FontAwesomeIcon.PuzzlePiece;
    public virtual int Order => 100;
    public virtual bool EnabledByDefault => false;
    public virtual bool HasPage => true;
    public virtual bool HasSettings => false;
    public bool IsEnabled { get; private set; }

    protected ModuleContext Context { get; private set; } = null!;

    public void Initialize(ModuleContext context)
    {
        Context = context;
        LoadState();
        OnInitialize();
    }

    public void Enable()
    {
        if (IsEnabled) return;
        OnEnable();
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled) return;
        IsEnabled = false;
        try
        {
            OnDisable();
        }
        finally
        {
            foreach (var w in windows) Context.WindowSystem.RemoveWindow(w);
            windows.Clear();
        }
    }

    public virtual void DrawPage() => Md3.Supporting("This module has no page.");

    public virtual void DrawSettings() { }

    public virtual void Dispose() => Disable();

    /// <summary>Called once after construction, before the module is enabled.</summary>
    protected virtual void OnInitialize() { }

    /// <summary>Hook events, register windows, etc.</summary>
    protected virtual void OnEnable() { }

    /// <summary>Undo everything done in <see cref="OnEnable"/>.</summary>
    protected virtual void OnDisable() { }

    /// <summary>Registers a window for the lifetime of the enabled module. Derive it from <see cref="Md3Window"/>.</summary>
    protected T AddWindow<T>(T window) where T : Window
    {
        Context.WindowSystem.AddWindow(window);
        windows.Add(window);
        return window;
    }

    private protected virtual void LoadState() { }
}

/// <summary>A module with its own persisted settings object.</summary>
public abstract class ModuleBase<TConfig> : ModuleBase where TConfig : class, new()
{
    protected TConfig Config { get; private set; } = new();

    protected void SaveConfig() => Context.Configuration.SaveModuleData(Id, Config);

    private protected override void LoadState() => Config = Context.Configuration.LoadModuleData<TConfig>(Id);
}
