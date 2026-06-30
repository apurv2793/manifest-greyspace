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

## Phase 4 — Progression System _(XP/leveling done ✅)_

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

### 4D · PlayerInventory
- `PlayerInventory.cs` — weapon slots (2), active skills (4 mapped to 1/2/3/4), passive boons
- Manages equip/unequip, validates prerequisites

**Milestone:** Kill enemies → XP fills bar → level up → skill point awarded → node unlocked via Inspector.

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
- Death → respawn at last checkpoint (or hub if no checkpoint activated)

**Milestone:** Progress after two missions. Quit. Reopen. Exact state restored.

---

## Phase 7 — Polish

### 7A · VFX framework
- `VFXManager.cs` — static singleton, `Spawn(effect, pos, color)` from anywhere
- Effects (all procedural primitives):
  - `HitSparks` — 4-6 cubes fly outward, shrink-die over 0.15s
  - `DeathBurst` — 8 spheres expand from enemy pos, fade
  - `DashAfterimage` — ghost copy of player, fades over 0.2s
  - `LevelUpBurst` — ring of gold particles expanding upward

### 7B · AudioManager stub
- `AudioManager.cs` — `Play(clipName)` no-ops until clips wired
- Registers: `sword_swing`, `hit_enemy`, `player_hit`, `dash`, `wave_clear`, `level_up`
- All calls already in engine — just no audio until Unity AudioClip assets added

### 7C · Camera polish
- Screen shake: `CameraShake(intensity, duration)` — positional offset coroutine on `Camera.main`
- Called on: player hit (light shake), boss attack (heavy shake), death (0.5s slow drop)
- Hit pulse: 1-frame micro-shake on landing a hit (already has hit stop)
- Zone clamp: camera lerp bounded to a min/max rect per zone (stub, wired in Phase 8)

### 7D · UI polish
- HP bar: smooth drain (bar chases actual value with lerp, not instant)
- Damage number colour: white=normal, yellow=crit (2× dmg), red=player hit — already in DamageNumber.cs?
- Level-up text burst: big "LEVEL UP" in centre screen, fades
- Wave-clear fanfare: brief text animation

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
