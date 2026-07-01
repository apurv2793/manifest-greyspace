# Manifest OS — Engine Build Coordination

## What this is

Instead of writing every script directly, we fan each engine feature out to
multiple AI models via Manifest OS, review the outputs, pick the best, and
send feedback so the router learns. Claude Code acts as orchestrator + arbiter.

## Workflow (per feature)

```
1. Write spec prompt  →  include all Unity constraints (see template below)
2. POST /api/os/compare  →  multiple models generate implementations in parallel
3. Claude Code reviews outputs  →  picks best (correctness + style + constraints)
4. POST /api/os/feedback  →  RouterMemory records which model won
5. Apply winning code to project  →  compile-test in Unity
6. Repeat  →  router gets smarter about Unity code over time
```

## Endpoint quick reference

Backend: http://localhost:8770

### Compare (fan out)
```bash
curl -s -X POST http://localhost:8770/api/os/compare \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "<your spec here>",
    "task_kind": "unity_code",
    "providers": ["nim", "groq"],
    "max_tokens": 3000
  }'
```

### Feedback (train router)
```bash
curl -s -X POST http://localhost:8770/api/os/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "call_id": <id from compare output>,
    "quality_score": 0.9,
    "verdict": "approved",
    "notes": "clean abstract class, correct shader-steal pattern"
  }'
```

### Check what the router learned
```bash
curl -s http://localhost:8770/api/os/memory/best/unity_code
```

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
