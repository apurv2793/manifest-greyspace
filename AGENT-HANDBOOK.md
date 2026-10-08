# Greyspace — Agent Handbook

Read this before changing anything. It applies to every agent: Claude in Claude Code,
local models routed by NexusHub, and Linux Claude on 419c.

## 1. What this repo is

An isometric action game in **Unity 6000.4.11f1, URP 17.0.3**, orthographic camera.
Everything visible is still built from code primitives; real art arrives later via the
Manifest 3D pipeline (see `Docs/GREYSPACE-CHARTER.md` §5).

- **Canonical copy:** `/Users/apurv2793/Manifest main/manifest-greyspace-clean` on the Mac
  (symlink `~/greyspace`, use it — the real path has a space). GitHub:
  `apurv2793/manifest-greyspace`, branch `main`.
- **Not canonical — never edit:** `/Users/apurv2793/Manifest main/manifest-greyspace`
  (old copy, no git) and the nested scratch projects `ManifestGreyspace/`, `My project/`,
  `Sample/` inside the canonical folder (gitignored). `manifest-unity-game` is a different
  project (Mara 3D capture), not a Greyspace copy.

## 2. Who decides what

| Role | Who |
|---|---|
| Vision, pillars, art/story calls, anything legal (Unity terms, licences, sign-ins) | **Apurv (owner)** |
| Architecture, coupled systems (combat feel, camera, lighting), visual judgement, final review | **Opus 5.5** (Claude Code on the Mac) |
| Model routing, bake-off, compile gate, maker/checker rounds | **NexusHub** (`http://127.0.0.1:3200`) |
| Bounded drafts: single-file C#, tests, `.asset` data, lint, log triage | **Local models via NexusHub** |
| Script authoring on 419c | **Linux Claude** — writes code, never verifies it (no Unity on ARM) |

Escalation: a task that fails 3 rounds at one tier moves up (local → GLM 5.1 → Opus).
`qwen3-koinon-t1` is a trading classifier — never used for code (NexusHub enforces this).

## 3. Read in this order

1. `Docs/GREYSPACE-CHARTER.md` — goal, quality bar (≥ 8.2 on the frontier-games rubric), pillars, decisions.
2. `Docs/superpowers/plans/2026-10-09-phase-0-foundation-v2.md` — current plan + status table.
3. `Docs/TOOLING.md` — Unity CLI / MCP setup and gotchas.
4. `Docs/COORDINATION.md` — older Manifest OS coordination log (history, still valid rules).
5. `Docs/DELIVERY-REVIEW-REGISTER.md` — review findings log.

## 4. Hard code rules (a violation fails review)

- No namespaces.
- No `Shader.Find()`. Materials only via `MaterialCache.Get(color)`.
- Movement: `transform.position +=` only. No Rigidbody, CharacterController or physics queries.
- **Input only through `InputRouter`** (`MoveAxis()`, `LightPressed()`, `HeavyPressed()`, `DashPressed()`,
  `SwapPressed()`, `InteractPressed()`, `SpecialPressed()`, `RetryPressed()`, `TryAimWorldPoint()`).
  `InputRouter.cs` is the only file allowed to touch `UnityEngine.Input`.
- Player: `GunCharacter.Instance`. Enemies: `EnemyBase.Active`, `GunEnemy.Active`, `Projectile.Active`.
  No `FindObjectsOfType` / `FindWithTag` in per-frame code.
- Content: `ContentLibrary.Mission(id)` / `ContentLibrary.Combo(name)` first, inline fallback second.
- Geometry: `GameObject.CreatePrimitive()`, remove its Collider, `SetParent(parent, false)`.
- Timers that must run during hit-stop (`Time.timeScale = 0`): `WaitForSecondsRealtime`.
- Never toggle a light's `enabled`/`SetActive` to dim it — change `intensity` (visible-light count is a shader key).
- Every new asset ships with its `.meta` file in the same commit.

## 5. The work loop

1. **Brief** — one task, one file where possible, with acceptance checks written down.
2. **Draft** — local model via NexusHub `greyspace.unity_code`, or Opus for coupled/architectural work.
3. **Check** — NexusHub `greyspace.check` (house-rule lint + real compile against the project, no Editor needed):
   `POST /v1/services/greyspace.check` with `{"input":{"files":[{"name":"X.cs","code":"…"}]}}`.
   Key: `/Users/apurv2793/Claude only/NexusHub/data/service-keys/greyspace.check.key`.
4. **Review** — Opus reads the diff. Code that compiles can still be wrong: past silent bugs include
   melee missing 3 of 4 enemy types, Stalkers giving 0 XP, a missing `[Serializable]` making save files `{}`.
5. **Play** — on the Mac: open the project, play it, look at it (§6). A screenshot alone never closes a task.
6. **Commit** — small commits, house message style, `Co-Authored-By` trailer. Push only with the owner's say-so.

## 6. Driving Unity (Mac only)

Always `cd ~/greyspace` and pass `--non-interactive` (otherwise the CLI can hang on prompts).

```bash
unity --non-interactive open ~/greyspace          # launches the right Editor version
unity --non-interactive pipeline list             # Server Reachable must be true before commands work
unity --non-interactive status
unity --non-interactive recompile                 # compile + report errors
unity --non-interactive test --mode EditMode      # junit report + exit code
unity --non-interactive commands --json           # live command list (beta — names change)
```

In-game debug commands (`Assets/Scripts/Debug/GreyspaceDebug.cs`), callable via `unity command eval`:

| Call | Does |
|---|---|
| `GreyspaceDebug.State()` | JSON snapshot: mode, mission, wave, HP, XP, enemies alive, fps |
| `GreyspaceDebug.GoHub()` / `GoMission("proving_ground")` | navigate |
| `GreyspaceDebug.SpawnEnemy("charger", x, z)` | stalker \| charger \| ranged \| shielder |
| `GreyspaceDebug.GrantSkillPoints(n)` / `SetGodMode(true)` | player tweaks |
| `GreyspaceDebug.Screenshot("/abs/path.png")` | Game-view capture |
| `GreyspaceDebug.StartAutopilot()` / `AutopilotReport()` / `StopAutopilot()` | `ComboBot` plays through `InputRouter` |

## 7. Working from 419c (Linux Claude)

- Repo at `~/manifest-greyspace-clean`. `git pull --rebase` before starting; it drifts.
- Unity cannot run there (ARM Linux, no Editor build). You may write and push C#, but say clearly
  in the commit/PR that it is **unverified**; the Mac verifies.
- Don't add new assets/folders without `.meta` files unless you flag it — the Mac must open the
  project and commit the generated `.meta` files, or asset references break.
- Local models: `.env` sets `OLLAMA_MODEL=qwen3.8-fixed:27b` until the bake-off picks a winner.

## 8. Known traps (learned the hard way)

- **Input System package:** Player Settings use "Both" input handlers, which defines `ENABLE_INPUT_SYSTEM`.
  Without `com.unity.inputsystem` installed, packages that check that define fail to compile
  (this broke `com.unity.pipeline`). The package is installed (1.19.0); don't remove it.
- **Claude desktop app + Unity MCP:** the app doesn't read `~/.zshrc`; the MCP entry must use the
  absolute path `/Users/apurv2793/.unity/bin/unity`.
- **Unity licence:** the Editor needs Hub signed in with an active Personal licence (active since
  2026-06-14). If `Editor.log` says "No valid Unity Editor license found", tell the owner to check Hub —
  it was transient on 2026-10-09 while Hub re-synced. Agents never click through Unity Hub.
- **Run in Background must stay on** (`PlayerSettings.runInBackground = true`, set 2026-10-09). With it
  off, Play mode freezes whenever the Editor window isn't focused — automated runs then see frame
  count stuck and the bot never moves.
- **CLI file paths must be under the REAL project path** (`/Users/apurv2793/Manifest main/manifest-greyspace-clean/...`),
  not the `~/greyspace` symlink — e.g. `capture_game_view --save_path` rejects symlinked paths as "outside the project root".
- **Charger / Ranged enemies have no stat defaults** (health 0). Mission assets set them; anything
  spawning them in code must set `health`, `speed`, `attackDamage`, `xpValue`.
- **ShielderEnemy hides `EnemyBase.Start()`** with its own `Start()`, so Shielders get no HP bar. Open bug.
- **`GreyspaceScene.ClearEnemies()` skips `EnemyBase.Active`**, so Charger/Ranged/Shielder survive a mission replay. Open bug (fixed by plan v2 Workstream C).
- **Models over-trust plausible code.** Static checks passed code that did not compile. The
  compile gate is required.

## 9. Never

- Never enter credentials, sign in, or accept terms on the owner's behalf.
- Never edit the non-canonical copies or the cloned reference repos.
- Never force-push or rewrite `main`'s history.
- Never commit `.env` files or keys.
- Never use `qwen3-koinon-t1` for code.
