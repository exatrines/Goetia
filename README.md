# Goetia

[日本語](README.ja.md)

![Party list with Attack, Bind, and Stop highlights](docs/screenshots/party-highlight-1280x720.png)

Goetia is a Dalamud plugin for **manual mark assist**. It highlights Attack / Bind / Stop hotbar slots in party list order (`<1>`–`<8>`).

You need to place `/mk` macros on the assigned hotbars yourself. When a module suggests a mark, a frame appears on the matching slot. It never issues `/mk`.

- Map hotbars in settings
- Enable the modules you need
- During combat, mark from the highlighted macros

## Install

1. Run `/xlsettings` and open the **Experimental** tab
2. Add this URL under **Custom Plugin Repositories**:

```
https://raw.githubusercontent.com/exatrines/DalamudPlugins/refs/heads/main/pluginmaster.json
```

3. Run `/xlplugins` and install **Goetia**

## Features

- **Hotbar highlighting** — Maps Attack / Bind / Stop hotbars to party list order, with per-rule outline color and thickness.
- **Preview overlay** — Optional overlay of party slots and hotbars, and which module is driving highlights (open from the main window Eye; close with × to turn off).

## Modules

- **Run Dynamis Delta** — Near/Far World → Stop
- **Run Dynamis Sigma** — Near/Far World → Stop; Dynamis ×1 then remainder Attack
- **Run Dynamis Omega** — Half1 then Half2 after FirstInLine clears

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
