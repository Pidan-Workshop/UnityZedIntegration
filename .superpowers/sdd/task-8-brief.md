### Task 8: End-to-End Verification

**Files:**
- Create: `Assets/Scripts/TestScript.cs` (temporary, for testing)

**Interfaces:**
- Consumes: All previous tasks
- Produces: Verified working integration

- [ ] **Step 1: Create a test C# script**

Write `Assets/Scripts/TestScript.cs`:

```csharp
using UnityEngine;

public class TestScript : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hello from Zed!");
    }
}
```

If `Assets/Scripts/` doesn't exist, create it in Unity first (right-click in Project window > Create > Folder > Scripts).

- [ ] **Step 2: Verify External Tools dropdown**

Open `Edit > Preferences > External Tools`. Open the "External Script Editor" dropdown. Confirm "Zed" appears in the list. Select "Zed".

- [ ] **Step 3: Verify .sln generation**

With Zed selected as the external editor, go to `Assets > Open C# Project`. This should call `OpenProject("", 0, 0)`, which triggers solution generation. Verify that a `.sln` file appears in the project root (e.g., `UnityZed.sln`).

- [ ] **Step 4: Verify script open with double-click**

Double-click `TestScript.cs` in the Unity Project window. Verify:
- If Zed is installed: Zed opens with the project folder and the file at the correct position
- If Zed is NOT installed: An error dialog appears guiding the user to Preferences

- [ ] **Step 5: Verify project auto-open**

If Zed is installed but not running: double-clicking should launch Zed and open the project folder.
If Zed is already running with this project: the existing window should focus and open the file.

- [ ] **Step 6: Clean up test script**

Delete `Assets/Scripts/TestScript.cs` and `Assets/Scripts/TestScript.cs.meta`.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "chore: verification complete, remove test script"
```
