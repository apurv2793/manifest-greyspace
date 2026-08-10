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
