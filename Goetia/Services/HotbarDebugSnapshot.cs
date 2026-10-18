using FFXIVClientStructs.FFXIV.Client.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Goetia.Services;

/// <summary>Debug JSON for Settings Copy JSON: plugin settings, module options, and HUD Action Bars.</summary>
internal static unsafe class HotbarDebugSnapshot
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    private static readonly string[] KnownModuleIds =
    [
        Configuration.ModuleIdDelta,
        Configuration.ModuleIdSigma,
        Configuration.ModuleIdOmega,
        Configuration.ModuleIdWrathOfTheHeavens,
        Configuration.ModuleIdWrothFlames,
    ];

    public static string BuildJson()
    {
        var viewport = ImGui.GetMainViewport();
        var dump = new Dump
        {
            LoggedIn = PluginServices.ClientState.IsLoggedIn,
            Viewport = new Rect
            {
                X = viewport.Pos.X,
                Y = viewport.Pos.Y,
                W = viewport.Size.X,
                H = viewport.Size.Y,
            },
            Settings = BuildSettings(),
            Modules = BuildModules(),
        };

        foreach (var (addonName, hotbarId) in HotbarHighlightService.HudActionBars)
            dump.Bars.Add(BuildBar(addonName, hotbarId));

        return JsonConvert.SerializeObject(dump, JsonSettings);
    }

    private static SettingsDump BuildSettings() =>
        new()
        {
            Attack = Column(C.AttackColumn),
            Bind = Column(C.BindColumn),
            Stop = Column(C.StopColumn),
            PreviewAttack = C.PreviewAttack,
            PreviewBind = C.PreviewBind,
            PreviewStop = C.PreviewStop,
            HighlightThickness = C.HighlightThickness,
            ShowPreview = C.ShowPreview,
            HotbarTableAssignedOnly = C.HotbarTableAssignedOnly,
        };

    private static ColumnDump Column(MarkColumnLayout column) =>
        new()
        {
            Hud = column.HotbarId + 1,
            BaseSlot = column.BaseSlot + 1,
        };

    private static Dictionary<string, ModuleDump> BuildModules()
    {
        var modules = new Dictionary<string, ModuleDump>(StringComparer.Ordinal);
        foreach (var id in KnownModuleIds)
            modules[id] = Module(id);

        foreach (var id in C.ModuleConfigs.Keys)
        {
            if (!modules.ContainsKey(id))
                modules[id] = Module(id);
        }

        return modules;
    }

    private static ModuleDump Module(string id) =>
        new()
        {
            Enabled = C.GetModuleEnabled(id),
            Config = ParseModuleConfig(id),
        };

    private static JToken ParseModuleConfig(string id)
    {
        if (!C.ModuleConfigs.TryGetValue(id, out var json) || string.IsNullOrWhiteSpace(json))
            return new JObject();

        try
        {
            return JToken.Parse(json);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static BarDump BuildBar(string addonName, byte hotbarId)
    {
        var bar = new BarDump
        {
            Hud = hotbarId + 1,
            Addon = addonName,
        };

        if (!HotbarHighlightService.TryGetAddon(addonName, out var addon))
            return bar;

        bar.Found = true;
        bar.Set = addon->RaptureHotbarId <= 9 ? addon->RaptureHotbarId + 1 : addon->RaptureHotbarId;
        bar.AddonVisible = addon->IsVisible;
        bar.X = addon->X;
        bar.Y = addon->Y;
        bar.Scale = addon->Scale;
        bar.SlotCount = addon->SlotCount;
        bar.VectorCount = addon->ActionBarSlotVector.Count;

        if (addon->RootNode != null)
        {
            bar.RootVisible = addon->RootNode->IsVisible();
            var root = addon->RootNode;
            bar.Root = new Rect
            {
                X = root->ScreenX,
                Y = root->ScreenY,
                W = root->Width * root->ScaleX,
                H = root->Height * root->ScaleY,
            };
        }

        var slotCount = Math.Min((int)addon->SlotCount, addon->ActionBarSlotVector.Count);
        for (var i = 0; i < slotCount; i++)
        {
            ref var barSlot = ref addon->ActionBarSlotVector[i];
            var slot = new SlotDump { Slot = i + 1 };
            if (HotbarHighlightService.TryPickSlotNode(ref barSlot, out var node, out var source))
            {
                slot.Node = source;
                slot.Visible = node->IsVisible();
                if (HotbarHighlightService.TryMeasureNode(node, out var min, out var max))
                {
                    slot.X = min.X;
                    slot.Y = min.Y;
                    slot.W = max.X - min.X;
                    slot.H = max.Y - min.Y;
                }
            }

            bar.Slots.Add(slot);
        }

        return bar;
    }

    private sealed class Dump
    {
        public bool LoggedIn { get; set; }
        public Rect Viewport { get; set; } = new();
        public SettingsDump Settings { get; set; } = new();
        public Dictionary<string, ModuleDump> Modules { get; set; } = new(StringComparer.Ordinal);
        public List<BarDump> Bars { get; } = [];
    }

    private sealed class SettingsDump
    {
        public ColumnDump Attack { get; set; } = new();
        public ColumnDump Bind { get; set; } = new();
        public ColumnDump Stop { get; set; } = new();
        public bool PreviewAttack { get; set; }
        public bool PreviewBind { get; set; }
        public bool PreviewStop { get; set; }
        public float HighlightThickness { get; set; }
        public bool ShowPreview { get; set; }
        public bool HotbarTableAssignedOnly { get; set; }
    }

    private sealed class ColumnDump
    {
        public int Hud { get; set; }
        public int BaseSlot { get; set; }
    }

    private sealed class ModuleDump
    {
        public bool Enabled { get; set; }
        public JToken Config { get; set; } = new JObject();
    }

    private sealed class BarDump
    {
        public int Hud { get; set; }
        public string Addon { get; set; } = "";
        public bool Found { get; set; }
        public int Set { get; set; }
        public bool AddonVisible { get; set; }
        public bool RootVisible { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public float Scale { get; set; }
        public int SlotCount { get; set; }
        public int VectorCount { get; set; }
        public Rect? Root { get; set; }
        public List<SlotDump> Slots { get; } = [];
    }

    private sealed class SlotDump
    {
        public int Slot { get; set; }
        public string? Node { get; set; }
        public bool Visible { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? W { get; set; }
        public float? H { get; set; }
    }

    private sealed class Rect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }
        public float H { get; set; }
    }
}
