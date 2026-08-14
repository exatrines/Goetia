using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using InteropGenerator.Runtime;

namespace Goetia.Services;

/// <summary>
/// Party HUD order (macro &lt;1&gt;–&lt;8&gt;).
/// AgentHUD supplies seats; IPartyList supplies live objects/statuses.
/// </summary>
internal sealed unsafe class PartyOrderService
{
    public const int MaxPartySize = 8;

    private readonly PartySlot[] _slots = new PartySlot[MaxPartySize];

    public IReadOnlyList<PartySlot> Slots => _slots;

    public void Refresh()
    {
        for (var i = 0; i < MaxPartySize; i++)
            _slots[i] = default;

        if (!PluginServices.ClientState.IsLoggedIn)
            return;

        if (FillFromAgentHudAndPartyList())
            return;

        TryFillLocalPlayerOnly();
    }

    private bool FillFromAgentHudAndPartyList()
    {
        var agent = AgentHUD.Instance();
        if (agent == null || agent->PartyMemberCount <= 0)
            return false;

        var nameToSeat = new Dictionary<string, int>(StringComparer.Ordinal);
        var hudBySeat = new (string Name, uint EntityId, ulong ContentId)[MaxPartySize];

        var hudCount = Math.Min(agent->PartyMemberCount, agent->PartyMembers.Length);
        for (var i = 0; i < hudCount; i++)
        {
            ref var hud = ref agent->PartyMembers[i];
            var name = ReadCString(hud.Name);
            var seat = hud.Index;
            if (seat is < 0 or >= MaxPartySize)
                continue;

            if (!string.IsNullOrEmpty(name))
                nameToSeat[name] = seat;

            hudBySeat[seat] = (name, hud.EntityId, hud.ContentId);

            if (hud.ContentId != 0 || (hud.EntityId != 0 && hud.EntityId != 0xE0000000) || !string.IsNullOrEmpty(name))
            {
                _slots[seat] = new PartySlot
                {
                    PartyIndex = seat,
                    EntityId = hud.EntityId,
                    ContentId = hud.ContentId,
                    Name = name,
                    IsOccupied = true,
                };
            }
        }

        var party = PluginServices.PartyList;
        if (party != null)
        {
            foreach (var member in party)
            {
                if (member == null)
                    continue;

                var name = member.Name.TextValue;
                if (string.IsNullOrEmpty(name))
                    name = member.GameObject?.Name.TextValue ?? string.Empty;

                var seat = ResolveSeat(name, nameToSeat);
                if (seat is < 0 or >= MaxPartySize)
                    continue;

                var entityId = member.EntityId;
                if (entityId == 0)
                    entityId = member.ObjectId;
                if (entityId == 0 && member.GameObject != null)
                    entityId = member.GameObject.EntityId;

                var contentId = (ulong)member.ContentId;
                if (contentId == 0)
                    contentId = hudBySeat[seat].ContentId;
                if (entityId == 0)
                    entityId = hudBySeat[seat].EntityId;

                var displayName = member.GameObject != null
                    ? member.GameObject.Name.TextValue
                    : name;
                if (string.IsNullOrEmpty(displayName))
                    displayName = hudBySeat[seat].Name;

                if (entityId == 0 && contentId == 0 && string.IsNullOrEmpty(displayName))
                    continue;

                _slots[seat] = new PartySlot
                {
                    PartyIndex = seat,
                    EntityId = entityId,
                    ContentId = contentId,
                    Name = displayName,
                    IsOccupied = true,
                };
            }
        }

        for (var i = 0; i < MaxPartySize; i++)
        {
            if (_slots[i].IsOccupied)
                return true;
        }

        return false;
    }

    private void TryFillLocalPlayerOnly()
    {
        var local = PluginServices.ObjectTable.LocalPlayer;
        if (local == null)
            return;

        _slots[0] = new PartySlot
        {
            PartyIndex = 0,
            EntityId = local.EntityId,
            ContentId = 0,
            Name = local.Name.TextValue,
            IsOccupied = true,
        };
    }

    public bool HasStatus(int partyIndex, uint statusId)
        => TryFindStatus(partyIndex, statusId, out _);

    public bool HasStatusParam(int partyIndex, uint statusId, int paramOrStacks)
        => TryFindStatus(partyIndex, statusId, out var param) && param == paramOrStacks;

    private bool TryFindStatus(int partyIndex, uint statusId, out int param)
    {
        param = 0;
        if (partyIndex is < 0 or >= MaxPartySize || !_slots[partyIndex].IsOccupied)
            return false;

        var entityId = _slots[partyIndex].EntityId;
        if (entityId != 0 && entityId != 0xE0000000)
        {
            var obj = PluginServices.ObjectTable.SearchByEntityId(entityId);
            if (obj is IBattleChara battle)
            {
                foreach (var status in battle.StatusList)
                {
                    if (status.StatusId != statusId)
                        continue;
                    param = status.Param;
                    return true;
                }
            }
        }

        var party = PluginServices.PartyList;
        if (party == null)
            return false;

        var slot = _slots[partyIndex];
        foreach (var member in party)
        {
            if (member == null || !MatchesSlot(member, slot))
                continue;

            foreach (var status in member.Statuses)
            {
                if (status.StatusId != statusId)
                    continue;
                param = status.Param;
                return true;
            }
        }

        return false;
    }

    private static int ResolveSeat(string name, Dictionary<string, int> nameToSeat)
    {
        if (string.IsNullOrEmpty(name))
            return -1;

        if (nameToSeat.TryGetValue(name, out var mapped))
            return mapped;

        foreach (var (hudName, hudSeat) in nameToSeat)
        {
            if (NamesMatch(hudName, name))
                return hudSeat;
        }

        return -1;
    }

    private static bool MatchesSlot(IPartyMember member, PartySlot slot)
    {
        var entityId = member.EntityId != 0 ? member.EntityId : member.ObjectId;
        if (entityId != 0 && slot.EntityId != 0 && entityId == slot.EntityId)
            return true;

        if (slot.ContentId != 0 && (ulong)member.ContentId == slot.ContentId)
            return true;

        var name = member.Name.TextValue;
        return !string.IsNullOrEmpty(name)
               && !string.IsNullOrEmpty(slot.Name)
               && NamesMatch(name, slot.Name);
    }

    private static bool NamesMatch(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
            return true;

        static string Base(string s)
        {
            var at = s.IndexOf('@');
            return at >= 0 ? s[..at] : s;
        }

        return string.Equals(Base(a), Base(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadCString(CStringPointer ptr)
    {
        var text = ptr.ToString();
        return string.IsNullOrEmpty(text) ? string.Empty : text;
    }
}

internal readonly struct PartySlot
{
    public int PartyIndex { get; init; }
    public uint EntityId { get; init; }
    public ulong ContentId { get; init; }
    public string Name { get; init; }
    public bool IsOccupied { get; init; }
}
