# Manifest Greyspace — Engine Plan

> **Goal:** Build a modular, reusable isometric action engine in Unity 6 URP.
> Combat feel and mechanical depth inspired by Hades. Everything else — story,
> world, progression structure, penalty system — is original.
>
> **Team:** Solo (you + Claude Max). Every system is built template-first:
> one working version in code, all tunable data in ScriptableObjects so content
> can be added without touching scripts.

---

## What we're taking from Hades (mechanics only)

| Take | Leave behind |
|------|-------------|
| Combat responsiveness (hit stop, i-frames, knockback) | Roguelike room structure |
| Melee + ranged + special attack system | Per-room boon selection |
| Enemy archetypes that combo together | Post-death full reset |
| Dash as a core mechanic | Olympian god theming |
| Visual/audio feedback polish | Any specific Hades content |

---

## Game structure (confirmed)

```
┌─────────────────────────────────────────────┐
│                  HUB WORLD                  │
│  Persistent space. Always returned to.      │
│  NPCs · Skill tree · Inventory · Mission    │
│  board · Story conversations · Secrets      │
└────────────┬────────────────────────────────┘
             │ enter mission
             ▼
┌─────────────────────────────────────────────┐
│               MISSION SCENE                 │
│  Self-contained. Discrete objective.        │
│  Multi-zone map with secrets + transitions  │
│  Enemies · Loot · Story moments             │
└────────────┬───────────────┬────────────────┘
             │ complete       │ die
             ▼               ▼
        return to hub    penalty applied
                         then return to hub
```

**Mission types** (selective — not all missions are story missions):

| Type | Gated by | Returns |
|------|----------|---------|
| Story mission | Story flag | Narrative progress, skill unlock |
| Side mission | Available from hub board | XP, loot, optional lore |
| Exploration | World discovery | Secrets, passive boons |
| *More TBD* | | |

**Key rules the engine enforces:**
- Player state (HP, inventory, skills) persists hub → mission → hub
- Missions are Unity scenes loaded additively over a persistent hub state
- Death in a mission triggers the penalty system, then returns to hub
- Story flags are the only hard gate; everything else is opt-in

---

## Current state (MVP done ✅)

- Isometric camera (orthographic, 40°/45°, smooth follow)
- Player: camera-relative WASD, mouse aim, left-click shoot, Shift dash + i-frames
- Enemy: chase AI, melee attack, world-space HP bar, hit flash, shrink-death
- Wave spawner, health HUD, YOU DIED screen, R to restart
- Git repo live, engine plan documented

---

## Phase 1 — Combat Engine
### Make every hit feel like it lands

This is the highest-leverage work. Hades' combat feels exceptional because of
five mechanics working together. Nothing else matters until these are right.

**1A · Melee attack (3-hit combo)**
- Left-click = sword swing, not projectile. Three swings per combo: fast → fast → heavy.
- Each swing has a hitbox active for a specific number of frames, then closes.
- Swing 3 does 2× damage and launches a knockback.
- `MeleeAttack.cs` — hitbox timing, combo counter, combo reset on timeout.

**1B · Hit stop**
- On landing a hit: game freezes for 4–6 frames (0.06–0.1 seconds), then resumes.
- This single mechanic makes hits feel physical. Without it, combat feels weightless.
- `CombatFeel.cs` — static singleton. `CombatFeel.HitStop(frames)` callable from anywhere.

**1C · Knockback**
- Enemies stagger backward on hit. Distance scales with damage and hit type.
- Player gets pushed back slightly when struck (creates spacing decisions).
- Added to the damage event — no extra component needed.

**1D · Floating damage numbers**
- On hit: a number rises from the enemy and fades out over 0.6 seconds.
- Color-coded: white = normal, yellow = critical, red = enemy hits player.
- `DamageNumber.cs` — spawned object, animates itself, self-destructs.

**1E · Special / ranged attack**
- Right-click: slower, high-damage projectile or short-range AOE depending on weapon.
- Separate cooldown from primary. Consumes a resource (mana, stamina — TBD with story).
- `SpecialAttack.cs` — decoupled from primary so each weapon overrides independently.

**Milestone:** Combat sandbox where fighting one enemy feels satisfying on its own.

---

## Phase 2 — Hub World & Mission System
### The skeleton the whole game hangs on

Two distinct scene types: the hub (always persistent) and missions (discrete, loaded on demand).

**2A · Hub world scene**
- A proper explorable space, not a menu. Player walks around it.
- Contains: mission entry points (doors, portals, NPCs), skill tree access,
  inventory screen, any story-critical NPCs.
- Persistent: NPC states, opened secrets, story flags all reflected visually.
- `HubManager.cs` — initialises hub state from save on load, keeps it in sync.

**2B · Mission entry / exit**
- Mission entry: a trigger zone in the hub (a door, a portal, an NPC conversation).
  Shows mission info card (name, type, suggested level) before confirming.
- On confirm: save hub state → async load mission scene → spawn player at mission start.
- Mission exit: objective complete → exit trigger → unload mission → restore hub →
  apply rewards → trigger any story flag changes.
- `MissionLoader.cs` — handles both directions. `MissionDefinition` ScriptableObject
  holds scene name, spawn point, mission type, story flag requirement, rewards.

**2C · Mission scene structure**
- Each mission is its own Unity scene: hand-crafted, multi-zone.
- Zones within a mission are camera-bounded areas. Camera clamps to the active zone,
  transitions smoothly when the player crosses a zone boundary.
- `ZoneBounds.cs` — trigger volume per zone, tells camera where to clamp.

**2D · Secrets & interactables**
- Destructible walls, pressure plates, hidden switches — all fire a UnityEvent on trigger.
- Designer wires them to whatever they open in Inspector. No code per secret.
- `Destructible.cs`, `PressurePlate.cs`, `Switch.cs` — three reusable components.

**2E · Enemy territory**
- Enemies belong to a zone trigger. Stop chasing if player leaves their zone.
- Prevents all enemies pooling at one location across a large map.
- `EnemyTerritory.cs` — one trigger volume per zone, auto-assigned to spawned enemies.

**2F · Death → hub return**
- On player death in a mission: freeze, penalty applied, fade out, hub restored.
- Player spawns back in hub at the last used mission entry point.
- Penalty system (Phase 4/6) hooks in here — engine just fires the event.

**2G · Minimap**
- Per-mission fog of war minimap. Clears as zones are entered.
- Hub has its own always-visible minimap with mission entry icons.

**Milestone:** Hub scene with two mission entry points. Mission 1 loads, player
completes it, returns to hub with rewards applied. Mission 2 requires a story
flag not yet set — entry is locked with a visible reason.

---

## Phase 3 — Weapon System
### Different weapons = different playstyles

**3A · Weapon base class**
- `WeaponBase.cs` — abstract. Defines `PrimaryAttack()`, `SpecialAttack()`, `OnDash()`.
- Player controller calls these methods. Doesn't know which weapon is equipped.
- Swapping weapons is `player.EquipWeapon(newWeapon)` — one line.

**3B · Starter weapon archetypes (3)**

| Weapon | Primary | Special | Dash behaviour |
|--------|---------|---------|----------------|
| Sword | 3-hit melee combo | Spinning AOE slash | Short dash, leaves afterimage |
| Bow | Charged shot (hold to power up) | Arrow rain (fires arc of 5) | Long dash, brief speed boost |
| Shield | Bash + parry window | Shield throw (bounces off walls) | Dash + brief block on entry |

**3C · Weapon acquisition**
- Found in the world (chest, reward, quest), not randomly assigned.
- Player can carry one equipped weapon + one stored. Swap with a key.
- Designed to be extended: adding weapon 4 means one new class + one ScriptableObject.

**Milestone:** All 3 weapons implemented and feel mechanically distinct.

---

## Phase 4 — Progression System
### Character growth through play and story

This replaces Hades' per-room boon selection entirely. Two parallel tracks:

**4A · Character level progression**
- Killing enemies and completing objectives grants XP.
- Leveling up gives one skill point to spend.
- Skill tree: branching paths, each node is a `SkillNode` ScriptableObject.
  Designer sets: name, description, cost, effect, prerequisites.
- Effects use the same `BoonEffect` types from combat (damage %, attack speed, etc.)
  so skills plug directly into the combat engine with no extra code.
- `SkillTree.cs` + `SkillTreeUI.cs` — accessible from a pause/hub screen.

**4B · Story-gated skill / boon unlocks**
- Certain skills are locked behind story flags, not XP.
- Example: "You helped the blacksmith → his weapon technique unlocks Armour Break."
- `StoryFlag.cs` — a string key set by narrative events. Skills check for required flags.
- Completely decoupled from story content: the engine checks flags, story systems set them.
- This means story writers can gate skills without touching the skill tree code.

**4C · Penalty system**
- *Structure TBD — to be designed with story context.*
- Placeholder: on death, a consequence is applied (lose some XP, a skill is temporarily
  disabled, an NPC reacts, a resource is reduced).
- Engine hook is ready: `PlayerDeath` fires an event. Whatever the penalty system is,
  it subscribes to that event. No combat code changes needed.
- `PenaltyManager.cs` — empty subscriber shell, ready to be filled.

**4D · Inventory / loadout**
- Player holds: 1 weapon, up to 4 active skills (mapped to keys), passive boons (unlimited).
- `PlayerInventory.cs` — manages slots, validates prerequisites, handles equip/unequip.
- UI: a simple grid screen accessible from pause.

**Milestone:** Player levels up, unlocks a skill, and it visibly changes combat behaviour.
Story flag manually set in Inspector triggers a locked skill to become available.

---

## Phase 5 — Enemy Variety
### Combat that requires decisions

**5A · Enemy base class**
- `EnemyBase.cs` — health, speed, XP value, attack damage, death loot, all Inspector-tunable.
- All enemies inherit this. Hit flash, HP bar, death animation are automatic.
- Adding a new enemy = new class (if unique behaviour) or just a new ScriptableObject
  with different stats (if it's a reskin).

**5B · Enemy archetypes (4 core)**

| Archetype | Behaviour pattern |
|-----------|------------------|
| Stalker | Chases player, attacks on contact — the baseline |
| Charger | Telegraphs with a pause, then sprints in a straight line |
| Ranged | Keeps distance, fires slow projectiles, retreats when player closes in |
| Shielder | Blocks frontal attacks, weak from behind — forces flanking |

**5C · Enemy combinations**
- Rooms and zones are authored with specific enemy mixes, not random spawns.
- A Shielder + two Ranged enemies behind it is a puzzle. Designer places these by hand.
- `SpawnGroup.cs` ScriptableObject: list of enemy types + positions relative to a spawn point.

**5D · Boss template**
- `BossEnemy.cs` — extends EnemyBase, adds phase thresholds.
- At each HP threshold, behaviour changes (new attack pattern, speed increase, spawns adds).
- Intro sequence: brief camera pan to boss, pause, fight begins.
- Fully data-driven: phases defined in a ScriptableObject, not hardcoded.

**Milestone:** A zone with Stalkers + Shielders + one Ranged enemy that requires
the player to change approach. A boss with two phases.

---

## Phase 6 — Meta-Progression
### What persists, what resets, what changes

*The penalty system design lives here but remains a placeholder until the story
structure is clearer. The engine scaffolding is built now so it can be filled in later.*

**6A · Persistent world state**
- What permanently changes: doors opened, NPCs met, story flags set, levels unlocked.
- Saved to disk. Never resets.
- `WorldState.cs` — dictionary of string keys → values. `WorldState.Set("key", value)`.

**6B · Run state (within a session)**
- What resets on death or on starting a new session (TBD with penalty design):
  could be XP since last checkpoint, temporary buffs, resource counts.
- `RunState.cs` — session-scoped, cleared on defined events.

**6C · Penalty system (placeholder)**
- The hook: `GameEvents.OnPlayerDeath` → `PenaltyManager.ApplyPenalty()`
- What `ApplyPenalty()` does is entirely story/design-driven.
- Could be: checkpoint rollback, skill lock, narrative consequence, resource drain.
- Engine is ready. Design fills it in.

**6D · Save / load**
- Auto-save on level transition and on returning to hub.
- `SaveManager.cs` — serialises WorldState + PlayerInventory + skill tree state to JSON.
- Single save slot for now. Multiple slots trivial to add later.

**Milestone:** Player progresses through two maps, closes the game, reopens —
position, inventory, and skill unlocks are exactly where they were left.

---

## Phase 7 — Polish Layer
### What separates "working" from "feeling right"

**7A · VFX framework**
- Hit sparks on melee impact
- Death burst (particles expand from enemy position)
- Dash afterimage (ghost trail of player sprite)
- Projectile trail

**7B · Audio manager**
- `AudioManager.cs` — singleton. `AudioManager.Play("sword_swing")` from anywhere.
- Separate channels: music, SFX, ambient.
- Music layering: a quiet ambient layer swells when enemies are nearby.

**7C · Camera behaviours**
- Zone boundary clamp (camera stops at zone edge)
- Boss intro pan (scripted camera movement)
- Death slow-motion + zoom (Time.timeScale drop + FOV change over 0.5 seconds)
- Hit pulse (one-frame micro-shake on successful hit)

**7D · UI polish**
- Health bar: smooth drain animation (bar lags behind actual value)
- Skill unlock: flash + sound cue when a new skill becomes available
- Level transition: fade to black, level name card, fade in

---

## Phase 8 — Content Pipeline
### Tools so you can build game content without touching engine code

By this phase, adding content is data entry, not programming.

**8A · Enemy creation**
- Duplicate any EnemyBase ScriptableObject, set stats, assign to a spawn point. Done.

**8B · Skill / boon creation**
- New ScriptableObject: name, description, effect type, value, prerequisites, story flag gate.
- Appears in skill tree automatically.

**8C · Map authoring**
- Tileable floor/wall prefabs on a snap grid.
- Zone boundary volumes placed visually.
- Spawn groups placed as prefabs with the enemy list set in Inspector.

**8D · Weapon creation**
- Implement two methods (`PrimaryAttack`, `SpecialAttack`) in a new class.
- Assign a ScriptableObject with stats and name.
- Appears in the weapon selection / inventory system automatically.

---

## Build order

```
Phase 1 — Combat feel          ← start here, always
Phase 2 — Maps & levels        ← gives combat a world to live in
Phase 3 — Weapon system        ← makes playstyle a choice
Phase 4 — Progression          ← XP, skill tree, story flags
Phase 5 — Enemy variety        ← combat needs problems to solve
Phase 6 — Meta / save          ← makes the game a continuous experience
Phase 7 — Polish               ← runs in parallel from Phase 4 onward
Phase 8 — Content pipeline     ← final step, frees you to make the game
```

**Phases 1–2** = playable combat in a real space.
**Phases 1–4** = a complete gameplay loop.
**Phases 1–6** = a shippable engine.
**Phases 7–8** = production ready.

---

## Solo dev principles (you + Claude Max)

- **One system at a time.** Finish Phase 1 before touching Phase 2.
- **ScriptableObjects for all data.** New enemy, weapon, skill = no code.
- **Engine events over direct references.** `GameEvents.OnPlayerDeath` not `FindObjectOfType<PenaltyManager>()`.
- **Placeholder > nothing.** Penalty system TBD? Ship the hook now, fill it later.
- **Document as you go.** Update this file when a phase completes or a decision changes.

---

## What's explicitly out of scope

- Story, characters, dialogue, lore — parallel track, not this repo yet
- Art assets — primitives until visual direction is locked
- Multiplayer
- Mobile / console ports
- Full procedural generation (hand-crafted maps, procedurally populated)
- The penalty system specifics — engine hook ready, design TBD

---

*Status: MVP complete. Phase 1 next.*
