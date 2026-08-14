using Dalamud.Interface;
using MirageUI.Layout;

namespace Goetia.UI;

/// <summary>Settings: hotbars and highlight style.</summary>
internal sealed class PluginSettingsWindow : Window
{
    private const string PageHotbars = "page:hotbars";
    private const string PageStyle = "page:style";
    private static readonly Vector2 DefaultSize = new(780, 560);
    private const float SettingsControlWidth = 96f;

    private ImRaii.ColorDisposable? _themeScope;
    private string _selectedId = PageHotbars;
    private string _sidebarSearch = string.Empty;

    public PluginSettingsWindow()
        : base(
            "Goetia Settings###goetiaSettings",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        Size = DefaultSize;
        SizeCondition = ImGuiCond.Always;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = DefaultSize,
            MaximumSize = DefaultSize,
        };
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        MirageTheme.EnsureDefaultsCaptured();
        _themeScope = MirageTheme.PushCustom(MirageTheme.ResolveAppliedColors());
    }

    public override void PostDraw()
    {
        MirageTheme.Pop(_themeScope);
        _themeScope = null;
        ImGui.PopStyleVar();
    }

    public override void Draw()
    {
        if (_selectedId is not (PageHotbars or PageStyle))
            _selectedId = PageHotbars;

        var state = CreateTwoColumnState();
        MirageUi.TwoColumn.Draw(state, DrawMainContent);

        if (!string.IsNullOrEmpty(state.SelectedId))
            _selectedId = state.SelectedId;
    }

    private MirageTwoColumnState CreateTwoColumnState() =>
        new()
        {
            ShowSidebarHeader = false,
            ShowSidebarFooter = false,
            ShowSearch = true,
            SearchHint = "Search…",
            SearchFilter = _sidebarSearch,
            AllowDeselect = false,
            Entries =
            [
                new MirageTwoColumnEntry
                {
                    Id = PageHotbars,
                    Label = "Hotbars",
                },
                new MirageTwoColumnEntry
                {
                    Id = PageStyle,
                    Label = "Style",
                },
            ],
            SelectedId = _selectedId,
            OnSelectionChanged = id =>
            {
                if (!string.IsNullOrEmpty(id))
                    _selectedId = id;
            },
            OnSearchFilterChanged = filter => _sidebarSearch = filter ?? string.Empty,
        };

    private void DrawMainContent()
    {
        if (_selectedId == PageStyle)
            DrawStyle();
        else
            DrawHotbars();
    }

    private static void DrawHotbars()
    {
        MirageUi.Header("Hotbars");
        MirageUi.Text(
            "Map Attack / Bind / Stop hotbars to party order. Place /mk macros yourself; Goetia never auto-marks.",
            MirageUi.Color.Secondary);
        MirageUi.Text(
            "Each normal hotbar is one row of 12 slots. Assign three bars and map eight consecutive slots to party order <1>–<8>.",
            MirageUi.Color.Secondary);

        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));
        DrawHotbarAssignmentTable();
        ImGui.Dummy(new Vector2(0f, ImGui.GetStyle().ItemSpacing.Y));

        MirageUi.SubHeader("Attack");
        DrawColumnEditor("Attack", C.AttackColumn, MarkRole.Attack);

        MirageUi.SubHeader("Bind");
        DrawColumnEditor("Bind", C.BindColumn, MarkRole.Bind);

        MirageUi.SubHeader("Stop");
        DrawColumnEditor("Stop", C.StopColumn, MarkRole.Stop);
    }

    private static void DrawStyle()
    {
        MirageUi.Header("Style");
        MirageUi.Text(
            "Outline thickness on suggested mark slots. Colors are set per rule.",
            MirageUi.Color.Secondary);

        var thickness = C.HighlightThickness;
        if (MirageUi.SliderFloat(
                "Thickness",
                ref thickness,
                1f,
                8f,
                "%.1f",
                width: SettingsControlWidth,
                labelWidth: 0f))
        {
            C.HighlightThickness = thickness;
            C.Save();
        }
    }

    private static void DrawColumnEditor(string label, MarkColumnLayout column, MarkRole role)
    {
        var hotbar = (int)column.HotbarId + 1;
        if (MirageUi.InputInt(
                "Hotbar (1–10)",
                ref hotbar,
                id: $"{label}Hotbar",
                width: SettingsControlWidth,
                labelWidth: 0f))
        {
            column.HotbarId = (byte)(Math.Clamp(hotbar, 1, 10) - 1);
            C.Save();
        }

        var maxBaseUi = MarkColumnLayout.SlotsPerHotbar - PartyOrderService.MaxPartySize + 1;
        var baseSlot = (int)column.BaseSlot + 1;
        if (MirageUi.InputInt(
                $"Base slot for <1> (1–{maxBaseUi})",
                ref baseSlot,
                id: $"{label}Base",
                width: SettingsControlWidth,
                labelWidth: 0f))
        {
            column.BaseSlot = (byte)(Math.Clamp(baseSlot, 1, maxBaseUi) - 1);
            C.Save();
        }

        var preview = C.GetColumnPreview(role);
        if (MirageUi.Checkbox($"Preview##{label}", ref preview))
        {
            C.SetColumnPreview(role, preview);
            C.Save();
        }
    }

    private static void DrawHotbarAssignmentTable()
    {
        const int hotbarCount = 10;
        const int slotCols = MarkColumnLayout.SlotsPerHotbar;
        const int tableCols = 2 + slotCols;
        const float hotbarColWidth = 48f;
        const float roleColWidth = 48f;
        const float slotColWidth = 24f;

        var assignedOnly = C.HotbarTableAssignedOnly;
        var visibleRows = 0;
        for (var bar = 0; bar < hotbarCount; bar++)
        {
            if (assignedOnly && HotbarRoleLabel(bar) == "None")
                continue;
            visibleRows++;
        }

        var rowH = ImGui.GetFrameHeightWithSpacing();
        var height = rowH * (visibleRows + 1) + ImGui.GetStyle().ScrollbarSize + 8f;
        if (!ImGui.BeginTable(
                "##hotbarAssignment",
                tableCols,
                ImGuiTableFlags.SizingFixedFit
                | ImGuiTableFlags.RowBg
                | ImGuiTableFlags.BordersInnerV
                | ImGuiTableFlags.ScrollX
                | ImGuiTableFlags.PreciseWidths,
                new Vector2(-1f, height)))
            return;

        ImGui.TableSetupColumn(
            "##hotbar",
            ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
            hotbarColWidth);
        ImGui.TableSetupColumn(
            "Role",
            ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
            roleColWidth);
        for (var i = 0; i < slotCols; i++)
        {
            ImGui.TableSetupColumn(
                (i + 1).ToString(),
                ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize,
                slotColWidth);
        }

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        ImGui.TableNextColumn();
        DrawAssignedOnlyToggle();
        ImGui.TableNextColumn();
        DrawTableCell("Role", dim: false);
        for (var i = 0; i < slotCols; i++)
        {
            ImGui.TableNextColumn();
            DrawTableCell($"S{i + 1}", dim: false);
        }

        for (var bar = 0; bar < hotbarCount; bar++)
        {
            var role = HotbarRoleLabel(bar);
            if (assignedOnly && role == "None")
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawTableCell($"HB{bar + 1}", dim: false);

            ImGui.TableNextColumn();
            DrawTableCell(role, dim: role == "None");

            for (var slot = 0; slot < slotCols; slot++)
            {
                ImGui.TableNextColumn();
                var tag = HotbarSlotTag(bar, slot);
                DrawTableCell(tag, dim: tag == "-");
            }
        }

        ImGui.EndTable();
    }

    private static void DrawAssignedOnlyToggle()
    {
        var size = new Vector2(ImGui.GetFrameHeight());
        ImGui.AlignTextToFramePadding();
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (avail - size.X) * 0.5f));

        var assignedOnly = C.HotbarTableAssignedOnly;
        if (!MirageUi.IconToggleButton(
                FontAwesomeIcon.BorderAll,
                FontAwesomeIcon.BorderAll,
                ref assignedOnly,
                id: "hotbarTableAssignedOnly",
                tooltipOff: "Show all hotbars",
                tooltipOn: "Show assigned hotbars only",
                size: size,
                border: true,
                accentWhenOn: true))
            return;

        C.HotbarTableAssignedOnly = assignedOnly;
        C.Save();
    }

    private static void DrawTableCell(string text, bool dim)
    {
        ImGui.AlignTextToFramePadding();
        var textSize = ImGui.CalcTextSize(text);
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (avail - textSize.X) * 0.5f));
        if (!dim)
        {
            ImGui.TextUnformatted(text);
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, MirageUi.GetColor(MirageUi.Color.Secondary));
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static string HotbarRoleLabel(int hotbarId)
    {
        var parts = new List<string>(3);
        if (C.AttackColumn.HotbarId == hotbarId)
            parts.Add("Attack");
        if (C.BindColumn.HotbarId == hotbarId)
            parts.Add("Bind");
        if (C.StopColumn.HotbarId == hotbarId)
            parts.Add("Stop");
        return parts.Count == 0 ? "None" : string.Join(", ", parts);
    }

    private static string HotbarSlotTag(int hotbarId, int slot)
    {
        var tags = new List<string>(3);
        AppendSlotTag(tags, C.AttackColumn, hotbarId, slot);
        AppendSlotTag(tags, C.BindColumn, hotbarId, slot);
        AppendSlotTag(tags, C.StopColumn, hotbarId, slot);
        return tags.Count == 0 ? "-" : string.Join(" ", tags);
    }

    private static void AppendSlotTag(List<string> tags, MarkColumnLayout column, int hotbarId, int slot)
    {
        if (column.HotbarId != hotbarId)
            return;

        var party = slot - column.BaseSlot;
        if (party is >= 0 and < PartyOrderService.MaxPartySize)
            tags.Add($"<{party + 1}>");
    }
}
