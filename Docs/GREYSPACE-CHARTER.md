# Greyspace — Quality Charter (v1 draft, 2026-10-07)

> **Goal in one line:** make Greyspace the best Claude-built Unity game out there — a
> moody isometric action game that beats the strongest Opus 5.5 games on looks *and*
> on how it plays — using a loop where the AI runs the game, looks at it, and fixes
> the weakest thing, again and again.

Status: **draft for approval.** Nothing here is built yet. Decisions marked ❓ need a
call from Apurv before work starts.

---

## 1. What the research showed

We read 8 repos: the 5 requested, plus 3 they pointed to that turned out to matter more.

| Repo | What it really is | What we take from it |
|---|---|---|
| [frontier-games](https://github.com/theolundqvist/frontier-games) | Gallery of 127 Opus 5.5 / GPT-6 Astra games, scored by Opus from video frames | **The scorecard we'll measure against.** Opus 5.5 averages 6.6/10, best is *Fall Line* 8.1. Gameplay is everyone's lowest score. **No Unity games in it at all.** |
| [awesome-opus-5.5-games](https://github.com/AgentsLoop/awesome-opus-5.5-games) | List of 909 AI-made games, 117 by Opus 5.5 | **Zero Opus 5.5 Unity isometric action games exist.** Best-looking: *Turbo Kart Rally* ("polished stylised indie"). Characters and animation are weak everywhere. One Unity project's AI critic said "WOWED" on a build that would have **crashed on frame one**. |
| [Nipale-ai overnight builds](https://github.com/Nipale-ai/opus-5-5-overnight-builds) (via the above) | How *Fall Line* was made | **The method that wins:** a long brief that sets a *floor* not a ceiling, hours of solo running, the AI screenshots its own game, measures the pixels, critiques like an art director, fixes the weakest part, repeats. "When you think you are finished, you are not." |
| [unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin) (official) | Unity's own Claude plugin + the **Unity CLI** | **How the AI finally gets to press Play.** Lets Claude play/stop the game, screenshot the Game View, read errors, run tests, tweak values live. Free, runs only on your Mac. Beta. |
| [ai-game-dev-plugin](https://github.com/IvanMurzak/ai-game-dev-plugin) → Unity-MCP | Setup recipe for a third-party Unity bridge | Good ideas (custom AI-callable game commands, "compile errors block Play"). **Not Unity-authorised** — the maintainer confirmed Unity hasn't approved it. Don't use. |
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | Most popular third-party bridge (14.7K stars) | Explicitly "not affiliated with Unity". Same ToS problem. Don't use. |
| [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios) | 49-agent "game studio" (25.8K stars) | Their own test: **the heavy-process build ranked last.** Take only: pillars, art bible, design templates, and the rule "no visible change is done until it's run and screenshotted". |
| [claude-unity-game-studio](https://github.com/IdoCohen560/claude-unity-game-studio) | Repackage of the above + one example | Its showcase is at Greyspace's *current* primitive level. Take its **game-feel matrix** (every action gets matched visual + sound + camera + hit-stop, all timed to fade together). |
| [claude-code-game-development](https://github.com/HermeticOrmus/claude-code-game-development) | 87 plugins, mostly unrelated copies, no games made | Small items: a Unity anti-patterns list, object pooling, playtest heatmaps. |

**The three things every top game had in common:**
1. **The AI looks at its own game** — screenshots at fixed moments + number checks
   (blown-out whites, crushed blacks, frame rate, console errors).
2. **Assets made by re-runnable scripts or real models**, not just code-built cubes.
   Cubes cap the visual score; characters are where everyone loses points.
3. **A written contract before splitting work** across multiple AI workers.

**Where Greyspace can win:** nobody has shipped a good-looking Opus-5.5 Unity
isometric action game, and nobody has solved characters. We already own a 3D asset
pipeline most of these projects lacked (see §5).

---

## 2. The quality bar

We measure with the same rubric frontier-games uses, so the comparison is fair:

| Category | Weight | Today (estimate) | Target |
|---|---|---|---|
| Visuals | 30% | ~3 (grey primitives) | **8.5** |
| Gameplay | 35% | ~5 (solid combo/enemy base) | **8.0** |
| Polish | 15% | ~4 | **8.0** |
| Ambition | 20% | ~5 | **8.0** |
| **Overall** | | **~4.3** | **≥ 8.2** (beat *Fall Line* 8.1) |

Two rules on top of the score, learned the hard way by others:
- **A pretty screenshot doesn't count.** Every milestone must also pass a *played*
  check — a scripted bot run plus Apurv actually playing it.
- **Honest scoring.** Blind comparisons against frames from the top games, done by a
  separate critic that has never seen our code. No self-grading.

---

## 3. Game pillars (❓ confirm or edit)

Short statements every design and art decision is checked against.

1. **Every hit lands.** Combat feel first: hit-stop, knockback, enemy wind-ups you
   can read, dodge with invulnerability frames, combo windows that feel fair.
2. **Grey world, sharp light.** A desaturated "greyspace" where light, sparks and
   magic carry all the colour. Readable at a glance from the isometric camera.
3. **Readable silhouettes.** You can tell every enemy type, every attack and the
   player apart in one second, even in a crowd.
4. **Small but finished.** A hub and a handful of missions that feel like a shipped
   game — menus, sound, save, onboarding — beat a large rough world.

---

## 4. How the AI will build: the See-Play-Fix loop

Today the AI writes code blind and you press Play. The core change is that the AI
gets eyes and hands in Unity.

### 4.1 The bridge (how the AI drives Unity)
- **Unity's official route only:** `unity-agent-plugin` + Unity CLI + `com.unity.pipeline`,
  registered with Claude Code via `unity mcp configure claude-code`.
- What that unlocks: play/stop the game, screenshot the Game View, read the error
  console, run tests, inspect the scene, change values live while playing.
- Two gaps we fill ourselves: **input simulation** (a small Greyspace command that
  presses keys/buttons in-game) and **beta instability** (always look up command
  names at runtime; commit to git before every big AI edit).
- ⚠️ **ToS note.** Unity's terms (17.2(ff)) ban AI agents and CLIs touching Unity
  unless via "Authorized Agentic Access", which they don't define on the page. Unity's
  own launch post for the CLI names Claude as supported and calls it free. That's the
  strongest position available — third-party bridges have none. ❓ Apurv to accept
  this, or ask Unity for written confirmation first.

### 4.2 Built-in test hooks in the game
A `GreyspaceDebug` command set the AI can call: jump to any mission, spawn an enemy,
grant a skill, set the camera, run an autopilot combo bot, take screenshots at fixed
moments, dump game state. (Every top game had this; it's what lets the AI *verify*
combat, not just look at it.)

### 4.3 The loop, per feature
1. **Brief** — a one-page "floor not ceiling" spec: what must exist, the feel targets,
   the reference frames it's judged against.
2. **Build** — code + assets.
3. **Play** — AI starts the game, proves frames are advancing, runs the bot.
4. **Measure** — screenshots at fixed moments; numbers: errors, frame rate,
   blown-out/black pixel share, combo timing.
5. **Critique** — a separate blind critic compares against reference frames, names
   the single weakest thing.
6. **Fix the weakest thing**, repeat 3–6 until the bar is hit.
7. **Human play** — Apurv plays it, logs feedback in the testing booklet.
8. **Commit with evidence** — screenshots saved to `Docs/evidence/`.

Memory between sessions lives in `DESIGN.md`, `PROGRESS.md` and an honest `NOTES.md`
(what's weak, what was measured).

### 4.4 Who does what (models)
| Role | Model | Why |
|---|---|---|
| Director, architect, blind critic, hard coupled systems (combat, camera, lighting) | **Opus 5.5** | Best at long solo runs and judgement; costly, so used where it matters |
| Bounded implementation tasks | **Sonnet 5.5** | Cheaper, fast, good inside a clear brief |
| Small self-contained scripts (pickups, UI widgets, data classes) | **GLM 5.1 via Manifest OS** | Proven 0.92 on our own tests; routed through compare → feedback |
| Script authoring on the Linux box (419c) | Linux Claude | Can't run Unity (no ARM build) — writes and pushes code, Mac verifies |

Parallel AI workers only *after* a written contract with file ownership (the Turbo
Kart / Claude-of-Duty lesson). Combat, camera and lighting are one coupled system —
one owner at a time.

---

## 5. Art direction & asset pipeline (❓ biggest decision)

Primitives alone cap Greyspace around visuals 5–6. Options:

| Option | What it means | Upside | Downside |
|---|---|---|---|
| **A. Stay pure code** | Keep building everything from cubes/capsules, add lighting + post-processing + shaders | No new tools, fully "AI-made" | Hits a ceiling; characters stay blobs |
| **B. AI-scripted Blender** | AI writes Blender scripts that build models, export to Unity | Re-runnable, still "AI-made", proven by top entries (Sprout Quest, Gravewake) | Animation still hard |
| **C. Manifest's own 3D pipeline** *(recommended)* | Use what Manifest already has: SDXL concept art, TripoSR / SF3D / Hunyuan3D 3D generation, the frozen **Mara** base body already rigged in Unity, Mixamo-style animation | Solves characters — the exact weak spot of every competitor. Assets we own | Needs a consistent art bible so generated pieces match; some cleanup |
| **D. Bought/free asset packs** | Kenney, Quaternius, etc. | Fastest | Generic look; loses the "made by our system" story |

**Recommendation: C for characters and hero props, B for environment kit pieces,
A's lighting/shader work on top of everything.**

Art bible to write first: palette (grey base + accent colours that mean something),
shape language per faction, silhouette rules, lighting moods per mission, VFX style.

Camera ❓: keep **orthographic** isometric, or switch to a **narrow-angle perspective**
camera (~30–35°) that still looks isometric but lets fog, depth-of-field and volumetric
light work properly. One top isometric game chose perspective for exactly this.
Recommendation: test both in the vertical slice, pick by screenshot.

---

## 6. Phases & milestones

Each phase ends only when its exit check passes — screenshots + bot run + human play.

### Phase 0 — Foundation (eyes, hands, house in order)
- Install the official Unity bridge; prove the AI can Play, screenshot, read errors.
- `GreyspaceDebug` command set + input simulation + autopilot combo bot.
- Split the 24 KB `GreyspaceScene.cs` "do-everything" file into hub, mission,
  HUD and world-building parts (the knowledge graph flagged it as weakest).
- Write pillars, art bible, combat feel matrix, `DESIGN.md` / `PROGRESS.md` / `NOTES.md`.
- Gather reference frames from the top games for the blind critic.
- **Exit:** AI runs a full mission start-to-finish on its own and reports back with
  screenshots, frame rate and zero errors.

### Phase 1 — Vertical slice: "one room, one fight"
- URP lighting pass: post-processing volume (bloom, tone mapping, colour grade), fog,
  light pools, shadows. Camera decision made here.
- Player character: real model + rig + core animations (idle, run, 3-hit combo,
  dodge, hit, death).
- One enemy fully finished to the same standard.
- Full feel matrix implemented for that fight.
- **Exit:** blind critic ≥ 7.5 on the slice vs reference frames; bot + human play pass.

### Phase 2 — Combat depth
- All 4 enemy types rebuilt to slice standard; first boss.
- Combo system v2, skill tree wired to visible abilities, dodge/parry.
- Data-driven balance + playtest telemetry (death heatmaps, time per fight).
- **Exit:** gameplay sub-score ≥ 7.5; bot runs 30 fights without softlocks.

### Phase 3 — World
- Hub + 2–3 missions with full art pass, zones, set dressing, distinct moods.
- Environment kit (Blender-scripted pieces) + props.
- **Exit:** every mission passes the "three-second test" — any random frame reads as
  a finished game.

### Phase 4 — Systems & experience
- Main menu, pause, settings, skill-tree UI, onboarding, save slots.
- Real audio: music per area, synthesised or generated SFX matched to the feel matrix.
- **Exit:** a new player can start, learn, play and save without explanation.

### Phase 5 — Polish, performance, external score
- Performance budget (frame-time p95, not just average), bug sweep.
- Final blind scoring against the frontier-games top 10; publish if it clears 8.2.
- **Exit:** overall ≥ 8.2, zero known crash bugs.

---

## 7. Light process (what we keep from the "game studio" repos)

Their own data says heavy process made worse games. We keep four review gates, each a
single pass:
1. **Pillar fit** — does this match the pillars?
2. **Visual consistency** — does it match the art bible? (judged from screenshots)
3. **Code review** — correctness + our hard rules.
4. **Play evidence** — it ran, it was screenshotted, it was played.

Full rigor only for combat, camera and lighting. Everything else stays light.

---

## 8. Risks

| Risk | What we do about it |
|---|---|
| Unity ToS ambiguity | Official route only; ❓ optional written confirmation from Unity |
| Unity CLI is beta, commands change | Discover commands at runtime; git commit before big edits |
| "Looks great, doesn't run" | Play evidence gate; bot runs; human play every milestone |
| Generated assets don't match each other | Art bible first; one style reference set; consistent lighting does a lot of the matching |
| Cost of long Opus runs ($10–70 per long session in others' data) | Opus only for director/critic/coupled systems; Sonnet + GLM for the rest |
| Unity can't run on 419c (ARM Linux) | 419c writes code only; all Play/verify on the Mac |
| Character animation is hard for everyone | Start from the already-rigged Mara body; animation library before custom work |

---

## 9. Decisions needed from Apurv (❓)

1. **Accept the official Unity CLI route** under the current ToS wording, or ask Unity first?
2. **Art route:** recommended C (Manifest 3D pipeline + Mara) + B (Blender-scripted kit) + lighting.
3. **Pillars:** keep the four above, or edit?
4. **Camera:** test orthographic vs narrow perspective in the slice — OK?
5. **Spend:** comfortable with long Opus 5.5 runs for the slice (likely several sessions)?

Once these are answered, Phase 0 gets a step-by-step implementation plan.

---

*Sources: the 8 repos above; Unity ToS §17.2(ff) — https://unity.com/legal/terms-of-service;
Unity CLI announcement — https://discussions.unity.com/t/announcing-the-unity-cli-a-new-way-to-connect-your-tools-and-agents/1731104*
