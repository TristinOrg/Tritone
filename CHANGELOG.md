# Changelog

All notable changes to Tritone are documented in this file. The format follows Keep a Changelog and package versions follow Semantic Versioning.

## [Unreleased]

### Added

- Generated strongly typed UI prefab views with deterministic sorting preprocessing.
- Window-owned reusable items, single-instance panels, and nested composition lifetimes.
- Extensible table compilation with inferred schemas, directory discovery, transactional output, and CI fixtures.
- End-to-end package guides for UI composition and configuration-table authoring.

### Changed

- Table diagnostics now report precise inferred source locations, distinguish duplicate names from conflicting schemas, and process multiple source directories deterministically.

### Fixed

- Invalid UI item and panel prefabs now release their asset references immediately.
- Generated C# output no longer writes indentation to otherwise empty lines.

## [0.1.0] - 2026-07-21

### Added

- Pure C# application kernel with deterministic module lifecycle, services, scopes, flows, models, entities, events, timers, tweening, dispatching, and diagnostics.
- Unity adapters for bootstrap, assets, scenes, UI, input, audio, pooling, saves, settings, localization, and configuration tables.
- Network sessions with reconnect support, request-response routing, protocol descriptors, compatibility checks, and connection handshakes.
- Resources, AssetBundle, and Addressables providers with scoped ownership and shared asynchronous requests.
- Transactional remote content updates, dependency-aware AssetBundle loading, and editor content builds.
- Addressables remote catalog updates, dependency preloading, progress reporting, cancellation, and cache cleanup.
- Table and network message code generation.
- EditMode and PlayMode test suites plus Unity Package CI validation.

[Unreleased]: https://github.com/TristinOrg/Tritone/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/TristinOrg/Tritone/releases/tag/v0.1.0
