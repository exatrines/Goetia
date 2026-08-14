using Goetia.Modules;

namespace Goetia.UI;

/// <summary>Preview overlay: party seats × hotbars and active sources.</summary>
internal sealed class PreviewOverlay
{
    private const int HotbarCount = 10;
    private const int TableColumns = 2 + HotbarCount;
    private static readonly Vector2 DefaultSize = new(560, 360);
    private static readonly Vector2 MinSize = new(420, 260);
    private static readonly Vector2 MaxSize = new(1200, 900);

    private readonly PartyOrderService _party;
    private readonly HighlightStore _store;
    private readonly ModuleHost _modules;
    private readonly List<HighlightEntry> _active = [];
    private readonly List<HighlightSourceSnapshot> _sources = [];

    public PreviewOverlay(PartyOrderService party, HighlightStore store, ModuleHost modules)
    {
        _party = party;
        _store = store;
        _modules = modules;
    }

    public void Draw()
    {
        if (!C.ShowPreview || !PluginServices.ClientState.IsLoggedIn)
            return;

        MirageTheme.EnsureDefaultsCaptured();
        var themeScope = MirageTheme.PushCustom(MirageTheme.ResolveAppliedColors());
        try
        {
            DrawWindow();
        }
        finally
        {
            MirageTheme.Pop(themeScope);
        }
    }

    private void DrawWindow()
    {
        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(MinSize, MaxSize);

        var flags = ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
        var open = true;
        if (!ImGui.Begin("Goetia Preview###goetiaPartyDebug", ref open, flags))
        {
            ImGui.End();
            CloseIfRequested(open);
            return;
        }

        var on = BuildSeatHotbarMatrix();

        if (ImGui.BeginTable(
                "##goetiaPartyOrderTable",
                TableColumns,
                ImGuiTableFlags.Borders
                | ImGuiTableFlags.RowBg
                | ImGuiTableFlags.SizingFixedFit
                | ImGuiTableFlags.NoHostExtendX))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 28f);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            for (var hb = 1; hb <= HotbarCount; hb++)
                ImGui.TableSetupColumn(hb.ToString(), ImGuiTableColumnFlags.WidthFixed, 28f);
            ImGui.TableHeadersRow();

            for (var seat = 0; seat < PartyOrderService.MaxPartySize; seat++)
            {
                var slot = _party.Slots[seat];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"<{seat + 1}>");

                ImGui.TableNextColumn();
                if (!slot.IsOccupied)
                    ImGui.TextDisabled("(empty)");
                else
                    ImGui.TextUnformatted(string.IsNullOrEmpty(slot.Name) ? "(no name)" : slot.Name);

                for (var hb = 0; hb < HotbarCount; hb++)
                {
                    ImGui.TableNextColumn();
                    if (on[seat, hb])
                        ImGui.TextColored(new Vector4(0.35f, 0.95f, 0.45f, 1f), "ON");
                    else
                        ImGui.TextDisabled("·");
                }
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted("Active sources");

        _store.CollectActiveSources(_sources);

        var childH = Math.Max(60f, ImGui.GetContentRegionAvail().Y);
        if (ImGui.BeginChild("##goetiaPartySources", new Vector2(0f, childH), border: true))
        {
            if (_sources.Count == 0)
            {
                ImGui.TextDisabled("(none)");
            }
            else
            {
                foreach (var source in _sources)
                {
                    ImGui.BulletText(ResolveSourceLabel(source.SourceId));

                    foreach (var entry in source.Entries)
                    {
                        if (entry.PartyIndex is < 0 or >= PartyOrderService.MaxPartySize)
                            continue;

                        var roleLabel = MarkRoleNames.Label(entry.Role);
                        var map = C.GetColumn(entry.Role).TryMapPartyIndex(entry.PartyIndex, out var hotbarId, out var slotId)
                            ? $"HB{hotbarId + 1}:{slotId + 1}"
                            : "unmapped";

                        ImGui.TextDisabled($"    <{entry.PartyIndex + 1}> {roleLabel} → {map}");
                    }
                }
            }
        }

        ImGui.EndChild();
        ImGui.End();
        CloseIfRequested(open);
    }

    private static void CloseIfRequested(bool open)
    {
        if (open)
            return;
        C.ShowPreview = false;
        C.Save();
    }

    private bool[,] BuildSeatHotbarMatrix()
    {
        var on = new bool[PartyOrderService.MaxPartySize, HotbarCount];

        _store.CollectActive(_active);

        foreach (var entry in _active)
        {
            if (entry.PartyIndex is < 0 or >= PartyOrderService.MaxPartySize)
                continue;

            if (!C.GetColumn(entry.Role).TryMapPartyIndex(entry.PartyIndex, out var hotbarId, out _))
                continue;

            if (hotbarId >= HotbarCount)
                continue;

            on[entry.PartyIndex, hotbarId] = true;
        }

        return on;
    }

    private string ResolveSourceLabel(string sourceId)
    {
        if (sourceId == HighlightStore.SourcePreviewAttack)
            return "Preview (Attack)";
        if (sourceId == HighlightStore.SourcePreviewBind)
            return "Preview (Bind)";
        if (sourceId == HighlightStore.SourcePreviewStop)
            return "Preview (Stop)";

        var module = _modules.Modules.FirstOrDefault(m => m.Id == sourceId);
        return module != null ? $"{module.DisplayName}  ({module.Id})" : sourceId;
    }
}
