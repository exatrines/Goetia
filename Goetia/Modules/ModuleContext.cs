namespace Goetia.Modules;

/// <summary>Per-frame party + highlight surface for modules.</summary>
internal sealed class ModuleContext
{
    public const int MaxPartySize = PartyOrderService.MaxPartySize;

    private readonly PartyOrderService _party;
    private readonly List<HighlightEntry> _buffer;

    internal ModuleContext(PartyOrderService party, List<HighlightEntry> buffer)
    {
        _party = party;
        _buffer = buffer;
    }

    internal void BeginFrame() => _buffer.Clear();

    public bool IsOccupied(int seat) =>
        seat is >= 0 and < MaxPartySize && _party.Slots[seat].IsOccupied;

    public bool HasStatus(int seat, uint statusId) => _party.HasStatus(seat, statusId);

    public bool HasStatusParam(int seat, uint statusId, int param)
        => _party.HasStatusParam(seat, statusId, param);

    public void SetHighlight(int seat, MarkRole role, Vector4 color)
    {
        if (seat is < 0 or >= MaxPartySize)
            return;
        _buffer.Add(new HighlightEntry(seat, role, color));
    }

    public void TakeSeats(
        List<int> seats,
        HashSet<int> claimed,
        MarkRole role,
        Vector4 color,
        int maxCount)
    {
        seats.Sort();
        var taken = 0;
        foreach (var seat in seats)
        {
            if (taken >= maxCount)
                break;
            SetHighlight(seat, role, color);
            claimed.Add(seat);
            taken++;
        }
    }

    public void TakeUnclaimed(
        HashSet<int> claimed,
        MarkRole role,
        Vector4 color,
        int maxCount,
        Func<int, bool>? predicate = null)
    {
        var seats = new List<int>();
        for (var i = 0; i < MaxPartySize; i++)
        {
            if (!IsOccupied(i) || claimed.Contains(i))
                continue;
            if (predicate != null && !predicate(i))
                continue;
            seats.Add(i);
        }

        TakeSeats(seats, claimed, role, color, maxCount);
    }

    public bool IsEnemyCasting(uint actionId)
    {
        foreach (var obj in PluginServices.ObjectTable)
        {
            if (obj is not Dalamud.Game.ClientState.Objects.Types.IBattleChara chara)
                continue;
            if (chara.IsCasting && chara.CastActionId == actionId)
                return true;
        }

        return false;
    }

    public bool AnyPartyHasStatus(uint statusId)
    {
        for (var i = 0; i < MaxPartySize; i++)
        {
            if (_party.Slots[i].IsOccupied && _party.HasStatus(i, statusId))
                return true;
        }

        return false;
    }
}
