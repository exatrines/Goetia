namespace Goetia.Services;

/// <summary>
/// Mark role and party-seat mapping onto one hotbar column (hotbar 0–9, 12 slots).
/// BaseSlot is the slot for party order &lt;1&gt;; seat i uses BaseSlot + i.
/// </summary>
public enum MarkRole
{
    Attack = 0,
    Bind = 1,
    Stop = 2,
}

public static class MarkRoleNames
{
    public static string Label(MarkRole role) => role switch
    {
        MarkRole.Attack => "Attack",
        MarkRole.Bind => "Bind",
        MarkRole.Stop => "Stop",
        _ => role.ToString(),
    };
}

[Serializable]
public sealed class MarkColumnLayout
{
    public const int SlotsPerHotbar = 12;

    public byte HotbarId { get; set; }
    public byte BaseSlot { get; set; }

    public static MarkColumnLayout CreateDefault(byte hotbarId) => new()
    {
        HotbarId = hotbarId,
        BaseSlot = 0,
    };

    public bool TryMapPartyIndex(int partyIndex, out byte hotbarId, out byte slotId)
    {
        hotbarId = HotbarId;
        slotId = 0;
        if (partyIndex < 0 || partyIndex >= PartyOrderService.MaxPartySize || HotbarId > 9)
            return false;

        var slot = BaseSlot + partyIndex;
        if (slot is < 0 or >= SlotsPerHotbar)
            return false;

        slotId = (byte)slot;
        return true;
    }
}
