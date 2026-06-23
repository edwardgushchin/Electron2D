#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from collections import OrderedDict, defaultdict
from dataclasses import dataclass
from pathlib import Path


FR_ID_RE = re.compile(r"\bFR-\d{3}\b")
FR_LINE_RE = re.compile(r"^\s*[-*]\s+\*\*(FR-\d{3})\*\*\s*[:\-]\s*(.*?)\s*$")
CHECKBOX_RE = re.compile(r"^\s*-\s*\[(?P<state>[ xX])\]\s*(?P<text>.*)$")
TASK_ID_RE = re.compile(r"^(T\d+)\b")

VAGUE_MARKERS = (
    "needs clarification",
    "tbd",
    "to be determined",
    "???",
)


@dataclass(frozen=True)
class FeaturePaths:
    feature_name: str
    feature_dir: Path
    spec_file: Path
    tasks_file: Path


def _find_repo_root(start: Path) -> Path:
    current = start.resolve()
    while True:
        if (current / ".git").exists():
            return current
        if current.parent == current:
            return start.resolve()
        current = current.parent


def _git_current_branch(repo_root: Path) -> str | None:
    try:
        out = subprocess.check_output(
            ["git", "rev-parse", "--abbrev-ref", "HEAD"],
            cwd=repo_root,
            stderr=subprocess.DEVNULL,
            text=True,
        ).strip()
        return out or None
    except Exception:
        return None


def _resolve_specs_root(repo_root: Path, specs_root_arg: str | None) -> Path:
    if specs_root_arg:
        p = Path(specs_root_arg)
        if not p.is_absolute():
            p = repo_root / p
        return p

    candidate = repo_root / "specs"
    if candidate.exists() and candidate.is_dir():
        return candidate

    project_candidate = repo_root / ".temp" / "spec-kit"
    if project_candidate.exists() and project_candidate.is_dir():
        return project_candidate

    return candidate


def _list_feature_dirs(specs_root: Path) -> list[Path]:
    if not specs_root.exists() or not specs_root.is_dir():
        return []

    feature_dirs: list[Path] = []
    for child in specs_root.iterdir():
        if not child.is_dir():
            continue
        if re.match(r"^\d{3}-[a-z0-9-]+$", child.name):
            feature_dirs.append(child)
    return sorted(feature_dirs, key=lambda p: p.name)


def _detect_feature_dir(repo_root: Path, specs_root: Path, feature_arg: str | None) -> Path:
    if feature_arg:
        p = Path(feature_arg)
        if not p.is_absolute():
            # treat as relative path first
            rel = repo_root / p
            if rel.exists() and rel.is_dir():
                return rel
            # treat as feature name
            return specs_root / feature_arg
        return p

    env_feature = os.getenv("SPECIFY_FEATURE")
    if env_feature:
        return specs_root / env_feature

    branch = _git_current_branch(repo_root)
    if branch and re.match(r"^\d{3}-", branch):
        return specs_root / branch

    feature_dirs = _list_feature_dirs(specs_root)
    if not feature_dirs:
        return specs_root / "<feature>"
    if len(feature_dirs) == 1:
        return feature_dirs[0]

    # pick the highest numeric prefix as a deterministic fallback
    return feature_dirs[-1]


def _resolve_feature_paths(repo_root: Path, feature_arg: str | None, specs_root_arg: str | None) -> FeaturePaths:
    specs_root = _resolve_specs_root(repo_root, specs_root_arg)
    feature_dir = _detect_feature_dir(repo_root, specs_root, feature_arg)

    feature_name = feature_dir.name
    spec_file = feature_dir / "spec.md"
    tasks_file = feature_dir / "tasks.md"
    return FeaturePaths(feature_name, feature_dir, spec_file, tasks_file)


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def _parse_fr_from_spec(spec_text: str) -> OrderedDict[str, str]:
    reqs: OrderedDict[str, str] = OrderedDict()
    for line in spec_text.splitlines():
        m = FR_LINE_RE.match(line)
        if not m:
            continue
        fr_id, text = m.group(1), (m.group(2) or "").strip()
        reqs.setdefault(fr_id, text)

    if reqs:
        return reqs

    # Fallback: collect any FR IDs in document
    for m in FR_ID_RE.finditer(spec_text):
        reqs.setdefault(m.group(0), "")
    return reqs


def _is_vague(text: str) -> bool:
    low = text.lower()
    return any(marker in low for marker in VAGUE_MARKERS)


def _analyze_requirements(spec_text: str, tasks_text: str) -> dict:
    frs = _parse_fr_from_spec(spec_text)

    mapping: dict[str, list[str]] = defaultdict(list)
    for line_no, line in enumerate(tasks_text.splitlines(), start=1):
        m = CHECKBOX_RE.match(line)
        if not m:
            continue

        text = m.group("text")
        task_id_match = TASK_ID_RE.match(text)
        task_id = task_id_match.group(1) if task_id_match else f"L{line_no}"

        fr_ids = sorted(set(FR_ID_RE.findall(text)))
        for fr_id in fr_ids:
            mapping[fr_id].append(task_id)

    total = len(frs)
    covered = sum(1 for fr_id in frs.keys() if mapping.get(fr_id))
    uncovered = [fr_id for fr_id in frs.keys() if not mapping.get(fr_id)]
    vague = [fr_id for fr_id, text in frs.items() if text and _is_vague(text)]

    coverage_pct = 0.0 if total == 0 else (covered * 100.0) / total

    # Normalize mapping to include only known FRs (plus any extras discovered in tasks)
    normalized_mapping: dict[str, list[str]] = OrderedDict()
    for fr_id in frs.keys():
        normalized_mapping[fr_id] = mapping.get(fr_id, [])
    for fr_id in sorted(mapping.keys() - set(frs.keys())):
        normalized_mapping[fr_id] = mapping[fr_id]

    return {
        "requirements_total": total,
        "covered_requirements": covered,
        "coverage_pct": round(coverage_pct, 1),
        "uncovered_requirements": uncovered,
        "vague_requirements": vague,
        "mapping": normalized_mapping,
    }


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Analyze FR coverage across spec.md and tasks.md")
    parser.add_argument("--feature", help="Feature directory name or path (defaults to current branch)")
    parser.add_argument(
        "--specs-root",
        help="Override specs root (defaults to ./specs, or docs/specs when present)",
    )
    args = parser.parse_args(argv)

    repo_root = _find_repo_root(Path.cwd())
    paths = _resolve_feature_paths(repo_root, args.feature, args.specs_root)

    if not paths.spec_file.exists():
        print(f"ERROR: spec file not found: {paths.spec_file}", file=sys.stderr)
        return 2
    if not paths.tasks_file.exists():
        print(f"ERROR: tasks file not found: {paths.tasks_file}", file=sys.stderr)
        return 2

    spec_text = _read_text(paths.spec_file)
    tasks_text = _read_text(paths.tasks_file)
    result = {
        "feature": paths.feature_name,
        "spec_file": str(paths.spec_file),
        "tasks_file": str(paths.tasks_file),
        **_analyze_requirements(spec_text, tasks_text),
    }

    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
