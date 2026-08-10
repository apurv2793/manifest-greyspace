# Delivery review register

This is a **living testing-feedback document**, not a one-time checklist. It tracks
everything built across both projects (Greyspace/Unity + manifest-cod-experiment/JS),
what's been verified where, and — the part that makes it a feedback loop, not just a
log — what you found when you actually tested each item and what that turns into for
the *next* plan/build cycle. This file itself is meant to persist across cycles: it
gets folded into `docs/qwen-memory/vault/` (both repos) via `graphify --update` and
re-indexed by `codegraph` after each round, so Qwen's own memory of "what was actually
tested and what broke" compounds instead of resetting. The granular per-behavior
checklist ("does the sword combo still feel right") is the **longlist**, and lives in
the interactive [`TESTING-BOOKLET.html`](TESTING-BOOKLET.html), not duplicated here.

## How to use this — the feedback loop

**Two separate testing tracks, at the bottom of this doc:** [JS Testing Track](#js-testing-track--do-this-now-no-mac-needed)
(do it now, in a browser, no Mac needed) and [Unity Testing Track](#unity-testing-track--mac-session-required)
(needs the Mac/Editor). Each is a self-contained, ordered walkthrough — start there if
you just want a checklist to run through. The Register table below is the underlying
detail each track's steps point back to.

1. Work the **Register** table top to bottom. Rows marked 🔴 **BLOCKER** first —
   other work in that batch depends on them.
2. For each row, actually test it (Editor, Play mode, or the JS build), then fill in
   its **Result** in the table using the legend below, and jump to that row's entry in
   **Test Feedback Log** to write what you actually saw — even a one-liner. This is the
   part a blank "Sign-off" column never captured: *why* it passed or failed, not just
   that it did.
3. Anything you mark 🟡 Partial or ❌ Fail automatically becomes a candidate for
   **Next Cycle Backlog** — I turn your notes there into scoped batches (Qwen writes,
   I orchestrate/verify) the same way every row in this register got built, and this
   file gets updated again at the end of that cycle. You don't need to file it
   anywhere else — the note you leave here *is* the input to the next cycle.

**Result legend:** ✅ Pass · ❌ Fail · 🟡 Partial (works but with issues) · ⬜ Not tested yet

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

| ID | Batch | Project | Item | What it is | Verified here | Needs on Mac | Status | Result |
|---|---|---|---|---|---|---|---|:---:|
| R-001 | 0 | JS | `tools/capture.mjs`, `tools/baseline.mjs`, `tools/profile.mjs` | Viewport/quality passthrough, mobile+low capture axes, frame-time profiler | ✅ ran, captured all 6 shots, profiler produced sane p50/p95/p99 | Nothing — fully verified here | Done | ✅ |
| R-002 | 0 | Unity | `Tools/validate_assets.py` | Static typo/GUID validator for hand-authored `.asset` files | ✅ proven against a deliberate synthetic typo before trusting it | Nothing — this is the Mac session's safety net, keep using it | Done | ✅ |
| R-003 | 0 | Both | `Docs/CONTENT-PACK-CONTRACT.md` | Shared pack-shape contract, mirrored in both repos | ✅ written, mirrored | Nothing | Done | ✅ |
| R-004 | 1 | JS | `src/content/` registry (Phase H1) | Content-pack system per the existing roadmap spec | ✅✅ base pack = 0-pixel diff (independently re-confirmed by me, not just the builder), toy pack = 278,373px visible diff, 176/176 tests | Nothing — fully verified here, gates are real | Done | ✅ |
| R-005 | 1 | Unity | `Assets/Content/Combos/*.asset` | Sword/Bow/Staff/Shield, values generated from `ComboData.cs` source (not hand-typed) | ✅ `validate_assets.py` clean, Sword hand-verified field-by-field against source | 🔴 **Play the game, confirm combo timings/feel are unchanged** (see `TESTING-BOOKLET.html` items `b1-swordtimings`, `b1-bowtimings`, `b1-shieldtimings`) | Structurally verified, behaviorally unverified | ⬜ |
| R-006 | 1 | Unity | `Assets/Content/Missions/*.asset` | ProvingGround/InnerSanctum, transcribed from `GreyspaceScene.cs` | ✅ `validate_assets.py` clean | Confirm both portals still work identically (`b1-missions`) | Structurally verified, behaviorally unverified | ⬜ |
| R-007 | 1 | Unity | `Assets/Resources/Skills/*.asset` + `ContentPack`/`ContentLibrary` | New starter skill tree (original content, no prior data existed) | ✅ `validate_assets.py` clean, prereq cross-references resolve | Confirm skill tree usable in Inspector/Play mode | Structurally verified, behaviorally unverified | ⬜ |
| R-008 | 1 | Unity | 🐛 Save/reload skill-restore bug fix | `SaveManager.cs`'s `Resources.LoadAll<SkillNode>("Skills")` was silently returning empty — `Assets/Resources/` didn't exist | Confirmed the bug and the missing folder independently before claiming this fix | 🔴 **Unlock 1-2 skills, save, reload, confirm they're still unlocked** (`b1-skillsrestore`) — this is the one item in Batch 1 with a real behavior change to confirm, not just "unchanged" | Fix in place, unverified that it actually restores | ⬜ |
| R-009 | 2 | JS | Device detection + quality auto-resolution | `src/core/device.js`, `resolveQuality()`, wired before `Config` construction | ✅✅ 193/193 tests, desktop default = 0-pixel diff (independently re-confirmed), spot-checked the resolution-order code directly against source myself, low-vs-high frame time gap confirmed real across 2 runs | Nothing — fully verified here, gates are real | Done | ✅ |
| R-010 | 2 | JS | Touch input + mobile viewport | Touch → same input state surface, `<meta viewport>` added | ✅ mobile capture (390×844) succeeds no errors; agent additionally verified with real Playwright touch/CDP emulation (iPhone 13 profile) — left-drag correctly produced `KeyW`, fire button correctly toggled the button bitmask | A real phone browser is still worth a manual pass eventually — emulation validates plumbing, not ergonomics/feel | Done (plumbing); ergonomics unverified | 🟡 |
| R-011 | 2 | Unity | `InputRouter.cs` — all 21 `Input.*` call sites centralized | Desktop branches are byte-identical to the old inline calls | ✅ confirmed zero direct `Input.*` calls remain outside `InputRouter.cs`, brace-balance + validator clean | 🔴 **Play normally, confirm desktop controls feel exactly the same** (`b2-desktopunchanged`) | Structurally verified, behaviorally unverified | ⬜ |
| R-012 | 2 | Unity | `TouchOverlay.cs` — drag-stick + tap buttons | Runtime-built uGUI touch controls, only active when `InputRouter.IsTouch` | ✅ same static checks; caught and fixed a real gap myself (no `EventSystem` existed in this project — buttons would've been silently inert without one) | 🔴 **Confirm buttons actually respond to taps, not just visible** (`b2-eventsystem`) — this is the item most likely to *look* right and silently not work | Structurally verified, behaviorally unverified, one known design gap (touch aim, flagged not guessed) | ⬜ |
| R-013 | 2 | Unity | 🔴 **BLOCKER** — URP Pipeline Assets (U2) | 4 quality-tier render pipeline assets | Not attempted here — plan explicitly says don't; GUID-cross-referenced, version-pinned, Editor-only | **Must be created in the Editor before U3 (material pooling) can proceed** | Not started — Mac-only | ⬜ |
| R-014 | 2 | Unity | 🔴 **BLOCKER** (newly found) — Android Build Profile | Mirrors `Windows.asset` but for Android target | Not attempted — discovered mid-task that this has the *same* risk class as URP (Editor-internal platform GUID + platform-settings class I can't safely reproduce blind) | **Create via File → Build Profiles → Add Android in the Editor** | Not started — Mac-only, same caution as R-013 | ⬜ |
| R-015 | 2 | Unity | Material pooling (U3) | Shared/pooled materials, replacing ~14 per-primitive clone sites | Not started | Blocked on R-013 | Blocked | ⬜ |
| R-016 | 3 | JS | Qwen end-to-end review — all 12 subsystems | Full-codebase AI review (materials, fx, physics, render, sky, world, weapons, ai, game, ui, audio, player, core, content), every concrete finding independently fact-checked against source | ✅ 4 subsystems clean; of 10 specific "bug" claims spot-checked elsewhere, **all 10 were false** (backwards logic, misread callee behavior, hallucinated file paths) — recorded in corrections.md so they don't resurface | Nothing code-side — no confirmed defects to fix. A human read-through of the review findings is still worth a skim | Done — no action items surfaced | ✅ |
| R-017 | 3 | Unity | Qwen end-to-end review — all 7 script groups | Full-codebase AI review (combat, enemies, player/input, progression/save, scene/systems, polish/VFX, content pipeline), concrete findings fact-checked | ✅ Confirmed 2 real, actionable issues (material pooling, hot-path scene searches — see R-019) plus several minor ones (PenaltyManager dead code — deferred by design, WorldState leaking internal dict — fixed); many other claims checked and found false, recorded in corrections.md | Nothing further code-side pending R-019's Mac verification | Done — findings actioned in R-019 | ✅ |
| R-018 | — | Both | AI-generated visuals roadmap + "finished game" design plans | Strategic direction docs: AAA-visuals-on-a-budget roadmap, and separate "take this to finished-game quality" design plans for both projects (menus, audio, UI/combat presentation polish) | ✅ Directionally reviewed, 2 hallucinated file-path references caught and corrected (recorded in JS corrections.md) | **Design only — nothing here has been implemented yet.** This is the next major work item, deliberately paused pending your direction/feedback before committing more build time to it | Design drafted, awaiting your review/steer | ⬜ |
| R-019 | 3 | Unity | 🐛 Confirmed fixes — material pooling + hot-path scene searches | New `MaterialCache.cs` (one shared Material per Color, replacing ~15 per-instance clone sites across enemies/player/VFX/portals/checkpoints); self-registering static registries (`EnemyBase.Active`, `GunEnemy.Active`, `Projectile.Active`, `GunCharacter.Instance`) replacing direct `FindObjectsOfType`/`FindObjectOfType` calls in `MeleeAttack`, `EnemyProjectile`, `NPCStub`, `Checkpoint`, `GreyspaceScene` | ✅ Structurally verified only: brace-balance clean across all 15 touched files, no remaining `FindObjectsOfType`/uncached-`Material` calls, cross-file symbol consistency confirmed by hand. Took 3 AI generation rounds — 2 introduced regressions in already-fixed files, caught and corrected before applying (see corrections.md process note). One post-commit correction: `MaterialCache.cs` used `Shader.Find()`, which violates a documented project hard constraint (shader-steal pattern only) — fixed | 🔴 **Cannot be compile-checked on this machine (no Unity Editor/C# toolchain) — this is unverified beyond static text inspection.** Open the project, confirm it compiles with no console errors, then play — confirm enemies/player/portals/checkpoints still look and behave the same, hit-flash and death-burst VFX still fade correctly (this is the specific behavior the fix touched) | Fix applied + committed, **not yet compiled or played** | ⬜ |
| R-020 | 4 | JS | Title screen + settings + radio-chatter wiring | New `src/ui/title.js` (UI-layer gate, independent of the game's boot->deploy auto-transition), settings modal reusing `ctx.settings`, `src/audio/vox.js` wired to round-start/kills/low-health via `ctx.events` | ✅✅ 238/238 tests pass, isolated before/after pixel-diff = 0 (both gated behind `!ctx.config.deterministic`, same as the existing pause menu). **Live-tested in a real browser, not just pixel-diff** — full click flow (title → settings → back → deploy → loadout menu) exercised via direct DOM dispatch. Found and fixed 5 real issues during review/live-testing: a missing import (hard crash), a phase-gating bug that made the title flash for one frame and vanish, a duplicate unwired settings file (dropped), a CSS bug showing the settings modal without opening it, and a z-order bug from a later fix that made the title unclickable. Also acted on live user feedback ("game starts abruptly") — deploy/debrief are now full-screen menu backdrops instead of small panels floating over an already-running game | Nothing — fully verified here, including live interaction, not just static gates | Done | ✅ |
| R-021 | 4 | Unity | Procedural audio — `AudioManager.cs` no-op stub replaced | Now 8 sounds total (`dash`, `player_hit`, `sword_swing`, `hit_enemy`, `level_up`, `wave_clear`, plus `death`/`victory` added in R-022) synthesized at bootstrap via `AudioClip.Create` + a hand-rolled envelope, played through a pooled `AudioSource` set — no audio assets, same approach already proven in the JS sibling project | ✅ Structurally verified only: brace-balance clean, no `Shader.Find()`, all sound names present with the unchanged `Play(string)` signature every call site uses. One severe bug caught before applying: the generated code declared the class `static` while also adding instance members and `AddComponent<AudioManager>()` — a flat C# contradiction, would not compile — fixed by following `VFXManager.cs`'s already-proven bootstrap pattern in this same codebase. Also fixed a `spatialBlend` bug that would have made every hit/UI sound fade with player distance from world origin | 🔴 **Cannot be compiled or heard on this machine.** Open the project, confirm it compiles, then play and confirm all 8 sounds actually play and sound reasonable (not just silent/erroring) | Fix applied + committed, **not yet compiled or heard** | ⬜ |
| R-022 | 4 | Unity | Death + mission-complete presentation | `OnPlayerDied()` and the mission-complete block now each play a dedicated sound ("death"/"victory"), spawn a VFX burst (reusing existing `DeathBurst`/`LevelUpBurst` effect types), and trigger `CameraShake` | ✅ Structurally verified only: brace-balance clean, no `Shader.Find()`/namespaces, all reused method calls checked against real signatures. One orchestrator error caught: `CameraShake.Shake()`'s real parameter order is `(intensity, duration)`, a build prompt stated it backwards — the actual call-site values still land in a reasonable range either way (compared to existing usage elsewhere), not worth a regeneration, but recorded in corrections.md. **Superseded by R-027 below**, which found and properly fixed the "ends abruptly" imperfection this row flagged as cosmetic — it was actually one of four real click/pop bugs | 🔴 **Cannot be compiled or heard here.** Play, die once and win once, confirm both moments have real weight (sound + visual burst + shake) | Fix applied + committed, **not yet compiled or heard** | ⬜ |
| R-023 | 5 | Both | Independent reviewer pass — nemotron (NIM) + Fable (end-to-end) | Two more independent AI models (neither Qwen, neither Claude) reviewed everything built so far, per the "three independent reviewers before sign-off" policy. Fable additionally ran its own verification (executed `npm test` itself, grepped source to confirm claims before reporting them) rather than reasoning from a diff alone | nemotron: 10 findings, spot-checked all 10 by hand — 1 real (the Vector2 regression, long since fixed), 9 false. Fable: markedly higher hit rate — every concrete claim it made checked out true on independent re-verification (see R-024 through R-029). Both repos' `docs/qwen-memory/corrections.md` updated with the full results so nothing here needs re-checking later | Still pending, deferred by your own instruction: **Kimi CLI** (Mac, after 2pm) and **Laguna** (Mac, once its backlog clears, ~2 days out) — not yet gathered | Done — findings actioned in the rows below | ✅ |
| R-024 | 5 | Unity | 🔴 **CRITICAL — 3 compile blockers fixed** | The project would **not have compiled** on the Mac as committed, before this fix: (1) `Mat()` was deleted from `EnemyBase.cs`/`WeaponBase.cs` in an earlier round, but `RangedEnemy.cs`, `ChargerEnemy.cs`, `ShielderEnemy.cs` — files that round never touched — still called it; (2) `MaterialCache.cs` had an unqualified `DestroyImmediate()` call inside a `static class` with no way to resolve it; (3) `AudioManager.cs`'s victory-sound generator tried to mark a value `const` that isn't a compile-time constant | Verified true by grep before touching anything — confirmed `Mat(` calls existed with no matching definition anywhere, confirmed the missing qualifier, confirmed the non-constant `const` line | 🔴 **This is the first thing to check on the Mac.** Open the project, look at the Console — confirm zero red compile errors before doing anything else | Fixed + committed (`6fe78ed`), **not yet compiled on the actual Editor** | ⬜ |
| R-025 | 5 | Unity | 🐛 Checkpoint material cache-poisoning fix | Activating one checkpoint was silently recoloring **every other inactive checkpoint** in the level — the "activated" color change was applied to the shared pooled material instead of swapping to a separate one | Verified true by reading the actual code (the mutation was there exactly as claimed); fix swaps to a distinct cached material on activation instead of recoloring in place | 🔴 If your test level has 2+ checkpoints: activate one, confirm the others stay their original (inactive) color instead of also turning gold | Fixed + committed, **not yet played** | ⬜ |
| R-026 | 5 | Unity | 🐛 Hit-flash pooling-defeat fix | The "flash white briefly when hit" effect was silently undoing the material-pooling optimization from R-019 the moment anything took a hit (Unity auto-clones a material the first time code reads/writes `.material` instead of `.sharedMaterial`) | Verified true against Unity's own documented `Renderer.material` behavior | Confirm hit-flash still looks the same (brief white flash, reverts cleanly) — if you have time, also check the Profiler's material count doesn't keep climbing through a long fight, which is what this was actually fixing | Fixed + committed, **not yet played** | ⬜ |
| R-027 | 5 | Unity | 🐛 Audio envelope/gate mismatch + victory-chord clipping fix | 4 of the 8 procedural sounds (death, level-up, wave-clear, victory's rising arpeggio) were getting cut off mid-fade instead of finishing their envelope — an audible click or pop on every note. Separately, victory's final held chord (7 notes at once) risked distorting at the loudest moment | Independently re-derived the exact cutoff percentages myself from the actual envelope math before trusting the finding (42%, 86%, 28%, 36% — all four matched exactly); fix generated by Qwen, diff double-checked — exactly 5 lines changed, nothing else touched, all four new envelope timings re-confirmed to actually fit their note windows | 🔴 Listen to all 4: die once (death sting), level up once, clear a wave, and win once — listen specifically to whether the victory ending's final chord sounds clean or distorted | Fixed + committed, **not yet heard** | ⬜ |
| R-028 | 5 | JS | 🐛 Vox misattribution + quality persistence + dead code/CSS cleanup | Kill-streak/"first kill" voice lines were triggering off **anyone's** death in the level, not just your own kills; your graphics-quality choice silently reset every time you reopened the game; a settings-button highlight was unreachable dead code; ~31 lines of dead unused CSS left over from a dropped screen | Verified true against source before fixing all 4; 238/238 automated tests pass after the fix; pixel-baseline re-captured and confirmed byte-identical to what was already committed (zero visual change, as expected) | Nothing — fully verified here, no Mac needed for this one | Done | ✅ |
| R-029 | 5 | JS | 🐛 Kill-streak retrigger + low-health regen-reset fix | The kill-streak sound replays on every kill once you're 3+ kills deep in a 10-second window, instead of once per streak. The low-health warning can go silent after you heal back to full and then take a big hit — because its "reset" only runs on damage events, never on healing | Verified true against source. Fix request prepared and handed to Qwen; **still queued as of this register update**, respecting the 10-minute cooldown between local Qwen calls | Nothing — will be fully verified here once applied, no Mac needed | **In progress — not yet applied.** Will update this row the moment it lands | ⬜ |

---

## Test Feedback Log

One entry per row that still needs real testing (rows already ✅ Done above are skipped
here — nothing to report). Fill in **Notes** after testing; leave **Result** blank here,
it's tracked in the table above — this section is *why*, the table is *what*.

### R-005 — Combo data assets (Sword/Bow/Staff/Shield)
**Test:** Play through each weapon's combo, confirm timings/feel are unchanged from before this pass.
**Notes:** _(your notes here)_

### R-006 — Mission assets (ProvingGround/InnerSanctum)
**Test:** Enter both mission portals, confirm they still work identically to before.
**Notes:** _(your notes here)_

### R-007 — Starter skill tree
**Test:** Confirm the skill tree is usable and displays correctly in Inspector/Play mode.
**Notes:** _(your notes here)_

### R-008 — Save/reload skill-restore fix
**Test:** Unlock 1-2 skills, save, reload, confirm they're still unlocked. This is the one
item in Batch 1 with a real behavior change to confirm, not just "unchanged."
**Notes:** _(your notes here)_

### R-010 — Touch input ergonomics (JS)
**Test:** On an actual phone browser, confirm touch controls feel right — plumbing is
already verified, this is purely a feel check.
**Notes:** _(your notes here)_

### R-011 — InputRouter desktop parity (Unity)
**Test:** Play normally, confirm desktop controls feel exactly the same as before.
**Notes:** _(your notes here)_

### R-012 — TouchOverlay buttons
**Test:** Confirm touch buttons actually respond to taps, not just visible. Known gap:
touch aim isn't wired yet (flagged, not a surprise if missing).
**Notes:** _(your notes here)_

### R-013 — 🔴 URP Pipeline Assets (BLOCKER)
**Test:** Create the 4 quality-tier pipeline assets in the Editor. Unblocks R-015.
**Notes:** _(your notes here)_

### R-014 — 🔴 Android Build Profile (BLOCKER)
**Test:** Create via File → Build Profiles → Add Android in the Editor. Unblocks real
device/touch testing.
**Notes:** _(your notes here)_

### R-015 — Material pooling (Unity, blocked on R-013)
**Test:** Not startable until R-013 lands.
**Notes:** _(your notes here)_

### R-018 — AAA-visuals roadmap + finished-game design plans
**Test:** Not a pass/fail test — this is a direction call. Read the roadmap/design docs
(referenced in corrections.md and this session's chat), tell me what to keep, cut, or
reprioritize.
**Notes:** _(your steer here)_

### R-019 — Material pooling + hot-path search fixes (Unity)
**Test:** Confirm the project compiles with no console errors, then play — confirm
enemies/player/portals/checkpoints look and behave the same, and hit-flash/death-burst/
dash-afterimage VFX still fade correctly (the specific behavior this fix touched).
**Notes:** _(your notes here)_

### R-021 — Procedural audio (Unity)
**Test:** Confirm the project compiles, then confirm each of the 8 sounds actually plays
(dash, a melee hit landing on the player, a sword swing, hitting an enemy, leveling up,
clearing a wave, dying, winning a mission) and sounds reasonable, not silent or erroring.
**Notes:** _(your notes here)_

### R-022 — Death + mission-complete presentation (Unity)
**Test:** Die once and win a mission once. Confirm both moments have real weight (sound,
visual burst, camera shake) instead of just a text swap.
**Notes:** _(your notes here)_

### R-024 — 🔴 3 compile blockers (Unity, CRITICAL)
**Test:** Open the project, check the Console for red compile errors before doing
anything else. If this fails, nothing else in this register can be tested — tell me
immediately and paste the exact error.
**Notes:** _(your notes here)_

### R-025 — Checkpoint cache-poisoning fix (Unity)
**Test:** If your test level has 2+ checkpoints, activate one and confirm the others
keep their original inactive color instead of also turning gold.
**Notes:** _(your notes here)_

### R-026 — Hit-flash pooling-defeat fix (Unity)
**Test:** Get hit a few times (player or enemy), confirm the white flash still looks
right — brief flash, clean revert to the normal color, nothing stuck white or flickering.
**Notes:** _(your notes here)_

### R-027 — Audio envelope/gate mismatch + victory-chord clipping fix (Unity)
**Test:** Die once, level up once, clear a wave, and win once. Listen for clicks/pops
on each, and listen closely to whether victory's final chord sounds clean (not
distorted/crackly).
**Notes:** _(your notes here)_

### R-029 — Kill-streak retrigger + low-health regen-reset fix (JS)
**Test:** Get a 3+ kill streak, confirm the streak sound plays once (not on every
kill after the 3rd). Separately: let health regen back to full, then take a big hit
that drops you straight below 25% — confirm the low-health warning still plays.
**Notes:** _(your notes here — this row will show as in-progress until the fix lands;
I'll let you know when it's ready to test)_

---

## Next Cycle Backlog

Empty for now — this fills in from the Test Feedback Log above once you've actually
tested things. Anything marked 🟡 Partial or ❌ Fail in the Register table becomes a
line here, and I turn it into a scoped batch (Qwen writes, I orchestrate/verify) the
same way this whole pass got built — same rigor, same 10-minute cooldown between
batches, same fact-checking against real source before anything gets applied.

---

## Status as of this register

**Everything doable without a Unity Editor is now done and committed**, on both
projects. This pass added a second and third independent review (nemotron, Fable —
R-023) on top of the earlier Qwen self-review (R-016/R-017), and Fable's pass in
particular surfaced real problems: **the Unity project would not have compiled** as
committed (R-024) — now fixed, but genuinely unverified until someone opens it in the
actual Editor. Everything fixed this round (R-024–R-029) is either committed-but-
unverified (Unity — needs the Mac) or committed-and-fully-verified (JS — didn't need
the Mac). One JS fix (R-029) is still in progress as of this register update — I'll
update that row the moment it lands.

The two Editor-only blockers from earlier (R-013, R-014) still stand and still gate
the remaining Unity render/build work.

**Paused, awaiting your steer:** R-018 (the visuals roadmap + "finished game quality"
design plans for both projects) is drafted but deliberately not yet implemented — that's
a large amount of further build work and it's worth confirming direction with you
before committing more time to it.

**Still pending, deferred by you:** Kimi CLI's review (Mac, after 2pm) and Laguna's
review (Mac, ~2 days out) — R-023 will get a follow-up entry once either comes in.

---

## JS Testing Track — do this now, no Mac needed

Everything here runs in a regular browser on this machine. Nothing in this track is
waiting on you to be at your Mac.

**1. Open the game.** From `manifest-cod-experiment/`, run `npm run dev` and open the
   URL it prints. This is the only setup step.

**2. Title screen → Settings → back.** Click through to Settings, change the graphics
   quality to something other than what's selected, close Settings, **fully reload the
   page** (not just close the menu), and reopen Settings — confirm your quality choice
   is still selected. *(This is R-029's sibling fix, R-028 — was broken before this
   pass, should now hold.)*

**3. Play a round, get 3+ kills within about 10 seconds of each other.** Listen for the
   kill-streak voice line — it should play once when the streak starts, not once per
   kill after that. *(R-029 — only testable once that fix lands; I'll flag when it's
   ready.)*

**4. Let your health regen back to full, then take a big hit that drops you straight
   below 25%.** Confirm the low-health warning still plays. *(Also R-029.)* Before this
   fix, a *fresh* drop below 25% right after a full heal could stay silent.

**5. Get a kill during an intense firefight where AI are also killing each other.**
   Confirm the "first kill"/streak vox lines only trigger on *your* kills, not every
   death in the scene. *(R-028 — this was the actual bug: it used to fire off anyone's
   death.)*

**6. General play.** Nothing else changed visually or in feel this pass — the CSS/dead-
   code cleanup (R-028) was confirmed invisible (zero-pixel-diff against what was
   already committed), so if anything *looks* different, that's worth flagging as a
   new issue, not an expected change.

Nothing above needs a specific test-feedback-log write-up beyond R-029 — jot pass/fail
there once you've gone through steps 3–4.

---

## Unity Testing Track — Mac session required

Work this list in order — later steps assume earlier ones passed. Each step names the
register row it verifies.

**1. Open the project. Look at the Console. (R-024, 🔴 do this first, always)**
   Confirm **zero red compile errors.** This is the step most likely to fail, and if it
   does, nothing else on this list can be tested — several files were fixed blind this
   pass (no C# compiler exists on the Linux side to check against), so this is the
   first real confirmation any of it actually works. If you see red errors, stop and
   tell me the exact error text.

**2. Checkpoints (R-025).** If your test level has 2+ checkpoints, activate one.
   Confirm the *other* checkpoints keep their original color — they should not also
   turn gold/active-colored.

**3. Combat — hit flash (R-026).** Get hit a few times, or land a few hits on an enemy.
   Confirm the white hit-flash still looks right: brief flash, clean revert, nothing
   stuck white or flickering oddly.

**4. Listen to all 8 sounds (R-021, R-027).** Dash, get hit, swing your sword, hit an
   enemy, level up, clear a wave, die once, win a mission once. For the last four
   (level-up, wave-clear, death, victory) — these were just fixed for audible
   clicks/pops — listen specifically for any leftover click or pop, and listen closely
   to whether victory's final held chord sounds clean or distorted.

**5. Combos and missions (R-005, R-006, R-007, R-008).** Play through each weapon's
   combo (Sword/Bow/Staff/Shield) — timings/feel should be unchanged. Enter both
   mission portals. Unlock 1-2 skills, save, reload, confirm they're still unlocked.

**6. Desktop controls (R-011) and touch controls if you have a device (R-012).**
   Confirm desktop controls feel exactly like before. If testing touch: confirm the
   on-screen buttons actually respond to taps, not just appear.

**7. The two Editor-only blockers, if you have time this session (R-013, R-014).**
   Create the 4 URP Pipeline Assets (unblocks material-pooling tier work) and the
   Android Build Profile (unblocks real device/touch testing). These are the only two
   items left that can *only* happen in the Editor — everything else on this list I
   could at least attempt blind and now need your eyes on.

**8. Anything else, fine-grained.** `TESTING-BOOKLET.html`'s Batch 1/2 sections have
   the finer gameplay-feel checks this register doesn't duplicate.

Write results into the Test Feedback Log section above (or just tell me in chat what
you saw) — pass/fail plus a one-liner on *why* is enough. Anything that fails becomes
a Next Cycle Backlog item automatically.
