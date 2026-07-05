### Task 1: Scaffold Package Structure

**Files:**
- Create: `Packages/com.unity.ide.zededitor/package.json`
- Create: `Packages/com.unity.ide.zededitor/Editor/ZedScriptEditor.asmdef`
- Create: `Packages/com.unity.ide.zededitor/Editor/`
- Create: `Packages/com.unity.ide.zededitor/README.md`
- Create: `Packages/com.unity.ide.zededitor/CHANGELOG.md`

**Interfaces:**
- Produces: package structure, `Unity.Zed.Editor` assembly available for later tasks

- [ ] **Step 1: Create directory structure**

```powershell
New-Item -ItemType Directory -Path "Packages/com.unity.ide.zededitor/Editor" -Force
```

- [ ] **Step 2: Write package.json**

```json
{
  "name": "com.unity.ide.zededitor",
  "displayName": "Zed Editor",
  "description": "Code editor integration for supporting Zed as code editor for Unity. Adds support for generating csproj/sln files, auto discovery of installations, and script opening with line/column navigation.",
  "version": "0.1.0",
  "unity": "2021.2",
  "dependencies": {
    "com.unity.ide.visualstudio": "2.0.0"
  },
  "keywords": ["zed", "editor", "code"],
  "category": "Editor"
}
```

- [ ] **Step 3: Write asmdef**

```json
{
    "name": "Unity.Zed.Editor",
    "references": [
        "Unity.VisualStudio.Editor"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Verify Unity detects the package**

Open Unity, check Console for any assembly compilation errors. There should be none since the Editor/ directory is empty.

- [ ] **Step 5: Commit**

```powershell
git add Packages/com.unity.ide.zededitor/
git commit -m "feat: scaffold com.unity.ide.zededitor package structure"
```

---

