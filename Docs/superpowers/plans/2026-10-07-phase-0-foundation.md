# Greyspace Phase 0 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the AI eyes and hands in Unity (official Unity CLI), make the game self-testable (input simulation, debug commands, autopilot bot), clean up the oversized scene file, pick the best local model by measurement, and write the design documents every later phase is judged against.

**Architecture:** Official Unity CLI + MCP drives the open Editor (play, screenshot, console, tests, `eval`). Inside the game, a thin `GameInput` wrapper merges real input with a simulated layer, `GreyspaceDebug` exposes static commands the CLI calls through `eval`, and a `ComboBot` plays missions unattended. `GreyspaceScene.cs` (589 lines) is split into focused plain-C# parts owned by a slim scene controller. A stdlib-only Python harness runs a local-model bake-off under Opus review.

**Tech Stack:** Unity 6000.4.11f1, URP 17.0.3, legacy Input Manager (Active Input Handling = Both), Unity Test Framework (EditMode + PlayMode), Unity CLI + `com.unity.pipeline` (beta), Ollama (Mac + 419c), Python 3 stdlib.

**Source spec:** `Docs/GREYSPACE-CHARTER.md` §4 (See-Play-Fix), §4.4 (models), §5 (art), §6 Phase 0, pillars v2 (§3, working version).

---

## Ground rules for every task

- **Hard project rules still apply:** no namespaces; no `Shader.Find()` (shader-steal: `new Material(existingRenderer.sharedMaterial)`, set both `_BaseColor` and `.color`); physics-free movement (`transform.position +=`); all geometry via `GameObject.CreatePrimitive()`; `WaitForSecondsRealtime` for anything that must survive `Time.timeScale = 0`.
- **One rule changes in this phase:** "`Input.GetKey(KeyCode.*)` only" becomes "**`GameInput.GetKey(KeyCode.*)` / `GameInput.GetKeyDown(KeyCode.*)` only**" (Task B3). Update `Docs/COORDINATION.md` in the same commit.
- **Commit before every AI edit to Unity scenes/assets** (Unity CLI changes are not all undoable).
- Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Work from repo root `/Users/apurv2793/Manifest main/manifest-greyspace-clean` unless stated.

## Workstreams and order

```
Task 0 (housekeeping)
A  Bridge ............ A1 → A2                      (needs Apurv at the Mac once, for installs)
B  Tests + input ..... B1 → B2 → B3                 (B1 needs A1 for headless test runs; can use Editor Test Runner instead)
C  Scene split ....... C1 → C2 → C3 → C4 → C5 → C6  (after B3)
D  Debug + bot ....... D1 → D2 → D3                 (after C6)
E  Local bake-off .... E1 → E2 → E3                 (independent; needs A1 for compile checks)
F  Design docs ....... F1..F5                       (independent; F3/F4 need Apurv's input)
X  Phase 0 exit check  (after everything)
```

## File map

| File | New/Modify | Responsibility |
|---|---|---|
| `Docs/TOOLING.md` | Create | Installed versions, discovered Unity CLI command list, how to run the loop |
| `Tools/see/see.sh` | Create | One-shot See loop: play → frames advance → screenshot → console → stop |
| `Assets/Scripts/Greyspace.asmdef` | Create | Runtime assembly so tests can reference game code |
| `Assets/Tests/EditMode/Greyspace.Tests.EditMode.asmdef` | Create | EditMode test assembly |
| `Assets/Tests/PlayMode/Greyspace.Tests.PlayMode.asmdef` | Create | PlayMode test assembly |
| `Assets/Scripts/Input/GameInputState.cs` | Create | Pure simulated-input state machine (unit-testable) |
| `Assets/Scripts/Input/GameInput.cs` | Create | Static facade: real input OR simulated |
| `Assets/Scripts/World/WorldPrims.cs` | Create | Static primitive/material/UI-rect helpers (moved from scene) |
| `Assets/Scripts/World/MissionCatalog.cs` | Create | Default mission definitions (moved from `SpawnPortals`) |
| `Assets/Scripts/World/HudController.cs` | Create | HUD build + XP/level/toast updates |
| `Assets/Scripts/World/HubBuilder.cs` | Create | Hub floor, decor, portals |
| `Assets/Scripts/World/MissionRunner.cs` | Create | Arena, wave loop, enemy spawning, mission state |
| `Assets/Scripts/GreyspaceScene.cs` | Modify | Shrinks to mode switching + wiring (~150 lines) |
| `Assets/Scripts/Debug/GreyspaceDebug.cs` | Create | Static commands callable from Unity CLI `eval` |
| `Assets/Scripts/Debug/GreyspaceSnapshot.cs` | Create | Serializable state snapshot |
| `Assets/Scripts/Debug/ComboBot.cs` | Create | Autopilot that plays missions via `GameInput.Sim` |
| `Tools/bakeoff/bakeoff.py` | Create | Local-model bake-off harness (stdlib only) |
| `Tools/bakeoff/briefs/*.md` | Create | Three real task briefs |
| `Docs/ART-BIBLE.md`, `Docs/STORY-BIBLE.md`, `Docs/FEEL-MATRIX.md`, `Docs/CRITIC-RUBRIC.md` | Create | Design references |
| `DESIGN.md`, `PROGRESS.md`, `NOTES.md` | Create | Cross-session AI memory |
| `Docs/reference/SOURCES.md` | Create | Reference-frame list (images gitignored) |

---

## Task 0: Housekeeping

**Files:** `Assets/Scripts/GameConfig.cs` (+ `.meta`), `Assets/Scripts/NPCStub.cs.meta`, `Assets/Scripts/ZoneBounds.cs.meta`

- [ ] **Step 1: Commit the stray files from the NIM speed test**

```bash
git status --short
git add Assets/Scripts/GameConfig.cs Assets/Scripts/GameConfig.cs.meta Assets/Scripts/NPCStub.cs.meta Assets/Scripts/ZoneBounds.cs.meta
git commit -m "chore: commit GameConfig registry (Phase 9A test output) and missing .meta files

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Expected: working tree clean except `Docs/GREYSPACE-CHARTER.md` edits, which get committed with Task F1.

---

## Workstream A — The bridge (AI can play and look)

### Task A1: Install the official Unity CLI, plugin and MCP

**Files:** Create `Docs/TOOLING.md`

**Needs Apurv:** the CLI installer is a remote script piped to a shell, and Unity must be open with the project. Ask before running Step 1.

- [ ] **Step 1: Install the CLI (ask Apurv first)**

```bash
curl -fsSL https://unity.com/install.sh -o /tmp/unity-install.sh
less /tmp/unity-install.sh          # read it before running
bash /tmp/unity-install.sh
unity --version
```

Expected: a version string like `0.1.0-beta.N`. (Unity Hub may already have installed it — run `unity --version` first.)

- [ ] **Step 2: Path-with-space check**

The project path contains a space (`Manifest main`). Third-party bridges reject that; check the official CLI.

```bash
ln -sfn "/Users/apurv2793/Manifest main/manifest-greyspace-clean" ~/greyspace
cd ~/greyspace && unity --help | head -40
```

From here on, run all `unity` commands from `~/greyspace`. If a command later fails with a path error, this symlink is the fix.

- [ ] **Step 3: Install the Claude Code plugin**

In Claude Code:
```
/plugin marketplace add Unity-Technologies/unity-agent-plugin
/plugin install unity@unity-agent-plugin
```
Expected: plugin listed by `/plugin`; skills `unity-cli`, `urp-postprocessing` available.

- [ ] **Step 4: Install the pipeline package and register MCP (Unity Editor open on this project)**

```bash
cd ~/greyspace
unity pipeline install
unity mcp configure claude-code
```
Expected: `Packages/manifest.json` gains `com.unity.pipeline`; `claude mcp list` shows the Unity server.

- [ ] **Step 5: Discover the live command list (commands are beta and get renamed)**

```bash
mkdir -p Docs/tooling
unity command --format json > Docs/tooling/unity-commands.json
python3 -c "import json;d=json.load(open('Docs/tooling/unity-commands.json'));print(sorted(c.get('name',c) if isinstance(c,dict) else c for c in (d if isinstance(d,list) else d.get('commands',[]))))"
```
Expected: list includes `editor_play`, `editor_stop`, `set_autotick`, `wait_for`, `capture_game_view`, `console`, `eval`, `run_tests`, `get_scene_hierarchy`. Note any renamed command.

- [ ] **Step 6: Record how `eval` takes its code argument**

```bash
unity command eval --help
```
Write the exact flag (e.g. `--code`) into `Docs/TOOLING.md` under "eval".

- [ ] **Step 7: Write `Docs/TOOLING.md`**

```markdown
# Tooling

## Versions (fill from command output on install day)
- Unity Editor: 6000.4.11f1
- Unity CLI: `<output of unity --version>`
- com.unity.pipeline: `<version from Packages/manifest.json>`
- unity-agent-plugin: `<version from /plugin>`

## Rules
- Run `unity` commands from `~/greyspace` (symlink — the real path has a space).
- Commands are beta: re-run `unity command --format json > Docs/tooling/unity-commands.json` after any CLI update and diff it.
- Commit before AI edits to scenes/assets.

## eval
Flag for code argument: `<from Step 6>`
Example: `unity command eval <flag> 'GreyspaceDebug.State()'`

## The See loop
`Tools/see/see.sh <label> [seconds]` — see Task A2.
```
Replace each `<…>` with the real value from that day's output before committing.

- [ ] **Step 8: Commit**

```bash
git add Docs/TOOLING.md Docs/tooling/unity-commands.json Packages/manifest.json Packages/packages-lock.json
git commit -m "tooling: install official Unity CLI + pipeline + MCP, record command list

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task A2: The See loop script

**Files:** Create `Tools/see/see.sh`

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# See loop: play the game, prove frames advance, screenshot, read errors, stop.
# Usage: Tools/see/see.sh <label> [seconds=5] [eval-before-capture]
set -euo pipefail
cd ~/greyspace
LABEL="${1:?label required}"; SECS="${2:-5}"; PRE_EVAL="${3:-}"
EVAL_FLAG="$(grep -m1 '^Flag for code argument:' Docs/TOOLING.md | sed 's/.*: `\(.*\)`/\1/')"
OUT="Docs/evidence/$(date +%Y%m%d-%H%M%S)-${LABEL}"
mkdir -p "$OUT"

need() { python3 - "$1" <<'PY' || { echo "MISSING CLI COMMAND: $1 (re-run discovery, see Docs/TOOLING.md)"; exit 3; }
import json,sys
d=json.load(open('Docs/tooling/unity-commands.json'))
items=d if isinstance(d,list) else d.get('commands',[])
names={(c.get('name') if isinstance(c,dict) else c) for c in items}
sys.exit(0 if sys.argv[1] in names else 1)
PY
}
for c in set_autotick clear_console editor_play wait_for eval capture_game_view console editor_stop; do need "$c"; done

unity command set_autotick
unity command clear_console
unity command editor_play
F0=$(unity command eval $EVAL_FLAG 'UnityEngine.Time.frameCount')
sleep 2
F1=$(unity command eval $EVAL_FLAG 'UnityEngine.Time.frameCount')
[ "$F1" != "$F0" ] || { echo "FRAMES NOT ADVANCING ($F0 -> $F1)"; unity command editor_stop; exit 4; }
[ -n "$PRE_EVAL" ] && unity command eval $EVAL_FLAG "$PRE_EVAL" > "$OUT/pre-eval.txt"
sleep "$SECS"
unity command capture_game_view --source screen --save_path "$PWD/$OUT/frame.png"
unity command eval $EVAL_FLAG 'GreyspaceDebug.State()' > "$OUT/state.json" || echo '{}' > "$OUT/state.json"
unity command console --tail 50 --level error > "$OUT/errors.txt" || true
unity command editor_stop
ERRS=$(grep -c . "$OUT/errors.txt" || true)
echo "{\"label\":\"$LABEL\",\"frames\":[\"$F0\",\"$F1\"],\"errorLines\":$ERRS,\"dir\":\"$OUT\"}" | tee "$OUT/summary.json"
```

Note: `GreyspaceDebug.State()` does not exist until Task D1; until then `state.json` is `{}`.

- [ ] **Step 2: Make executable and run it**

```bash
chmod +x Tools/see/see.sh
Tools/see/see.sh baseline-hub 3
```
Expected: `Docs/evidence/<stamp>-baseline-hub/frame.png` shows the hub; `summary.json` has two different frame numbers; `errorLines` = 0. Open `frame.png` and look at it.

If a flag differs in the installed CLI (e.g. `capture_game_view` arguments), fix the script to match `unity command capture_game_view --help` and note it in `Docs/TOOLING.md`.

- [ ] **Step 3: Commit (evidence images are kept — they are the project's visual history)**

```bash
git add Tools/see/see.sh Docs/evidence/
git commit -m "tooling: See loop script — play, verify frames, screenshot, errors, stop

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Workstream B — Test harness and input simulation

### Task B1: Test framework and assemblies

**Files:** Create `Assets/Scripts/Greyspace.asmdef`, `Assets/Tests/EditMode/Greyspace.Tests.EditMode.asmdef`, `Assets/Tests/PlayMode/Greyspace.Tests.PlayMode.asmdef`, `Assets/Tests/EditMode/SmokeTests.cs`

- [ ] **Step 1: Add the Unity Test Framework package**

Unity: Window → Package Manager → Unity Registry → "Test Framework" → Install (or `unity command` package add if listed in `Docs/tooling/unity-commands.json`). Confirm `"com.unity.test-framework"` appears in `Packages/manifest.json`.

- [ ] **Step 2: Runtime assembly**

`Assets/Scripts/Greyspace.asmdef`:
```json
{
    "name": "Greyspace",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```
uGUI (`UnityEngine.UI`) lives in the `UnityEngine.UI` assembly — add it: `"references": ["UnityEngine.UI"]`. Wait for recompile; console must show zero errors.

- [ ] **Step 3: Test assemblies**

`Assets/Tests/EditMode/Greyspace.Tests.EditMode.asmdef`:
```json
{
    "name": "Greyspace.Tests.EditMode",
    "references": ["Greyspace", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false
}
```

`Assets/Tests/PlayMode/Greyspace.Tests.PlayMode.asmdef`:
```json
{
    "name": "Greyspace.Tests.PlayMode",
    "references": ["Greyspace", "UnityEngine.TestRunner"],
    "includePlatforms": [],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false
}
```

- [ ] **Step 4: Smoke test**

`Assets/Tests/EditMode/SmokeTests.cs`:
```csharp
using NUnit.Framework;

public class SmokeTests
{
    [Test]
    public void SceneStateXPCurveStartsAt100()
    {
        SceneState.level = 1;
        Assert.AreEqual(100, SceneState.XPToNextLevel);
    }
}
```

- [ ] **Step 5: Run**

```bash
cd ~/greyspace && unity test --mode EditMode
```
Expected: 1 passed, exit code 0. (Fallback: Window → General → Test Runner → Run All.)

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Greyspace.asmdef* Assets/Tests Packages/manifest.json Packages/packages-lock.json
git commit -m "test: add Unity Test Framework, Greyspace runtime assembly, EditMode/PlayMode test assemblies

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task B2: `GameInputState` — simulated input (TDD)

**Files:** Create `Assets/Scripts/Input/GameInputState.cs`, `Assets/Tests/EditMode/GameInputStateTests.cs`

- [ ] **Step 1: Write failing tests**

`Assets/Tests/EditMode/GameInputStateTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class GameInputStateTests
{
    [Test]
    public void TapIsDownForExactlyOneFrame()
    {
        var s = new GameInputState();
        s.Tap(KeyCode.J);
        s.Tick();
        Assert.IsTrue(s.GetKeyDown(KeyCode.J));
        Assert.IsTrue(s.GetKey(KeyCode.J));
        s.Tick();
        Assert.IsFalse(s.GetKeyDown(KeyCode.J));
        Assert.IsFalse(s.GetKey(KeyCode.J));
    }

    [Test]
    public void TapHeldForNFrames()
    {
        var s = new GameInputState();
        s.Tap(KeyCode.W, 3);
        for (int i = 0; i < 3; i++) { s.Tick(); Assert.IsTrue(s.GetKey(KeyCode.W), "frame " + i); }
        s.Tick();
        Assert.IsFalse(s.GetKey(KeyCode.W));
    }

    [Test]
    public void PressHoldsUntilRelease_DownOnlyFirstFrame()
    {
        var s = new GameInputState();
        s.Press(KeyCode.D);
        s.Tick();
        Assert.IsTrue(s.GetKeyDown(KeyCode.D));
        s.Tick();
        Assert.IsFalse(s.GetKeyDown(KeyCode.D));
        Assert.IsTrue(s.GetKey(KeyCode.D));
        s.Release(KeyCode.D);
        Assert.IsFalse(s.GetKey(KeyCode.D));
    }

    [Test]
    public void NothingIsDownBeforeFirstTick()
    {
        var s = new GameInputState();
        s.Tap(KeyCode.E);
        Assert.IsFalse(s.GetKeyDown(KeyCode.E));
    }

    [Test]
    public void ReleaseAllClearsEverything()
    {
        var s = new GameInputState();
        s.Press(KeyCode.A); s.Tap(KeyCode.K, 5);
        s.MouseScreenOverride = new Vector3(10, 20, 0);
        s.Tick();
        s.ReleaseAll();
        s.Tick();
        Assert.IsFalse(s.GetKey(KeyCode.A));
        Assert.IsFalse(s.GetKey(KeyCode.K));
        Assert.IsNull(s.MouseScreenOverride);
    }
}
```

- [ ] **Step 2: Run — expect compile failure**

```bash
cd ~/greyspace && unity test --mode EditMode
```
Expected: FAIL — `GameInputState` not found.

- [ ] **Step 3: Implement**

`Assets/Scripts/Input/GameInputState.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

// Simulated input layer. Pure state — never reads UnityEngine.Input — so it is unit-testable.
// Call Tick() once per frame before gameplay reads (GameInput does this lazily).
public class GameInputState
{
    readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
    readonly HashSet<KeyCode> pendingDown = new HashSet<KeyCode>();
    readonly HashSet<KeyCode> downThisFrame = new HashSet<KeyCode>();
    readonly Dictionary<KeyCode, int> tapFramesLeft = new Dictionary<KeyCode, int>();
    readonly List<KeyCode> scratch = new List<KeyCode>();

    public Vector3? MouseScreenOverride;

    // Hold until Release().
    public void Press(KeyCode k)
    {
        if (!held.Contains(k)) pendingDown.Add(k);
        held.Add(k);
        tapFramesLeft.Remove(k);
    }

    // Hold for `frames` frames, then auto-release.
    public void Tap(KeyCode k, int frames = 1)
    {
        pendingDown.Add(k);
        held.Add(k);
        tapFramesLeft[k] = Mathf.Max(1, frames);
    }

    public void Release(KeyCode k)
    {
        held.Remove(k);
        pendingDown.Remove(k);
        tapFramesLeft.Remove(k);
    }

    public void ReleaseAll()
    {
        held.Clear(); pendingDown.Clear(); downThisFrame.Clear(); tapFramesLeft.Clear();
        MouseScreenOverride = null;
    }

    public void Tick()
    {
        downThisFrame.Clear();

        scratch.Clear();
        foreach (var kv in tapFramesLeft) if (kv.Value <= 0) scratch.Add(kv.Key);
        foreach (var k in scratch) { tapFramesLeft.Remove(k); held.Remove(k); }

        foreach (var k in pendingDown) downThisFrame.Add(k);
        pendingDown.Clear();

        scratch.Clear();
        scratch.AddRange(tapFramesLeft.Keys);
        foreach (var k in scratch) tapFramesLeft[k]--;
    }

    public bool GetKey(KeyCode k)     => held.Contains(k);
    public bool GetKeyDown(KeyCode k) => downThisFrame.Contains(k);
}
```

- [ ] **Step 4: Run — expect pass**

```bash
cd ~/greyspace && unity test --mode EditMode
```
Expected: 6 passed.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Input Assets/Tests/EditMode/GameInputStateTests.cs*
git commit -m "feat(input): GameInputState — testable simulated key layer (tap/press/release, per-frame down)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task B3: `GameInput` facade and replacing all input reads

**Files:** Create `Assets/Scripts/Input/GameInput.cs`; Modify `GunCharacter.cs:188-191,204,218,266,271-274`, `MeleeAttack.cs:28-29`, `NPCStub.cs:42`, `WeaponPickup.cs:56`, `GreyspaceScene.cs` (5 reads in `UpdateHub`/`UpdateMission`), `Docs/COORDINATION.md`

- [ ] **Step 1: Write the facade**

`Assets/Scripts/Input/GameInput.cs`:
```csharp
using UnityEngine;

// All gameplay input goes through here: real keyboard/mouse OR GameInput.Sim (bots, AI tests).
// Mouse buttons use KeyCode.Mouse0 / KeyCode.Mouse1.
public static class GameInput
{
    public static readonly GameInputState Sim = new GameInputState();
    static int lastTickFrame = -1;

    static void EnsureTicked()
    {
        if (Time.frameCount == lastTickFrame) return;
        lastTickFrame = Time.frameCount;
        Sim.Tick();
    }

    public static bool GetKey(KeyCode k)     { EnsureTicked(); return Input.GetKey(k) || Sim.GetKey(k); }
    public static bool GetKeyDown(KeyCode k) { EnsureTicked(); return Input.GetKeyDown(k) || Sim.GetKeyDown(k); }

    public static Vector3 MousePosition
    {
        get { EnsureTicked(); return Sim.MouseScreenOverride ?? Input.mousePosition; }
    }
}
```

- [ ] **Step 2: Replace every read (mechanical)**

```bash
cd "/Users/apurv2793/Manifest main/manifest-greyspace-clean/Assets/Scripts"
for f in GunCharacter.cs MeleeAttack.cs NPCStub.cs WeaponPickup.cs GreyspaceScene.cs; do
  sed -i '' \
    -e 's/Input\.GetMouseButtonDown(0)/GameInput.GetKeyDown(KeyCode.Mouse0)/g' \
    -e 's/Input\.GetMouseButtonDown(1)/GameInput.GetKeyDown(KeyCode.Mouse1)/g' \
    -e 's/Input\.GetKeyDown(/GameInput.GetKeyDown(/g' \
    -e 's/Input\.GetKey(/GameInput.GetKey(/g' \
    -e 's/Input\.mousePosition/GameInput.MousePosition/g' "$f"
done
grep -n "Input\.\(Get\|mouse\)" *.cs | grep -v "GameInput\." || echo "no raw Input reads left"
```
Expected: `no raw Input reads left`. (`GameInput.cs` itself is in `Input/`, not touched.)

- [ ] **Step 3: Update the hard rule**

In `Docs/COORDINATION.md`, replace the line `- Input: Input.GetKey(KeyCode.*) only` with:
```
- Input: GameInput.GetKey / GameInput.GetKeyDown (KeyCode.*) and GameInput.MousePosition only — never UnityEngine.Input directly (bots and AI tests drive GameInput.Sim)
```

- [ ] **Step 4: Verify compile + behaviour**

```bash
cd ~/greyspace && unity test --mode EditMode && Tools/see/see.sh input-facade 3
```
Expected: tests pass; `errorLines` 0; then Apurv (or the bot later) confirms WASD/J/K/Tab/E still work by hand once.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts Docs/COORDINATION.md
git commit -m "feat(input): route all gameplay input through GameInput (real + simulated)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Workstream C — Split `GreyspaceScene.cs` (behaviour must not change)

Shape after the split: `GreyspaceScene` (MonoBehaviour) owns a shared `SceneContext` and four plain-C# parts. Coroutines stay on the MonoBehaviour; parts return `IEnumerator`.

### Task C1: Behaviour baseline before touching anything

**Files:** Create `Docs/evidence/refactor-baseline/` (via script)

- [ ] **Step 1: Capture hub, mission start, mission mid-wave**

```bash
Tools/see/see.sh refactor-baseline-hub 3
```
Then by hand (bot doesn't exist yet): play into The Proving Ground, note what you see at wave 1. Save a screenshot via the CLI:
```bash
cd ~/greyspace && unity command editor_play && sleep 2
# walk to portal + press E manually in the Game view, wait for wave 1
unity command capture_game_view --source screen --save_path "$PWD/Docs/evidence/refactor-baseline-mission.png"
unity command editor_stop
```

- [ ] **Step 2: Write the manual check list into `Docs/evidence/refactor-baseline/CHECKS.md`**

```markdown
# Refactor behaviour checks (must all still hold after C6)
1. Hub: sandstone courtyard, obelisk, 2 portals (Proving Ground open, Inner Sanctum locked)
2. Walk to Proving Ground portal: prompt "[E] Enter The Proving Ground (3 waves · +75 XP)"
3. Locked portal prompt shows "Clear The Proving Ground first"
4. E enters mission: grey arena, Bow pickup orb at (4,0,4), "Wave 1 / 3"
5. Killing enemies raises XP bar; level-up flashes gold + burst
6. Clearing 3 waves: "MISSION COMPLETE +75 XP"; E → hub, R → replay
7. Dying: "YOU DIED", R retry, E hub
8. Controls panel top-right; HP bar lerps
```

- [ ] **Step 3: Commit**

```bash
git add Docs/evidence
git commit -m "test: behaviour baseline before GreyspaceScene split

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task C2: Extract `WorldPrims` (static helpers) and `SceneContext`

**Files:** Create `Assets/Scripts/World/WorldPrims.cs`, `Assets/Scripts/World/SceneContext.cs`; Modify `GreyspaceScene.cs:531-588`

- [ ] **Step 1: Create `SceneContext`** — the shared state every part reads/writes (moved from `GreyspaceScene` fields lines 12-25).

`Assets/Scripts/World/SceneContext.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shared state for the scene parts (hub, mission, HUD). Owned by GreyspaceScene.
public class SceneContext
{
    public MonoBehaviour host;                 // runs coroutines
    public GameObject playerGO, worldRoot, hudRoot;
    public GunCharacter player;
    public Image healthFill, xpFill;
    public Text waveText, statusText, promptText, xpText, levelText;

    // Hub
    public readonly List<MissionPortal> portals = new List<MissionPortal>();

    // Mission
    public int wave, totalWaves;
    public bool playerDead, missionComplete;
    public MissionDefinition currentMission;

    public void ClearWorld()
    {
        if (worldRoot) Object.Destroy(worldRoot);
        worldRoot = null;
    }
}
```

- [ ] **Step 2: Create `WorldPrims`** — move `Tile`, `Block`, `Prim`, `Mat`, `Rect` verbatim from `GreyspaceScene.cs` lines 543-588, made static and taking the parent explicitly:

`Assets/Scripts/World/WorldPrims.cs`:
```csharp
using UnityEngine;

// Primitive builders shared by hub/mission/HUD. Shader-steal pattern for URP materials.
public static class WorldPrims
{
    public static void Tile(Transform parent, Vector3 pos, Color c)
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        t.transform.SetParent(parent);
        t.transform.position = pos;
        t.transform.localScale = new Vector3(0.98f, 0.07f, 0.98f);
        Object.Destroy(t.GetComponent<Collider>());
        t.GetComponent<Renderer>().material = Mat(c);
    }

    public static GameObject Block(Transform parent, Vector3 pos, Vector3 scale, Color c)
        => Prim(parent, PrimitiveType.Cube, pos, scale, c);

    public static GameObject Prim(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(parent);
        g.transform.position = pos; g.transform.localScale = scale;
        Object.Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().material = Mat(c);
        return g;
    }

    public static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        Object.DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    public static GameObject Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
```

- [ ] **Step 3: Repoint `GreyspaceScene`** — delete its `Tile/Block/Prim/Mat/Rect/ClearWorld` methods; replace every call:
  - `Tile(p, c)` → `WorldPrims.Tile(ctx.worldRoot.transform, p, c)`
  - `Block(p, s, c)` → `WorldPrims.Block(ctx.worldRoot.transform, p, s, c)`
  - `Prim(t, p, s, c)` → `WorldPrims.Prim(ctx.worldRoot.transform, t, p, s, c)`
  - `Mat(c)` → `WorldPrims.Mat(c)`; `Rect(parent, n)` → `WorldPrims.Rect(parent, n)`; `ClearWorld()` → `ctx.ClearWorld()`
  - Replace the field block (lines 12-25) with `SceneContext ctx = new SceneContext();` and prefix every former field use with `ctx.` (e.g. `playerGO` → `ctx.playerGO`, `wave` → `ctx.wave`). In `Start()` add `ctx.host = this;` as the first line.

Keep `enum Mode`, `mode`, and `INTERACT_RADIUS` on `GreyspaceScene`.

- [ ] **Step 4: Verify**

```bash
cd ~/greyspace && unity test --mode EditMode && Tools/see/see.sh split-c2 3
```
Expected: tests pass, `errorLines` 0, `frame.png` matches the C1 hub screenshot by eye.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts
git commit -m "refactor: extract SceneContext + WorldPrims from GreyspaceScene (no behaviour change)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task C3: Extract `HudController`

**Files:** Create `Assets/Scripts/World/HudController.cs`; Modify `GreyspaceScene.cs` (`BuildHUD` 364-481, `ShowRewardToast` 482-496, `UpdateXP` 497-504, `LevelUpFlash` 38-49, `OnEnemyKilled` 31-36)

- [ ] **Step 1: Create the class and move code**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Builds and updates the HUD. Moved verbatim from GreyspaceScene.
public class HudController
{
    readonly SceneContext ctx;
    public HudController(SceneContext ctx) { this.ctx = ctx; }

    public void Build()            { /* body of GreyspaceScene.BuildHUD, fields prefixed ctx., Rect → WorldPrims.Rect */ }
    public IEnumerator RewardToast() { /* body of ShowRewardToast */ yield break; }
    public void UpdateXP()         { /* body of UpdateXP */ }
    public IEnumerator LevelUpFlash() { /* body of LevelUpFlash */ yield break; }
}
```
Move each method body **verbatim** into the matching method above (the comments mark where; delete the placeholder `yield break;` lines once the real bodies are in — they contain their own `yield` statements). Every field use becomes `ctx.<field>`.

In `GreyspaceScene`: add `HudController hud;`, construct in `Start()` after `ctx.host = this;` as `hud = new HudController(ctx);`, replace `BuildHUD()` → `hud.Build()`, `UpdateXP()` → `hud.UpdateXP()`, `StartCoroutine(ShowRewardToast())` → `StartCoroutine(hud.RewardToast())`, and `OnEnemyKilled` body becomes:
```csharp
bool leveledUp = SceneState.AddXP(xpValue);
hud.UpdateXP();
if (leveledUp) StartCoroutine(hud.LevelUpFlash());
```

- [ ] **Step 2: Verify** — same command as C2 with label `split-c3`. Expected: identical HUD in `frame.png`, 0 errors.

- [ ] **Step 3: Commit** — `git commit -am "refactor: extract HudController from GreyspaceScene (no behaviour change)"` (+ Co-Authored-By line; `git add Assets/Scripts/World/HudController.cs*` first).

### Task C4: Extract `MissionCatalog` + `HubBuilder`

**Files:** Create `Assets/Scripts/World/MissionCatalog.cs`, `Assets/Scripts/World/HubBuilder.cs`; Modify `GreyspaceScene.cs` (`EnterHub` 61-93, `BuildHubFloor` 94-111, `BuildHubDecor` 112-133, `SpawnPortals` 134-154, `AddPortal` 155-165, `UpdateHub` 319-347)

- [ ] **Step 1: `MissionCatalog`** (data moved out of `SpawnPortals` so the debug commands can find missions by id)

```csharp
using System.Collections.Generic;
using UnityEngine;

// Default missions. Debug commands and the hub both read from here.
public static class MissionCatalog
{
    static List<MissionDefinition> cache;

    public static List<MissionDefinition> All()
    {
        if (cache != null) return cache;
        var m1 = ScriptableObject.CreateInstance<MissionDefinition>();
        m1.missionId = "proving_ground"; m1.displayName = "The Proving Ground";
        m1.waves = 3; m1.rewardXP = 75; m1.description = "Survive three waves of enemies.";

        var m2 = ScriptableObject.CreateInstance<MissionDefinition>();
        m2.missionId = "inner_sanctum"; m2.displayName = "The Inner Sanctum";
        m2.waves = 5; m2.rewardXP = 150;
        m2.requiredFlag = "proving_ground_complete"; m2.lockedReason = "Clear The Proving Ground first";

        cache = new List<MissionDefinition> { m1, m2 };
        return cache;
    }

    public static MissionDefinition Find(string id) => All().Find(m => m.missionId == id);
}
```

- [ ] **Step 2: `HubBuilder`** — `Build()` = old `EnterHub` world-building part + `BuildHubFloor` + `BuildHubDecor` + portal spawning; `Update(System.Action<MissionDefinition> enterMission)` = old `UpdateHub` body (with `EnterMission(x)` → `enterMission(x)`).

```csharp
using UnityEngine;

public class HubBuilder
{
    readonly SceneContext ctx;
    const float INTERACT_RADIUS = 2.8f;
    public HubBuilder(SceneContext ctx) { this.ctx = ctx; }

    public void Build()
    {
        /* old EnterHub body from after the mode/flag resets through world construction,
           calling BuildFloor(), BuildDecor(), SpawnPortals() */
    }

    void BuildFloor()   { /* old BuildHubFloor body */ }
    void BuildDecor()   { /* old BuildHubDecor body */ }

    void SpawnPortals()
    {
        var all = MissionCatalog.All();
        AddPortal(new Vector3(0, 0, 12),  Quaternion.Euler(0, 180, 0), all[0]);
        AddPortal(new Vector3(-10, 0, 6), Quaternion.Euler(0, 135, 0), all[1]);
    }

    void AddPortal(Vector3 pos, Quaternion rot, MissionDefinition def) { /* old AddPortal body, portals → ctx.portals */ }

    public void Update(System.Action<MissionDefinition> enterMission) { /* old UpdateHub body */ }
}
```
Move bodies verbatim (comments mark where). `GreyspaceScene.EnterHub()` keeps the mode switch and resets (`mode = Mode.Hub;` etc.) and calls `hubBuilder.Build()`; `UpdateHub()` becomes `hubBuilder.Update(EnterMission);`.

- [ ] **Step 3: Verify** — label `split-c4`; additionally walk to both portals by hand and check prompts (CHECKS.md items 1-3).

- [ ] **Step 4: Commit** — `refactor: extract MissionCatalog + HubBuilder from GreyspaceScene (no behaviour change)`.

### Task C5: Extract `MissionRunner`

**Files:** Create `Assets/Scripts/World/MissionRunner.cs`; Modify `GreyspaceScene.cs` (`EnterMission` 167-194, `SpawnWeaponPickup` 195-204, `BuildArenaFloor` 205-214, `WaveLoop` 215-283, `SpawnEnemy` 284-295, `SpawnFromPrefab` 296-304, `ClearEnemies` 537-541, `UpdateMission` 348-363)

- [ ] **Step 1: Create and move**

```csharp
using System.Collections;
using UnityEngine;

public class MissionRunner
{
    readonly SceneContext ctx;
    public MissionRunner(SceneContext ctx) { this.ctx = ctx; }

    public void Build(MissionDefinition def) { /* old EnterMission body after the mode switch: state resets, ClearWorld, arena, pickup, Checkpoint.ResetForNewMission */ }
    public IEnumerator WaveLoop()            { /* old WaveLoop body */ yield break; }
    public void SpawnEnemy(Vector3 pos)      { /* old SpawnEnemy body */ }
    public void SpawnFromPrefab(GameObject prefab, Vector3 pos) { /* old SpawnFromPrefab body */ }
    public void ClearEnemies()               { /* old ClearEnemies body, also clear EnemyBase */ }

    void SpawnWeaponPickup(ComboData data, Color color, Vector3 pos) { /* old body */ }
    void BuildArenaFloor()                   { /* old body */ }

    public void Update(System.Action goHub, System.Action<MissionDefinition> retry) { /* old UpdateMission body: EnterHub() → goHub(), EnterMission(m) → retry(m) */ }
}
```
Move bodies verbatim; remove the placeholder `yield break;` once the real `WaveLoop` body is in. In `ClearEnemies`, add `foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>()) Object.Destroy(e.gameObject);` (the old version only cleared `GunEnemy` — a latent bug when replaying with the newer enemy types).

`GreyspaceScene.EnterMission(def)` keeps `mode = Mode.Mission;`, then `missionRunner.Build(def); StartCoroutine(missionRunner.WaveLoop());`. `UpdateMission()` becomes `missionRunner.Update(EnterHub, EnterMission);`.

- [ ] **Step 2: Verify** — label `split-c5`; play the full CHECKS.md list by hand (items 4-8).

- [ ] **Step 3: Commit** — `refactor: extract MissionRunner from GreyspaceScene; ClearEnemies now also clears EnemyBase types`.

### Task C6: Confirm the split

- [ ] **Step 1: Size check**

```bash
wc -l Assets/Scripts/GreyspaceScene.cs Assets/Scripts/World/*.cs
```
Expected: `GreyspaceScene.cs` ≤ 170 lines; no single World file > 250 lines.

- [ ] **Step 2: Full CHECKS.md pass** (by hand, all 8 items) and `Tools/see/see.sh split-final 3`. Record result in `PROGRESS.md` (created in F1; if F1 isn't done yet, note in the commit message).

- [ ] **Step 3: Rebuild the knowledge graph** — run `/graphify --update` on `Assets/Scripts` and confirm the "Scene & World Manager" cohesion score improved from 0.08.

- [ ] **Step 4: Commit** any graph/doc updates.

---

## Workstream D — Debug commands and autopilot

### Task D1: `GreyspaceSnapshot` + `GreyspaceDebug.State()`

**Files:** Create `Assets/Scripts/Debug/GreyspaceSnapshot.cs`, `Assets/Scripts/Debug/GreyspaceDebug.cs`, `Assets/Tests/PlayMode/DebugStateTests.cs`; Modify `GreyspaceScene.cs` (add accessors)

- [ ] **Step 1: Failing PlayMode test**

`Assets/Tests/PlayMode/DebugStateTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DebugStateTests
{
    [UnityTest]
    public IEnumerator StateReportsHubAfterBoot()
    {
        new GameObject("Scene").AddComponent<GreyspaceScene>();
        yield return null; yield return null;
        var snap = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State());
        Assert.AreEqual("Hub", snap.mode);
        Assert.Greater(snap.playerHp, 0);
    }

    [UnityTest]
    public IEnumerator GoMissionSwitchesMode()
    {
        new GameObject("Scene").AddComponent<GreyspaceScene>();
        yield return null;
        GreyspaceDebug.GoMission("proving_ground");
        yield return null;
        var snap = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State());
        Assert.AreEqual("Mission", snap.mode);
        Assert.AreEqual("proving_ground", snap.missionId);
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in Object.FindObjectsOfType<GameObject>())
            if (go.scene.name != "DontDestroyOnLoad") Object.Destroy(go);   // keep Unity's test runner alive
        GameInput.Sim.ReleaseAll();
    }
}
```

- [ ] **Step 2: Run — expect failure** (`unity test --mode PlayMode`): types missing.

- [ ] **Step 3: Snapshot type**

`Assets/Scripts/Debug/GreyspaceSnapshot.cs`:
```csharp
using UnityEngine;

[System.Serializable]
public class GreyspaceSnapshot
{
    public string mode;
    public string missionId;
    public int wave, totalWaves;
    public bool playerDead, missionComplete;
    public int playerHp, playerMaxHp;
    public Vector3 playerPos;
    public int level, xp, skillPoints;
    public int enemiesAlive;
    public int frame;
    public float fps;
}
```

- [ ] **Step 4: Scene accessors** — add to `GreyspaceScene`:

```csharp
public static GreyspaceScene Instance { get; private set; }
void Awake() { Instance = this; }

public GreyspaceSnapshot Snapshot()
{
    return new GreyspaceSnapshot
    {
        mode = mode.ToString(),
        missionId = ctx.currentMission != null ? ctx.currentMission.missionId : "",
        wave = ctx.wave, totalWaves = ctx.totalWaves,
        playerDead = ctx.playerDead, missionComplete = ctx.missionComplete,
        playerHp = ctx.player != null ? ctx.player.health : 0,
        playerMaxHp = ctx.player != null ? ctx.player.maxHealth : 0,
        playerPos = ctx.playerGO != null ? ctx.playerGO.transform.position : Vector3.zero,
        level = SceneState.level, xp = SceneState.xp,
        skillPoints = ctx.player != null ? ctx.player.inventory.skillTree.skillPoints : 0,
        enemiesAlive = FindObjectsOfType<GunEnemy>().Length + FindObjectsOfType<EnemyBase>().Length,
        frame = Time.frameCount,
        fps = Time.unscaledDeltaTime > 0 ? 1f / Time.unscaledDeltaTime : 0f
    };
}

public void DebugGoHub() => EnterHub();
public bool DebugGoMission(string id)
{
    var def = MissionCatalog.Find(id);
    if (def == null) return false;
    EnterMission(def);
    return true;
}
public SceneContext DebugContext => ctx;
```

- [ ] **Step 5: Debug commands (state + navigation)**

`Assets/Scripts/Debug/GreyspaceDebug.cs`:
```csharp
using UnityEngine;

// Static commands for the Unity CLI: `unity command eval <flag> 'GreyspaceDebug.State()'`.
// Every method is safe to call when not playing (returns an error string / no-op).
public static class GreyspaceDebug
{
    static GreyspaceScene S => GreyspaceScene.Instance;

    public static string State()
        => S == null ? "{\"error\":\"not playing\"}" : JsonUtility.ToJson(S.Snapshot());

    public static string GoHub()
    {
        if (S == null) return "not playing";
        S.DebugGoHub(); return "ok";
    }

    public static string GoMission(string id)
    {
        if (S == null) return "not playing";
        return S.DebugGoMission(id) ? "ok" : "unknown mission " + id;
    }
}
```

- [ ] **Step 6: Run — expect pass** (`unity test --mode PlayMode`): 2 passed.

- [ ] **Step 7: Live check through the CLI**

```bash
Tools/see/see.sh debug-state 2 'GreyspaceDebug.GoMission("proving_ground")'
cat Docs/evidence/*-debug-state/state.json
```
Expected: `"mode":"Mission","missionId":"proving_ground"`; `frame.png` shows the arena.

- [ ] **Step 8: Commit** — `feat(debug): GreyspaceDebug.State/GoHub/GoMission + snapshot, CLI-callable`.

### Task D2: Spawn, grant, god mode, screenshot commands

**Files:** Modify `Assets/Scripts/Debug/GreyspaceDebug.cs`, `Assets/Scripts/GunCharacter.cs` (`TakeDamage`), `Assets/Tests/PlayMode/DebugStateTests.cs`

- [ ] **Step 1: Failing tests** — append to `DebugStateTests`:

```csharp
[UnityTest]
public IEnumerator SpawnEnemyAddsOne()
{
    new GameObject("Scene").AddComponent<GreyspaceScene>();
    yield return null;
    GreyspaceDebug.GoMission("proving_ground");
    yield return null;
    int before = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State()).enemiesAlive;
    Assert.AreEqual("ok", GreyspaceDebug.SpawnEnemy("charger", 5, 5));
    yield return null;
    int after = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State()).enemiesAlive;
    Assert.AreEqual(before + 1, after);
}

[UnityTest]
public IEnumerator GodModeBlocksDamage()
{
    new GameObject("Scene").AddComponent<GreyspaceScene>();
    yield return null;
    GreyspaceDebug.SetGodMode(true);
    var p = Object.FindObjectOfType<GunCharacter>();
    int hp = p.health;
    p.TakeDamage(50);
    Assert.AreEqual(hp, p.health);
}
```

- [ ] **Step 2: Run — expect fail.**

- [ ] **Step 3: God-mode flag** — in `GunCharacter`, add field `[HideInInspector] public bool debugGodMode;` and make the first line of `TakeDamage`:
```csharp
if (isDead || invincible || debugGodMode) return;
```
(replacing the existing `if (isDead || invincible) return;`).

- [ ] **Step 4: Commands** — add to `GreyspaceDebug`:

```csharp
public static string SpawnEnemy(string type, float x, float z)
{
    if (S == null) return "not playing";
    var ctx = S.DebugContext;
    Vector3 pos = new Vector3(x, 0, z);
    GameObject go = new GameObject("Debug_" + type);
    go.transform.position = pos;
    Transform target = ctx.playerGO != null ? ctx.playerGO.transform : null;
    switch (type)
    {
        case "stalker": { var e = go.AddComponent<GunEnemy>(); e.player = target; break; }
        case "charger": { var e = go.AddComponent<ChargerEnemy>(); e.player = target; break; }
        case "ranged":  { var e = go.AddComponent<RangedEnemy>();  e.player = target; break; }
        case "shielder":{ var e = go.AddComponent<ShielderEnemy>(); e.player = target; break; }
        default: Object.Destroy(go); return "unknown type " + type + " (stalker|charger|ranged|shielder)";
    }
    return "ok";
}

public static string GrantSkillPoints(int n)
{
    if (S == null) return "not playing";
    S.DebugContext.player.inventory.skillTree.AddSkillPoints(n);
    return "ok";
}

public static string SetGodMode(bool on)
{
    if (S == null) return "not playing";
    S.DebugContext.player.debugGodMode = on;
    return "ok";
}

public static string Screenshot(string absolutePath)
{
    if (S == null) return "not playing";
    ScreenCapture.CaptureScreenshot(absolutePath);
    return "ok";
}
```
Before writing the switch, open `ChargerEnemy.cs`, `RangedEnemy.cs`, `ShielderEnemy.cs` and confirm their stat defaults are set in their own `Start()` or field initialisers (ShielderEnemy sets them in `Start()` from `_health` etc.). If any enemy relies on the spawner to set `health`, set `health` in its `case` the same way `GreyspaceScene.SpawnEnemy` does today.

- [ ] **Step 5: Run — expect 4 passed.**

- [ ] **Step 6: Commit** — `feat(debug): SpawnEnemy/GrantSkillPoints/SetGodMode/Screenshot commands`.

### Task D3: `ComboBot` autopilot

**Files:** Create `Assets/Scripts/Debug/ComboBot.cs`, `Assets/Tests/PlayMode/ComboBotTests.cs`; Modify `GreyspaceDebug.cs`

- [ ] **Step 1: Failing test**

`Assets/Tests/PlayMode/ComboBotTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ComboBotTests
{
    [UnityTest, Timeout(180000)]
    public IEnumerator BotClearsAtLeastOneWave()
    {
        new GameObject("Scene").AddComponent<GreyspaceScene>();
        yield return null;
        GreyspaceDebug.SetGodMode(true);
        GreyspaceDebug.GoMission("proving_ground");
        GreyspaceDebug.StartAutopilot();
        Time.timeScale = 3f;
        float end = Time.realtimeSinceStartup + 120f;
        int startWave = 0;
        while (Time.realtimeSinceStartup < end)
        {
            var s = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State());
            if (startWave == 0) startWave = s.wave;
            if (s.wave > startWave || s.missionComplete) break;
            yield return null;
        }
        Time.timeScale = 1f;
        var final = JsonUtility.FromJson<GreyspaceSnapshot>(GreyspaceDebug.State());
        Assert.IsTrue(final.wave > startWave || final.missionComplete, "bot did not clear a wave: " + GreyspaceDebug.State());
    }

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1f;
        foreach (var go in Object.FindObjectsOfType<GameObject>())
            if (go.scene.name != "DontDestroyOnLoad") Object.Destroy(go);   // keep Unity's test runner alive
        GameInput.Sim.ReleaseAll();
    }
}
```

- [ ] **Step 2: Run — expect fail** (`StartAutopilot` missing).

- [ ] **Step 3: Implement the bot**

`Assets/Scripts/Debug/ComboBot.cs`:
```csharp
using UnityEngine;

// Autopilot: walks to the nearest enemy, aims at it, attacks L-L-H, dashes away when low.
// Drives the game ONLY through GameInput.Sim, exactly like a player would.
public class ComboBot : MonoBehaviour
{
    public float attackRange = 1.7f;
    public float retreatHpFraction = 0.25f;
    public float stepGap = 0.22f;               // seconds between combo presses

    static readonly KeyCode[] Combo = { KeyCode.J, KeyCode.J, KeyCode.K };
    int comboIndex;
    float nextPress;
    public int attacksIssued, dashesIssued;

    GunCharacter player;

    void Start() { player = GetComponent<GunCharacter>(); }

    void OnDisable() { GameInput.Sim.ReleaseAll(); }

    void Update()
    {
        if (player == null || player.isDead || Camera.main == null) { GameInput.Sim.ReleaseAll(); return; }

        Transform target = NearestEnemy();
        ReleaseMove();
        if (target == null) return;

        Vector3 to = target.position - transform.position; to.y = 0;
        GameInput.Sim.MouseScreenOverride = Camera.main.WorldToScreenPoint(target.position);

        bool low = player.health < player.maxHealth * retreatHpFraction;
        if (low && to.magnitude < 3f && Time.time >= nextPress)
        {
            MoveAlong(-to.normalized);
            GameInput.Sim.Tap(KeyCode.Space);
            dashesIssued++;
            nextPress = Time.time + 0.6f;
            return;
        }

        if (to.magnitude > attackRange) { MoveAlong(to.normalized); return; }

        if (Time.time >= nextPress)
        {
            GameInput.Sim.Tap(Combo[comboIndex]);
            comboIndex = (comboIndex + 1) % Combo.Length;
            attacksIssued++;
            nextPress = Time.time + stepGap;
        }
    }

    void MoveAlong(Vector3 worldDir)
    {
        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();
        float v = Vector3.Dot(worldDir, fwd), h = Vector3.Dot(worldDir, right);
        if (v >  0.3f) GameInput.Sim.Press(KeyCode.W);
        if (v < -0.3f) GameInput.Sim.Press(KeyCode.S);
        if (h >  0.3f) GameInput.Sim.Press(KeyCode.D);
        if (h < -0.3f) GameInput.Sim.Press(KeyCode.A);
    }

    void ReleaseMove()
    {
        GameInput.Sim.Release(KeyCode.W); GameInput.Sim.Release(KeyCode.S);
        GameInput.Sim.Release(KeyCode.A); GameInput.Sim.Release(KeyCode.D);
    }

    Transform NearestEnemy()
    {
        Transform best = null; float bestD = float.MaxValue;
        foreach (var e in FindObjectsOfType<GunEnemy>()) Consider(e.transform, ref best, ref bestD);
        foreach (var e in FindObjectsOfType<EnemyBase>()) Consider(e.transform, ref best, ref bestD);
        return best;
    }

    void Consider(Transform t, ref Transform best, ref float bestD)
    {
        float d = (t.position - transform.position).sqrMagnitude;
        if (d < bestD) { bestD = d; best = t; }
    }
}
```

Add to `GreyspaceDebug`:
```csharp
public static string StartAutopilot()
{
    if (S == null) return "not playing";
    var p = S.DebugContext.playerGO;
    if (p.GetComponent<ComboBot>() == null) p.AddComponent<ComboBot>();
    return "ok";
}

public static string StopAutopilot()
{
    if (S == null) return "not playing";
    var bot = S.DebugContext.playerGO.GetComponent<ComboBot>();
    if (bot != null) Object.Destroy(bot);
    GameInput.Sim.ReleaseAll();
    return "ok";
}
```

Note: `ReleaseMove()` then `Press()` in the same frame re-sends a "down" each frame for held movement keys; movement uses `GetKey` (held), so this is harmless. If a future system reacts to `GetKeyDown(W)`, switch to tracking the previous direction instead.

- [ ] **Step 4: Run — expect pass** (`unity test --mode PlayMode`). If the bot stalls, watch it with `Tools/see/see.sh bot-debug 15 'GreyspaceDebug.GoMission("proving_ground")'` after manually calling `StartAutopilot`, and tune `attackRange` against `ComboData.Sword()` light range (1.8).

- [ ] **Step 5: Commit** — `feat(debug): ComboBot autopilot driving GameInput.Sim + PlayMode test`.

---

## Workstream E — Local model bake-off (Opus directs, local builds)

### Task E1: Get the local models reachable

- [ ] **Step 1: 419c — open a tunnel (don't expose Ollama to the network)**

Do NOT use `qwen3-koinon-t1` anywhere in this bake-off: it is YUGA·SOL's MANAS T1 BUY/SELL/HOLD classifier (fixed system prompt, `num_predict 32`) and lives only on the Mac. 419c is an NVIDIA GB10 with 121 GB unified memory (CPU and GPU share it).

```bash
ssh -f -N -L 11435:localhost:11434 419c
curl -s http://localhost:11435/api/tags | python3 -c "import json,sys;print([m['name'] for m in json.load(sys.stdin)['models']])"
```
Expected: list includes `qwen3.8-fixed:27b`, `nemotron-3.5-lightning:30b`, `ornith-1.5:35b`.

- [ ] **Step 2: Mac — Laguna S 2.1 via LM Studio**

Laguna S 2.1 (Poolside, 71 GB) is already downloaded and loaded in LM Studio on the Mac; LM Studio serves an OpenAI-compatible API on port 1234.

```bash
~/.lmstudio/bin/lms server status            # expect: running on port 1234
~/.lmstudio/bin/lms ls | grep -i laguna       # expect: poolside/laguna-s-2.1 ... LOADED
curl -s http://localhost:1234/v1/models | python3 -c "import json,sys;print([m['id'] for m in json.load(sys.stdin)['data']])"
```
Expected: `poolside/laguna-s-2.1` in the list. If not loaded: `~/.lmstudio/bin/lms load poolside/laguna-s-2.1`; if the server is down: `~/.lmstudio/bin/lms server start`.

Memory note (Mac, 128 GB): Laguna S takes ~71 GB while loaded; it is the only Mac contestant, so nothing else needs unloading. The harness calls models one at a time.

- [ ] **Step 3: Remove the dead router slot** — in `/Users/apurv2793/Manifest main/manifest/core/manas/nim_router.py`, the `UNITY_CODE` list has an Ollama slot naming `qwen3-coder-next:q4_K_M`, a model that was never pulled (the old "Ollama 404"). Qwen3 coder models are out of the bake-off (owner decision 2026-10-07), so delete that one `ModelSlot(Provider.OLLAMA, "qwen3-coder-next:q4_K_M", ...)` line. Local builders are reached through the bake-off harness until a winner is chosen; wiring the winner into the router is Task E3 Step 5. Commit in the `manifest` repo: `fix(router): remove never-installed qwen3-coder-next slot from unity_code`.

### Task E2: Bake-off harness and briefs

**Files:** Create `Tools/bakeoff/bakeoff.py`, `Tools/bakeoff/briefs/01-damage-calc.md`, `Tools/bakeoff/briefs/02-health-orb.md`, `Tools/bakeoff/briefs/03-charger-telegraph.md`, `Tools/bakeoff/.gitignore`

- [ ] **Step 1: The harness**

`Tools/bakeoff/bakeoff.py`:
```python
#!/usr/bin/env python3
"""Run each brief against each local model; save raw output + timing for Opus to review.

Usage: python3 Tools/bakeoff/bakeoff.py [--models name,...] [--briefs 01,02]
Output: Tools/bakeoff/runs/<stamp>/<model>/<brief>.cs + results.jsonl
"""
import argparse, json, re, time, urllib.request
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).parent
# name -> (api, base_url, model). api: "ollama" = native /api/chat, "openai" = /v1/chat/completions (LM Studio)
MODELS = {
    "mac-laguna-s-2.1":   ("openai", "http://localhost:1234",  "poolside/laguna-s-2.1"),
    "419c-qwen3.8-fixed": ("ollama", "http://localhost:11435", "qwen3.8-fixed:27b"),
    "419c-nemotron-3.5":  ("ollama", "http://localhost:11435", "nemotron-3.5-lightning:30b"),
    "419c-ornith-1.5":    ("ollama", "http://localhost:11435", "ornith-1.5:35b"),
}
SYSTEM = (HERE / "briefs" / "_house_rules.md").read_text()

def call(api, base, model, prompt, timeout=1800):
    msgs = [{"role": "system", "content": SYSTEM}, {"role": "user", "content": prompt}]
    if api == "ollama":
        url, payload = base + "/api/chat", {
            "model": model, "stream": False, "messages": msgs,
            "options": {"num_predict": 12000, "num_ctx": 32768, "temperature": 0.2}}
    else:  # openai-compatible (LM Studio)
        url, payload = base + "/v1/chat/completions", {
            "model": model, "stream": False, "messages": msgs,
            "max_tokens": 12000, "temperature": 0.2}
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers={"Content-Type": "application/json"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=timeout) as r:
        d = json.load(r)
    secs = time.time() - t0
    if api == "ollama":
        return d["message"].get("content", ""), d.get("done_reason", ""), secs
    ch = d["choices"][0]
    return ch["message"].get("content", ""), ch.get("finish_reason", ""), secs

def extract_cs(text):
    m = re.findall(r"```(?:csharp|cs)?\s*\n(.*?)```", text, re.S)
    return max(m, key=len) if m else text

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--models", default=",".join(MODELS))
    ap.add_argument("--briefs", default="")
    a = ap.parse_args()
    briefs = sorted(p for p in (HERE / "briefs").glob("[0-9]*.md")
                    if not a.briefs or p.name[:2] in a.briefs.split(","))
    out = HERE / "runs" / datetime.now().strftime("%Y%m%d-%H%M%S")
    out.mkdir(parents=True)
    with open(out / "results.jsonl", "w") as log:
        for name in a.models.split(","):
            api, base, model = MODELS[name]
            for b in briefs:
                try:
                    text, reason, secs = call(api, base, model, b.read_text())
                    code = extract_cs(text)
                    d = out / name; d.mkdir(exist_ok=True)
                    (d / (b.stem + ".cs")).write_text(code)
                    (d / (b.stem + ".raw.md")).write_text(text)
                    rec = {"model": name, "brief": b.stem, "secs": round(secs, 1),
                           "chars": len(code), "done_reason": reason, "error": None}
                except Exception as e:
                    rec = {"model": name, "brief": b.stem, "error": str(e)}
                print(json.dumps(rec)); log.write(json.dumps(rec) + "\n"); log.flush()
    print("outputs in", out)

if __name__ == "__main__":
    main()
```

- [ ] **Step 2: House rules (system prompt)**

`Tools/bakeoff/briefs/_house_rules.md`:
```markdown
You write Unity 6 C# for Greyspace (URP, orthographic isometric action game).
Hard rules, never break them:
- No namespaces.
- No Shader.Find(). Materials: new Material(GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>().sharedMaterial) then set BOTH mat.SetColor("_BaseColor", c) AND mat.color = c; destroy the temp primitive with DestroyImmediate.
- Movement: transform.position += only. No Rigidbody, no CharacterController, no Physics queries.
- Input: GameInput.GetKey / GameInput.GetKeyDown (KeyCode.*) only. Never UnityEngine.Input.
- Geometry: GameObject.CreatePrimitive() only; remove its Collider; parent with SetParent(transform, false).
- Use WaitForSecondsRealtime for timers that must work during hit-stop (Time.timeScale = 0).
Output ONE C# file in a single ```csharp block. No explanation outside the block.
```

- [ ] **Step 3: Brief 01 — pure logic** (`Tools/bakeoff/briefs/01-damage-calc.md`):

```markdown
Write `public static class DamageCalc` with:
- `public static int Compute(int baseDamage, float multiplier, bool crit, float critMultiplier, float resistFraction)`
  returns Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier * (crit ? critMultiplier : 1f) * (1f - Mathf.Clamp01(resistFraction)))).
- `public static bool RollCrit(float chance, System.Random rng)` returns rng.NextDouble() < chance (chance clamped 0..1).
Acceptance: Compute(10,1.5f,true,2f,0.25f)==23 ; Compute(10,1f,false,2f,1f)==1 ; Compute(0,1,false,2,0)==1.
```

- [ ] **Step 4: Brief 02 — gameplay MonoBehaviour** (`Tools/bakeoff/briefs/02-health-orb.md`):

```markdown
Write `public class HealthOrb : MonoBehaviour`:
- Public fields: `int healAmount = 25`, `float pickupRadius = 1.4f`, `Color color = new Color(0.3f, 1f, 0.45f)`.
- Start(): build a glowing look from primitives: a Sphere (scale 0.35) floating at y=0.6 and a flat Cylinder ring (scale 0.6, 0.02, 0.6) at y=0.02, both using the shader-steal material rule with `color`.
- Update(): bob the sphere (sin, amplitude 0.12, speed 2.5); spin it slowly; find the player with FindObjectOfType<GunCharacter>() (cache it); when within pickupRadius (Vector3.Distance) heal: player.health = Mathf.Min(player.maxHealth, player.health + healAmount); then call VFXManager.Spawn(EffectType.HitSparks, transform.position + Vector3.up * 0.6f, color) and Destroy(gameObject).
`GunCharacter` has public int health, public int maxHealth. `VFXManager.Spawn(EffectType, Vector3, Color)` exists.
```

- [ ] **Step 5: Brief 03 — change existing code** (`Tools/bakeoff/briefs/03-charger-telegraph.md`): the brief is the current `Assets/Scripts/ChargerEnemy.cs` source pasted under this instruction (generate the file so it's always current):

```bash
{ echo 'Modify the ChargerEnemy below. During its telegraph (wind-up) state, make the body pulse between its normal colour and bright red (period 0.15 s) instead of whatever it does now, and restore the normal colour when the charge starts. Change NOTHING else: same fields, same states, same timings. Return the complete modified file.'; echo; echo '```csharp'; cat Assets/Scripts/ChargerEnemy.cs; echo '```'; } > Tools/bakeoff/briefs/03-charger-telegraph.md
```

- [ ] **Step 6: Ignore run outputs** — `Tools/bakeoff/.gitignore`:
```
runs/
```

- [ ] **Step 7: Commit** — `tools: local-model bake-off harness + 3 Greyspace briefs`.

### Task E3: Run, review under Opus, decide defaults

**Files:** Create `Docs/BAKEOFF-RESULTS.md`; Modify `Docs/GREYSPACE-CHARTER.md` §4.4

- [ ] **Step 1: Run** — `python3 Tools/bakeoff/bakeoff.py` (4 models × 3 briefs = expect 12 records; reasoning models and Laguna S can take minutes each).

- [ ] **Step 2: Compile gate per output** — for each `.cs`, copy into `Assets/Scripts/_Bakeoff/` one at a time, `unity command recompile` (name per `Docs/tooling/unity-commands.json`), `unity command console --tail 50 --level error`, record pass/fail, delete the file. For brief 03 compile it as a replacement of `ChargerEnemy.cs` on a throwaway branch (`git switch -c bakeoff-tmp`), then `git switch - && git branch -D bakeoff-tmp`.

- [ ] **Step 3: Opus review with up to 3 feedback rounds** — for each failing or flawed output, Opus writes specific fixes (not "try again"), re-sends brief + previous output + fixes to the same model (manual `call` via a short python one-liner or a `--followup` run), max 3 rounds. Score each final result:

| Criterion | Points |
|---|---|
| Compiles (gate — fail = 0 total) | — |
| Hard-rule adherence | 0–3 |
| Meets acceptance / behaviour spec | 0–4 |
| House style, no dead code | 0–2 |
| Rounds needed (0 → +1, 1 → +0.5, 2–3 → 0) | 0–1 |

- [ ] **Step 4: Write `Docs/BAKEOFF-RESULTS.md`** — table model × brief with score, rounds, seconds; verdict per model ("default builder", "logic-only", "don't use"); escalation observations.

- [ ] **Step 5: Update charter §4.4** with the chosen default builder per task type and commit both docs: `docs: local-model bake-off results; set default builders`. If the winner should also be reachable through Manifest OS, add it as a `UNITY_CODE` slot in `nim_router.py` (Laguna via LM Studio needs an OpenAI-compatible provider entry pointing at `http://localhost:1234/v1`).

---

## Workstream F — Design documents and AI memory

### Task F1: Memory files

**Files:** Create `DESIGN.md`, `PROGRESS.md`, `NOTES.md` at repo root

- [ ] **Step 1: Write them**

`DESIGN.md`:
```markdown
# Greyspace — Design (living summary)
Source of truth for vision: Docs/GREYSPACE-CHARTER.md. This file = the short version an agent reads first.

## Pillars (working version 2026-10-07)
1. Every choice leaves a mark.
2. Who you are is how you fight (instinct / discipline / habit + player skill).
3. Many hands, one story (several playable heroes, companions, fates by choice).
4. Every chapter feels different; every hit lands (mastery, dread, mystery, power on one combat core).

## Art rule
Grey world, sharp light. See Docs/ART-BIBLE.md.

## Current engine
Unity 6000.4.11f1, URP 17.0.3, orthographic isometric (camera test due in Phase 1).
Scene controller: GreyspaceScene + World/{HubBuilder, MissionRunner, HudController, WorldPrims, MissionCatalog}.
Input: GameInput only. Debug: GreyspaceDebug (CLI eval). Bot: ComboBot.
```

`PROGRESS.md`:
```markdown
# Progress log (newest first)
Each entry: date · what changed · evidence dir · next step.

- 2026-10-07 · Phase 0 plan written (Docs/superpowers/plans/2026-10-07-phase-0-foundation.md) · — · start Task 0
```

`NOTES.md`:
```markdown
# Honest notes — weaknesses and measurements
Write what is weak, with numbers. Never delete entries; mark them resolved.

- Visuals: all primitives; estimated 3/10 on frontier-games rubric (charter §2).
- GreyspaceScene.cs was 589 lines / cohesion 0.08 (graphify) before Phase 0 split.
- Ollama 404 root cause: router named qwen3-coder-next:q4_K_M, never pulled.
- 419c cannot run Unity (ARM Linux).
```

- [ ] **Step 2: Commit** with the pending charter edits: `git add DESIGN.md PROGRESS.md NOTES.md Docs/GREYSPACE-CHARTER.md && git commit -m "docs: AI memory files (DESIGN/PROGRESS/NOTES); charter decisions + pillars v2"`.

### Task F2: Feel matrix

**Files:** Create `Docs/FEEL-MATRIX.md`

- [ ] **Step 1: Inventory current feedback per event** — read `MeleeAttack.cs`, `CombatFeel.cs`, `CameraShake.cs`, `VFXManager.cs`, `DamageNumber.cs`, `EnemyBase.cs`, `GunCharacter.cs` and fill the "today" column.

- [ ] **Step 2: Write the matrix** with these rows and columns (all cells filled; "—" means deliberately nothing):

Rows: light hit lands · heavy hit lands · combo finisher · enemy killed · player hit · player dash · dodge through attack (i-frames) · shield blocks (Shielder) · enemy wind-up (telegraph) · level up · wave clear · mission complete · instinct trigger (new, Phase 1+).

Columns: Visual (VFX) · Sound (clip id) · Camera (shake intensity/duration) · Hit-stop (frames) · Tween/animation · Today (what exists) · Priority (P0/P1/P2).

Presets block: `Light / Medium / Heavy` = shake (0.06/0.10s, 0.12/0.15s, 0.3/0.4s), hit-stop (3, 5, 8 frames), with the rule "sound, particles and shake fade end within ±1 frame of each other".

- [ ] **Step 3: Commit** — `docs: combat feel matrix (every action: visual/sound/camera/hit-stop)`.

### Task F3: Art bible v0 (needs Apurv's input on mood references)

**Files:** Create `Docs/ART-BIBLE.md`

- [ ] **Step 1: Ask Apurv** (one question at a time, multiple-choice where possible): 3–5 reference games/films for mood; the accent colours and what each means; how dark the world should feel.
- [ ] **Step 2: Write sections:** Visual rule ("grey world, sharp light"); palette (base greys with hex values + accent colours with meaning); lighting moods per area type (hub, combat arena, dread, mystery, power); shape language (player factions vs enemies); silhouette rules (readable in 1 second at default zoom — test via `GreyspaceDebug.Screenshot`); VFX style; character pipeline note (Manifest 3D pipeline, Mara base body); environment kit note (Blender-scripted); "never" list.
- [ ] **Step 3: Commit** — `docs: art bible v0`.

### Task F4: Story bible seed (needs Apurv)

**Files:** Create `Docs/STORY-BIBLE.md`

- [ ] **Step 1: Ask Apurv** (one at a time): the world's premise in 2–3 sentences; the playable heroes besides Mara (names, one-line each); what kind of consequences matter most (who lives/dies, factions, the world changing, how others see you).
- [ ] **Step 2: Write sections:** premise; tone (from the world_design prompt: mythic, terse, morally complex, no chosen ones, post-collapse); cast table (hero · instinct · discipline path · example habit); consequence model (what is tracked — use `WorldState` keys — and the rule "every major choice shows a visible result within the same chapter"); trait model (instinct / discipline / habit definitions + how each maps to code: instinct = triggered state, discipline = `SkillTree`, habit = counters in `WorldState`); chapter-style map (which chapter leans mastery/dread/mystery/power); open questions.
- [ ] **Step 3: Commit** — `docs: story bible seed (premise, cast, consequence + trait models)`.

### Task F5: Reference frames and critic rubric

**Files:** Create `Docs/reference/SOURCES.md`, `Docs/reference/.gitignore`, `Docs/CRITIC-RUBRIC.md`

- [ ] **Step 1: Collect frames** (internal comparison only — images are gitignored, sources committed):

```bash
mkdir -p Docs/reference/frames
printf '*.png\n*.jpg\n*.jpeg\n*.webp\n*.gif\nframes/\n' > Docs/reference/.gitignore
```
Pull 1–3 hero screenshots each from: bridge-mind/turbo-kart-rally, Nipale-ai/opus-5-5-overnight-builds (Fall Line), the frontier-games entries `voxel-musou`, `play-de-claude`, `the-ashen-gate`, az9713/gauntlet-loop-unity-cli-demo, Werdna1976/PathOfWerdna. For each repo: `gh api repos/OWNER/REPO/contents/<image path from README> --jq .download_url` then `curl -L -o Docs/reference/frames/<repo>-<n>.png <url>`. Add 3–5 frames from shipped isometric action games Apurv picks in F3 as the aspirational bar.

- [ ] **Step 2: `Docs/reference/SOURCES.md`** — one line per frame: file name · source URL · what it represents (e.g. "best Opus 5.5 lighting", "isometric readability").

- [ ] **Step 3: `Docs/CRITIC-RUBRIC.md`**:

```markdown
# Blind critic rubric

Weights (same as theolundqvist/frontier-games): Visuals 0.30 · Gameplay 0.35 · Polish 0.15 · Ambition 0.20.
Each 1–10 with confidence low/medium/high. Overall = weighted mean.

## Protocol
1. Critic is a fresh subagent with NO access to the code or our docs except this rubric.
2. Show pairs (ours vs reference frame) in random order, unlabeled. Ask: which is better, and why, per category.
3. Then score ours alone on each category with the single weakest thing named.
4. Gameplay may not be scored above 6 from stills alone — needs a bot run report + a human play note.
5. A screenshot verdict never closes a milestone alone (the "WOWED but crashes" rule): exit checks also need errorLines = 0, a bot run, and Apurv's play note.

## Numeric checks on every frame
- Blown-out share: pixels with luminance > 0.98 — target < 2%.
- Crushed share: luminance < 0.02 — target < 15% (it's a dark world; flag if > 15%).
- Console errors: 0. FPS (from snapshot): ≥ 60 on the Mac at 1080p.
```

- [ ] **Step 4: Commit** — `docs: reference frame sources + blind critic rubric`.

---

## Task X: Phase 0 exit check

- [ ] **Step 1: Unattended mission run**

```bash
Tools/see/see.sh phase0-exit 60 'GreyspaceDebug.SetGodMode(true); GreyspaceDebug.GoMission("proving_ground"); GreyspaceDebug.StartAutopilot()'
```
If the CLI `eval` takes one expression only, run the three calls as three `unity command eval` lines before `see.sh` instead and set the third argument to `GreyspaceDebug.StartAutopilot()`.

- [ ] **Step 2: Pass criteria (all must hold)**
  - `summary.json`: frames advancing, `errorLines` = 0
  - `state.json`: `wave` ≥ 2 or `missionComplete` true
  - `frame.png` shows combat in the arena
  - `unity test --mode EditMode` and `--mode PlayMode` both green
  - `GreyspaceScene.cs` ≤ 170 lines
  - `Docs/BAKEOFF-RESULTS.md` names a default local builder
  - F1–F5 docs exist (F3/F4 may be "v0 pending Apurv")

- [ ] **Step 3: Log** — add the result and evidence path to `PROGRESS.md`, update `NOTES.md` with measured numbers, commit `chore: Phase 0 exit check — <pass|fail> (<evidence dir>)`.

- [ ] **Step 4: Hand back to Apurv** — short plain-language summary + the exit screenshot; propose Phase 1 planning.
