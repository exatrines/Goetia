# Goetia

[日本語](README.ja.md)

Goetia is a Dalamud plugin for **manual mark assist**. It highlights Attack / Bind / Stop hotbar slots in party HUD order (`<1>`–`<8>`).

You place `/mk` macros on those bars yourself. When a builtin module suggests a mark, Goetia draws a frame on the matching slot. It never issues `/mk`.

Map three hotbars in settings, enable the Dynamis modules you need, then mark from the highlighted macros during the pull.

## Install

1. Run `/xlsettings` and open the **Experimental** tab
2. Add this URL under **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/exatrines/DalamudPlugins/refs/heads/main/pluginmaster.json
```

3. Run `/xlplugins` and install **Goetia**

## Features

- **Hotbar highlighting** — Attack / Bind / Stop columns mapped to party order; per-rule outline color and thickness
- **Run Dynamis Delta** — Near/Far World → Stop
- **Run Dynamis Sigma** — Near/Far World Stop; Dynamis ×1 then remainder Attack
- **Run Dynamis Omega** — Half1 then Half2 after FirstInLine clears
- **Preview overlay** — optional overlay of seats × hotbars and which module is driving highlights (main window eye; close with × to turn off)

## Commands

| Command | Description |
| --- | --- |
| `/goetia` | Toggle the main window (module list) |
| `/goetia settings` | Toggle plugin settings (`config` / `s` also work) |

## For developers

1. Build: `dotnet build Goetia.sln -c Release -p:Platform=x64`
2. Point Dalamud’s **dev plugin** path at `Goetia/bin/Release/`
3. Enable **Goetia** in the plugin installer (dev)

[MirageUI](https://github.com/exatrines/MirageUI) is included as a git submodule for the shared UI kit.

## License

[AGPL-3.0-or-later](LICENSE)
