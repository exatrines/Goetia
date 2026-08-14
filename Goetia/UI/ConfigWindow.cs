using Dalamud.Interface;
using Goetia.Modules;
using MirageUI.Layout;

namespace Goetia.UI;

/// <summary>Main window: module list and per-module config.</summary>
internal sealed class ConfigWindow : Window
{
    private static readonly Vector2 DefaultSize = new(780, 560);

    private readonly ModuleHost _modules;
    private readonly Action _openPluginSettings;
    private ImRaii.ColorDisposable? _themeScope;
    private string? _selectedId;
    private string _sidebarSearch = string.Empty;

    public ConfigWindow(ModuleHost modules, Action openPluginSettings)
        : base(
            "Goetia###goetiaMain",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        _modules = modules;
        _openPluginSettings = openPluginSettings;
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
        EnsureValidSelection();

        var state = CreateTwoColumnState();
        MirageUi.TwoColumn.Draw(state, DrawMainContent);

        if (!string.IsNullOrEmpty(state.SelectedId))
            _selectedId = state.SelectedId;
    }

    private MirageTwoColumnState CreateTwoColumnState()
    {
        var version = PluginServices.PluginInterface.Manifest.AssemblyVersion?.ToString() ?? "1.0.0";

        return new MirageTwoColumnState
        {
            ShowSidebarHeader = true,
            ShowSidebarFooter = false,
            ShowSearch = true,
            SearchHint = "Search modules…",
            SearchFilter = _sidebarSearch,
            ShowEntryToggle = true,
            AllowDeselect = false,
            SidebarHeader = new MirageTwoColumnSidebarHeader
            {
                Title = PluginServices.PluginInterface.Manifest.Name ?? "Goetia",
                Subtitle = $"v{version}",
                TrailingActions =
                [
                    new MirageTwoColumnTrailingAction
                    {
                        Id = "preview",
                        Icon = FontAwesomeIcon.Eye,
                        Tooltip = "Preview",
                        OnClick = TogglePreview,
                    },
                    new MirageTwoColumnTrailingAction
                    {
                        Id = "settings",
                        Icon = FontAwesomeIcon.Cog,
                        Tooltip = "Settings",
                        OnClick = _openPluginSettings,
                    },
                ],
            },
            Entries = BuildEntries(),
            SelectedId = _selectedId,
            OnSelectionChanged = id =>
            {
                if (!string.IsNullOrEmpty(id))
                    _selectedId = id;
            },
            OnSearchFilterChanged = filter => _sidebarSearch = filter ?? string.Empty,
            OnEnabledChanged = OnModuleEnabledChanged,
        };
    }

    private static void TogglePreview()
    {
        C.ShowPreview = !C.ShowPreview;
        C.Save();
    }

    private void OnModuleEnabledChanged(string id, bool enabled)
    {
        var module = _modules.Modules.FirstOrDefault(m => m.Id == id);
        if (module != null)
            _modules.SetEnabled(module, enabled);
    }

    private List<MirageTwoColumnEntry> BuildEntries()
    {
        var entries = new List<MirageTwoColumnEntry>(_modules.Modules.Count);
        foreach (var module in _modules.Modules)
        {
            entries.Add(new MirageTwoColumnEntry
            {
                Id = module.Id,
                Label = module.DisplayName,
                Enabled = module.IsEnabled,
            });
        }

        return entries;
    }

    private void EnsureValidSelection()
    {
        var list = _modules.Modules;
        if (list.Count == 0)
        {
            _selectedId = null;
            return;
        }

        if (_selectedId != null && list.Any(m => m.Id == _selectedId))
            return;

        _selectedId = list[0].Id;
    }

    private void DrawMainContent()
    {
        var module = _modules.Modules.FirstOrDefault(m => m.Id == _selectedId);
        if (module == null)
        {
            MirageUi.Header("Modules");
            MirageUi.Text("Module not found.", MirageUi.Color.Secondary);
            return;
        }

        MirageUi.HeaderWithBool(module.DisplayName, module.IsEnabled);

        try
        {
            module.DrawConfig();
        }
        catch (Exception ex)
        {
            ImGui.TextWrapped($"DrawConfig error: {ex.Message}");
        }
    }
}
