using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using YonaGamba.Modules;
using YonaGamba.Theme;
using YonaGamba.Windows;

namespace YonaGamba;

public sealed class Plugin : IDalamudPlugin
{
    // Dalamud services, available to every module.
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public const string CommandName = "/ygamba";

    public Configuration Configuration { get; }
    public WindowSystem WindowSystem { get; } = new("YonaGamba");
    public ModuleManager Modules { get; }
    public MainWindow MainWindow { get; }

    public Plugin()
    {
        var saved = PluginInterface.GetPluginConfig() as Configuration;
        Configuration = saved ?? new Configuration();
        if (saved == null) ToolboxMigration.Run(Configuration, "minigames", false);

        Md3Theme.Apply(Configuration.DarkMode);
        Md3Type.Initialize(PluginInterface.UiBuilder);

        Modules = new ModuleManager(Configuration, WindowSystem);
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Yona Gamba. \"/ygamba settings\" opens its settings.",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        CommandManager.RemoveHandler(CommandName);

        WindowSystem.RemoveAllWindows();
        Modules.Dispose();
        Md3Type.Dispose();
    }

    private void OpenMainUi() => MainWindow.IsOpen = true;

    private void OpenConfigUi() => MainWindow.Open(MainWindow.SettingsPageId);

    private void OnCommand(string command, string args)
    {
        var arg = args.Trim();
        if (arg.Length == 0)
        {
            MainWindow.Toggle();
            return;
        }

        if (arg is "settings" or "config")
        {
            MainWindow.Open(MainWindow.SettingsPageId);
            return;
        }

        ChatGui.PrintError($"[Yona] Unknown option \"{arg}\". Try {CommandName} or {CommandName} settings.");
    }
}
