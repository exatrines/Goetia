using FFXIVClientStructs.FFXIV.Client.UI;
using Goetia.Modules;

namespace Goetia.Services;

/// <summary>Draws highlight frames on hotbar slots by (hotbar, slot) position.</summary>
internal sealed unsafe class HotbarHighlightService
{
    private static readonly string[] ActionBarAddonNames =
    [
        "_ActionBar",
        "_ActionBar01",
        "_ActionBar02",
        "_ActionBar03",
        "_ActionBar04",
        "_ActionBar05",
        "_ActionBar06",
        "_ActionBar07",
        "_ActionBar08",
        "_ActionBar09",
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

        var drawList = ImGui.GetForegroundDrawList();
        var thickness = Math.Max(1f, C.HighlightThickness);

        foreach (var addonName in ActionBarAddonNames)
            DrawAddonMatches(addonName, drawList, thickness);
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
        SyncPreviewSource(HighlightStore.SourcePreviewAttack, MarkRole.Attack);
        SyncPreviewSource(HighlightStore.SourcePreviewBind, MarkRole.Bind);
        SyncPreviewSource(HighlightStore.SourcePreviewStop, MarkRole.Stop);
    }

    private void SyncPreviewSource(string sourceId, MarkRole role)
    {
        if (!C.GetColumnPreview(role))
        {
            _store.ClearSource(sourceId);
            return;
        }

        _previewBuffer.Clear();
        var color = GoetiaModule.DefaultColorRemaining;
        for (var i = 0; i < PartyOrderService.MaxPartySize; i++)
            _previewBuffer.Add(new HighlightEntry(i, role, color));

        _store.Replace(sourceId, _previewBuffer);
    }

    private void ClearPreviewSources()
    {
        _store.ClearSource(HighlightStore.SourcePreviewAttack);
        _store.ClearSource(HighlightStore.SourcePreviewBind);
        _store.ClearSource(HighlightStore.SourcePreviewStop);
    }

    private void TryAddSlot(HighlightEntry entry)
    {
        var column = C.GetColumn(entry.Role);
        if (!column.TryMapPartyIndex(entry.PartyIndex, out var hotbarId, out var slotId))
            return;

        _slotColors[(hotbarId, slotId)] = ImGui.ColorConvertFloat4ToU32(entry.Color);
    }

    private void DrawAddonMatches(string addonName, ImDrawListPtr drawList, float thickness)
    {
        var addonHandle = PluginServices.GameGui.GetAddonByName(addonName, 1);
        if (addonHandle == nint.Zero)
            return;

        var addon = (AddonActionBarBase*)addonHandle.Address;
        if (addon == null || !addon->IsVisible || addon->RootNode == null || !addon->RootNode->IsVisible())
            return;

        if (addon->RaptureHotbarId > 9)
            return;

        var hotbarId = addon->RaptureHotbarId;
        var slotCount = Math.Min((int)addon->SlotCount, addon->ActionBarSlotVector.Count);
        if (slotCount <= 0)
            return;

        for (var i = 0; i < slotCount; i++)
        {
            if (!_slotColors.TryGetValue((hotbarId, (byte)i), out var color))
                continue;

            ref var barSlot = ref addon->ActionBarSlotVector[i];
            if (!TryGetSlotScreenRect(ref barSlot, out var min, out var max))
                continue;

            drawList.AddRect(min, max, color, 2f, ImDrawFlags.None, thickness);
        }
    }

    private static bool TryGetSlotScreenRect(ref ActionBarSlot barSlot, out Vector2 min, out Vector2 max)
    {
        min = default;
        max = default;

        var node = barSlot.IconFrame;
        if (node == null && barSlot.Icon != null)
            node = &barSlot.Icon->AtkResNode;

        if (node == null || !node->IsVisible())
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
}
