# Zed Window Behavior Design

**Date:** 2026-07-09
**Project:** UnityZedIntegration

## Overview

Add a project setting that controls how Unity opens Zed windows when scripts are opened from Unity. The default behavior should reduce accidental cross-project reuse when multiple Unity projects are open, while still allowing users who prefer one Zed window to keep that workflow.

## Goals

- Add a configurable Zed window behavior in `Edit > Project Settings > Zed Editor`.
- Default to a smart behavior that opens a new Zed workspace for the current project, reuses it while the workspace is alive, and forgets it after the Zed window closes.
- Preserve a single-window workflow through an explicit setting.
- Add an explicit always-new-workspace mode.
- Keep the launch logic cross-platform and limited to Zed CLI flags confirmed by `zed --help`.

## Non-Goals

- Do not attempt OS-level window enumeration or UI automation.
- Do not inspect Zed private databases or runtime IPC state.
- Do not add dependencies outside Unity and the existing Visual Studio Editor package.

## Window Behaviors

`Smart`
: Default. The first launch for a project directory in the current Unity editor process passes `--new --wait` so the project gets its own workspace window and Unity keeps a lightweight watcher for that workspace. Later launches while the watcher is alive omit window flags and pass the project folder plus selected file, allowing Zed to focus or reuse the matching workspace. When the Zed workspace window closes, the watcher exits and Unity forgets the project, so the next launch creates a fresh workspace again.

`Reuse Current Window`
: Passes `--existing` to Zed. This supports users who intentionally want Unity opens to use an existing Zed window.

`Always New Workspace`
: Passes `--new` for every Unity open request.

## Components

- `ZedWindowBehavior`: enum stored in project settings.
- `ZedProjectSettings`: serializes and exposes the selected behavior.
- `ZedProjectSettingsProvider`: draws an enum popup under the Open In Zed section.
- `ZedProcess`: builds arguments from the selected behavior and keeps an in-memory watcher process for project directories opened by Smart mode during the current Unity editor session.
- Editor tests: cover argument construction for each behavior without launching Zed.

## Error Handling

Existing executable discovery and process-start error handling remains unchanged. Smart mode only records a project directory as opened after `Process.Start` succeeds, and removes it when the `--wait` watcher exits.

## Testing

Add Unity Editor tests for argument construction:

- Smart mode before the project is known open includes `--new --wait`.
- Smart mode after the project is known open omits window flags.
- Reuse Current Window includes `--existing`.
- Always New Workspace includes `--new`.
- Project-only opens still include the selected window flag and project directory.
