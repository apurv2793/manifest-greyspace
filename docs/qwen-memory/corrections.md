# Qwen review corrections — persistent memory (Greyspace/Unity)

This file exists because a local model has no memory between calls — every review
prompt starts from zero. Rather than let the same category of mistake repeat across
every script-group review, corrections land here and get included as context in every
subsequent review prompt sent to Qwen. Update this file every time Claude finds a Qwen
claim to be wrong (or right, if the pattern is worth reinforcing) after checking it
against the real code.

This is the Unity/C# sibling of `manifest-cod-experiment/docs/qwen-memory/corrections.md`
— same practice, separate file because the codebases (and Qwen's mistakes in each) are
different.

## Cross-project pattern already confirmed on the JS sibling project

Across the JS engine's full 12-subsystem review, every one of the 10 "confident,
detailed, plausible-sounding `[BUG]` claim" spot-checks came back **false** on
independent verification (backwards logic, misreading a callee's own reset/guard
behavior, unit mismatches, missing a constructor-time fallback). There is no reason to
expect a different base rate here — treat every finding below as a lead to check, not
a fact, from the first review onward.

## Confirmed TRUE findings (Qwen got these right — real, pre-existing issues)

### `new Material(...)` / `CreatePrimitive` without pooling across combat + enemies
**Qwen claimed:** `MeleeAttack.cs`, `SpecialAttack.cs`, `Projectile.cs`, `EnemyBase.cs`,
`GunEnemy.cs`, `RangedEnemy.cs`, `NPCStub.cs` all clone a fresh `Material` per instance
(via a shared `Mat(Color)` helper or inline `new Material(...)`) and spawn primitives via
`GameObject.CreatePrimitive` with no pooling, defeating SRP batching/GPU instancing.
**Verified true** — this matches a *pre-existing, already-documented* architecture gap
(the batch plan's "U3: MaterialCache.cs" work item, ~14 per-primitive clone sites,
explicitly blocked on U2/URP Pipeline Assets which need the Unity Editor on the Mac).
Qwen re-discovering this independently is a good sign, not a new problem — but also not
news; don't treat it as an urgent new finding, it's already tracked as blocked work
(`R-015` in `Docs/DELIVERY-REVIEW-REGISTER.md`).

### Direct `FindObjectsOfType`/`FindObjectOfType` in hot paths
**Qwen claimed:** `MeleeAttack.HandleInput()`, `EnemyProjectile.Update()`,
`NPCStub.Update()`, and others call `FindObjectsOfType<T>()`/`FindObjectOfType<T>()`
directly in per-frame code, causing O(N) scene scans every frame instead of cached
references.
**Verified true** by direct grep — these calls are real and exactly where claimed. This
is a genuine, actionable performance finding (not previously documented elsewhere).

## Confirmed factual corrections (Qwen got these wrong — do not repeat)

### `CameraShake.Shake()`'s real parameter order is `(intensity, duration)`
**This one's on the orchestrator, not Qwen** — a review-prompt build request stated
"`CameraShake.Shake(float duration, float magnitude)`" without re-verifying against the
actual method signature. The real signature (`CameraShake.cs`) is `Shake(float
intensity, float duration)` — reversed. Qwen wrote `CameraShake.Shake(0.8f, 0.5f)` for
a death moment and `CameraShake.Shake(0.6f, 1.0f)` for a victory moment under the wrong
assumption; both still produce reasonable shakes purely by luck (existing call sites
use comparable small values in both argument positions), so this wasn't worth a
generation re-run, but any FUTURE prompt referencing this method must state the real
order: intensity first, duration second.

### `MeleeAttack.cs` arc-angle check is correct, not a bug
**Qwen claimed:** `Vector3.Angle(transform.forward, toE) > h.arcAngle * 0.5f` is wrong
because "the angle between forward and offset vector is not half-arc if arc is a total
sector angle," overextending the hitbox (e.g. claimed a 90° arc allows ~127° coverage).
**Actual math:** `Vector3.Angle` returns the unsigned angle in `[0°, 180°]` between the
two vectors. If `arcAngle` is the *total* cone width, checking whether that angle exceeds
`arcAngle * 0.5` is exactly the standard, correct way to test "is this point within a
cone of total width `arcAngle` centered on forward" — a target at exactly the cone edge
is `arcAngle/2` degrees off-axis by definition. Do not flag this pattern without
independently re-deriving the geometry; it's the textbook check, not an approximation.

### `RangedEnemy.FireProjectile()` already destroys on range-exceeded
**Qwen claimed:** the loop's `traveled > 18f` exit "never calls `Destroy(proj)` on loop
exit... only `yield break`s without destroying," leaking projectiles.
**Actual code:** `if (traveled > 18f) { Destroy(proj); yield break; }` — `Destroy(proj)`
is right there, before the `yield break`. Read the actual exit-branch code, not just the
control-flow keyword, before flagging a leak.

### `ContentLibrary.Combo()` already null-guards the foreach
**Qwen claimed:** the `foreach (var c in Active.combos)` loop "crashes on `c.name`" if
`combos` contains null entries, no null-safety.
**Actual code:** `if (c != null && c.name == weaponName) return c;` — the null check is
already there, inline in the same condition. Read the full loop body, not just the
`foreach` header, before flagging missing null-safety.

### `InputRouter`'s signal-consume coalescing is a real but low-severity edge case
**Qwen claimed:** `Signal*()`/`Consume()` boolean-flag pattern drops rapid repeated taps —
two `SignalLight()` calls before one `LightPressed()` poll register as only one press.
**Verified true as described**, but same-severity as Unity's own `Input.GetKeyDown()`
edge-trigger semantics (a well-understood, generally-accepted limitation of any
single-frame boolean edge flag) — only loses input if two taps land inside the same
unconsumed window (a few ms at 60-144fps), far faster than realistic human tap speed.
Log as a minor, low-priority note, not a critical input-loss bug as the original framing
implied.

## Process note: Qwen regresses previously-correct files when asked to touch them again

The material-pooling + FindObjectsOfType-hot-path fix took 3 generation rounds to land
correctly, and the pattern each time was the same: files Qwen got RIGHT in an earlier
round would silently revert to the WRONG pattern in a later round, even when that later
round's prompt never asked it to change that specific file's already-correct logic.
Concretely: round 1 correctly routed `GunEnemy.cs` and `NPCStub.cs` through the new
shared `MaterialCache.Get()`. Round 2 (asked to fix unrelated compile errors + expand
scope to other files) silently reverted `GunEnemy.cs` back to a local, uncached
`Mat()` helper. Round 3 (asked to fix only `GunEnemy.cs` + add one missing `using` line
to `EnemyBase.cs`) correctly fixed `GunEnemy.cs` back, but introduced the SAME
regression fresh into `EnemyBase.cs` — which had been correct since round 1 and was
never supposed to change beyond the one import line.

**What this means for how multi-file fix rounds get applied:** never trust "the last
round's output" as the source of truth for a file just because it's the most recent.
Diff every regenerated file against the last known-correct version of *that specific
file*, not just against the immediately-prior round. When applying a multi-round fix,
build the final file set by taking each file's best verified-correct version across
ALL rounds, not just the latest round's output wholesale — several files that were
never flagged as broken (`GunCharacter.cs`, `WeaponBase.cs`, `MeleeAttack.cs`,
`EnemyProjectile.cs`, `SpecialAttack.cs`) were also never re-applied by later rounds
(since Qwen correctly didn't re-output unchanged files) and had to be manually pulled
from round 1's output and applied separately — a plain "apply the latest round" copy
would have missed them entirely and left half the fix uncompiled/inconsistent.

## Independent (non-Qwen) reviewer findings — nemotron via NIM

First cross-check from a genuinely different model family (not Qwen, not Claude) on
this session's Unity+JS diffs, per the "three independent reviewers" register policy.
10 findings, spot-checked 5:

**Confirmed real** — Vector3-literal regression: 4 `RectTransform.offsetMin`/
`anchoredPosition` assignments in `GreyspaceScene.cs`'s `BuildHUD()` had drifted from
the original `Vector2` literals to `Vector3` across this session's multiple
regeneration rounds. **Not a compile error or behavior change** (Unity's `Vector2` has
a documented implicit conversion from `Vector3`, silently drops z) — purely a
type-consistency cleanup, fixed anyway since it was zero-risk.

**Confirmed false** (4 of 5 spot-checked):
- "AudioManager.cs static class contains instance members" — already fixed earlier
  this session (nemotron was likely working from stale context, not the current file).
- "Division by zero risk in GunCharacter.UpdateIdle if Time.deltaTime is zero" — no
  division exists in that method at all, only `Mathf.Lerp` (multiplication-based).
- "Potential null reference in updateDebriefScreen if game/s is null" — the existing
  `on` boolean gate already makes this impossible (`on` can only be true if `game` is
  truthy, since `phase` is derived from `game?.getPhase?.()`).
- "_audible() may not handle this.ac being null" — already uses optional chaining
  (`this.ac?.state === 'running'`), correctly returns false without throwing.

**Confirmed false** (remaining 4, checked in a follow-up pass):
- "VFXManager.Spawn missing default case for unhandled effect type" — the default case
  already exists (`EffectCoroutine`'s switch, logs a warning and exits safely); `effect`
  is an enum (value type), a null reference was never possible here regardless.
- "MaterialCache needs try-catch for failed shader/material creation" — the only calls
  involved (`CreatePrimitive`, reading its renderer's `sharedMaterial`) are built-in
  Unity calls with no realistic runtime failure mode; a generic "add more error
  handling" suggestion, not a concrete issue.
- "`.ui-debrief .best-badge` CSS may cause display issues if not tested" — it's the
  standard `display:none` → `.on{display:block}` toggle used throughout the file; the
  claim names no actual problem.
- "`disposeTitle` may not fully clean up DOM elements if exceptions occur" — the real
  function is 2 lines (`el.remove()` ×2, `if`-guarded), and `.remove()` doesn't throw.
  Nemotron also cited the wrong line range for this one (65-158 vs. the real 155-158) —
  a sign it was working from a stale/wrong location, not the current file.

**Final tally: 10 findings, 1 confirmed real (the Vector2 regression), 9 confirmed
false.** All 10 are now checked — no open gap.

**Takeaway:** same pattern as every review this session regardless of source model —
confident, detailed, plausible-sounding claims are not evidence. This is the first
finding from ANY reviewer (Qwen included) that was a genuine, confirmed regression
rather than a false positive - worth noting that independent cross-review earns its
keep even at a low hit rate (1 real finding in 10 checked).

## Independent (non-Qwen) reviewer findings — Fable, end-to-end progress review

Second cross-check from a different model family, this one doing a full read-through
with its own verification (grepping to confirm claims before reporting them, unlike
nemotron's single-pass review). Notably higher hit rate than any other reviewer this
session.

**Confirmed real, fixed and committed (`6fe78ed`):**
- **3 compile blockers** — the project would not have built on the Mac as committed.
  `Mat()` was deleted from `EnemyBase.cs`/`WeaponBase.cs` in an earlier round but
  `RangedEnemy.cs`, `ChargerEnemy.cs`, `ShielderEnemy.cs` (never touched by that
  round) still called it — exactly the "apply the latest round wholesale misses
  files" failure mode this file already documents above, caught here for real.
  `MaterialCache.cs` had an unqualified `DestroyImmediate()` in a static class (no
  `using static UnityEngine.Object`). `AudioManager.cs`'s `GenerateVictory()` had a
  `const` initialized from `notes.Length * noteDuration`, neither of which is a
  compile-time constant.
- **`Checkpoint.cs` cache poisoning** — `Activate()` mutated the *shared*
  `MaterialCache` entry for `InactiveColor` in place, so one checkpoint activating
  would recolor every other inactive checkpoint's material too. Fixed to swap to a
  separate cached material instead of mutating in place.
- **Hit-flash defeats its own pooling** — `EnemyBase`/`GunEnemy`/`GunCharacter`'s
  `HitFlash()` coroutines captured/restored via the `.material` getter/setter, which
  Unity auto-instantiates a per-renderer clone on first access — silently defeats
  `MaterialCache`'s whole purpose the moment anything takes a hit. Fixed to use
  `.sharedMaterial` throughout (pooling-safe, doesn't auto-instantiate).

**Confirmed real, NOT yet fixed (audible but non-blocking — logged for a Qwen
fix pass, not urgent enough to block a Mac session):**
- Envelope-vs-note-window mismatches cause audible truncation clicks in 4
  generators: `GenerateDeath` (cuts at ~42% amplitude ×3), `GenerateLevelUp` (~86%
  ×4), `GenerateWaveClear` (~28% ×4, mild), `GenerateVictory`'s arpeggio (~36% ×7).
  Independently re-derived the exact cutoff percentages from `Envelope()`'s own
  attack/decay/sustain/release math — all four match Fable's numbers precisely.
  Fix: shrink each `Envelope(...)` call so attack+decay+release ≤ the note's gate
  window.
- `GenerateVictory`'s held final chord: 7 simultaneous notes at up to 0.7 amplitude
  each with `sustainLevel=1.0` risks summing well past ±1.0 (theoretical peak ~4.9) —
  real clipping risk on the payoff moment. Fix: divide by note count or apply
  headroom scaling.

**Confirmed false / not worth acting on:** none — every concrete claim in this
review checked out true. (Vague ones like ".meta files should be committed
Mac-side" and "AudioManager.Play()'s comment about stealing sources is wrong" are
process notes, not code bugs — the .meta point is Mac-only work, the comment fix is
cosmetic and skipped.)

## Independent (non-Qwen) reviewer findings — Kimi, re-run after a stale-clone failure

Kimi's first attempt reviewing this project was discarded entirely — every specific
claim (e.g. "`MaterialCache.cs` does not exist," "`AudioManager.cs` is a 9-line
stub") was checked against real source and was false, describing a version of the
code from before today's fixes. Root cause: it quoted a `Mat()` method verbatim
that matches this repo's git history from before the compile-blocker fix — almost
certainly a stale clone, not the live working directory. The re-run added a
mandatory freshness check (compare `git log -1` hash + canary line counts before
reviewing anything) and passed — real citations this time.

**Confirmed true, fixed:** `WeaponPickup.cs` still used `FindObjectOfType
<GunCharacter>()` instead of `GunCharacter.Instance` — a real leftover from the
R-019 registry migration (that file was outside every generation round's scope,
same failure class already documented above). While fixing it, also found (myself,
not from Kimi) that the same file still used the old manual
`new Material(temp-primitive-clone)` pattern instead of `MaterialCache.Get()` — the
R-019 *material-pooling* migration missed this file too. Fixed both.

**Confirmed false — this one's worth remembering:** "the 4 audio generators'
envelopes finish before the note's full time window, leaving the tail silent —
extend the release time so envelope duration matches the window." This is
describing the *correct, intended* result of the envelope/gate-mismatch fix earlier
this session (R-027) — those envelopes were deliberately shrunk to fit *under* the
gate window *with headroom*, specifically so playback finishes cleanly before
truncation risk returns. A brief trailing silence after a full natural decay is
harmless and expected; it is not the same defect class as "envelope exceeds the
window and gets cut off mid-decay" (the actual, now-fixed bug). Kimi's proposed fix
would remove that headroom and reintroduce the original click risk. Do not "fix"
intentional envelope headroom without checking whether it's there on purpose.

## Qwen-only verification sweep (batch 6 of ~9: enemies + polish_vfx + progression_save)

Qwen's self-check produced its highest count yet (15 TRUE across the 3 files). I
checked the most concrete/consistent ones myself before trusting any of it — all
false or already-known:

- **4 separate "VFXManager violates material pooling" claims, all FALSE.**
  `MakeMat()`'s per-instance clone (`new Material(template)` from the
  `MaterialCache.Get()` template) is deliberate and necessary — each spawned
  particle independently animates its own fade-out `mat.color`, so sharing one
  cached instance across particles would be the exact cache-poisoning bug already
  found and fixed in `Checkpoint.cs` this session. This was already confirmed
  correct by an earlier independent review this session ("VFXManager's per-instance
  clone via MakeMat is the right call" — see Fable's findings above). The
  underlying "no object pooling for spawned VFX primitives" observation is real but
  not new — it's the already-tracked, deliberately-deferred R-015 item, blocked on
  URP Pipeline Assets, not a fresh finding.
- "HitSparks XZ-only vs. DeathBurst/LevelUpBurst full 3D" and "hardcoded per-effect
  speed/timing, no per-effect randomization" — both FALSE as bugs. Deliberate
  per-effect visual character choices (ground-plane sparks vs. omnidirectional
  burst), not defects.
- "Shield angle check may fail due to rotation misalignment, needs local-space
  transform" (`ShielderEnemy.cs:71`) — FALSE. Both `transform.forward` and
  `sourcePos - transform.position` are already world-space vectors in Unity;
  comparing them directly via `Vector3.Angle` is exactly correct, nothing to
  transform.
- "SaveManager missing null checks before iterating `saveData.unlockedSkillNames`/
  `activeSlotNames`" — real as a fact (no field initializers, so an old/corrupted
  save could deserialize these as null), but the entire `Load()` body is already
  wrapped in one try/catch that logs and returns `false` on any exception — a null
  field fails the load safely rather than crashing. Existing behavior is
  reasonable, not an urgent gap.

Not independently re-checked this batch (lower-confidence/vaguer claims, or
"design choice" framing already in Qwen's own verdict): knockback physics
model, projectile pool-return, SaveData null-MissionId serialization edge case,
GunEnemy hpBarWidth, and the several NOT-CHECKABLE-STATICALLY items.

Net: every claim independently checked this batch was false or already-known.

## Qwen-only verification sweep (batch 5 of ~9: combat + content_pipeline)

Per your instruction, Qwen re-checked its own past findings via `qwen-code` CLI
against the current code (real file access, no compiler on this machine so static
only). combat: 0 TRUE / 8 STALE-CODE-CHANGED (already fixed by MaterialCache.Get()
this session, exactly the pooling work already tracked) / 7 FALSE-or-not-checkable.
content_pipeline: 1 TRUE, checked myself:

- "`ContentLibrary`'s `_loaded` flag never resets, stale state possible" — FALSE.
  `_loaded`/`_active` are set once on first access and cached for the app's
  lifetime — the same deliberate "load once, keep forever" pattern already used by
  `AudioManager`'s own bootstrap (`instance` static field). This would only matter
  if content packs could be swapped at runtime, which isn't a feature anywhere in
  this codebase. In the Editor, Play Mode's domain reload already resets static
  fields to default between sessions unless that's been explicitly disabled
  (not the case here). No real failure mode identified.

Net: 0 real findings from this batch. Combat's 8 "stale-code-changed" findings
are worth noting positively rather than as gaps — they're old material-pooling
observations that this session's `MaterialCache.Get()` migration already resolved,
confirmed still-fixed by Qwen's own re-read.

## Subsystems reviewed so far

| Script group | Findings claimed | Findings confirmed real | Notes |
|---|---|---|---|
| `combat` | 15 | ~7 (material/primitive pooling, real but pre-known) | Arc-angle claim wrong (see above); rest unverified |
| `enemies` | 20 | ~9 (material pooling + FindObjectsOfType, real) | 2 spot-checked false (RangedEnemy destroy claim); rest unverified |
| `player_input` | (not yet spot-checked) | — | — |
| `progression_save` | (not yet spot-checked) | — | — |
| `scene_systems` | (not yet spot-checked) | — | — |
| `polish_vfx` | (not yet spot-checked) | — | — |
| `content_pipeline` | 7 | 0 confirmed of 2 checked | Null-safety claim wrong (see above) |
