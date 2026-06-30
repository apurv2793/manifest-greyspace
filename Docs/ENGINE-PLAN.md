# Manifest Greyspace — Engine Plan

> Goal: Build a reusable, modular action-roguelike engine in Unity 6 URP,
> inspired by Hades' feel and mechanics. Game content, story, and lore are
> developed separately and dropped into this engine once it's ready.

---

## What "engine" means here

Hades has two layers:
1. **Engine** — combat feel, room flow, progression loop, systems (weapons, boons, enemies). This is what we're building.
2. **Content** — Zagreus, Olympian gods, Greek underworld. This is what you'll replace with your own game.

Everything below is engine. None of it assumes your story.

---

## Current state (MVP done ✅)

- Isometric camera (orthographic, 40°/45°, follows player)
- Player: WASD movement (camera-relative), mouse aim, left-click shoot, Shift dash + i-frames
- Enemy: chase AI, melee attack, world-space HP bar, hit flash, death animation
- Wave spawner, health HUD, YOU DIED screen, R to restart

---

## Phase 1 — Combat Engine (make fights feel like Hades)

This is the highest-leverage phase. Hades' combat feels incredible because of
five mechanics working together. Everything else is secondary.

### 1A: Melee attack system
- **What:** Left-click = sword slash (not just projectile). A fast 3-hit combo with distinct arc/timing per swing.
- **Why:** Hades is primarily melee. The rhythm of light attacks is what makes combat feel physical.
- **Template:** `MeleeAttack.cs` — hitbox active for N frames, combo counter, reset timer.

### 1B: Hit stop + screen shake
- **What:** When you hit an enemy, the game freezes for 3–6 frames. Screen shakes slightly.
- **Why:** This single mechanic makes hits feel like they *land*. Without it, combat feels like screensavers colliding.
- **Template:** `CombatFeel.cs` — static singleton. `CombatFeel.HitStop(0.05f)` from anywhere.

### 1C: Knockback
- **What:** Enemies stagger backward when hit. Player gets pushed back slightly when taking damage.
- **Why:** Gives hits spatial weight. Also creates distance-management decisions.
- **Template:** Add `knockbackForce` to damage events. `transform.position += dir * force`.

### 1D: Damage numbers
- **What:** Floating text showing damage dealt, fades upward.
- **Why:** Makes player progression visible. "+25" vs "+120" after an upgrade feels rewarding.
- **Template:** `DamageNumber.cs` — spawned prefab, rises and fades over 0.8s.

### 1E: Special attack (cast)
- **What:** Right-click fires a slower, high-damage projectile or places an AOE.
- **Why:** Hades always has a "cast" resource. Creates resource management in fights.
- **Template:** `SpecialAttack.cs` — separate cooldown, distinct visual.

**Deliverable:** A combat sandbox where fighting enemies feels punchy and reactive.

---

## Phase 2 — Room System (structure of a run)

A Hades run is a sequence of self-contained rooms. Each room is an arena —
enemies spawn, you clear them, doors open, you pick a reward and move on.

### 2A: Room template
- **What:** A walled arena with 2–4 door slots. Enemies spawn on entry. Doors locked until clear.
- **Template:** `Room.cs` — tracks enemy count, fires `OnRoomCleared` event.
- **Scene:** `RoomTemplate.unity` — reusable prefab with walls, floor, spawn points, door slots.

### 2B: Door / transition system
- **What:** Doors appear after room clear. Player walks into a door → loads next room.
- **Template:** `Door.cs` — holds `RoomType` (combat / shop / treasure / boss / rest).
- **Visual:** Door shows an icon for what's behind it (sword = combat, coin = shop, etc.)

### 2C: Dungeon graph (run layout)
- **What:** Generates a simple graph of rooms per floor: linear with occasional branches.
  ```
  Start → Combat → Combat → [Shop OR Treasure] → Combat → Boss
  ```
- **Template:** `DungeonGenerator.cs` — builds the graph at run start, rooms load on demand.
- **Not a full procedural engine** — fixed layouts per floor are fine for now, randomized later.

### 2D: Room types
| Type | What happens |
|------|-------------|
| Combat | Enemies spawn, clear to proceed |
| Shop | Spend currency on upgrades |
| Treasure | Free boon/item choice |
| Rest | Restore health (limited per run) |
| Boss | Named enemy, harder, ends the floor |
| Start | Safe room between floors |

**Deliverable:** A playable loop — enter room, fight, choose door, repeat until boss.

---

## Phase 3 — Weapon System (swappable playstyles)

Hades has 6 weapon types (sword, spear, bow, etc.), each with completely different
feel. This is what makes runs feel distinct.

### 3A: Weapon base class
- **What:** Abstract `WeaponBase.cs` with `OnPrimaryAttack()`, `OnSpecialAttack()`, `OnDash()`.
- Each weapon overrides these. The player script just calls the weapon — doesn't know which one.

### 3B: Starter weapons (3 archetypes)
| Weapon | Primary | Special | Dash |
|--------|---------|---------|------|
| Sword | 3-hit melee combo | Spinning AOE | Short dash |
| Bow | Charged ranged shot | Rain of arrows | Long dash |
| Shield | Block + bash | Throw shield (bounces) | Dash + slam |

### 3C: Weapon selection
- Player picks a weapon at the start of each run from 3 random options.
- Run starts only after weapon is chosen.
- **Template:** `WeaponSelectUI.cs` — 3 cards with weapon name, description, and icon.

**Deliverable:** 3 weapons that feel mechanically distinct. Player makes a meaningful choice at run start.

---

## Phase 4 — Boon / Upgrade System (run-to-run variety)

Boons are what make every Hades run feel different. After each room you pick one
upgrade from 3 options. They modify your existing abilities.

### 4A: Boon data structure
```
Boon {
  name: string
  description: string
  rarity: Common / Rare / Epic / Heroic
  effect: BoonEffect (scriptable object)
}
```
- **Template:** `BoonDefinition.cs` (ScriptableObject) — designers create boons without touching code.

### 4B: Effect types (start with these 6)
| Effect | What it does |
|--------|-------------|
| DamageMultiplier | +X% to all damage |
| AttackSpeed | Primary fires X% faster |
| DashDamage | Dash deals damage to enemies passed through |
| ProjectilePierce | Projectiles pass through N enemies |
| LifeOnKill | Restore N HP per kill |
| AOERadius | Special attack area grows |

### 4C: Boon selection UI
- After room clear, show 3 random boons (weighted by rarity).
- Player picks one. Applied immediately.
- **Template:** `BoonSelectUI.cs` — 3 cards, highlight on hover, confirm on click.

### 4D: Boon stack system
- Player can hold multiple boons. They stack where logical, replace where exclusive.
- **Template:** `BoonInventory.cs` — list of active effects, queried by combat scripts.

**Deliverable:** Each run feels mechanically unique by room 3. Builds toward a "build."

---

## Phase 5 — Enemy Variety (combat depth)

One enemy type makes combat solved in 30 seconds. Hades uses enemy *combinations*
to create situations, not just harder versions of the same thing.

### 5A: Enemy base class
- **What:** `EnemyBase.cs` — health, speed, attack damage, death reward, all configurable.
- All specific enemies inherit from this. Same death animation, hit flash, HP bar.

### 5B: Enemy archetypes (4 to start)
| Archetype | Behaviour |
|-----------|-----------|
| Charger | Idle → locks on → sprints in a straight line, high damage |
| Ranged | Keeps distance, fires slow projectiles at player |
| Shielder | Walks toward player, blocks frontal attacks, must be hit from behind |
| Bomber | Rushes player, explodes on contact or death, AOE damage |

### 5C: Boss template
- Named enemy with 3 phases (HP thresholds change behaviour).
- Intro animation (brief pause before fight starts — Hades does this).
- **Template:** `BossEnemy.cs` — extends `EnemyBase`, adds phase transitions.

### 5D: Enemy spawner (room-aware)
- Reads room's spawn points and difficulty rating.
- Spawns combinations, not just quantities: "2 Chargers + 1 Ranged" is a puzzle.
- **Template:** `EnemySpawner.cs` — takes a `WaveDefinition` ScriptableObject per room.

**Deliverable:** Rooms that require different tactics based on what spawns.

---

## Phase 6 — Meta-Progression (reason to keep playing)

This is what pulls players back after death. Hades calls it "the mirror." You earn
a persistent currency each run that funds permanent upgrades.

### 6A: Run state
- **What:** All boons, weapons, HP at time of death. Reset on death.
- **Template:** `RunState.cs` — singleton, persists during a run, wiped on death.

### 6B: Persistent currency
- Drop "shards" (name it whatever fits your game) from enemies and rooms.
- **Two types:** run currency (resets on death) and meta currency (persists forever).
- **Template:** `CurrencyManager.cs`

### 6C: Between-run upgrade screen
- Appears after death before next run starts.
- Spend meta currency on permanent buffs: max HP, base damage, starting boon, etc.
- **Template:** `MetaUpgradeScreen.cs` + `MetaUpgrade.cs` (ScriptableObject per upgrade)

### 6D: Save system
- Saves meta currency and purchased upgrades between sessions.
- **Template:** `SaveManager.cs` — JSON to `Application.persistentDataPath`.

**Deliverable:** Death doesn't feel like a full reset. Each run makes the next one slightly easier.

---

## Phase 7 — Polish Layer (what separates feeling from fighting)

This phase makes the engine *feel* like Hades rather than just working like it.

### 7A: VFX framework
- Hit sparks (small burst of particles on impact)
- Death burst (larger explosion of particles)
- Dash trail (ghosting effect behind player)
- Projectile trail

### 7B: Audio manager
- `AudioManager.cs` — singleton, plays SFX by name, manages music layers.
- Hades has dynamic music that layers instruments as combat intensifies.
- Minimum: background track + combat track that crossfades on room enter.

### 7C: Camera polish
- Room-enter zoom-out (camera pulls back slightly when entering new room)
- Death zoom-in (slow zoom on player death)
- Hit zoom pulse (very subtle, 1 frame)

### 7D: UI polish
- Animated health bar (smooth drain, not snap)
- Boon card animations (slide in on select)
- Room transition (fade to black, fade in)

---

## Phase 8 — Content Templates (handoff to game design)

These are the blank templates your game's content plugs into.
By this point, a designer can add content without touching engine code.

### 8A: Room template kit
- Prefab set: walls, floors, pillars, hazard zones (lava, spikes)
- Snap grid for fast level design
- Door placement tool

### 8B: Enemy template
- Duplicate `EnemyBase`, set stats in Inspector, done.
- No code required for basic enemy types.

### 8C: Boon template
- New ScriptableObject → fill in name, description, effect type, values → done.
- Designer creates boons without opening a script.

### 8D: Weapon template
- Implement `OnPrimaryAttack()` and `OnSpecialAttack()` in a new class.
- Visual model swaps automatically.

---

## Build order (what to do when)

```
Phase 1 (Combat feel)     ← highest value, do first
Phase 2 (Room system)     ← needed to have a "game"
Phase 3 (Weapons)         ← makes runs feel different
Phase 4 (Boons)           ← makes runs feel replayable
Phase 5 (Enemy variety)   ← makes combat require thinking
Phase 6 (Meta-progression)← makes death feel like progress
Phase 7 (Polish)          ← can happen in parallel with 5+6
Phase 8 (Content tools)   ← final step, for your team to use
```

Phases 1–2 = a playable combat loop.
Phases 1–4 = a full roguelike run.
Phases 1–6 = a complete engine.
Phases 7–8 = production-ready.

---

## What stays out of scope (intentionally)

- Story, characters, lore, dialogue — not our job yet
- Art direction — we use primitives until character design is locked
- Multiplayer — single-player only
- Platform ports — macOS/Windows PC first
- Full procedural generation — hand-crafted room layouts, procedurally assembled

---

## File structure (target)

```
Assets/
  Scripts/
    Combat/         ← attack, damage, hit stop, knockback
    Player/         ← controller, weapons, boons
    Enemies/        ← base class, archetypes, boss
    Rooms/          ← room, doors, dungeon generator
    Systems/        ← save, currency, run state
    UI/             ← HUD, boon select, meta screen
    VFX/            ← hit effects, trails, audio
  Prefabs/
    Enemies/
    Rooms/
    UI/
    VFX/
  ScriptableObjects/
    Weapons/
    Boons/
    Enemies/
    Upgrades/
  Scenes/
    MainMenu
    GameLoop        ← the actual dungeon
    BetweenRuns     ← upgrade screen
Docs/
  ENGINE-PLAN.md    ← this file
```

---

*Last updated: MVP complete. Phase 1 next.*
