# Zed Window Behavior Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configurable Zed window behavior for Unity script opens.

**Architecture:** Store a `ZedWindowBehavior` enum in `ZedProjectSettings`, expose it in `ZedProjectSettingsProvider`, and have `ZedProcess` translate it into Zed CLI flags. Smart mode uses a per-project `--wait` watcher process so the first project launch creates a new workspace, later launches can reuse it, and closing the Zed workspace clears the state.

**Tech Stack:** Unity 2021.2 Editor package, C#, Unity SettingsProvider, Unity Test Framework/NUnit.

## Global Constraints

- Use only Zed CLI flags confirmed by local `zed --help`: `--new`, `--existing`, and `--wait`.
- Do not launch Zed from automated tests.
- Keep existing executable discovery and source file filtering behavior unchanged.
- Keep the default behavior configurable and backwards-compatible for single-window users.

---

### Task 1: Add Window Behavior Tests And Test Assembly

**Files:**
- Create: `Packages/com.pidan-workshop.unity-zed/Tests/Editor/ZedProcessTests.cs`
- Create: `Packages/com.pidan-workshop.unity-zed/Tests/Editor/ZedProcessTests.cs.meta`
- Create: `Packages/com.pidan-workshop.unity-zed/Tests/Editor/Unity.Zed.Editor.Tests.asmdef`
- Create: `Packages/com.pidan-workshop.unity-zed/Tests/Editor/Unity.Zed.Editor.Tests.asmdef.meta`
- Create: `Packages/com.pidan-workshop.unity-zed/Tests.meta`
- Create: `Packages/com.pidan-workshop.unity-zed/Tests/Editor.meta`

**Interfaces:**
- Consumes: planned `ZedProcess.BuildArguments(string projectDirectory, string filePath, int line, int column, ZedWindowBehavior behavior, bool smartProjectKnownOpen)`.
- Produces: failing tests that define expected CLI arguments.

- [ ] **Step 1: Write failing tests**

Add tests for Smart, Reuse Current Window, Always New Workspace, and project-only launch arguments.

- [ ] **Step 2: Run tests and verify they fail**

Run Unity Editor tests for `Unity.Zed.Editor.Tests`. Expected: compile failure because `ZedWindowBehavior` and the testable `BuildArguments` API do not exist yet.

### Task 2: Implement Settings And Launch Argument Behavior

**Files:**
- Create: `Packages/com.pidan-workshop.unity-zed/Editor/AssemblyInfo.cs`
- Create: `Packages/com.pidan-workshop.unity-zed/Editor/AssemblyInfo.cs.meta`
- Modify: `Packages/com.pidan-workshop.unity-zed/Editor/ZedProjectSettings.cs`
- Modify: `Packages/com.pidan-workshop.unity-zed/Editor/ZedProjectSettingsProvider.cs`
- Modify: `Packages/com.pidan-workshop.unity-zed/Editor/ZedProcess.cs`

**Interfaces:**
- Produces: `internal enum ZedWindowBehavior`, `ZedProjectSettings.WindowBehavior`, and `ZedProcess.BuildArguments(...)`.

- [ ] **Step 1: Add `InternalsVisibleTo`**

Expose internal editor types to `Unity.Zed.Editor.Tests`.

- [ ] **Step 2: Add `ZedWindowBehavior` and settings serialization**

Default `ZedProjectSettings.WindowBehavior` to `Smart`.

- [ ] **Step 3: Add Project Settings UI**

Render an enum popup labeled `Window Behavior` in the Open In Zed section.

- [ ] **Step 4: Implement argument construction**

Map `Smart` before first launch to `--new --wait`; map `AlwaysNewWorkspace` to `--new`; map `ReuseCurrentWindow` to `--existing`; omit flags for Smart after the project is known open.

- [ ] **Step 5: Run tests and verify they pass**

Run Unity Editor tests for `Unity.Zed.Editor.Tests`. Expected: all new tests pass.

### Task 3: Update User Documentation

**Files:**
- Modify: `Packages/com.pidan-workshop.unity-zed/README.md`
- Modify: `Packages/com.pidan-workshop.unity-zed/CHANGELOG.md`

**Interfaces:**
- Consumes: final behavior names from Task 2.
- Produces: README and changelog entries that describe the setting.

- [ ] **Step 1: Document the setting**

Add the new window behavior option to Features, Usage, and Settings.

- [ ] **Step 2: Update changelog**

Add an Unreleased entry for configurable Zed window behavior.

- [ ] **Step 3: Verify final diff**

Run `git diff --check` and inspect changed files.
