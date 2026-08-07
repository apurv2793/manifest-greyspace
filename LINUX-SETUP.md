# Working on this repo from 419c (Linux/ARM)

## What this box can and cannot do

**Cannot**: run Unity Editor. This is aarch64 (ARM) Linux — Unity Editor ships
official builds for Linux x86_64 only, no ARM Linux binary exists (checked: only
Ubuntu desktop-shell Unity libraries are present, unrelated to the game engine).
No compiling, no Play mode, no screenshots, no headless batchmode builds here.

**Can**: edit/write C# scripts, review diffs, run the Manifest OS coordination loop
(compare -> review -> feedback -- pure HTTP, no Unity needed), commit and push.
All compiling and Play-mode verification happens back on the Mac, in Unity.

## Setup

```bash
# already done if you're reading this from a checkout -- otherwise:
git clone https://github.com/apurv2793/manifest-greyspace.git
cd manifest-greyspace-clean
```

## Environment (NIM + local LLM, same pattern as manifest-cod-experiment)

`.env` at repo root (gitignored):

```
NVIDIA_API_KEY=<same key as manifest-cod-experiment/.env on this box>
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=qwen3-koinon-t1:latest
```

Verify:
```bash
curl -s -o /dev/null -w '%{http_code}\n' -H "Authorization: Bearer $NVIDIA_API_KEY" https://integrate.api.nvidia.com/v1/models
curl -s http://localhost:11434/api/tags   # confirms ollama daemon is up
```

## Manifest OS coordination -- network caveat

The Manifest OS server normally runs on the Mac (`manifest/`), bound to
`127.0.0.1:8770` -- **not reachable from 419c over Tailscale as currently
configured**. Two options, pick one before trying to call `/api/os/compare`
from here:

1. Ask for the Mac's uvicorn to bind `0.0.0.0:8770` instead of `127.0.0.1`
   (security tradeoff -- exposes it to the whole Tailscale tailnet, not just
   localhost). Then call it from 419c as `http://100.x.x.x:8770/api/os/...`
   using the Mac's Tailscale IP.
2. Run a second Manifest OS instance locally on 419c (same repo, own `.env`
   with the NVIDIA_API_KEY above) if working disconnected from the Mac.

Don't assume either is already done -- confirm which one applies before
routing a spec through /compare from this box.

## Workflow

1. Edit/write `.cs` files under `Assets/Scripts/` (or route specs through
   Manifest OS `/api/os/compare` per option above, review outputs the same
   way this project always has -- correctness, style, constraint adherence).
2. Commit with the project's existing message conventions (see `git log`).
3. Push.
4. Tell whoever has the Mac open in Unity to pull and verify in Play mode --
   this box cannot self-verify a Unity change.

## Hard constraints (same as always for this project)

No namespaces. No `Shader.Find()` -- shader-steal pattern
(`new Material(existingRenderer.sharedMaterial)`, set both `_BaseColor` and
`.color`). Physics-free movement (`transform.position +=` only).
`Input.GetKey(KeyCode.*)` only. All geometry via
`GameObject.CreatePrimitive()`. `WaitForSecondsRealtime` for anything
surviving `Time.timeScale = 0`. See `Docs/COORDINATION.md` and
`Docs/ENGINE-PHASES-REMAINING.md` for full context and current build status.
