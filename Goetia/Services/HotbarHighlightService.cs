using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Goetia.Modules;

namespace Goetia.Services;

/// <summary>
/// Draws highlight frames on HUD Action Bars 1–10.
/// Settings Hotbar N matches that HUD bar, and any bar currently showing hotbar set N.
/// </summary>
internal sealed unsafe class HotbarHighlightService
{
    internal static readonly (string AddonName, byte HotbarId)[] HudActionBars =
    [
        ("_ActionBar", 0),
        ("_ActionBar01", 1),
        ("_ActionBar02", 2),
        ("_ActionBar03", 3),
        ("_ActionBar04", 4),
        ("_ActionBar05", 5),
        ("_ActionBar06", 6),
        ("_ActionBar07", 7),
        ("_ActionBar08", 8),
        ("_ActionBar09", 9),
    ];

    private static readonly (string SourceId, MarkRole Role)[] PreviewRoles =
    [
        (HighlightStore.SourcePreviewAttack, MarkRole.Attack),
        (HighlightStore.SourcePreviewBind, MarkRole.Bind),
        (HighlightStore.SourcePreviewStop, MarkRole.Stop),
    ];

    private readonly HighlightStore _store;
    private readonly List<HighlightEntry> _active = [];
    private readonly Dictionary<(byte HotbarId, byte SlotId), uint> _slotColors = [];
    private readonly List<HighlightEntry> _previewBuffer = [];

    public HotbarHighlightService(HighlightStore store) => _store = store;

    public void Draw()
    {
        if (!PluginServices.ClientState.IsLoggedIn)
        {
            ClearPreviewSources();
            return;
        }

        SyncPreviewSources();
        BuildTargetSlots();
        if (_slotColors.Count == 0)
            return;

        var viewport = ImGui.GetMainViewport();
        var drawList = ImGui.GetForegroundDrawList(viewport);
        var origin = viewport.Pos;
        var thickness = Math.Max(1f, C.HighlightThickness);
        foreach (var (addonName, hotbarId) in HudActionBars)
            DrawHudBar(addonName, hotbarId, drawList, origin, thickness);
    }

    private void BuildTargetSlots()
    {
        _slotColors.Clear();
        _store.CollectActive(_active);
        foreach (var entry in _active)
            TryAddSlot(entry);
    }

    private void SyncPreviewSources()
    {
        foreach (var (sourceId, role) in PreviewRoles)
            SyncPreviewSource(sourceId, role);
    }

    private void ClearPreviewSources()
    {
        foreach (var (sourceId, _) in PreviewRoles)
            _store.ClearSource(sourceId);
    }

    private void SyncPreviewSource(string sourceId, MarkRole role)
    {
        if (!C.GetColumnPreview(role))
        {
            _store.ClearSource(sourceId);
            return;
        }

        _previewBuffer.Clear();
        var color = PreviewColor(role);
        for (var i = 0; i < PartyOrderService.MaxPartySize; i++)
            _previewBuffer.Add(new HighlightEntry(i, role, color));

        _store.Replace(sourceId, _previewBuffer);
    }

    private void TryAddSlot(HighlightEntry entry)
    {
        var column = C.GetColumn(entry.Role);
        if (!column.TryMapPartyIndex(entry.PartyIndex, out var hotbarId, out var slotId))
            return;

        _slotColors[(hotbarId, slotId)] = ImGui.ColorConvertFloat4ToU32(entry.Color);
    }

    private void DrawHudBar(
        string addonName,
        byte hudHotbarId,
        ImDrawListPtr drawList,
        Vector2 origin,
        float thickness)
    {
        if (!TryGetAddon(addonName, out var addon))
            return;

        var setId = addon->RaptureHotbarId;
        var slotCount = Math.Min((int)addon->SlotCount, addon->ActionBarSlotVector.Count);
        if (slotCount <= 0)
            return;

        for (var i = 0; i < slotCount; i++)
        {
            if (!TryGetSlotColor(hudHotbarId, setId, (byte)i, out var color))
                continue;

            ref var barSlot = ref addon->ActionBarSlotVector[i];
            if (!TryGetSlotScreenRect(ref barSlot, out var min, out var max))
                continue;

            drawList.AddRect(origin + min, origin + max, color, 2f, ImDrawFlags.None, thickness);
        }
    }

    private bool TryGetSlotColor(byte hudHotbarId, byte setId, byte slotId, out uint color)
    {
        if (_slotColors.TryGetValue((hudHotbarId, slotId), out color))
            return true;
        if (setId <= 9 && setId != hudHotbarId && _slotColors.TryGetValue((setId, slotId), out color))
            return true;
        color = 0;
        return false;
    }

    private static Vector4 PreviewColor(MarkRole role) => role switch
    {
        MarkRole.Attack => GoetiaModule.DefaultColorYellow,
        MarkRole.Bind => GoetiaModule.DefaultColorPurple,
        MarkRole.Stop => GoetiaModule.DefaultColorRed,
        _ => GoetiaModule.DefaultColorYellow,
    };

    internal static bool TryGetAddon(string addonName, out AddonActionBarBase* addon)
    {
        addon = null;
        var handle = PluginServices.GameGui.GetAddonByName(addonName, 1);
        if (handle == nint.Zero)
            return false;

        addon = (AddonActionBarBase*)handle.Address;
        return addon != null;
    }

    private static bool TryGetSlotScreenRect(ref ActionBarSlot barSlot, out Vector2 min, out Vector2 max)
    {
        min = default;
        max = default;
        return TryPickSlotNode(ref barSlot, out var node, out _) && TryMeasureNode(node, out min, out max);
    }

    internal static bool TryMeasureNode(AtkResNode* node, out Vector2 min, out Vector2 max)
    {
        min = default;
        max = default;
        if (node == null)
            return false;

        var scaleX = 1f;
        var scaleY = 1f;
        for (var p = node; p != null; p = p->ParentNode)
        {
            scaleX *= p->ScaleX;
            scaleY *= p->ScaleY;
        }

        min = new Vector2(node->ScreenX, node->ScreenY);
        max = min + new Vector2(node->Width * scaleX, node->Height * scaleY);
        return max.X > min.X && max.Y > min.Y;
    }

    internal static bool TryPickSlotNode(ref ActionBarSlot barSlot, out AtkResNode* node, out string source)
    {
        var iconNode = barSlot.Icon != null ? &barSlot.Icon->AtkResNode : null;
        var dragNode = DragDropNode(ref barSlot);

        if (IsUsableSlotNode(barSlot.IconFrame, allowHidden: false))
            return SetPicked(barSlot.IconFrame, "IconFrame", out node, out source);
        if (IsUsableSlotNode(iconNode, allowHidden: false))
            return SetPicked(iconNode, "Icon", out node, out source);
        if (IsUsableSlotNode(dragNode, allowHidden: false))
            return SetPicked(dragNode, "DragDrop", out node, out source);
        if (IsUsableSlotNode(barSlot.IconFrame, allowHidden: true))
            return SetPicked(barSlot.IconFrame, "IconFrameHidden", out node, out source);
        if (IsUsableSlotNode(iconNode, allowHidden: true))
            return SetPicked(iconNode, "IconHidden", out node, out source);
        if (IsUsableSlotNode(dragNode, allowHidden: true))
            return SetPicked(dragNode, "DragDropHidden", out node, out source);

        node = null;
        source = "";
        return false;
    }

    private static bool SetPicked(AtkResNode* picked, string name, out AtkResNode* node, out string source)
    {
        node = picked;
        source = name;
        return true;
    }

    private static AtkResNode* DragDropNode(ref ActionBarSlot barSlot)
    {
        if (barSlot.ComponentDragDrop == null)
            return null;

        var owner = barSlot.ComponentDragDrop->OwnerNode;
        return owner != null ? &owner->AtkResNode : null;
    }

    private static bool IsUsableSlotNode(AtkResNode* node, bool allowHidden)
    {
        if (node == null || node->Width <= 0 || node->Height <= 0)
            return false;
        return allowHidden || node->IsVisible();
    }
}
