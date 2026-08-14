# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.2] - 2026-08-14

### Added

- Plugin icon for Dalamud installer and in-game plugin list.

### Changed

- Point installer screenshot (`ImageUrls`) at the `main` branch.
- Clarify README setup, modules, and party-list wording.

## [1.0.1] - 2026-08-14

### Added

- README hero and plugin installer screenshot (party list Attack / Bind / Stop highlights).

## [1.0.0] - 2026-08-14

### Added

- Manual mark assist: highlight Attack / Bind / Stop hotbar slots by party HUD order (`<1>`–`<8>`). Never issues `/mk`.
- TOP modules: Run Dynamis Delta, Sigma, and Omega (C#).
- Main window: module list with enable toggles, per-module Rules and Options, and an eye icon for the Preview overlay.
- Settings: Hotbars (role mapping, assignment table, preview checkboxes) and Style (outline thickness).
- Preview overlay: party seats × hotbars and which source is driving highlights (close with ×).
- Per-rule outline colors (Near/Far World red, Dynamis stacks purple, Remaining yellow; reload resets to defaults).

[Unreleased]: https://github.com/exatrines/Goetia/compare/v1.0.2...HEAD
[1.0.2]: https://github.com/exatrines/Goetia/releases/tag/v1.0.2
[1.0.1]: https://github.com/exatrines/Goetia/releases/tag/v1.0.1
[1.0.0]: https://github.com/exatrines/Goetia/releases/tag/v1.0.0
