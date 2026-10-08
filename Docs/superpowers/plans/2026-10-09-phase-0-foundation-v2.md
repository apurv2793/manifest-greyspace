# Greyspace Phase 0 — Foundation Plan v2 (reconciled with main)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Supersedes:** `2026-10-07-phase-0-foundation.md` (written before the 33 Aug 7–10 upstream commits). Read that file only for detail this one points back to; where they disagree, **this file wins**.

**Goal:** Give the AI eyes and hands in Unity, make the game self-testable (debug commands + autopilot bot that drives the real input path), split the oversized scene file, and write the design docs every later phase is judged against.

**Architecture:** Official Unity CLI + `com.unity.pipeline` drive the open Editor. Inside the game, `GreyspaceDebug` exposes static commands for `unity command eval`; `ComboBot` plays missions by feeding `InputRouter` (action signals + a new sim move/aim override). `GreyspaceScene.cs` (610 lines) is split into focused parts afterwards. Model orchestration and the bake-off are owned by **NexusHub** (owner decision 2026-10-07).

**Tech Stack:** Unity 6000.4.11f1, URP 17.0.3, legacy Input via `InputRouter`, `MaterialCache`, `ContentLibrary`, Unity CLI 1.0.0-beta.13, `com.unity.pipeline` 0.8.0-exp.1, Unity Test Framework (to add).

---

## Status as of 2026-10-09

| Item | Status | Notes |
|---|---|---|
| Task 0 housekeeping | ✅ | `GameConfig.cs` parked (superseded by `ContentLibrary`/`ContentPack`); missing `.meta` files for 419c-added assets committed (61d3b7a) |
| A1 Unity CLI + pipeline + plugin + MCP | ✅ | See `Docs/TOOLING.md`. MCP registered with absolute path. Owner signed licence 2026-10-09 |
| A2 See loop script | 🟡 | Editor connected; play/eval/capture/console all work. Frame-1 freeze FIXED 2026-10-09: macOS App Nap (NSAppSleepDisabled=YES for com.unity3d.UnityEditor5.x, needs an Editor restart). Proof: `wait_for frameCount changed` met. `capture_game_view --save_path` writes under `Assets/` (move the PNG out). `Tools/see/see.sh` still to write |
| B1 Test framework + asmdefs | ⏳ | `com.unity.test-framework` not installed; no asmdefs exist |
| ~~B2/B3 GameInput~~ | ❌ dropped | `InputRouter` already centralises input. Bot uses `InputRouter` (D3) |
| C Scene split | ⏳ | Re-mapped below (610 lines now) |
| D Debug commands + ComboBot | ✅ | Bot cleared The Proving Ground unattended: 3/3 waves + 2 extra spawns, 54 attacks in ~33 s, missionComplete, level 2, 0 game errors. Evidence `Docs/evidence/20261009-025331-phase0-D-verify/`. Found + fixed: no AudioListener in scene → all sound inaudible + 19k warnings |
| E Bake-off | ➡️ NexusHub | Exam v2 (12 briefs) delivered to NexusHub `exams/incoming/greyspace.unity_code.v2.json`. NexusHub owns models, scoring, compile gate |
| F Docs | ⏳ | `AGENT-HANDBOOK.md` written 2026-10-09; FEEL-MATRIX, ART/STORY bibles, CRITIC-RUBRIC still to do (F3/F4 need owner) |
| X Exit check | ⏳ | After A2, B1, C, D |

## Hard rules (current — supersede the v1 list)

- No namespaces. No `Shader.Find()` — materials via `MaterialCache.Get(color)`.
- Movement: `transform.position +=` only. No Rigidbody / CharacterController / physics queries.
- **Input: `InputRouter` only** (`MoveAxis()`, `LightPressed()`, …, `TryAimWorldPoint()`). Never `UnityEngine.Input` outside `InputRouter.cs`.
- Player: `GunCharacter.Instance`. Enemies: `EnemyBase.Active` / `GunEnemy.Active` — no `FindObjectsOfType` in per-frame code.
- Content: `ContentLibrary.Mission(id)` / `ContentLibrary.Combo(name)` before inline fallbacks.
- Geometry via `GameObject.CreatePrimitive()`, collider removed, parented with `SetParent(…, false)`.
- `WaitForSecondsRealtime` for anything that must run during hit-stop.
- **Every new asset ships with its `.meta`** — 419c (no Unity) must not commit assets without them; if it must, the Mac commits the generated `.meta` on next open.
- Run `unity` from `~/greyspace` with `--non-interactive`.

---

## A2 — See loop (unchanged intent, updated commands)

Write `Tools/see/see.sh` exactly as v1 Task A2 but:
- prefix every call with `unity --non-interactive`;
- before writing it, run `unity --non-interactive commands --json > Docs/tooling/unity-commands.json` and `unity --non-interactive command eval --help`, and use the real command/flag names (v1's names came from docs, not the installed CLI);
- the state call is `GreyspaceDebug.State()` (D1 below).

## B1 — Test framework (unchanged)

As v1 Task B1, with one change: the runtime asmdef must reference `UnityEngine.UI` **and** `Unity.Pipeline` only if any runtime script uses it (none does today — leave it out). Verify with `unity --non-interactive test --mode EditMode`.

## C — Scene split, re-mapped to the 610-line file

Line numbers as of 61d3b7a — re-grep before each step (`grep -nE "^\s*(void|IEnumerator|public|static|GameObject)" Assets/Scripts/GreyspaceScene.cs`).

| New file | Moves from `GreyspaceScene.cs` |
|---|---|
| `World/SceneContext.cs` | fields 13–25 (`playerGO, worldRoot, hudRoot, player, healthFill, xpFill, *Text, portals, wave, totalWaves, playerDead, missionComplete, currentMission`) |
| `World/WorldPrims.cs` | `Tile` 573, `Block` 583, `Prim` 593, `Rect` 603 (no `Mat` any more — `MaterialCache` replaced it) |
| `World/HudController.cs` | `BuildHUD` 393–510, `ShowRewardToast` 511, `UpdateXP` 526, `LevelUpFlash` 38 |
| `World/HubBuilder.cs` | `EnterHub` world part 65–97, `BuildHubFloor` 98, `BuildHubDecor` 116, `SpawnPortals` 138 (keeps the `ContentLibrary.Mission` lookup + inline fallback), `AddPortal` 170, `UpdateHub` 348 |
| `World/MissionRunner.cs` | `EnterMission` body 182, `SpawnWeaponPickup` 210, `BuildArenaFloor` 220, `WaveLoop` 230, `SpawnEnemy` 306, `SpawnFromPrefab` 318, `ClearEnemies` 566, `UpdateMission` 377 |

**Drop v1's `MissionCatalog`** — `ContentLibrary` is the catalogue. Debug lookups use `ContentLibrary.Mission(id)`.

**Fix while moving `ClearEnemies`:** it clears `GunEnemy.Active` and `Projectile.Active` but **not `EnemyBase.Active`** — Charger/Ranged/Shielder survive a mission replay. Add `foreach (var e in EnemyBase.Active.ToArray()) Destroy(e.gameObject);` (`ToArray` because `OnDisable` mutates the list).

Process per extraction is v1 C2–C6 (baseline screenshots + CHECKS.md, extract, See loop, commit). Exit: `GreyspaceScene.cs` ≤ 170 lines.

## D — Debug commands + ComboBot (built now, against the current scene)

**D0 — InputRouter sim override.** Add to `InputRouter`:
```csharp
// Bot / AI-test override. Null = normal behaviour. Set by ComboBot; never by gameplay code.
public static Vector2? SimMoveAxis;
public static Vector3? SimAimPoint;
```
`MoveAxis()`: `if (SimMoveAxis.HasValue) return SimMoveAxis.Value;` first.
`TryAimWorldPoint()`: `if (SimAimPoint.HasValue) { point = SimAimPoint.Value; return true; }` first.
Desktop and touch behaviour are byte-identical when both are null.

**D1 — State + navigation.** `GreyspaceScene` gains `public static GreyspaceScene Instance`, `Snapshot()`, `DebugGoHub()`, `DebugGoMission(string id)` (uses `ContentLibrary.Mission`, then the same inline fallback data as `SpawnPortals`). `Debug/GreyspaceSnapshot.cs` + `Debug/GreyspaceDebug.cs` as v1 D1, with `enemiesAlive = EnemyBase.Active.Count + GunEnemy.Active.Count`.

**D2 — Spawn / grant / god mode / screenshot.** As v1 D2. God mode = `GunCharacter.debugGodMode` checked at the top of `TakeDamage`.

**D3 — ComboBot** drives `InputRouter.SimMoveAxis` (camera-relative, converted to the same (h,v) space `MoveAxis` returns), `InputRouter.SimAimPoint` (target position) and `InputRouter.SignalLight()/SignalHeavy()/SignalDash()` — the exact path a touch player uses. Clears both overrides in `OnDisable`.

**Verification:** compile via NexusHub `greyspace.check` (no Editor needed) or `unity --non-interactive recompile`; behaviour via PlayMode tests once B1 lands, and via `unity command eval 'GreyspaceDebug.State()'` in the See loop.

## E — handed to NexusHub

NexusHub owns: model routing, the bake-off (Laguna S 2.1, Ornith 1.5, Qwen3.8-fixed, Gemma 4, Kolibri-1; koinon locked out), compile gate, scoring (compile gate → rules 0–3 → behaviour 0–4 → style 0–2 → rounds 0–1). Greyspace supplies briefs and the Play-mode half of the gate (A2 + D).

## F — docs

v1 F1–F5 stand, plus **`AGENT-HANDBOOK.md`** (repo root) — the operating manual for any agent (Claude, local model via NexusHub, Linux Claude on 419c).

## X — exit check

As v1 Task X, using `GreyspaceDebug.SetGodMode(true); GreyspaceDebug.GoMission("proving_ground"); GreyspaceDebug.StartAutopilot()` through the See loop.
