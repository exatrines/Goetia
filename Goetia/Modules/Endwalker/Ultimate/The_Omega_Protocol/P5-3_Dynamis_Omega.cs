namespace Goetia.Modules;

/// <summary>
/// TOP P5 Run Dynamis Omega — Half1 then Half2 while active.
/// Half1 until FirstInLine clears; then Half2.
/// </summary>
internal sealed class DynamisOmegaModule : GoetiaModule
{
    private bool _active;
    private bool _sawNearOrFar;
    private bool _sawFirst;
    private bool _half2;

    public override string Id => Configuration.ModuleIdOmega;
    public override string DisplayName => "Run Dynamis Omega";
    public override IReadOnlySet<uint>? ValidTerritories => TopP5.Territories;

    private OmegaConfig Config => GetConfig<OmegaConfig>();

    public override void OnReset()
    {
        _active = false;
        _sawNearOrFar = false;
        _sawFirst = false;
        _half2 = false;
    }

    public override void OnUpdate(ModuleContext ctx)
    {
        TickActive(ctx);

        if (_active)
            UpdateHalf(ctx);
        else
        {
            _sawFirst = false;
            _half2 = false;
        }

        if (!_active)
            return;

        if (_half2)
            CollectHalf2(ctx);
        else
            CollectHalf1(ctx);
    }

    public override void DrawConfig()
    {
        var c = Config;
        MirageUi.SubHeader("Rules");
        MirageUi.Text("Territory: TOP (1122)", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"Start: Run Dynamis Omega cast ({TopP5.CastDynamisOmega})",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"End: after Near/Far World ({TopP5.StatusHelloNear}/{TopP5.StatusHelloFar}) has appeared once, then none remain on party",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule: Half1", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"1. FirstInLine + Near/Far World → {MarkRoleNames.Label(c.Half1FirstNearFarHotbar)}",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"2. Dynamis ×2 + SecondInLine + Near/Far World → {MarkRoleNames.Label(c.Half1Dynamis2Hotbar)} (max 2)",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"3. Fill from Dynamis ×2 (max 2 total) → {MarkRoleNames.Label(c.Half1Dynamis2Hotbar)}",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"4. Remaining → {MarkRoleNames.Label(c.Half1RemainingHotbar)}",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule: Half2", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"1. SecondInLine + Near/Far World → {MarkRoleNames.Label(c.Half2SecondNearFarHotbar)}",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"2. Dynamis ×3 → {MarkRoleNames.Label(c.Half2Dynamis3Hotbar)} (max 2)",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"3. Remaining → {MarkRoleNames.Label(c.Half2RemainingHotbar)}",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text(
            $"Switch: after FirstInLine ({TopP5.StatusFirstInLine}) has appeared once, then none remain on party (Half1 → Half2)",
            MirageUi.Color.Secondary);

        MirageUi.SubHeader("Options");
        var changed = false;
        if (DrawMarkHotbar(
                "Half1: FirstInLine + Near/Far World",
                ref c.Half1FirstNearFarHotbar,
                ref c.Half1FirstNearFarColor,
                DefaultColorNearFarWorld))
            changed = true;
        if (DrawMarkHotbar(
                "Half1: Dynamis ×2 (max 2)",
                ref c.Half1Dynamis2Hotbar,
                ref c.Half1Dynamis2Color,
                DefaultColorDynamis))
            changed = true;
        if (DrawMarkHotbar(
                "Half1: Remaining",
                ref c.Half1RemainingHotbar,
                ref c.Half1RemainingColor,
                DefaultColorRemaining))
            changed = true;
        if (DrawMarkHotbar(
                "Half2: SecondInLine + Near/Far World",
                ref c.Half2SecondNearFarHotbar,
                ref c.Half2SecondNearFarColor,
                DefaultColorNearFarWorld))
            changed = true;
        if (DrawMarkHotbar(
                "Half2: Dynamis ×3 (max 2)",
                ref c.Half2Dynamis3Hotbar,
                ref c.Half2Dynamis3Color,
                DefaultColorDynamis))
            changed = true;
        if (DrawMarkHotbar(
                "Half2: Remaining",
                ref c.Half2RemainingHotbar,
                ref c.Half2RemainingColor,
                DefaultColorRemaining))
            changed = true;
        if (changed)
            SaveConfig(c);
    }

    private void TickActive(ModuleContext ctx)
    {
        if (ctx.IsEnemyCasting(TopP5.CastDynamisOmega))
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

    private void UpdateHalf(ModuleContext ctx)
    {
        if (ctx.AnyPartyHasStatus(TopP5.StatusFirstInLine))
            _sawFirst = true;
        else if (_sawFirst)
            _half2 = true;
    }

    private void CollectHalf1(ModuleContext ctx)
    {
        var c = Config;
        var claimed = new HashSet<int>();
        var bindRole = c.Half1Dynamis2Hotbar;

        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || !ctx.HasStatus(i, TopP5.StatusFirstInLine) || !TopP5.HasNearOrFar(ctx, i))
                continue;
            ctx.SetHighlight(i, c.Half1FirstNearFarHotbar, c.Half1FirstNearFarColor);
            claimed.Add(i);
        }

        var bindPriority = new List<int>();
        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || claimed.Contains(i))
                continue;
            if (!ctx.HasStatusParam(i, TopP5.StatusDynamis, 2))
                continue;
            if (!ctx.HasStatus(i, TopP5.StatusSecondInLine) || !TopP5.HasNearOrFar(ctx, i))
                continue;
            bindPriority.Add(i);
        }

        ctx.TakeSeats(bindPriority, claimed, bindRole, c.Half1Dynamis2Color, 2);

        var bindCount = bindPriority.Count;
        if (bindCount > 2)
            bindCount = 2;

        if (bindCount < 2)
        {
            var bindRest = new List<int>();
            for (var i = 0; i < ModuleContext.MaxPartySize; i++)
            {
                if (!ctx.IsOccupied(i) || claimed.Contains(i))
                    continue;
                if (ctx.HasStatusParam(i, TopP5.StatusDynamis, 2))
                    bindRest.Add(i);
            }

            ctx.TakeSeats(bindRest, claimed, bindRole, c.Half1Dynamis2Color, 2 - bindCount);
        }

        ctx.TakeUnclaimed(
            claimed,
            c.Half1RemainingHotbar,
            c.Half1RemainingColor,
            ModuleContext.MaxPartySize);
    }

    private void CollectHalf2(ModuleContext ctx)
    {
        var c = Config;
        var claimed = new HashSet<int>();

        for (var i = 0; i < ModuleContext.MaxPartySize; i++)
        {
            if (!ctx.IsOccupied(i) || !ctx.HasStatus(i, TopP5.StatusSecondInLine) || !TopP5.HasNearOrFar(ctx, i))
                continue;
            ctx.SetHighlight(i, c.Half2SecondNearFarHotbar, c.Half2SecondNearFarColor);
            claimed.Add(i);
        }

        ctx.TakeUnclaimed(
            claimed,
            c.Half2Dynamis3Hotbar,
            c.Half2Dynamis3Color,
            2,
            seat => ctx.HasStatusParam(seat, TopP5.StatusDynamis, 3));

        ctx.TakeUnclaimed(
            claimed,
            c.Half2RemainingHotbar,
            c.Half2RemainingColor,
            ModuleContext.MaxPartySize);
    }

    public sealed class OmegaConfig
    {
        public MarkRole Half1FirstNearFarHotbar = MarkRole.Stop;
        public Vector4 Half1FirstNearFarColor = DefaultColorNearFarWorld;
        public MarkRole Half1Dynamis2Hotbar = MarkRole.Bind;
        public Vector4 Half1Dynamis2Color = DefaultColorDynamis;
        public MarkRole Half1RemainingHotbar = MarkRole.Attack;
        public Vector4 Half1RemainingColor = DefaultColorRemaining;
        public MarkRole Half2SecondNearFarHotbar = MarkRole.Stop;
        public Vector4 Half2SecondNearFarColor = DefaultColorNearFarWorld;
        public MarkRole Half2Dynamis3Hotbar = MarkRole.Bind;
        public Vector4 Half2Dynamis3Color = DefaultColorDynamis;
        public MarkRole Half2RemainingHotbar = MarkRole.Attack;
        public Vector4 Half2RemainingColor = DefaultColorRemaining;
    }
}
