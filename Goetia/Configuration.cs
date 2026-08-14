using Dalamud.Configuration;
using Newtonsoft.Json;

namespace Goetia;

/// <summary>Persisted plugin settings.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const string ModuleIdDelta = "TOP.DynamisDelta";
    public const string ModuleIdSigma = "TOP.DynamisSigma";
    public const string ModuleIdOmega = "TOP.DynamisOmega";

    public int Version { get; set; } = 1;

    public Dictionary<string, string> ModuleConfigs { get; set; } = new(StringComparer.Ordinal);

    public bool EnableDynamisDelta { get; set; } = true;
    public bool EnableDynamisSigma { get; set; } = true;
    public bool EnableDynamisOmega { get; set; } = true;

    public MarkColumnLayout AttackColumn { get; set; } = MarkColumnLayout.CreateDefault(0);
    public MarkColumnLayout BindColumn { get; set; } = MarkColumnLayout.CreateDefault(1);
    public MarkColumnLayout StopColumn { get; set; } = MarkColumnLayout.CreateDefault(2);

    public float HighlightThickness { get; set; } = 3f;
    public bool HotbarTableAssignedOnly { get; set; } = true;
    public bool PreviewAttack { get; set; }
    public bool PreviewBind { get; set; }
    public bool PreviewStop { get; set; }

    [JsonProperty("DebugShowPartyOrder")]
    public bool ShowPreview { get; set; }

    public MirageColorSettings? ThemeColors { get; set; }

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        AttackColumn ??= MarkColumnLayout.CreateDefault(0);
        BindColumn ??= MarkColumnLayout.CreateDefault(1);
        StopColumn ??= MarkColumnLayout.CreateDefault(2);
        ModuleConfigs ??= new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public void Save() => _pluginInterface?.SavePluginConfig(this);

    public bool GetModuleEnabled(string id, bool defaultValue = true) => id switch
    {
        ModuleIdDelta => EnableDynamisDelta,
        ModuleIdSigma => EnableDynamisSigma,
        ModuleIdOmega => EnableDynamisOmega,
        _ => defaultValue,
    };

    public void SetModuleEnabled(string id, bool enabled)
    {
        switch (id)
        {
            case ModuleIdDelta:
                EnableDynamisDelta = enabled;
                break;
            case ModuleIdSigma:
                EnableDynamisSigma = enabled;
                break;
            case ModuleIdOmega:
                EnableDynamisOmega = enabled;
                break;
        }
    }

    public MarkColumnLayout GetColumn(MarkRole role) => role switch
    {
        MarkRole.Attack => AttackColumn,
        MarkRole.Bind => BindColumn,
        MarkRole.Stop => StopColumn,
        _ => StopColumn,
    };

    public bool GetColumnPreview(MarkRole role) => role switch
    {
        MarkRole.Attack => PreviewAttack,
        MarkRole.Bind => PreviewBind,
        MarkRole.Stop => PreviewStop,
        _ => false,
    };

    public void SetColumnPreview(MarkRole role, bool enabled)
    {
        switch (role)
        {
            case MarkRole.Attack:
                PreviewAttack = enabled;
                break;
            case MarkRole.Bind:
                PreviewBind = enabled;
                break;
            case MarkRole.Stop:
                PreviewStop = enabled;
                break;
        }
    }
}
