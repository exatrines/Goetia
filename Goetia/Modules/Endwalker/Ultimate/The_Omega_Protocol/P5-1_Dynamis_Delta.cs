namespace Goetia.Modules;

/// <summary>TOP P5 Run Dynamis Delta — Near/Far World highlight.</summary>
internal sealed class DynamisDeltaModule : GoetiaModule
{
    private bool _active;
    private bool _sawNearOrFar;

    public override string Id => Configuration.ModuleIdDelta;
    public override string DisplayName => "Run Dynamis Delta";
    public override IReadOnlySet<uint>? ValidTerritories => TopP5.Territories;

    private DeltaConfig Config => GetConfig<DeltaConfig>();

    public override void OnReset()
    {
        _active = false;
        _sawNearOrFar = false;
    }

    public override void OnUpdate(ModuleContext ctx)
    {
        TickActive(ctx);
        if (!_active)
            return;

        var role = Config.NearFarHotbar;
        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || !TopP5.HasNearOrFar(ctx, i))
                continue;
            ctx.SetHighlight(i, role, Config.NearFarColor);
        }
    }

    public override void DrawConfig()
    {
        var c = Config;
        MirageUi.SubHeader("Rules");
        MirageUi.Text("Territory: TOP (1122)", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"Start: Run Dynamis Delta cast ({TopP5.CastDynamisDelta})",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"End: after Near/Far World ({TopP5.StatusHelloNear}/{TopP5.StatusHelloFar}) has appeared once, then none remain on party",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule:", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"1. Near/Far World → {MarkRoleNames.Label(c.NearFarHotbar)}",
            MirageUi.Color.Secondary);

        MirageUi.SubHeader("Options");
        if (DrawMarkHotbar(
                "Near/Far World",
                ref c.NearFarHotbar,
                ref c.NearFarColor,
                DefaultColorNearFarWorld))
            SaveConfig(c);
    }

    private void TickActive(ModuleContext ctx)
    {
        if (ctx.IsEnemyCasting(TopP5.CastDynamisDelta))
            _active = true;

        if (!_active)
            return;

        if (TopP5.AnyNearOrFar(ctx))
            _sawNearOrFar = true;

        if (!_sawNearOrFar)
            return;

        if (TopP5.AnyNearOrFar(ctx))
            return;

        OnReset();
    }

    public sealed class DeltaConfig
    {
        public MarkRole NearFarHotbar = MarkRole.Stop;
        public Vector4 NearFarColor = DefaultColorNearFarWorld;
    }
}
