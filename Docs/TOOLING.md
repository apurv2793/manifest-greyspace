# Tooling

## Versions (installed 2026-10-08)
- Unity Editor: 6000.4.11f1 (arm64, /Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app)
- Unity CLI: 1.0.0-beta.13 (~/.unity/bin/unity; PATH line added to ~/.zshrc)
- com.unity.pipeline: 0.8.0-exp.1
- unity-agent-plugin (Claude Code, user scope): 0.1.8-beta
- Claude Code MCP: `unity-editor-mcp` → `/Users/apurv2793/.unity/bin/unity mcp` (stdio, user scope). Use the ABSOLUTE path: the Claude desktop app doesn't read ~/.zshrc, so a bare `unity` command fails to launch ("Executable not found").

## Rules
- Run `unity` commands from `~/greyspace` (symlink — the real path has a space).
- Commands are beta: refresh `Docs/tooling/unity-commands.json` after any CLI update and diff it.
- Commit before AI edits to scenes/assets.
- In non-interactive shells always pass `--non-interactive`; otherwise the CLI can hang on prompts.

## Prerequisite (owner action)
The Editor needs a signed-in Unity account + active licence. Status on install day:
`auth.loggedIn false`, no active licence → `unity open` waits forever. Owner signs in once
(Unity Hub, or `unity auth login` in their own terminal) and activates a Personal licence.

## eval
Flag for code argument: `(fill once the Editor is connected — run: unity command eval --help)`
