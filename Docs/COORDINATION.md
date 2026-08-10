# Manifest OS — Engine Build Coordination

## What this is

**Status: design doc, not yet running code.** Nothing currently listens on
`localhost:8770` and no server implementation exists on disk — the phase build
queue below happened via ad-hoc calls to individual providers, not a real backend.
This doc was reviewed by 5 independent AI reviewers before any rebuild (nemotron via
NIM, Qwen local, Fable, Groq, Kimi — see "Independent review synthesis" below) and
the design changed as a direct result. Read that section before building anything
from the sections that follow — it changes what "Manifest OS" primarily does.

The original idea: fan each engine feature out to multiple AI models, review the
outputs, pick the best, and send feedback so a router learns. Claude Code acts as
orchestrator + arbiter.

**What actually changed after review:** the project's own data (see "Phase build
queue" below) shows one model won essentially every recorded generation comparison —
fanning out *generation* added cost and latency without changing the outcome. What
*did* find real bugs this session, repeatedly, was fanning multiple independent
models out to *review* one already-written implementation. So the rebuilt backend's
primary mode is review fan-out; generation keeps a single routed model with one
cheap fallback-on-timeout, not a full multi-model compare, per finding 1 below.

## Workflow (per feature, revised)

```
1. Generate: single routed model (RouterMemory's current best for this task_kind)
   writes the implementation, from a spec prompt with all project constraints.
   On timeout/failure, retry once with the next-ranked provider — not a full fan-out.
2. Verify (automated, before any human/AI reads it):
   - JS: npm test + tools/capture.mjs + tools/imagediff.mjs (all real, all local)
   - Unity: static lint (constraint greps: no Shader.Find, no namespace, no
     Rigidbody, no dangling symbol refs) + a "compiled-on-Mac: pending" flag —
     this machine has no C# compiler, so Unity can never get a real pass/fail here
   Anything that fails automated verify is auto-rejected before step 3, no API calls
   spent on review.
3. Review fan-out: 2-4 independently-configured models each review the same
   artifact for real bugs, THIS is where the multi-model comparison actually pays
   off (see finding 1). Each verifies its own claims against source before reporting
   — the primer used for this always includes "don't just say something's wrong,
   read the current file and cite file:line."
4. Arbiter (Claude Code) cross-references every reviewer's findings against each
   other and against corrections.md before trusting anything, applies confirmed
   fixes, records per-finding true/false verdicts back to RouterMemory (not just a
   single quality_score — see finding 2).
5. Repeat — RouterMemory gets smarter about which providers are reliable
   generators vs. reliable reviewers for each task_kind, and which providers have
   gone flaky/unreachable (see "Provider health" below).
```

## Endpoint quick reference

Backend: http://localhost:8770 (not yet built — see status note above)

### Generate (single model + one fallback, not full fan-out)
```bash
curl -s -X POST http://localhost:8770/api/os/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "<your spec here>",
    "task_kind": "unity_code",
    "provider": "auto",
    "max_tokens": 3000
  }'
```
`"provider": "auto"` picks RouterMemory's current best for this task_kind among
*healthy* providers (see Provider health) and retries once with the next-ranked
provider on timeout/error before giving up.

### Review (the primary fan-out mode)
```bash
curl -s -X POST http://localhost:8770/api/os/review \
  -H "Content-Type: application/json" \
  -d '{
    "artifact": { "type": "file", "path": "Assets/Scripts/AudioManager.cs", "content": "..." },
    "prompt": "Review for: real bugs, incorrect API usage, logic errors. Verify every claim against the content above before reporting it.",
    "providers": ["nim", "groq", "kimi"],
    "max_tokens": 3000
  }'
```
Returns per-provider findings + confidence, not a single winner — the arbiter
cross-references and verifies before deciding what's real (step 4 above).

### Feedback (train router — separate calls for generation vs. review outcomes)
```bash
# Generation outcome
curl -s -X POST http://localhost:8770/api/os/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "call_id": <id from generate output>,
    "quality_score": 0.9,
    "verdict": "approved",
    "notes": "clean abstract class, correct shader-steal pattern"
  }'

# Review outcome — records per-finding verdicts, not one scalar (finding 2)
curl -s -X POST http://localhost:8770/api/os/review/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "review_id": 42,
    "accepted_findings": [ { "finding_id": "nim-3", "file": "src/ai/index.js", "line": 118 } ],
    "rejected_findings": [ { "finding_id": "groq-1", "reason": "false_positive" } ],
    "model_scores": { "nim": 0.9, "groq": 0.3, "kimi": 0.95 }
  }'
```

### Check what the router learned
```bash
curl -s http://localhost:8770/api/os/memory/best/unity_code          # best generator
curl -s http://localhost:8770/api/os/review/best/unity_code          # best reviewer (true-positive rate)
curl -s http://localhost:8770/api/os/healthz                         # provider health snapshot
```

---

## Independent review synthesis (5 reviewers, before any rebuild)

Reviewed by nemotron (NIM), Qwen (local), Fable, Groq, and Kimi. Verified claims
against real source where checkable — see both repos' `docs/qwen-memory/
corrections.md` for the process. Findings that survived verification:

1. **Generation fan-out is low-value; review fan-out is where this pattern
   actually pays off — all 5 reviewers reached this independently.** The evidence
   is this doc's own phase build queue: GLM won **10 of 10** explicit head-to-head
   comparisons against Groq (verified by hand-count three separate times this
   session — two reviewers gave different, both-wrong counts, 13 and 9, before this
   number was confirmed). Fanning generation out to 2+ providers changed the
   winning outcome zero times. What did find real, confirmed bugs — the Vector2
   regression, 3 compile blockers, the Checkpoint cache-poisoning bug, the
   hit-flash pooling defeat, the audio envelope/clipping issues — was exactly this
   pattern applied to *review*, not generation. One dissent worth keeping (Kimi):
   don't eliminate generation fan-out entirely — SaveManager.cs and VFXManager.cs
   only got usable output from a second provider because the first (NIM) timed out
   twice; keep one cheap fallback-on-timeout, not a full compare, for generation.
2. **`quality_score` as a single hand-typed float doesn't scale, and undercounts
   what's actually learnable.** The rejection notes already in the phase table are
   mostly mechanically detectable (markdown fences: 5+ rows; missing `using`
   lines: 4 rows; namespace slips; Rigidbody usage) — the backend should
   auto-reject on a lint layer before any model/human reads the output, and store
   review outcomes as **per-finding true/false verdicts**, not one scalar (see the
   `/api/os/review/feedback` shape above) — with 5 reviewers and ~20 historical
   generations, a single quality number is too little signal; per-finding
   true/false records are the only statistically honest data at this sample size.
3. **No handling anywhere for a provider being flaky or gone — and this isn't
   hypothetical, it happened to 3 of the 4 providers actually tried this session**
   (GLM unreachable on NIM after repeated attempts, Groq itself timed out twice
   during the original build, Kimi 404'd on NIM despite being listed as available).
   Needs: per-provider health tracking (success rate, last error, avg latency),
   automatic quarantine after N consecutive failures, and — this is the part
   easiest to get wrong — failures must be recorded as outcomes, not silently
   dropped. A provider that goes quiet should show up as "quarantined, 3x timeout"
   in `/api/os/memory/best`, not just stop appearing in the data.
4. **JS extension needs its own template, not a find-replace of the Unity one** —
   different hard constraints (no `Math.random()`, use `ctx.rng`; no new npm deps;
   `ShaderMaterial` not `RawShaderMaterial` attribute conventions; never read
   `reference/`). But JS is arguably the *better* fit for this backend, not a
   lesser one: it has a fully local, objective, already-existing verify step
   (`npm test`, deterministic pixel capture, `imagediff` exits non-zero on any
   pixel change) that Unity fundamentally cannot have on this machine (no C#
   compiler, no Editor — verification is static-lint-plus-deferred-Mac-compile at
   best). Design the verify step in the workflow above as a per-project pluggable
   command for exactly this reason.
5. **Smaller concrete additions worth keeping:** redact provider API keys in any
   logged call/feedback payload (never store them in RouterMemory); a `call_id`
   that hashes (prompt + task_kind + provider + model_version) so identical
   requests can reuse cached output instead of re-spending tokens; a `/healthz`
   endpoint; explicitly document that the backend binds `127.0.0.1` only (true
   today, but never actually stated); a machine-readable schema for the hard-
   constraint block in spec templates (today it's prose — nothing enforces a
   generation actually satisfied "no namespaces," which is exactly the failure
   mode the model-characteristics table below already documents for two providers).

Full per-reviewer output archived in `/tmp/.../scratchpad/manifest-os-review/` this
session (nemotron/qwen/fable/groq raw text + Kimi's full 3-section review) if any
finding above needs re-tracing to its source.

---

## Spec prompt template (Unity code)

```
Write a Unity 6 C# [CLASS NAME] for Greyspace (URP isometric action game).

PURPOSE: [one sentence]

RULES (hard constraints — never violate):
- No namespaces
- No Shader.Find() — use shader-steal: new Material(existingRenderer.sharedMaterial)
- Physics-free movement: transform.position += only, no Rigidbody
- Input: Input.GetKey(KeyCode.*) only
- All geometry via GameObject.CreatePrimitive()
- WaitForSecondsRealtime for anything that must survive Time.timeScale=0

FIELDS (public, Inspector-settable):
[list fields]

BEHAVIOUR:
[describe exactly what it does, state machine if applicable]

OUTPUT: clean, compilable C# only. No explanations, no markdown fences.
```

---

## Model characteristics (as RouterMemory learns)

| Model | Strength | Watch for |
|-------|----------|-----------|
| GLM 5.1 (NIM) | Clean C# structure, follows constraints | Occasional namespace slip |
| Qwen3-Coder (NIM/Ollama) | Good state machines, concise | May use Rigidbody if not explicit |
| Llama-3.3-70b (Groq) | Fast, readable | Looser constraint adherence |
| Mistral-Medium (NIM) | Good for system/manager classes | Verbose |

---

## Phase build queue

Track which features have gone through the coordination loop:

| Feature | Status | Best model | Score |
|---------|--------|------------|-------|
| WeaponBase.cs | parked | GLM 5.1 (NIM) | 0.65 — orphaned; ComboData carries weapons instead, migrate later |
| WeaponPickup.cs | ✅ applied (rewired to ComboData, WeaponBase had no consumer) | GLM 5.1 (NIM) | 0.82 → fixed |
| EnemyBase.cs | ✅ applied | GLM 5.1 (NIM) | 0.72 → fixed |
| ChargerEnemy.cs | ✅ applied | GLM 5.1 (NIM) | 0.78 → fixed |
| RangedEnemy.cs | ✅ applied | GLM 5.1 (NIM) | 0.72 → fixed |
| ShielderEnemy.cs | ✅ applied | GLM 5.1 (NIM) | 0.88 vs Groq 0.2 (rejected — broken Invoke/GetComponentsInChildren) |
| SpawnGroup.cs | ✅ applied, wired into WaveLoop | GLM 5.1 (NIM) | 0.85 vs Groq 0.4 (rejected — missing using UnityEngine) |
| SkillNode.cs | ✅ applied | GLM 5.1 (NIM) | 0.8 vs Groq 0.25 (rejected — invalid attribute syntax, missing using) → nested EffectType enum on apply |
| SkillTree.cs | ✅ applied | GLM 5.1 (NIM) | 0.9 vs Groq 0.5 (rejected — missing using System for Math.Max) |
| PlayerInventory.cs | ✅ applied, wired as GunCharacter.inventory | GLM 5.1 (NIM) | 0.85 vs Groq 0.7 (markdown fences again) |
| PenaltyManager.cs | ✅ applied (hand-written stub, no coordination call needed) | — | — |
| WorldState.cs | ✅ applied | GLM 5.1 (NIM) | 0.92 vs Groq 0.55 (fences again) |
| StoryFlags.cs GetAll/LoadFrom | ✅ applied (hand-written, mechanical addition) | — | — |
| SaveManager.cs | ✅ applied | Groq 70B (NIM timed out twice) | 0.55 → fixed (missing [Serializable], missing usings) |
| Checkpoint.cs | ✅ applied, wired reset into EnterMission | GLM 5.1 (NIM) | 0.9 vs Groq 0.5 (fences, no collider cleanup) |
| VFXManager.cs | ✅ applied, wired into melee/death/dash/levelup | Groq 70B (NIM timed out) | 0.7 → fixed real bug (ring-expand offset missing Time.deltaTime, would've been explosive) |
| AudioManager.cs | ✅ applied (hand-written stub, no coordination call needed) | — | — |
| CameraShake.cs | ✅ applied, wired into player-hit/death | GLM 5.1 (NIM) | 0.9 vs Groq 0.1 (rejected — does not compile, StartCoroutine on bare GameObject) |
| ZoneBounds.cs | ✅ applied, wired into EnemyBase + GunCharacter camera | GLM 5.1 (NIM) | 0.9 vs Groq 0.75 (fences) |
| NPCStub.cs | ✅ applied | GLM 5.1 (NIM) | 0.82 vs Groq 0.65 (fences, stale IsPlayerNearby on null player) → .sharedMaterial fixed to .material |
