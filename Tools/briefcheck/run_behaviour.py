#!/usr/bin/env python3
"""Run the `behaviour` checks from a NexusHub exam file against a connected Unity Editor.

The draft code for each brief must already be compiled into the project the Editor has open.

Usage:
  python3 Tools/briefcheck/run_behaviour.py <exam.json> --project <abs project path> [--ids id1,id2]

Each brief's behaviour block:
  mode   editor | play | tests | null(skip)
  steps  [{"eval": "<C# body returning a value>"} | {"waitSeconds": n}]
  A step result starting with "FAIL" fails the brief. Every check step must return "PASS" or a non-FAIL value.
Exit code 0 when every run brief passed, 1 otherwise. Prints one JSON line per brief.
"""
import argparse, json, subprocess, sys, time

def unity(project, *args, timeout=90):
    cmd = ["unity", "--non-interactive", *args]
    p = subprocess.run(cmd, cwd=project, capture_output=True, text=True, timeout=timeout)
    return p.returncode, p.stdout + p.stderr

def eval_code(project, code):
    rc, out = unity(project, "command", "--result-only", "eval", "--code", code)
    try:
        d = json.loads(out[out.index("{"):])
    except ValueError:
        return f"FAIL: unparseable eval output: {out[:200]}"
    if not d.get("success"):
        diags = "; ".join(str(x.get("message", x)) for x in (d.get("diagnostics") or []))[:300]
        return f"FAIL: eval error {diags or out[:200]}"
    return str(d.get("result"))

def ensure_play(project):
    unity(project, "command", "--result-only", "set_autotick")
    unity(project, "command", "--result-only", "editor_play")
    unity(project, "status", "--until-ready", "--project-path", project, "--format", "json", timeout=150)
    rc, out = unity(project, "command", "--result-only", "wait_for", "--",
                    "--condition", '{"member":"UnityEngine.Time.frameCount","op":"changed"}', "--timeout_s", "10", timeout=60)
    if '"met": true' not in out:
        raise SystemExit("INVALID RUN: frames not advancing (App Nap / focus?) — not a model failure")

def error_count(project):
    rc, out = unity(project, "command", "--result-only", "console_status")
    try:
        return json.loads(out[out.index("{"):]).get("consoleErrors", 0)
    except ValueError:
        return -1

def run_case(project, case):
    b = case.get("behaviour")
    if not b:
        return {"id": case["id"], "status": "skipped", "reason": "no behaviour block"}
    if b["mode"] == "tests":
        f = b["runTests"]
        rc, out = unity(project, "test", "--mode", f["mode"], "--test-filter", f["filter"], timeout=600)
        return {"id": case["id"], "status": "pass" if rc == 0 else "fail", "detail": out[-400:]}
    errs0 = error_count(project)
    results = []
    for step in b["steps"]:
        if "waitSeconds" in step:
            time.sleep(step["waitSeconds"])
            continue
        r = eval_code(project, step["eval"])
        results.append(r)
        if r.startswith("FAIL"):
            return {"id": case["id"], "status": "fail", "failedAt": len(results), "result": r}
    new_errs = error_count(project) - errs0
    if new_errs > 0:
        return {"id": case["id"], "status": "fail", "result": f"FAIL: {new_errs} new console error(s)", "steps": results}
    return {"id": case["id"], "status": "pass", "steps": results}

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("exam"); ap.add_argument("--project", required=True); ap.add_argument("--ids", default="")
    a = ap.parse_args()
    cases = json.load(open(a.exam))["cases"]
    if a.ids:
        want = set(a.ids.split(",")); cases = [c for c in cases if c["id"] in want]
    playing = False
    ok = True
    for c in cases:
        b = c.get("behaviour") or {}
        if b.get("mode") == "play":
            # Fresh Play session per gameplay brief, so one draft's leftovers
            # (spawned objects, player state) can't change another's score.
            if playing:
                unity(a.project, "command", "--result-only", "editor_stop")
            ensure_play(a.project); playing = True
        r = run_case(a.project, c)
        print(json.dumps(r)); sys.stdout.flush()
        ok &= r["status"] in ("pass", "skipped")
    if playing:
        unity(a.project, "command", "--result-only", "editor_stop")
    sys.exit(0 if ok else 1)

if __name__ == "__main__":
    main()
