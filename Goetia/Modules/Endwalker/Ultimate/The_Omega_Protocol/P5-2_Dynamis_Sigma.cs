namespace Goetia.Modules;

/// <summary>TOP P5 Run Dynamis Sigma — Near/Far World then Dynamis×1 then remainder.</summary>
internal sealed class DynamisSigmaModule : GoetiaModule
{
    private bool _active;
    private bool _sawNearOrFar;

    public override string Id => Configuration.ModuleIdSigma;
    public override string DisplayName => "Run Dynamis Sigma";
    public override IReadOnlySet<uint>? ValidTerritories => TopP5.Territories;

    private SigmaConfig Config => GetConfig<SigmaConfig>();

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

        var c = Config;
        var claimed = new HashSet<int>();

        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || !TopP5.HasNearOrFar(ctx, i))
                continue;
            ctx.SetHighlight(i, c.NearFarHotbar, c.NearFarColor);
            claimed.Add(i);
        }

        ctx.TakeUnclaimed(
            claimed,
            c.DynamisParam1Hotbar,
            c.DynamisParam1Color,
            2,
            seat => ctx.HasStatusParam(seat, TopP5.StatusDynamis, 1));

        ctx.TakeUnclaimed(
            claimed,
            c.RemainingHotbar,
            c.RemainingColor,
            ModuleContext.MaxPartySize);
    }

    public override void DrawConfig()
    {
        var c = Config;
        MirageUi.SubHeader("Rules");
        MirageUi.Text("Territory: TOP (1122)", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"Start: Run Dynamis Sigma cast ({TopP5.CastDynamisSigma})",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"End: after Near/Far World ({TopP5.StatusHelloNear}/{TopP5.StatusHelloFar}) has appeared once, then none remain on party",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule:", MirageUi.Color.Secondary);
        MirageUi.Text($"1. Near/Far World → {MarkRoleNames.Label(c.NearFarHotbar)}", MirageUi.Color.Secondary);
        MirageUi.Text($"2. Dynamis ×1 → {MarkRoleNames.Label(c.DynamisParam1Hotbar)} (max 2)", MirageUi.Color.Secondary);
        MirageUi.Text($"3. Remaining → {MarkRoleNames.Label(c.RemainingHotbar)}", MirageUi.Color.Secondary);

        MirageUi.SubHeader("Options");
        var changed = false;
        if (DrawMarkHotbar(
                "Near/Far World",
                ref c.NearFarHotbar,
                ref c.NearFarColor,
                DefaultColorNearFarWorld))
            changed = true;
        if (DrawMarkHotbar(
                "Dynamis ×1 (max 2)",
                ref c.DynamisParam1Hotbar,
                ref c.DynamisParam1Color,
                DefaultColorDynamis))
            changed = true;
        if (DrawMarkHotbar(
                "Remaining",
                ref c.RemainingHotbar,
                ref c.RemainingColor,
                DefaultColorRemaining))
            changed = true;
        if (changed)
            SaveConfig(c);
    }

    private void TickActive(ModuleContext ctx)
    {
        if (ctx.IsEnemyCasting(TopP5.CastDynamisSigma))
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

    public sealed class SigmaConfig
    {
        public MarkRole NearFarHotbar = MarkRole.Stop;
        public Vector4 NearFarColor = DefaultColorNearFarWorld;
        public MarkRole DynamisParam1Hotbar = MarkRole.Attack;
        public Vector4 DynamisParam1Color = DefaultColorDynamis;
        public MarkRole RemainingHotbar = MarkRole.Attack;
        public Vector4 RemainingColor = DefaultColorRemaining;
    }
}
