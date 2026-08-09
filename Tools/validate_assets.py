#!/usr/bin/env python3
"""
Static validator for hand-authored Unity .asset files.

Why this exists: Unity's YAML importer silently defaults any unrecognized or
mistyped field to its C# default value instead of erroring — a typo'd field name
in a hand-written ComboData.asset doesn't fail to load, it just produces a combo
that (say) does 0 damage, discovered a Mac Play-mode session later. This script
catches that class of mistake here, for free, before it ever reaches the Editor.

Checks, per .asset file under Assets/:
  1. The asset's m_Script GUID resolves to a real <Script>.cs.meta in Assets/Scripts/.
  2. Every top-level field key on the MonoBehaviour maps to an actual public field
     (or public property) declared on that C# class (Unity-reserved m_* keys are
     always allowed and skipped).
  3. Every {fileID: N, guid: G} reference inside the asset resolves to a GUID that
     actually exists somewhere in the project (catches a copy-pasted stale GUID).
  4. No two .meta files in the project share a GUID (would silently misroute Unity's
     asset database — extremely rare, but catastrophic and easy to check for free).

This does NOT type-check values, evaluate array lengths and so on — it is a floor,
not full validation. Real correctness still needs a Mac Play-mode pass. But it turns
"field name typo" and "stale GUID" from a Mac-session bug hunt into a one-second
local check.

Usage: python3 Tools/validate_assets.py [--verbose]
Exit 0 = clean. Exit 1 = problems found (printed).
"""
import re
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"

VERBOSE = "--verbose" in sys.argv


# ---------------------------------------------------------------- YAML loading --
# Unity's .asset/.meta YAML tags each document with a custom class-ID marker, e.g.
# `--- !u!114 &11400000` for a MonoBehaviour. We don't need the class ID for these
# checks — only the key/value content — so the simplest robust approach is to strip
# the `!u!<N>` tag token entirely, leaving plain `--- &11400000`, which is standard
# YAML anchor syntax PyYAML parses natively with no custom-tag machinery needed.
UNITY_TAG_RE = re.compile(r"^(---\s*)!u!\d+(\s*&\d+.*)$", re.MULTILINE)


def load_unity_yaml(path):
    """Returns a list of top-level documents (Unity files can have multiple ---
    separated docs, though a plain ScriptableObject .asset has exactly one)."""
    text = path.read_text(encoding="utf-8", errors="replace")
    text = "\n".join(l for l in text.splitlines() if not l.startswith("%"))
    text = UNITY_TAG_RE.sub(r"\1\2", text)
    try:
        return list(yaml.safe_load_all(text))
    except yaml.YAMLError as e:
        print(f"  PARSE ERROR in {path.relative_to(ROOT)}: {e}")
        return []


# --------------------------------------------------------- GUID / script index --
def collect_guids():
    """meta_path -> guid, and guid -> meta_path (reverse), across the whole project."""
    guid_to_meta = {}
    dupes = []
    for meta in ASSETS.rglob("*.meta"):
        text = meta.read_text(encoding="utf-8", errors="replace")
        m = re.search(r"^guid:\s*([0-9a-f]{32})", text, re.MULTILINE)
        if not m:
            continue
        guid = m.group(1)
        if guid in guid_to_meta:
            dupes.append((guid, guid_to_meta[guid], meta))
        else:
            guid_to_meta[guid] = meta
    return guid_to_meta, dupes


# `[Attr(...)] public <Type> <name> ...` — Type can have generics/arrays/dots
# (List<int>, Foo[], A.B); a leading Inspector attribute like [HideInInspector] or
# [TextArea(2,3)] is common in this codebase and must not hide the field after it.
# Excludes declarations that are obviously not fields by checking what follows the name.
PUBLIC_DECL_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*public\s+(?:static\s+|readonly\s+)*"
    r"[\w<>\[\],\.]+\??\s+(\w+)\s*(?=[=;{])",
    re.MULTILINE,
)
NOT_A_TYPE_KEYWORD = {"class", "struct", "enum", "interface", "void"}
CLASS_RE = re.compile(r"\bclass\s+(\w+)\s*(?::\s*[\w<>,\s]+)?\s*{", re.MULTILINE)


def collect_script_fields():
    """script class name -> set of public field names, scanned from Assets/Scripts/*.cs.
    Regex-based, not a real C# parser — good enough to catch a typo'd YAML key
    against a real field name, not meant to be exhaustive type-checking."""
    class_fields = {}
    for cs in (ASSETS / "Scripts").glob("*.cs"):
        text = cs.read_text(encoding="utf-8", errors="replace")
        classes = CLASS_RE.findall(text)
        fields = {m.group(1) for m in PUBLIC_DECL_RE.finditer(text)}
        # Drop false positives from "public class Foo {" / "public struct Bar {" etc,
        # where the generic regex above matched the type keyword as the "type" and the
        # class/struct name as the "field name".
        declared_types = set(re.findall(r"\bpublic\s+(?:static\s+)?(?:class|struct|enum|interface)\s+(\w+)", text))
        fields -= declared_types
        for c in classes:
            class_fields.setdefault(c, set()).update(fields)
    return class_fields


# Unity's own reserved MonoBehaviour keys — always allowed, never a project field.
UNITY_RESERVED = {
    "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance",
    "m_PrefabAsset", "m_GameObject", "m_Enabled", "m_EditorHideFlags",
    "m_Script", "m_Name", "m_EditorClassIdentifier",
}


def script_guid_for_class(class_fields_meta):
    """.cs filename stem -> guid, from Assets/Scripts/*.cs.meta."""
    out = {}
    for meta in (ASSETS / "Scripts").glob("*.cs.meta"):
        stem = meta.name[: -len(".cs.meta")]
        text = meta.read_text(encoding="utf-8", errors="replace")
        m = re.search(r"^guid:\s*([0-9a-f]{32})", text, re.MULTILINE)
        if m:
            out[stem] = m.group(1)
    return out


def main():
    problems = []

    guid_to_meta, dupes = collect_guids()
    for guid, a, b in dupes:
        problems.append(f"DUPLICATE GUID {guid}: {a.relative_to(ROOT)} and {b.relative_to(ROOT)}")

    class_fields = collect_script_fields()
    class_to_guid = script_guid_for_class(class_fields)
    guid_to_class = {g: c for c, g in class_to_guid.items()}

    asset_files = sorted(ASSETS.rglob("*.asset"))
    checked = 0
    for asset in asset_files:
        docs = load_unity_yaml(asset)
        for doc in docs:
            if not isinstance(doc, dict) or "MonoBehaviour" not in doc:
                continue  # not a ScriptableObject (e.g. a BuildProfile or plain asset) — out of scope
            body = doc["MonoBehaviour"]
            script_ref = body.get("m_Script", {})
            guid = script_ref.get("guid") if isinstance(script_ref, dict) else None

            class_name = guid_to_class.get(guid) if guid else None
            if not class_name:
                # Not backed by a script in Assets/Scripts/ — a Unity/package built-in
                # type (BuildProfile, VolumeProfile, URP settings, ...) whose script
                # lives compiled into the Editor or a package, not as a local .cs file.
                # Out of scope for this validator by design: we can't check fields we
                # have no source for, and it isn't project-authored content anyway.
                if VERBOSE:
                    print(f"  (skip {asset.relative_to(ROOT)}: not backed by a project script — engine/package asset)")
                continue
            checked += 1

            known_fields = class_fields.get(class_name, set())
            for key in body:
                if key in UNITY_RESERVED:
                    continue
                if key not in known_fields:
                    problems.append(
                        f"{asset.relative_to(ROOT)}: field '{key}' has no matching public field "
                        f"on {class_name} (typo, or the field was renamed?)"
                    )

            # Any nested {fileID: N, guid: G} must resolve to a real project GUID.
            def walk(node, path=""):
                if isinstance(node, dict):
                    if "guid" in node and isinstance(node.get("guid"), str) and len(node["guid"]) == 32:
                        g = node["guid"]
                        if g not in guid_to_meta:
                            problems.append(f"{asset.relative_to(ROOT)}: dangling reference at '{path}' — guid {g} not found in project")
                    for k, v in node.items():
                        walk(v, f"{path}.{k}")
                elif isinstance(node, list):
                    for i, v in enumerate(node):
                        walk(v, f"{path}[{i}]")

            walk(body)

    print(f"checked {checked} ScriptableObject asset(s) across {len(asset_files)} .asset file(s), "
          f"{len(guid_to_meta)} GUIDs indexed, {len(class_fields)} script classes scanned")

    if problems:
        print(f"\n{len(problems)} problem(s):")
        for p in problems:
            print(f"  FAIL  {p}")
        return 1

    print("clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
