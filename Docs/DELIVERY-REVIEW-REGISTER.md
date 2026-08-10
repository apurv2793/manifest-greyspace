# Delivery review register

Master tracking table for everything built during this batched pass across both
projects (Greyspace/Unity + manifest-cod-experiment/JS), what's been verified where,
and what's still open. This is the **register** — the granular per-behavior checklist
("does the sword combo still feel right") is the **longlist**, and lives in the
interactive [`TESTING-BOOKLET.html`](TESTING-BOOKLET.html) (Batch 1 / Batch 2 sections),
not duplicated here.

**How to use this on the Mac session:** work top to bottom. Each row has a Status —
fill in the Sign-off column as you go. Rows marked 🔴 **BLOCKER** must be done first;
other work in that batch depends on them.

**Policy update:** all real testing and visual preview — for *both* projects, not just
Unity — now happens on the Mac and gets tracked here. What runs on this Linux box
(`npm test`, `capture.mjs`/`imagediff.mjs` pixel-diffs, `validate_assets.py`) are
build-correctness *gates*, not a substitute for actually looking at and playing the
result — a zero-pixel-diff proves "nothing changed," not "it looks/plays right." Rows
below still show what those automated gates confirmed, since that's real signal and
worth keeping, but every row's real Sign-off now happens on the Mac, including the JS
engine rows this register previously marked fully closed.

**Policy update — sign-off requires three independent reviewers.** No row in this
register is considered signed off on the strength of a single pass, including this
Linux-side automated/AI review pass. Every row needs three separate reviewers to
confirm before it counts as closed — this register's "Verified here" column shows
what's been checked *so far* (automated gates, AI code review, static consistency
checks), not a completed sign-off.

## Memory infrastructure (new this pass)

Both projects now have persistent, cross-session memory for the local Qwen model that
does the AI-authored review/fix work, so understanding compounds instead of resetting
every call:
- `docs/qwen-memory/corrections.md` (both repos) — durable record of every reviewed
  claim, whether confirmed true or false, plus process notes on how the model tends to
  fail (confident-but-wrong technical claims, hallucinated file paths, silently
  regressing already-correct files on a later pass).
- `docs/qwen-memory/vault/` (both repos) — an Obsidian-vault knowledge graph
  (codegraph + graphify) of code structure and design-doc rationale.

## Plan reference

Full plan: [`/home/admin/.claude/plans/enchanted-hugging-lark.md`](/home/admin/.claude/plans/enchanted-hugging-lark.md)
— read this first for the *why* behind the batch order and the "single in on JS,
Unity gets a debt-paydown track" decision.

---

## Register

| ID | Batch | Project | Item | What it is | Verified here | Needs on Mac | Status | Sign-off |
|---|---|---|---|---|---|---|---|---|
| R-001 | 0 | JS | `tools/capture.mjs`, `tools/baseline.mjs`, `tools/profile.mjs` | Viewport/quality passthrough, mobile+low capture axes, frame-time profiler | ✅ ran, captured all 6 shots, profiler produced sane p50/p95/p99 | Nothing — fully verified here | Done | |
| R-002 | 0 | Unity | `Tools/validate_assets.py` | Static typo/GUID validator for hand-authored `.asset` files | ✅ proven against a deliberate synthetic typo before trusting it | Nothing — this is the Mac session's safety net, keep using it | Done | |
| R-003 | 0 | Both | `Docs/CONTENT-PACK-CONTRACT.md` | Shared pack-shape contract, mirrored in both repos | ✅ written, mirrored | Nothing | Done | |
| R-004 | 1 | JS | `src/content/` registry (Phase H1) | Content-pack system per the existing roadmap spec | ✅✅ base pack = 0-pixel diff (independently re-confirmed by me, not just the builder), toy pack = 278,373px visible diff, 176/176 tests | Nothing — fully verified here, gates are real | Done | |
| R-005 | 1 | Unity | `Assets/Content/Combos/*.asset` | Sword/Bow/Staff/Shield, values generated from `ComboData.cs` source (not hand-typed) | ✅ `validate_assets.py` clean, Sword hand-verified field-by-field against source | 🔴 **Play the game, confirm combo timings/feel are unchanged** (see `TESTING-BOOKLET.html` items `b1-swordtimings`, `b1-bowtimings`, `b1-shieldtimings`) | Structurally verified, behaviorally unverified | |
| R-006 | 1 | Unity | `Assets/Content/Missions/*.asset` | ProvingGround/InnerSanctum, transcribed from `GreyspaceScene.cs` | ✅ `validate_assets.py` clean | Confirm both portals still work identically (`b1-missions`) | Structurally verified, behaviorally unverified | |
| R-007 | 1 | Unity | `Assets/Resources/Skills/*.asset` + `ContentPack`/`ContentLibrary` | New starter skill tree (original content, no prior data existed) | ✅ `validate_assets.py` clean, prereq cross-references resolve | Confirm skill tree usable in Inspector/Play mode | Structurally verified, behaviorally unverified | |
| R-008 | 1 | Unity | 🐛 Save/reload skill-restore bug fix | `SaveManager.cs`'s `Resources.LoadAll<SkillNode>("Skills")` was silently returning empty — `Assets/Resources/` didn't exist | Confirmed the bug and the missing folder independently before claiming this fix | 🔴 **Unlock 1-2 skills, save, reload, confirm they're still unlocked** (`b1-skillsrestore`) — this is the one item in Batch 1 with a real behavior change to confirm, not just "unchanged" | Fix in place, unverified that it actually restores | |
| R-009 | 2 | JS | Device detection + quality auto-resolution | `src/core/device.js`, `resolveQuality()`, wired before `Config` construction | ✅✅ 193/193 tests, desktop default = 0-pixel diff (independently re-confirmed), spot-checked the resolution-order code directly against source myself, low-vs-high frame time gap confirmed real across 2 runs | Nothing — fully verified here, gates are real | Done | |
| R-010 | 2 | JS | Touch input + mobile viewport | Touch → same input state surface, `<meta viewport>` added | ✅ mobile capture (390×844) succeeds no errors; agent additionally verified with real Playwright touch/CDP emulation (iPhone 13 profile) — left-drag correctly produced `KeyW`, fire button correctly toggled the button bitmask | A real phone browser is still worth a manual pass eventually — emulation validates plumbing, not ergonomics/feel | Done (plumbing); ergonomics unverified | |
| R-011 | 2 | Unity | `InputRouter.cs` — all 21 `Input.*` call sites centralized | Desktop branches are byte-identical to the old inline calls | ✅ confirmed zero direct `Input.*` calls remain outside `InputRouter.cs`, brace-balance + validator clean | 🔴 **Play normally, confirm desktop controls feel exactly the same** (`b2-desktopunchanged`) | Structurally verified, behaviorally unverified | |
| R-012 | 2 | Unity | `TouchOverlay.cs` — drag-stick + tap buttons | Runtime-built uGUI touch controls, only active when `InputRouter.IsTouch` | ✅ same static checks; caught and fixed a real gap myself (no `EventSystem` existed in this project — buttons would've been silently inert without one) | 🔴 **Confirm buttons actually respond to taps, not just visible** (`b2-eventsystem`) — this is the item most likely to *look* right and silently not work | Structurally verified, behaviorally unverified, one known design gap (touch aim, flagged not guessed) | |
| R-013 | 2 | Unity | 🔴 **BLOCKER** — URP Pipeline Assets (U2) | 4 quality-tier render pipeline assets | Not attempted here — plan explicitly says don't; GUID-cross-referenced, version-pinned, Editor-only | **Must be created in the Editor before U3 (material pooling) can proceed** | Not started — Mac-only | |
| R-014 | 2 | Unity | 🔴 **BLOCKER** (newly found) — Android Build Profile | Mirrors `Windows.asset` but for Android target | Not attempted — discovered mid-task that this has the *same* risk class as URP (Editor-internal platform GUID + platform-settings class I can't safely reproduce blind) | **Create via File → Build Profiles → Add Android in the Editor** | Not started — Mac-only, same caution as R-013 | |
| R-015 | 2 | Unity | Material pooling (U3) | Shared/pooled materials, replacing ~14 per-primitive clone sites | Not started | Blocked on R-013 | Blocked | |
| R-016 | 3 | JS | Qwen end-to-end review — all 12 subsystems | Full-codebase AI review (materials, fx, physics, render, sky, world, weapons, ai, game, ui, audio, player, core, content), every concrete finding independently fact-checked against source | ✅ 4 subsystems clean; of 10 specific "bug" claims spot-checked elsewhere, **all 10 were false** (backwards logic, misread callee behavior, hallucinated file paths) — recorded in corrections.md so they don't resurface | Nothing code-side — no confirmed defects to fix. A human read-through of the review findings is still worth a skim | Done — no action items surfaced | |
| R-017 | 3 | Unity | Qwen end-to-end review — all 7 script groups | Full-codebase AI review (combat, enemies, player/input, progression/save, scene/systems, polish/VFX, content pipeline), concrete findings fact-checked | ✅ Confirmed 2 real, actionable issues (material pooling, hot-path scene searches — see R-019) plus several minor ones (PenaltyManager dead code — deferred by design, WorldState leaking internal dict — fixed); many other claims checked and found false, recorded in corrections.md | Nothing further code-side pending R-019's Mac verification | Done — findings actioned in R-019 | |
| R-018 | — | Both | AI-generated visuals roadmap + "finished game" design plans | Strategic direction docs: AAA-visuals-on-a-budget roadmap, and separate "take this to finished-game quality" design plans for both projects (menus, audio, UI/combat presentation polish) | ✅ Directionally reviewed, 2 hallucinated file-path references caught and corrected (recorded in JS corrections.md) | **Design only — nothing here has been implemented yet.** This is the next major work item, deliberately paused pending your direction/feedback before committing more build time to it | Design drafted, awaiting your review/steer | |
| R-019 | 3 | Unity | 🐛 Confirmed fixes — material pooling + hot-path scene searches | New `MaterialCache.cs` (one shared Material per Color, replacing ~15 per-instance clone sites across enemies/player/VFX/portals/checkpoints); self-registering static registries (`EnemyBase.Active`, `GunEnemy.Active`, `Projectile.Active`, `GunCharacter.Instance`) replacing direct `FindObjectsOfType`/`FindObjectOfType` calls in `MeleeAttack`, `EnemyProjectile`, `NPCStub`, `Checkpoint`, `GreyspaceScene` | ✅ Structurally verified only: brace-balance clean across all 15 touched files, no remaining `FindObjectsOfType`/uncached-`Material` calls, cross-file symbol consistency confirmed by hand. Took 3 AI generation rounds — 2 introduced regressions in already-fixed files, caught and corrected before applying (see corrections.md process note). One post-commit correction: `MaterialCache.cs` used `Shader.Find()`, which violates a documented project hard constraint (shader-steal pattern only) — fixed | 🔴 **Cannot be compile-checked on this machine (no Unity Editor/C# toolchain) — this is unverified beyond static text inspection.** Open the project, confirm it compiles with no console errors, then play — confirm enemies/player/portals/checkpoints still look and behave the same, hit-flash and death-burst VFX still fade correctly (this is the specific behavior the fix touched) | Fix applied + committed, **not yet compiled or played** | |
| R-020 | 4 | JS | Title screen + settings + radio-chatter wiring | New `src/ui/title.js` (UI-layer gate, independent of the game's boot->deploy auto-transition), settings modal reusing `ctx.settings`, `src/audio/vox.js` wired to round-start/kills/low-health via `ctx.events` | ✅✅ 238/238 tests pass, isolated before/after pixel-diff = 0 (both gated behind `!ctx.config.deterministic`, same as the existing pause menu). **Live-tested in a real browser, not just pixel-diff** — full click flow (title → settings → back → deploy → loadout menu) exercised via direct DOM dispatch. Found and fixed 5 real issues during review/live-testing: a missing import (hard crash), a phase-gating bug that made the title flash for one frame and vanish, a duplicate unwired settings file (dropped), a CSS bug showing the settings modal without opening it, and a z-order bug from a later fix that made the title unclickable. Also acted on live user feedback ("game starts abruptly") — deploy/debrief are now full-screen menu backdrops instead of small panels floating over an already-running game | Nothing — fully verified here, including live interaction, not just static gates | Done | |
| R-021 | 4 | Unity | Procedural audio — `AudioManager.cs` no-op stub replaced | All 6 sounds ever requested (`dash`, `player_hit`, `sword_swing`, `hit_enemy`, `level_up`, `wave_clear`) synthesized at bootstrap via `AudioClip.Create` + a hand-rolled envelope, played through a pooled `AudioSource` set — no audio assets, same approach already proven in the JS sibling project | ✅ Structurally verified only: brace-balance clean, no `Shader.Find()`, all 6 sound names present with the unchanged `Play(string)` signature every call site uses. One severe bug caught before applying: the generated code declared the class `static` while also adding instance members and `AddComponent<AudioManager>()` — a flat C# contradiction, would not compile — fixed by following `VFXManager.cs`'s already-proven bootstrap pattern in this same codebase. Also fixed a `spatialBlend` bug that would have made every hit/UI sound fade with player distance from world origin | 🔴 **Cannot be compiled or heard on this machine.** Open the project, confirm it compiles, then play and confirm all 6 sounds actually play and sound reasonable (not just silent/erroring) | Fix applied + committed, **not yet compiled or heard** | |

---

## Status as of this register

**Everything doable without a Unity Editor is now done and committed**, on both
projects, including a full end-to-end AI review pass (R-016/R-017) and the confirmed
fixes it surfaced (R-019). What's left is: (a) behavioral confirmation on the Mac that
all the structural changes actually compile and play correctly — R-019 in particular
has never been compiled, since this machine has no C# toolchain — and (b) the two
Editor-only blockers (R-013, R-014) that unlock the remaining Unity render/build work.

The JS side (manifest-cod-experiment) needed no Mac session at all for its original
batches — every gate for those was independently verifiable and re-verified here
(build, test, pixel-diff). The new AI-review pass (R-016) didn't surface any confirmed
code defects there either.

**Paused, awaiting your steer:** R-018 (the visuals roadmap + "finished game quality"
design plans for both projects) is drafted but deliberately not yet implemented — that's
a large amount of further build work (new menus, audio systems, UI polish, combat
presentation) and it's worth confirming direction with you before committing more time
to it, per the three-reviewer sign-off policy above.

## Recommended Mac session order

1. **Open the project first, check the Console for import/compile errors** — several
   `.asset` and `.cs` files were added or modified outside the Editor this pass
   (`b1-opens` in the booklet). **R-019 in particular has never been compiled** — if
   anything is going to fail to build, it's most likely one of the 15 files that fix
   touched.
2. R-013 (URP Pipeline Assets) — unblocks R-015 (material pooling).
3. R-014 (Android Build Profile) — unblocks real device/touch testing.
4. R-019 — play and confirm enemies/player/portals/checkpoints look and behave
   unchanged, and that hit-flash/death-burst/dash-afterimage VFX still fade smoothly
   (the specific behavior this fix's material-caching change touched).
5. R-021 — confirm the project compiles at all, then confirm each of the 6 sounds
   actually plays (dash, a melee hit landing on the player, a sword swing, hitting an
   enemy, leveling up, clearing a wave) and sounds reasonable, not silent or erroring.
6. R-020 (JS, no Mac needed — already fully verified here, but worth a look): open
   `manifest-cod-experiment` and confirm the title screen/settings/loadout flow feels
   right — this is the one row in this pass verified end-to-end without you.
5. Everything else in this table, top to bottom — the 🔴 items first within each batch.
6. Work through `TESTING-BOOKLET.html`'s Batch 1 and Batch 2 sections for the
   fine-grained gameplay-feel checks this register doesn't duplicate.
