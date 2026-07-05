# Task 7: ZedPreferences — Complete

**Commit:** `b745421` — `feat: add ZedPreferences SettingsProvider for Zed configuration`

**File created:** `Packages/com.unity.ide.zededitor/Editor/ZedPreferences.cs` (100 lines)

**Self-review:**
- Code copied verbatim from task-7-brief.md — no deviations.
- `#if UNITY_EDITOR_WIN` / `#else` pattern used (safe on all Unity versions; avoids `UNITY_EDITOR_LINUX` which is 2022.2+).
- Consumes `ZedDiscovery.FindZed()`, `ZedDiscovery.InvalidateCache()`, `ZedEditorPrefs.*` as specified.
- Produces `[SettingsProvider]` at `Preferences/External Tools/Zed Editor` with:
  - Executable path text field + Browse button
  - Discovery status (green "Found at:" / red "Not found")
  - "Detect Again" button
  - Additional Args text field
- All four dependent APIs from earlier tasks are correctly referenced.
