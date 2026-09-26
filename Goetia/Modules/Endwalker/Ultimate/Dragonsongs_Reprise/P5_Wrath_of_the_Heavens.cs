namespace Goetia.Modules;

/// <summary>DSR P5 Wrath of the Heavens — Thunderstruck highlight.</summary>
internal sealed class WrathOfTheHeavensModule : GoetiaModule
{
    private bool _active;
    private bool _sawThunderstruck;

    public override string Id => Configuration.ModuleIdWrathOfTheHeavens;
    public override string DisplayName => "Wrath of the Heavens";
    public override IReadOnlySet<uint>? ValidTerritories => Dsr.Territories;

    private WrathConfig Config => GetConfig<WrathConfig>();

    public override void OnReset()
    {
        _active = false;
        _sawThunderstruck = false;
    }

    public override void OnUpdate(ModuleContext ctx)
    {
        TickActive(ctx);
        if (!_active || !_sawThunderstruck)
            return;

        var c = Config;
        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || !ctx.HasStatus(i, Dsr.StatusThunderstruck))
                continue;
            ctx.SetHighlight(i, c.ThunderstruckHotbar, c.ThunderstruckColor);
        }
    }

    public override void DrawConfig()
    {
        var c = Config;
        MirageUi.SubHeader("Rules");
        MirageUi.Text("Territory: DSR (968)", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"Start: Wrath of the Heavens cast ({Dsr.CastWrathOfTheHeavens})",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"End: after Thunderstruck ({Dsr.StatusThunderstruck}) has appeared once, then none remain on party",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule:", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"1. Thunderstruck → {MarkRoleNames.Label(c.ThunderstruckHotbar)}",
            MirageUi.Color.Secondary);

        MirageUi.SubHeader("Options");
        if (DrawMarkHotbar(
                "Thunderstruck",
                ref c.ThunderstruckHotbar,
                ref c.ThunderstruckColor,
                DefaultColorRed))
            SaveConfig(c);
    }

    private void TickActive(ModuleContext ctx)
    {
        if (ctx.IsEnemyCasting(Dsr.CastWrathOfTheHeavens))
            _active = true;

        if (!_active)
            return;

        if (ctx.AnyPartyHasStatus(Dsr.StatusThunderstruck))
            _sawThunderstruck = true;

        if (!_sawThunderstruck)
            return;

        if (ctx.AnyPartyHasStatus(Dsr.StatusThunderstruck))
            return;

        OnReset();
    }

    public sealed class WrathConfig
    {
        public MarkRole ThunderstruckHotbar = MarkRole.Stop;
        public Vector4 ThunderstruckColor = DefaultColorRed;
    }
}
