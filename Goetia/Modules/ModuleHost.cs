using Dalamud.Game.ClientState.Conditions;

namespace Goetia.Modules;

/// <summary>Runs builtin modules and applies their highlights.</summary>
internal sealed class ModuleHost
{
    private readonly PartyOrderService _party;
    private readonly HighlightStore _store;
    private readonly List<HighlightEntry> _frameBuffer = [];
    private readonly ModuleContext _context;
    private readonly List<GoetiaModule> _modules;

    private uint _lastTerritory;
    private bool _wasInCombat;
    private bool _loggedIn;

    public ModuleHost(PartyOrderService party, HighlightStore store)
    {
        _party = party;
        _store = store;
        _context = new ModuleContext(party, _frameBuffer);
        _modules =
        [
            new DynamisDeltaModule(),
            new DynamisSigmaModule(),
            new DynamisOmegaModule(),
        ];

        foreach (var module in _modules)
            module.IsEnabled = C.GetModuleEnabled(module.Id, module.EnabledDefault);
    }

    public IReadOnlyList<GoetiaModule> Modules => _modules;

    public void SetEnabled(GoetiaModule module, bool enabled)
    {
        if (module.IsEnabled == enabled)
            return;

        module.IsEnabled = enabled;
        C.SetModuleEnabled(module.Id, enabled);
        C.Save();

        if (!enabled)
        {
            SafeReset(module);
            _store.ClearSource(module.Id);
        }
    }

    public void Update()
    {
        if (!PluginServices.ClientState.IsLoggedIn)
        {
            if (_loggedIn)
            {
                ResetAll();
                _store.ClearAll();
            }

            _loggedIn = false;
            return;
        }

        _loggedIn = true;
        _party.Refresh();

        var territory = PluginServices.ClientState.TerritoryType;
        var inCombat = PluginServices.Condition[ConditionFlag.InCombat];

        if (territory != _lastTerritory)
        {
            ResetAll();
            _store.ClearAll();
            _lastTerritory = territory;
        }
        else if (_wasInCombat && !inCombat)
        {
            ResetAll();
            _store.ClearAll();
        }

        _wasInCombat = inCombat;

        foreach (var module in _modules)
            TickModule(module, territory);
    }

    private void TickModule(GoetiaModule module, uint territory)
    {
        if (!module.IsEnabled || !IsTerritoryValid(module, territory))
        {
            _store.ClearSource(module.Id);
            return;
        }

        try
        {
            _context.BeginFrame();
            module.OnUpdate(_context);
            _store.Replace(module.Id, _frameBuffer);
        }
        catch (Exception ex)
        {
            PluginServices.Log.Error(ex, "Goetia: OnUpdate {0}", module.Id);
            _store.ClearSource(module.Id);
        }
    }

    private void ResetAll()
    {
        foreach (var module in _modules)
            SafeReset(module);
    }

    private static void SafeReset(GoetiaModule module)
    {
        try
        {
            module.OnReset();
        }
        catch (Exception ex)
        {
            PluginServices.Log.Error(ex, "Goetia: OnReset {0}", module.Id);
        }
    }

    private static bool IsTerritoryValid(GoetiaModule module, uint territory)
    {
        var set = module.ValidTerritories;
        return set == null || set.Count == 0 || set.Contains(territory);
    }
}
