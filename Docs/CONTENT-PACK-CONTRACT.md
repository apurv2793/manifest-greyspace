# Content-pack contract (shared across manifest-greyspace-clean and manifest-cod-experiment)

This is the one page both projects' content-pack systems agree to, so a future third
gameplay style inherits a proven shape instead of two incompatible ones. Mirrored
verbatim in both repos — if you change this file, change the other copy too.

## Shape

A pack is a named bundle of swappable content:

```
Pack := {
  id,              # unique string, selects the pack ("base", "toy", "zelda", ...)
  displayName,
  weapons[],        # or combos[] — per-project vocabulary, same idea
  enemies[] / factions[],
  palette,          # visual identity — colors, materials
  levels[],          # level/mission definitions
  modes[],          # game-mode definitions, once a mode/game subsystem exists
}
```

Per-project translation:
- **manifest-cod-experiment (JS)**: `ctx.content.get('weapons')` etc., resolved via
  `?pack=` URL param. See `docs/superpowers/plans/2026-08-03-phase-f-g-h-roadmap.md`
  Phase H1 for the full spec this implements.
- **manifest-greyspace-clean (Unity)**: a `ContentPack` ScriptableObject referencing
  `ComboData[]`/`MissionDefinition[]`/`SkillNode[]` assets, resolved via
  `ContentLibrary.Combo(id)` etc.

## Selection

- JS: `?pack=<id>` URL param, default `base`.
- Unity: an active-pack reference (`ContentLibrary`), Inspector-settable, default `Base`.

## Tolerance (both projects, same rule)

**An absent table or entry falls back to the subsystem's built-in default rather than
erroring.** This is the "peek-style tolerance" both codebases already use elsewhere —
a pack that only overrides one weapon doesn't need to redeclare everything else. The
Unity side's fallback is literally the pre-existing static factory methods
(`ComboData.Sword()` etc.) kept in place specifically to serve this role; deleting them
before packs are proven working removes the thing you'd fall back to.

## Acceptance test (both projects, same idea)

A pack that changes one thing (one stat, one palette color) must:
1. Produce **zero visible difference** from the default when NOT selected.
2. Produce a **visible, correct difference** when selected.

JS: enforced via `tools/capture.mjs` + `tools/imagediff.mjs` (zero-pixel gate for #1,
manual visual check for #2). Unity: enforced via `Tools/validate_assets.py` (structural
correctness) plus a Mac Play-mode check (behavioral correctness — this machine cannot
verify #2 for Unity on its own).
