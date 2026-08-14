using Dalamud.Interface;
using Newtonsoft.Json;

namespace Goetia.Modules;

/// <summary>Builtin highlight module. Never casts /mk; only suggests hotbar slots.</summary>
internal abstract class GoetiaModule
{
    public abstract string Id { get; }
    public virtual string DisplayName => Id;
    public virtual bool EnabledDefault => true;
    public virtual IReadOnlySet<uint>? ValidTerritories => null;

    public bool IsEnabled { get; set; }

    public virtual void OnReset() { }
    public abstract void OnUpdate(ModuleContext context);
    public virtual void DrawConfig() { }

    protected T GetConfig<T>() where T : class, new()
    {
        if (C.ModuleConfigs.TryGetValue(Id, out var json) && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var loaded = JsonConvert.DeserializeObject<T>(json);
                if (loaded != null)
                    return loaded;
            }
            catch (Exception ex)
            {
                PluginServices.Log.Warning(ex, "Goetia: bad module config for {0}", Id);
            }
        }

        return new T();
    }

    protected void SaveConfig<T>(T config) where T : class
    {
        C.ModuleConfigs[Id] = JsonConvert.SerializeObject(config);
        C.Save();
    }

    public static readonly Vector4 DefaultColorNearFarWorld = new(0.90f, 0.20f, 0.20f, 1f);
    public static readonly Vector4 DefaultColorDynamis = new(0.58f, 0.28f, 0.88f, 1f);
    public static readonly Vector4 DefaultColorRemaining = new(1.00f, 0.90f, 0.15f, 1f);

    private static readonly string[] MarkHotbarLabels = ["Attack", "Bind", "Stop"];
    private const float MarkHotbarComboWidth = 86f;
    private static readonly ImGuiColorEditFlags MarkColorFlags =
        ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs;

    protected static bool DrawMarkHotbar(
        string label,
        ref MarkRole role,
        ref Vector4 color,
        Vector4 defaultColor)
    {
        var frame = ImGui.GetFrameHeight();
        var gap = ImGui.GetStyle().ItemInnerSpacing.X;
        var groupW = MarkHotbarComboWidth + (gap * 2f) + (frame * 2f) + 8f;
        var tableId = $"##MarkHotbar_{label}";
        if (!ImGui.BeginTable(
                tableId,
                3,
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoPadOuterX,
                new Vector2(-1f, 0f)))
            return false;

        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##gap", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##widgets", ImGuiTableColumnFlags.WidthFixed, groupW);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        var changed = false;
        var text = MarkRoleNames.Label(role);
        if (MirageUi.Dropdown(
                string.Empty,
                ref text,
                MarkHotbarLabels,
                allowClear: false,
                id: label,
                width: MarkHotbarComboWidth)
            && Array.IndexOf(MarkHotbarLabels, text) is >= 0 and var index)
        {
            role = (MarkRole)index;
            changed = true;
        }

        ImGui.SameLine(0f, gap);
        if (MirageUi.ColorEdit4(
                string.Empty,
                ref color,
                MarkColorFlags,
                id: $"color_{label}",
                width: frame))
            changed = true;

        ImGui.SameLine(0f, gap);
        if (MirageUi.IconButton(
                FontAwesomeIcon.Sync,
                id: $"resetColor_{label}",
                size: new Vector2(frame),
                tooltip: "Reset to default"))
        {
            color = defaultColor;
            changed = true;
        }

        ImGui.EndTable();
        return changed;
    }
}
