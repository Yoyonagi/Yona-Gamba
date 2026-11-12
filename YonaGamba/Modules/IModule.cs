using System;
using Dalamud.Interface;

namespace YonaGamba.Modules;

/// <summary>
/// A self-contained feature hosted by the toolbox. Implementations are discovered
/// automatically by <see cref="ModuleManager"/>; most modules should derive from
/// <see cref="ModuleBase"/> or <see cref="ModuleBase{TConfig}"/> instead of implementing this directly.
/// </summary>
public interface IModule : IDisposable
{
    /// <summary>Stable identifier used for saved state and "/yona &lt;id&gt;". Never change it after release.</summary>
    string Id { get; }

    string Name { get; }
    string Description { get; }
    FontAwesomeIcon Icon { get; }

    /// <summary>Sort order in the navigation rail and on the Home page.</summary>
    int Order { get; }

    bool EnabledByDefault { get; }

    /// <summary>Whether the module gets its own destination in the navigation rail.</summary>
    bool HasPage { get; }

    /// <summary>Whether <see cref="DrawSettings"/> should appear on the Settings page.</summary>
    bool HasSettings { get; }

    bool IsEnabled { get; }

    void Initialize(ModuleContext context);
    void Enable();
    void Disable();

    void DrawPage();
    void DrawSettings();
}
