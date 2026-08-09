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

---

## Status as of this register

**Everything doable without a Unity Editor is now done and committed**, on both
projects. What's left is exclusively: (a) behavioral confirmation on the Mac that the
Unity-side structural changes actually play correctly, and (b) the two Editor-only
blockers (R-013, R-014) that unlock the remaining Unity render/build work.

The JS side (manifest-cod-experiment) needed no Mac session at all — every gate for
it was independently verifiable and re-verified here (build, test, pixel-diff). It's
the one track in this whole plan that's genuinely fully done, not "done pending
review."

## Recommended Mac session order

1. **Open the project first, check the Console for import errors** — several `.asset`
   and `.cs` files were added outside the Editor this pass (`b1-opens` in the booklet).
2. R-013 (URP Pipeline Assets) — unblocks R-015 (material pooling).
3. R-014 (Android Build Profile) — unblocks real device/touch testing.
4. Everything else in this table, top to bottom — the 🔴 items first within each batch.
5. Work through `TESTING-BOOKLET.html`'s Batch 1 and Batch 2 sections for the
   fine-grained gameplay-feel checks this register doesn't duplicate.
