using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;

namespace Goetia.Services;

/// <summary>Dalamud services injected at plugin startup.</summary>
internal static class PluginServices
{
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    internal static ICommandManager CommandManager { get; private set; } = null!;
    internal static IFramework Framework { get; private set; } = null!;
    internal static IClientState ClientState { get; private set; } = null!;
    internal static IObjectTable ObjectTable { get; private set; } = null!;
    internal static IChatGui ChatGui { get; private set; } = null!;
    internal static IPluginLog Log { get; private set; } = null!;
    internal static IGameGui GameGui { get; private set; } = null!;
    internal static ICondition Condition { get; private set; } = null!;
    internal static IPartyList PartyList { get; private set; } = null!;

    internal static void Init(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IFramework framework,
        IClientState clientState,
        IObjectTable objectTable,
        IChatGui chatGui,
        IPluginLog log,
        IGameGui gameGui,
        ICondition condition,
        IPartyList partyList)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        Framework = framework;
        ClientState = clientState;
        ObjectTable = objectTable;
        ChatGui = chatGui;
        Log = log;
        GameGui = gameGui;
        Condition = condition;
        PartyList = partyList;
    }

    internal static void Clear()
    {
        PluginInterface = null!;
        CommandManager = null!;
        Framework = null!;
        ClientState = null!;
        ObjectTable = null!;
        ChatGui = null!;
        Log = null!;
        GameGui = null!;
        Condition = null!;
        PartyList = null!;
    }
}
