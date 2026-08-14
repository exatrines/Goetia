using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Goetia.Modules;
using Goetia.UI;

namespace Goetia;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/goetia";

    internal static Configuration C = null!;

    private readonly WindowSystem _windowSystem;
    private readonly ConfigWindow _configWindow;
    private readonly PluginSettingsWindow _settingsWindow;
    private readonly PartyOrderService _partyOrder;
    private readonly HighlightStore _highlightStore;
    private readonly HotbarHighlightService _hotbarHighlight;
    private readonly ModuleHost _moduleHost;
    private readonly PreviewOverlay _previewOverlay;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IFramework framework,
        IClientState clientState,
        IObjectTable objectTable,
        ITextureProvider textureProvider,
        IChatGui chatGui,
        IPluginLog log,
        IGameGui gameGui,
        ICondition condition,
        IPartyList partyList)
    {
        PluginServices.Init(
            pluginInterface,
            commandManager,
            framework,
            clientState,
            objectTable,
            chatGui,
            log,
            gameGui,
            condition,
            partyList);

        C = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        C.Initialize(pluginInterface);
        C.ThemeColors ??= MirageColorSettings.CreateDefault();

        MirageUi.ConfigureTheme(() => C.ThemeColors ?? MirageColorSettings.CreateDefault());
        MirageUi.Init(pluginInterface, textureProvider, log);

        _partyOrder = new PartyOrderService();
        _highlightStore = new HighlightStore();
        _hotbarHighlight = new HotbarHighlightService(_highlightStore);
        _moduleHost = new ModuleHost(_partyOrder, _highlightStore);
        _previewOverlay = new PreviewOverlay(_partyOrder, _highlightStore, _moduleHost);

        _settingsWindow = new PluginSettingsWindow();
        _configWindow = new ConfigWindow(_moduleHost, TogglePluginSettings);
        _windowSystem = new WindowSystem("Goetia");
        _windowSystem.AddWindow(_configWindow);
        _windowSystem.AddWindow(_settingsWindow);

        pluginInterface.UiBuilder.Draw += DrawUi;
        pluginInterface.UiBuilder.OpenConfigUi += TogglePluginSettings;
        pluginInterface.UiBuilder.OpenMainUi += ToggleMain;

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle Goetia. /goetia settings for Hotbars & Style.",
        });

        framework.Update += OnFrameworkUpdate;
    }

    private void OnCommand(string command, string args)
    {
        var token = (args ?? string.Empty).Trim();
        if (token.Length == 0)
        {
            ToggleMain();
            return;
        }

        if (token.Equals("config", StringComparison.OrdinalIgnoreCase)
            || token.Equals("settings", StringComparison.OrdinalIgnoreCase)
            || token.Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            TogglePluginSettings();
            return;
        }

        PluginServices.ChatGui.PrintError(
            $"Unknown argument: {token}. Use /goetia or /goetia settings.");
    }

    private void ToggleMain() => _configWindow.Toggle();

    private void TogglePluginSettings() => _settingsWindow.Toggle();

    private void OnFrameworkUpdate(IFramework framework) => _moduleHost.Update();

    private void DrawUi()
    {
        _windowSystem.Draw();
        _hotbarHighlight.Draw();
        _previewOverlay.Draw();
    }

    public void Dispose()
    {
        PluginServices.Framework.Update -= OnFrameworkUpdate;
        PluginServices.CommandManager.RemoveHandler(CommandName);
        PluginServices.PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginServices.PluginInterface.UiBuilder.OpenConfigUi -= TogglePluginSettings;
        PluginServices.PluginInterface.UiBuilder.OpenMainUi -= ToggleMain;

        C.Save();
        MirageUi.Dispose();

        _windowSystem.RemoveAllWindows();
        PluginServices.Clear();
        C = null!;
    }
}
