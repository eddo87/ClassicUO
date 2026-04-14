# Grid Container Port from TazUO - Implementation Plan (v2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the Grid Container (grid backpack) feature from TazUO into ClassicUO, replacing the freeform item layout with a uniform grid view when enabled.

**Architecture:** Copy TazUO's GridContainer system into ClassicUO with targeted adaptations for ClassicUO's different base class hierarchy (`ResizableGump : Gump` instead of TazUO's `ResizableGump : AnchorableGump`). Strip anchor/lock features that depend on TazUO's `AnchorableGump` chain. Replace `World.Instance` with parameter passing. Stub `ObjectActionQueue` with direct `GameActions` calls.

**Tech Stack:** C# (.NET 10), FNA/MonoGame rendering, Native AOT (`-p:NativeLib=Shared`)

**Source repos:**
- TazUO (donor): `C:/Users/EdoardoCiccarelli/Documents/GitHub/TazUO/`
- ClassicUO (target): `C:/Users/EdoardoCiccarelli/Documents/GitHub/ClassicUO/`
- Branch: `grid-container` (create from `map-navigator`)

**Build:** `export PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/Installer" && cd scripts && bash build-naot.sh`

**Deploy:** Copy `bin/dist/` to `C:/Users/EdoardoCiccarelli/Desktop/UO/ClassicUO-Dev/`, overlay `Data/`, `settings.json`, `login.cfg`, `uo.cfg` from original install.

---

## Critical Adaptations (from architect review)

| Issue | TazUO | ClassicUO | Resolution |
|-------|-------|-----------|------------|
| ResizableGump chain | `ResizableGump : AnchorableGump` | `ResizableGump : Gump` | Strip `AnchorType`, `IsLocked`, `SetLockStatus` from GridContainer. Don't modify ClassicUO's ResizableGump (risks breaking other gumps). |
| `World.Instance` | Static singleton | Does not exist | Change `OpenOrUpdate(serial, graphic)` to `OpenOrUpdate(World world, serial, graphic)`, pass `world` from PacketHandlers call site |
| `ObjectActionQueue` | TazUO subsystem | Does not exist | Replace `ObjectActionQueue.Instance.Enqueue(QuickLoot)` with `GameActions.PickUp(serial, ...)` or remove single-click-loot for now |
| `Player.Backpack` | Direct property | Does not exist | Replace with `World.Player.FindItemByLayer(Layer.Backpack)` |
| Integration file | `OpenContainer.cs` | Does not exist | Actual file: `PacketHandlers.cs`, method `OpenContainer()` ~line 1423 |
| Missing Profile props | - | - | Add `CorpseSingleClickLoot`, `BackPackLocked`; add `[JsonConverter(typeof(Point2Converter))]` on Point properties |
| `ANCHOR_TYPE.DISABLED` | Enum value = 17 | Does not exist | Remove anchor-related code from GridContainer rather than modifying AnchorableGump |

---

## File Map

### New files to create:
| File | Source | Lines | Purpose |
|------|--------|-------|---------|
| `src/.../Game/UI/Controls/ResizableStaticPic.cs` | Copy from TazUO | 108 | Resizable art control |
| `src/.../Game/Managers/GridContainerSaveData.cs` | Copy from TazUO | 437 | JSON persistence (AOT-safe) |
| `src/.../Game/UI/Gumps/GridContainer.cs` | Copy + adapt | 2399 | Core grid gump (needs edits) |
| `src/.../Game/UI/Gumps/GridHighLight/*.cs` | Copy from TazUO | 7 files | Item highlighting system |

### Files to modify:
| File | Change |
|------|--------|
| `src/.../Game/UI/Gumps/GumpType.cs` | Add `GridContainer = 8787` |
| `src/.../Configuration/Profile.cs` | Add ~27 grid properties |
| `src/.../Network/PacketHandlers.cs` | Add grid vs traditional decision at ~line 1423 |

---

## Task 1: Foundation — GumpType + Profile settings (parallel-safe)

**Files:**
- Modify: `src/ClassicUO.Client/Game/UI/Gumps/GumpType.cs`
- Modify: `src/ClassicUO.Client/Configuration/Profile.cs`

- [ ] **Step 1: Add GridContainer enum to GumpType.cs**

Add before closing brace of enum:
```csharp
GridContainer = 8787,
```

- [ ] **Step 2: Add grid container properties to Profile.cs GlobalProfile class**

Add after the last existing property block. Critical: `Point` properties MUST have `[JsonConverter(typeof(Point2Converter))]`:

```csharp
// Grid Container
public bool UseGridLayoutContainerGumps { get; set; } = false;
public bool GridContainersDefaultToOldStyleView { get; set; } = false;
public int GridContainerSearchMode { get; set; } = 1;
public bool EnableGridContainerAnchor { get; set; } = false;
public byte GridBorderAlpha { get; set; } = 75;
public ushort GridBorderHue { get; set; } = 0;
public byte GridContainersScale { get; set; } = 100;
public bool GridContainerScaleItems { get; set; } = true;
public bool GridEnableContPreview { get; set; } = true;
public int Grid_BorderStyle { get; set; } = 0;
public int Grid_DefaultColumns { get; set; } = 5;
public int Grid_DefaultRows { get; set; } = 5;
public bool Grid_UseContainerHue { get; set; } = false;
public bool Grid_HideBorder { get; set; } = false;
public ushort AltGridContainerBackgroundHue { get; set; } = 0x0000;
public bool DisableTargetingGridContainers { get; set; } = false;
public bool CorpseSingleClickLoot { get; set; } = false;
public bool BackPackLocked { get; set; } = false;
public int GridHighlightSize { get; set; } = 1;
public bool GridHighlightProperties { get; set; } = true;
public bool GridHighlightShowRuleName { get; set; } = true;
public bool GridHighlight_CorpseOnly { get; set; } = false;

[JsonConverter(typeof(Point2Converter))]
public Point BackpackGridPosition { get; set; } = new Point(100, 100);

[JsonConverter(typeof(Point2Converter))]
public Point BackpackGridSize { get; set; } = new Point(300, 300);
```

- [ ] **Step 3: Build to verify**

```bash
cd scripts && bash build-naot.sh 2>&1 | tail -3
```

- [ ] **Step 4: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/GumpType.cs src/ClassicUO.Client/Configuration/Profile.cs
git commit -m "feat: add GridContainer GumpType and Profile settings"
```

---

## Task 2: Port ResizableStaticPic control (parallel-safe)

**Files:**
- Create: `src/ClassicUO.Client/Game/UI/Controls/ResizableStaticPic.cs`

- [ ] **Step 1: Copy from TazUO**

```bash
cp ../TazUO/src/ClassicUO.Client/Game/UI/Controls/ResizableStaticPic.cs \
   src/ClassicUO.Client/Game/UI/Controls/ResizableStaticPic.cs
```

- [ ] **Step 2: Review and remove any TazUO-specific imports**

The file should only use standard ClassicUO namespaces: `ClassicUO.Assets`, `ClassicUO.Renderer`, `ClassicUO.Game.UI.Controls`, `Microsoft.Xna.Framework`. Remove anything else.

- [ ] **Step 3: Build to verify**

```bash
cd scripts && bash build-naot.sh 2>&1 | grep "error CS"
```

- [ ] **Step 4: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Controls/ResizableStaticPic.cs
git commit -m "feat: add ResizableStaticPic control"
```

---

## Task 3: Port GridContainerSaveData (depends: Task 1)

**Files:**
- Create: `src/ClassicUO.Client/Game/Managers/GridContainerSaveData.cs`

- [ ] **Step 1: Copy from TazUO**

```bash
cp ../TazUO/src/ClassicUO.Client/Game/Managers/GridContainerSaveData.cs \
   src/ClassicUO.Client/Game/Managers/GridContainerSaveData.cs
```

- [ ] **Step 2: Review and adapt**

Check:
- `ProfileManager.ProfilePath` exists in ClassicUO (it does)
- `GridContainerSerializerContext` has `[JsonSerializable]` for all types including `Dictionary<uint, GridContainerSlotEntry>` (it does)
- `CUOEnviroment.ExecutablePath` exists in ClassicUO (verify)
- Remove any TazUO-specific logging calls

- [ ] **Step 3: Build to verify**

```bash
cd scripts && bash build-naot.sh 2>&1 | grep "error CS"
```

- [ ] **Step 4: Commit**

```bash
git add src/ClassicUO.Client/Game/Managers/GridContainerSaveData.cs
git commit -m "feat: add GridContainerSaveData persistence"
```

---

## Task 4: Port GridHighLight system (parallel-safe with Task 3)

**Files:**
- Create: `src/ClassicUO.Client/Game/UI/Gumps/GridHighLight/` (7 files)

- [ ] **Step 1: Copy entire directory**

```bash
mkdir -p src/ClassicUO.Client/Game/UI/Gumps/GridHighLight
cp ../TazUO/src/ClassicUO.Client/Game/UI/Gumps/GridHighLight/*.cs \
   src/ClassicUO.Client/Game/UI/Gumps/GridHighLight/
```

- [ ] **Step 2: Fix TazUO-specific references in each file**

For each file, check and fix:
- Replace `Language.Instance.XYZ` with hardcoded English strings
- Verify `ProfileManager.CurrentProfile` grid properties match Task 1
- If any file uses `System.Text.Json` with reflection, convert to source generators
- Remove `TazUOOptions` references if any

- [ ] **Step 3: Build iteratively**

```bash
cd scripts && bash build-naot.sh 2>&1 | grep "error CS" | head -20
```

Fix errors one by one until clean.

- [ ] **Step 4: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/GridHighLight/
git commit -m "feat: add GridHighLight item highlighting system"
```

---

## Task 5: Port GridContainer.cs — the big one (depends: Tasks 1-4)

**Files:**
- Create: `src/ClassicUO.Client/Game/UI/Gumps/GridContainer.cs`

This is 2,399 lines and requires the most adaptation. The following specific changes MUST be made after copying.

- [ ] **Step 1: Copy from TazUO**

```bash
cp ../TazUO/src/ClassicUO.Client/Game/UI/Gumps/GridContainer.cs \
   src/ClassicUO.Client/Game/UI/Gumps/GridContainer.cs
```

- [ ] **Step 2: Fix World.Instance references**

Find the static `OpenOrUpdate` method (~line 1141). Change signature from:
```csharp
public static void OpenOrUpdate(uint serial, ushort graphic)
```
to:
```csharp
public static void OpenOrUpdate(World world, uint serial, ushort graphic)
```

Replace ALL `World.Instance` references inside this method with the `world` parameter.

Also search the entire file for any other `World.Instance` and replace with stored `World` reference (the constructor receives `World world` already).

- [ ] **Step 3: Fix AnchorableGump / anchor references**

Search for and remove or comment out:
- Any line setting `AnchorType = ANCHOR_TYPE.DISABLED` or `ANCHOR_TYPE.NONE` — remove the line
- Any line referencing `SetLockStatus` — remove
- Any line checking `IsLocked` that comes from AnchorableGump — replace with `false` or remove the block
- Any `ANCHOR_TYPE` enum reference — remove

Search patterns: `AnchorType`, `ANCHOR_TYPE`, `SetLockStatus`, `IsLocked` (verify each — some `IsLocked` may refer to item lock, not gump lock)

- [ ] **Step 4: Fix Player.Backpack references**

Replace all `World.Player.Backpack` with `World.Player.FindItemByLayer(Layer.Backpack)`.

Also add null checks since `FindItemByLayer` can return null:
```csharp
var backpack = World.Player?.FindItemByLayer(Layer.Backpack);
if (backpack == null) return;
```

- [ ] **Step 5: Fix ObjectActionQueue references**

Search for `ObjectActionQueue`. Replace single-click-loot enqueue calls with direct pickup:
```csharp
// TazUO: ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.QuickLoot(_item), ActionPriority.MoveItem);
// ClassicUO: GameActions.PickUp(world, _item.Serial, 0, 0, _item.Amount);
```

If `ObjectActionQueueItem` or `ActionPriority` are referenced elsewhere, stub them out.

- [ ] **Step 6: Fix Language/localization references**

Search for `Language.Instance`. Replace with hardcoded English strings:
```csharp
// TazUO: Language.Instance.GridContainers
// ClassicUO: "Grid Containers"
```

- [ ] **Step 7: Fix BackPackLocked reference**

Line ~273: `IsLocked = IsPlayerBackpack && ProfileManager.CurrentProfile.BackPackLocked;`

Since we stripped gump-level IsLocked (Step 3), simplify this to just skip the line or use a local bool that the grid uses for its own lock icon logic.

- [ ] **Step 8: Build iteratively**

```bash
cd scripts && bash build-naot.sh 2>&1 | grep "error CS" | head -20
```

Expect 10-30 errors on first try. Fix them in batches:
1. Missing types/methods → stub or adapt
2. Wrong signatures → match ClassicUO's API
3. Missing properties → add to Profile.cs if needed

Keep building until 0 errors.

- [ ] **Step 9: Commit**

```bash
git add src/ClassicUO.Client/Game/UI/Gumps/GridContainer.cs
git commit -m "feat: add GridContainer gump (ported from TazUO)"
```

---

## Task 6: Wire into PacketHandlers.cs (depends: Task 5)

**Files:**
- Modify: `src/ClassicUO.Client/Network/PacketHandlers.cs` (~line 1423, method `OpenContainer`)

**IMPORTANT:** The file is `PacketHandlers.cs`, NOT `OpenContainer.cs`.

- [ ] **Step 1: Find the ContainerGump creation block**

Search for `new ContainerGump` in `PacketHandlers.cs`. It should be around line 1527. Read the surrounding context (lines 1400-1540) to understand the flow.

- [ ] **Step 2: Add grid container check before ContainerGump creation**

Insert before the existing `ContainerGump` creation, after the GridLoot check:

```csharp
if (ProfileManager.CurrentProfile.UseGridLayoutContainerGumps && graphic != 0x091A)
{
    GridContainer.OpenOrUpdate(world, serial, graphic);
    return;
}
```

The `world` parameter is already available in the `OpenContainer` method signature.

- [ ] **Step 3: Build to verify**

```bash
cd scripts && bash build-naot.sh 2>&1 | grep "error CS"
```

- [ ] **Step 4: Commit**

```bash
git add src/ClassicUO.Client/Network/PacketHandlers.cs
git commit -m "feat: wire grid container into container open handler"
```

---

## Task 7: Build, deploy, smoke test

- [ ] **Step 1: Full clean build**

```bash
export PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/Installer"
cd scripts && bash build-naot.sh 2>&1 | tail -5
```

- [ ] **Step 2: Deploy**

```bash
DIST="bin/dist"
DEST="C:/Users/EdoardoCiccarelli/Desktop/UO/ClassicUO-Dev"
ORIG="C:/Users/EdoardoCiccarelli/Desktop/UO/ClassicUOLauncher-win-x64-release/ClassicUO"
rm -rf "$DEST" && cp -r "$DIST" "$DEST"
cp -r "$ORIG/Data" "$DEST/Data"
cp "$ORIG/settings.json" "$ORIG/settings_ex.json" "$ORIG/login.cfg" "$ORIG/uo.cfg" "$DEST/"
```

- [ ] **Step 3: Smoke test**

1. Launch RazorEnhanced → ClassicUO-Dev
2. Log in
3. Open CUO options → enable "Use Grid Layout Container Gumps" (or edit settings.json: `"use_grid_layout_container_gumps": true`)
4. Open backpack → should show grid
5. Open a chest → should show grid
6. Open a corpse → test grid loot
7. Test search, sort, resize
8. Verify traditional view still works when setting is off

- [ ] **Step 4: Push to branch**

```bash
git push origin grid-container
```

---

## Notes

- **Default OFF:** `UseGridLayoutContainerGumps` defaults to `false`
- **Graphic 0x091A excluded:** Always uses traditional view
- **Anchor/lock features stripped:** TazUO's gump anchor and lock system depends on `AnchorableGump` in the inheritance chain. ClassicUO's `ResizableGump` doesn't extend `AnchorableGump`. Rather than risk breaking `ResizableJournal` and other gumps, we strip anchor features. Can be added back later if needed.
- **Single-click loot simplified:** TazUO uses `ObjectActionQueue` for queued item actions. We replace with direct `GameActions` calls since the queue system is a large TazUO-specific subsystem.
- **AOT safe:** All JSON serialization uses source generators. No reflection-based serialization.
