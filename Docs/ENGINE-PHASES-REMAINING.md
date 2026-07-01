# Greyspace Engine — Remaining Phases & Enhancements

> **Status as of Phase 2 complete:**
> Phase 1 (combat feel) ✅ · Phase 2 (hub + mission system) ✅ · Phase 4 XP/leveling wired ✅

> **Build coordination:** All code generation goes through Manifest OS `/api/os/compare`
> with `task_kind: "unity_code"`. Claude Code reviews outputs, sends `/api/os/feedback`,
> RouterMemory learns. See COORDINATION.md for workflow.

---

## Phase 3 — Weapon System

**Goal:** Weapons are found in the world, not pre-equipped. Each feels mechanically distinct.

### 3A · WeaponBase (modularity foundation)
- Abstract `WeaponBase.cs` MonoBehaviour: `PrimaryAttack()`, `SpecialAttack()`, `OnDash()`
- `GunCharacter` calls weapon methods — no weapon logic in the player controller
- `EquipWeapon(WeaponBase w)` one-liner swap

### 3B · Weapon implementations (3 starters)
| Weapon | Primary | Special | Dash mod |
|--------|---------|---------|----------|
| Sword | 3-hit melee combo (existing ComboData) | Spinning AOE slash | Short dash + afterimage |
| Bow | Charged shot (hold J to power) | Arrow rain — 5 projectiles in arc | Long dash |
| Shield | Bash + parry window | Shield throw (bounces) | Dash + brief block |

_Staff → replaced by Shield per plan. Staff ComboData kept as hidden archetype._

### 3C · Weapon pickups
- `WeaponPickup.cs` — glowing orb in world, slow bob animation
- Walk over → prompts "Pick up [Weapon]? E=Yes / walk away=No"
- Carry limit: 1 equipped + 1 stored. Picking up when full: swaps stored.
- Pickups spawned by mission spawner (controlled, not random)

**Milestone:** Player starts with Sword. Finds Bow pickup mid-mission. Tab swaps between them.

---

## Phase 4 — Progression System ✅ done

### 4A · Skill tree
- `SkillNode.cs` ScriptableObject: name, description, XP cost, effect type + value, prerequisites
- Effect types: `DamageBonus`, `SpeedBonus`, `MaxHPBonus`, `DashCharges`, `ComboExtend`
- `SkillTree.cs` — player's unlocked nodes, validates prerequisites
- **No UI yet** — unlocks via console/Inspector for now; UI in Phase 7

### 4B · Story-gated unlocks
- `StoryFlags.cs` already exists — skill nodes check `requiredFlag` field
- Example: clearing Inner Sanctum sets `inner_sanctum_complete` → unlocks "Armour Break" node

### 4C · Penalty system stub
- `PenaltyManager.cs` — empty subscriber on `GameEvents.OnPlayerDeath`
- No behaviour — placeholder for story-driven design later
- Never instantiated yet (`new PenaltyManager()` not called anywhere) — inert on purpose

### 4D · PlayerInventory
- `PlayerInventory.cs` — weapon slots (2), active skills (4 mapped to 1/2/3/4), passive boons
- Manages equip/unequip, validates prerequisites
- Wired as `GunCharacter.inventory` field

**Milestone:** Kill enemies → XP fills bar → level up → skill point awarded → node unlocked via Inspector.

### How to see this in Unity (until Phase 7 builds real UI)
- **SkillNode** — real ScriptableObject: Project window → right-click → `Create > Game > SkillNode`,
  fill fields in Inspector like any other asset (same pattern as `MissionDefinition`/`SpawnGroup`).
- **PlayerInventory** — `[System.Serializable]`, so `GunCharacter` → `Inventory` → `Active Slots` (4)
  shows in Inspector at runtime; drag a SkillNode asset in to test a slot.
- **SkillTree.unlocked** — a `HashSet<SkillNode>`, Unity can't serialize it, stays invisible in
  Inspector regardless. Unlock for testing via code/console: `player.inventory.skillTree.TryUnlock(node)`.
- **PenaltyManager** — nothing to see until something calls `new PenaltyManager()`.

---

## Phase 5 — Enemy Variety

**Goal:** Combat requires decisions, not just mashing.

### 5A · EnemyBase
- Abstract MonoBehaviour: health, speed, attackDamage, xpValue, all Inspector-tunable
- HP bar, hit flash, shrink-death, knockback — all automatic (no per-enemy code)
- Abstract `UpdateBehaviour()` — each enemy implements AI
- On death: `GameEvents.FireEnemyDied(xpValue)`

### 5B · Enemy archetypes
| Enemy | Behaviour | Default HP | XP |
|-------|-----------|------------|-----|
| **Stalker** | Chase + melee (current GunEnemy, refactored to EnemyBase) | 30 | 10 |
| **Charger** | Telegraph pause → sprint in fixed dir → recover | 45 | 20 |
| **Ranged** | Keep 8-12u distance, fire slow projectiles, retreat if too close | 25 | 15 |
| **Shielder** | Slow chase, frontal 80% dmg reduction (3 hits breaks shield) | 60 | 25 |

### 5C · SpawnGroup
- `SpawnGroup.cs` ScriptableObject: list of `{enemyType, count}` pairs
- `GreyspaceScene.WaveLoop` reads SpawnGroup per wave instead of hardcoded Stalkers
- Mission definition references an array of SpawnGroups (one per wave)

### 5D · Boss template
- `BossEnemy.cs` — extends EnemyBase, phase thresholds (HP %), behaviour changes per phase
- Brief camera pan on spawn (coroutine)
- Stats defined in ScriptableObject

**Milestone:** Wave 1 = Stalkers, Wave 2 = Stalkers + Charger, Wave 3 = Shielder + 2 Ranged.

---

## Phase 6 — Meta-Progression & Save

### 6A · WorldState
- `WorldState.cs` — `Dictionary<string, string>` for permanent flags (doors, NPCs, story)
- `Set(key, value)`, `Get(key)`, `Has(key)` — same pattern as StoryFlags but persistent
- Serialised to JSON on every save

### 6B · SessionState
- Already partially in `SceneState.cs` — formalise it
- Tracks XP gained, enemies killed, damage taken this session
- Cleared/applied on hub return

### 6C · SaveManager
- `SaveManager.cs` — serialises `WorldState + PlayerInventory + SkillTree + CheckpointState` to JSON
- Save file: `Application.persistentDataPath/save.json`
- Auto-save triggers: hub entry, checkpoint activated
- `SaveManager.Load()` on game start — applies state to SceneState

### 6D · Checkpoint
- `Checkpoint.cs` — proximity trigger, activates on approach
- Saves current position + scene snapshot
- Death → respawn at last checkpoint (or hub if no checkpoint activated) — **deferred**:
  `R` still does a full mission restart from wave 1. Checkpoint.cs itself works (save/load,
  position tracking), but death doesn't yet route through it. Revisit when wave-preserving
  respawn is actually needed.

**Milestone:** Progress after two missions. Quit. Reopen. Exact state restored.

---

## Phase 7 — Polish ✅ done

### 7A · VFX framework ✅
- `VFXManager.cs` — static singleton, `Spawn(effect, pos, color)` from anywhere
- Effects (all procedural primitives): `HitSparks`, `DeathBurst`, `DashAfterimage`, `LevelUpBurst`
- Wired: melee hits (HitSparks), enemy death — both `EnemyBase` and legacy `GunEnemy` (DeathBurst),
  dash (DashAfterimage), level-up (LevelUpBurst)

### 7B · AudioManager stub ✅
- `AudioManager.cs` — `Play(clipName)` no-ops until clips wired
- Call sites wired: `sword_swing` (windup start), `hit_enemy` (melee connects), `player_hit`
  (GunCharacter.TakeDamage), `dash`, `wave_clear`, `level_up`

### 7C · Camera polish — partial
- `CameraShake.cs` ✅ — additive offset coroutine on `Camera.main`, correctly undoes previous
  offset before reapplying so it doesn't fight the LateUpdate lerp-follow
- Wired: player hit (light, 0.12/0.15s), player death (heavier, 0.3/0.4s)
- **Not built**: true "0.5s slow drop" death camera animation (used a heavier shake instead —
  a real slow-zoom/drop is a distinct feature, add if the shake alone doesn't read as death)
- **Not built**: hit pulse micro-shake on landing a hit (CombatFeel.HitStop already covers the
  timing-freeze feel; a shake on top wasn't added to avoid stacking effects unrequested)
- Zone clamp — still deferred to Phase 8 per original plan

### 7D · UI polish — partial
- HP bar ✅ — now lerps toward target value (6/sec) instead of snapping instantly
- Damage number colour — white=normal ✅, red=player hit ✅ (new). **Yellow=crit not built** —
  no crit-chance/multiplier system exists anywhere in the engine to hang it off; would be
  inventing a new mechanic, not polish. Build the crit system first if this is wanted.
- Level-up text burst / wave-clear fanfare — not built as dedicated animations; existing
  `LevelUpFlash` (colour flash) and wave-clear text now also trigger VFX/audio, which covers
  the "response" requirement without new UI animation code

### Bugs found and fixed while wiring this phase
- `MeleeAttack.DoMeleeHit` only ever scanned `GunEnemy` — Charger/Ranged/Shielder (all
  `EnemyBase`-derived) took **zero melee damage** since Phase 5. Now scans both hierarchies;
  Shielder routes through `TakeDamageDirectional` for its shield-reduction logic.
- `GunEnemy.Die()` never called `GameEvents.FireEnemyDied()` — killing the default Stalker
  enemy gave **zero XP**. Added `xpValue` field (default 10, matches EnemyBase table) and the
  missing event fire.

**Milestone:** Every combat action has a visual/audio response. Game feels alive without art assets.

---

## Phase 8 — Content Pipeline & Modularity

### 8A · Enemy creation pipeline
- New enemy = new ScriptableObject (stats) + optional new class (if unique AI)
- `EnemyBase` handles everything else

### 8B · Skill/boon creation
- New skill = new `SkillNode` ScriptableObject → appears in skill tree automatically

### 8C · Mission authoring
- `MissionDefinition` ScriptableObject already exists
- Extend with: SpawnGroup array per wave, checkpoint positions, zone bounds
- Hub portal auto-reads MissionDefinition — no code changes to add a mission

### 8D · Weapon creation
- Implement `PrimaryAttack()` + `SpecialAttack()` → one file
- Assign `WeaponDefinition` ScriptableObject (name, stats, pickup sprite colour)
- Appears in pickup system automatically

### 8E · Zone system (within missions)
- `ZoneBounds.cs` — trigger volumes, camera clamps to bounds when player inside
- `EnemyTerritory.cs` — enemies stop chasing when player leaves their zone
- Missions become multi-zone sequences rather than single arenas

---

## Enhancements (beyond original plan)

| Enhancement | Phase | Notes |
|-------------|-------|-------|
| Pause menu | 7 | ESC → pause overlay, resume / quit |
| Minimap | 7 | Hub: always-on. Mission: fog of war per zone |
| NPC stubs | 8 | Interactable shape, `OnInteract()` UnityEvent |
| Difficulty scaling | 5 | Mission definition: difficultyMult field |
| Mission variety | 8 | Timed, boss-only, escort stub types |
| Inventory screen | 7 | Tab/I to open grid UI |
| Death penalty design | deferred | `PenaltyManager` stub ready, design TBD with story |
| Story flags UI | 8 | Debug overlay showing active flags |

---

## Build order

```
Phase 3 (weapons)     → weapons complete before progression matters
Phase 5 (enemies)     → combat depth feeds Phase 4 XP value
Phase 4 full (skills) → XP has meaning once enemies vary
Phase 6 (save)        → locks in progression permanently
Phase 7 (polish)      → runs in parallel from Phase 5 onward
Phase 8 (pipeline)    → content unlocked, engine production-ready
```

---

## Manifest OS coordination

Every script in Phases 3–8 is generated via:
```
POST /api/os/compare  { task_kind: "unity_code", prompt: <spec>, providers: ["nim","groq"] }
```
Claude Code reviews → picks best → `POST /api/os/feedback` → RouterMemory learns.
See `Docs/COORDINATION.md` for the full workflow and prompt templates.
