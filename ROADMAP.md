# Tritone Roadmap

This document is the shared source of truth for ongoing development. Keep it in
the repository so that a new Codex task, on any computer, can recover the
current direction without relying on chat history.

- Last reviewed: 2026-08-18
- Current release: `v0.1.0`
- Current development target: `v0.2.0` release candidate

## Current focus

Development for the planned `v0.2.0` scope is complete. The next session should
prepare and publish the release:

1. Review the `v0.2.0` entries under `Unreleased` in `CHANGELOG.md`.
2. Change `package.json` to `0.2.0` and replace `Unreleased` with a dated
   `0.2.0` heading.
3. Run package validation plus EditMode and PlayMode tests once more.
4. Commit the release metadata, tag `v0.2.0`, push the tag, and verify the
   GitHub workflow.

## Shipped in v0.1.0

- [x] Pure C# kernel with deterministic module lifecycle and scoped services.
- [x] Flows, models, entities, events, timers, tweening, dispatching, and
  diagnostics.
- [x] Unity adapters for assets, scenes, UI, input, audio, pooling, saves,
  settings, localization, and tables.
- [x] Network sessions, request-response routing, reconnect support, protocol
  compatibility, and generated binary messages.
- [x] Resources, AssetBundle, and Addressables providers.
- [x] Transactional content updates and editor content builds.
- [x] Unity Package Manager metadata, package validation, and CI test jobs.

## v0.2.0 - Authoring workflow and UI composition

Work already completed on `main` after `v0.1.0`:

- [x] Generate strongly typed references for UI prefabs.
- [x] Preprocess UI sorting hierarchies.
- [x] Add reusable window item and panel composition.
- [x] Add an extensible table compiler with transactional output.
- [x] Infer table schemas from source data.
- [x] Discover table sources from configured directories.

Remaining work, in priority order:

- [x] Harden UI composition lifecycle behavior and failure recovery.
- [x] Add end-to-end documentation for generated views, windows, panels, and
  items.
- [x] Expand table compiler diagnostics with source, row, and column context.
- [x] Define and test duplicate table names, conflicting inferred schemas, and
  deterministic output ordering across multiple source directories.
- [x] Add sample table sources and generated output to the CI test project.
- [x] Update `Documentation~/index.md` so the package documentation covers the
  post-`v0.1.0` authoring workflows.
- [x] Run `.ci/Validate-Package.ps1` and all available Unity EditMode and
  PlayMode tests.
- [x] Update `CHANGELOG.md` and choose the final `0.2.0` development scope.
- [ ] Publish the `v0.2.0` release metadata and tag.

Validation recorded on 2026-08-18:

- Package validation: 369 Unity assets.
- EditMode: 153 passed, 0 failed, 0 skipped.
- PlayMode: 2 passed, 0 failed, 0 skipped.
- Unity Console: no errors or warnings after the full test run.

## Later candidates

These are intentionally unordered and are not commitments for `v0.2.0`:

- Sample project demonstrating bootstrap, content delivery, tables, and UI as
  one coherent application.
- Profiling baselines for per-frame kernel paths, pooled assets, UI lifecycle,
  and network dispatch.
- Additional table source and output formats through the compiler extension
  points.
- Editor tooling for inspecting module dependencies, active scopes, loaded
  assets, and UI ownership.
- Platform build coverage beyond the current Unity 2022.3 package checks.

Promote a candidate into a versioned milestone only after its scope and
acceptance criteria are written down.

## Definition of done

A roadmap item is complete only when:

- Its public behavior and failure behavior are covered by tests.
- Public APIs follow the repository XML documentation and naming rules.
- The pure C# Kernel remains independent of `UnityEngine`.
- Relevant README and package documentation are updated.
- Package validation passes and available Unity tests introduce no regression.
- `CHANGELOG.md` is updated when the change is user-visible.

## Cross-computer handoff

At the beginning of a development session:

1. Pull the latest branch and inspect `git status` before making changes.
2. Read this file, then read the latest entries in `git log --oneline`.
3. Start with the first unchecked item under **Current focus** or the active
   milestone.
4. Treat unchecked items as planned work, not as proof that no partial
   implementation exists; inspect the related code and tests first.

At the end of a development session:

1. Check completed items and rewrite **Current focus** with one concrete next
   action.
2. Record newly discovered follow-up work under the appropriate milestone.
3. Update the `Last reviewed` date.
4. Run `graphify update .` when Graphify is available and commit the refreshed
   graph together with the code.
5. Commit and push the code, tests, documentation, and roadmap update so the
   next computer receives the full handoff.

