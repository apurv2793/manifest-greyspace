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
