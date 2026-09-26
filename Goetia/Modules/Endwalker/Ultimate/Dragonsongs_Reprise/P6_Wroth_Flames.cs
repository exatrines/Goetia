namespace Goetia.Modules;

/// <summary>DSR P6 Wroth Flames — Spreading / Entangled / remaining highlight.</summary>
internal sealed class WrothFlamesModule : GoetiaModule
{
    private bool _active;
    private bool _sawFlames;

    public override string Id => Configuration.ModuleIdWrothFlames;
    public override string DisplayName => "Wroth Flames";
    public override IReadOnlySet<uint>? ValidTerritories => Dsr.Territories;

    private WrothConfig Config => GetConfig<WrothConfig>();

    public override void OnReset()
    {
        _active = false;
        _sawFlames = false;
    }

    public override void OnUpdate(ModuleContext ctx)
    {
        TickActive(ctx);
        if (!_active || !_sawFlames)
            return;

        var c = Config;
        var claimed = new HashSet<int>();

        ctx.TakeUnclaimed(
            claimed,
            c.SpreadingHotbar,
            c.SpreadingColor,
            ModuleContext.MaxPartySize,
            seat => ctx.HasStatus(seat, Dsr.StatusSpreadingFlames));

        ctx.TakeUnclaimed(
            claimed,
            c.EntangledHotbar,
            c.EntangledColor,
            ModuleContext.MaxPartySize,
            seat => ctx.HasStatus(seat, Dsr.StatusEntangledFlames));

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
        MirageUi.Text("Territory: DSR (968)", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"Start: Wroth Flames cast ({Dsr.CastWrothFlames})",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"End: after Spreading/Entangled Flames ({Dsr.StatusSpreadingFlames}/{Dsr.StatusEntangledFlames}) has appeared once, then none remain on party",
            MirageUi.Color.Secondary);
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        MirageUi.Text("Rule:", MirageUi.Color.Secondary);
        MirageUi.Text(
            $"1. Spreading Flames → {MarkRoleNames.Label(c.SpreadingHotbar)}",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"2. Entangled Flames → {MarkRoleNames.Label(c.EntangledHotbar)}",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            $"3. Remaining → {MarkRoleNames.Label(c.RemainingHotbar)}",
            MirageUi.Color.Secondary);

        MirageUi.SubHeader("Options");
        var changed = false;
        if (DrawMarkHotbar(
                "Spreading Flames",
                ref c.SpreadingHotbar,
                ref c.SpreadingColor,
                DefaultColorYellow))
            changed = true;
        if (DrawMarkHotbar(
                "Entangled Flames",
                ref c.EntangledHotbar,
                ref c.EntangledColor,
                DefaultColorPurple))
            changed = true;
        if (DrawMarkHotbar(
                "Remaining",
                ref c.RemainingHotbar,
                ref c.RemainingColor,
                DefaultColorRed))
            changed = true;
        if (changed)
            SaveConfig(c);
    }

    private void TickActive(ModuleContext ctx)
    {
        if (ctx.IsEnemyCasting(Dsr.CastWrothFlames))
            _active = true;

        if (!_active)
            return;

        if (Dsr.AnyWrothFlames(ctx))
            _sawFlames = true;

        if (!_sawFlames)
            return;

        if (Dsr.AnyWrothFlames(ctx))
            return;

        OnReset();
    }

    public sealed class WrothConfig
    {
        public MarkRole SpreadingHotbar = MarkRole.Attack;
        public Vector4 SpreadingColor = DefaultColorYellow;
        public MarkRole EntangledHotbar = MarkRole.Bind;
        public Vector4 EntangledColor = DefaultColorPurple;
        public MarkRole RemainingHotbar = MarkRole.Stop;
        public Vector4 RemainingColor = DefaultColorRed;
    }
}
