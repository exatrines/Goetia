namespace Goetia.Services;

/// <summary>Ephemeral highlight requests from modules, keyed by source id + party seat + role.</summary>
internal sealed class HighlightStore
{
    public const string SourcePreviewAttack = "preview:attack";
    public const string SourcePreviewBind = "preview:bind";
    public const string SourcePreviewStop = "preview:stop";
    private readonly Dictionary<string, List<HighlightEntry>> _bySource = new(StringComparer.Ordinal);

    public void Replace(string sourceId, IReadOnlyList<HighlightEntry> entries)
    {
        if (string.IsNullOrEmpty(sourceId))
            return;

        if (entries.Count == 0)
        {
            _bySource.Remove(sourceId);
            return;
        }

        _bySource[sourceId] = entries.ToList();
    }

    public void ClearSource(string sourceId)
    {
        if (!string.IsNullOrEmpty(sourceId))
            _bySource.Remove(sourceId);
    }

    public void ClearAll() => _bySource.Clear();

    public void CollectActive(List<HighlightEntry> output)
    {
        output.Clear();
        var seen = new HashSet<(int, MarkRole)>();
        foreach (var list in _bySource.Values)
        {
            foreach (var entry in list)
            {
                if (entry.PartyIndex is < 0 or >= PartyOrderService.MaxPartySize)
                    continue;
                if (!seen.Add((entry.PartyIndex, entry.Role)))
                    continue;
                output.Add(entry);
            }
        }
    }

    public void CollectActiveSources(List<HighlightSourceSnapshot> output)
    {
        output.Clear();
        foreach (var (sourceId, list) in _bySource)
        {
            if (list.Count == 0)
                continue;
            output.Add(new HighlightSourceSnapshot(sourceId, list));
        }
    }
}

internal readonly struct HighlightEntry(int partyIndex, MarkRole role, Vector4 color)
{
    public int PartyIndex { get; } = partyIndex;
    public MarkRole Role { get; } = role;
    public Vector4 Color { get; } = color;
}

internal sealed class HighlightSourceSnapshot(string sourceId, IReadOnlyList<HighlightEntry> entries)
{
    public string SourceId { get; } = sourceId;
    public IReadOnlyList<HighlightEntry> Entries { get; } = entries;
}
