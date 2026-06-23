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


SC_ID_RE = re.compile(r"\bSC-\d{3}\b")
SC_LINE_RE = re.compile(r"^\s*[-*]\s+\*\*(SC-\d{3})\*\*\s*[:\-]\s*(.*?)\s*$")
CHECKBOX_RE = re.compile(r"^\s*-\s*\[(?P<state>[ xX])\]\s*(?P<text>.*)$")
TASK_ID_RE = re.compile(r"^(T\d+)\b")

MEASURABLE_HINT_RE = re.compile(r"\b\d+(?:\.\d+)?\b")


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
            rel = repo_root / p
            if rel.exists() and rel.is_dir():
                return rel
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


def _parse_sc_from_spec(spec_text: str) -> OrderedDict[str, str]:
    scs: OrderedDict[str, str] = OrderedDict()
    for line in spec_text.splitlines():
        m = SC_LINE_RE.match(line)
        if not m:
            continue
        sc_id, text = m.group(1), (m.group(2) or "").strip()
        scs.setdefault(sc_id, text)

    if scs:
        return scs

    for m in SC_ID_RE.finditer(spec_text):
        scs.setdefault(m.group(0), "")
    return scs


def _is_measurable(text: str) -> bool:
    low = text.lower()
    if "needs clarification" in low or "tbd" in low:
        return False
    if MEASURABLE_HINT_RE.search(text):
        return True
    return any(hint in low for hint in ("%", "ms", "sec", "second", "minute", "p95", "p99", "fps", "req/s"))


def _metric_type(text: str) -> str:
    low = text.lower()
    if any(k in low for k in ("p95", "p99", "latency", "throughput", "req/s", "fps", "memory", "cpu")):
        return "performance"
    if any(k in low for k in ("wcag", "a11y", "accessibility", "screen reader", "keyboard")):
        return "accessibility"
    if any(k in low for k in ("support ticket", "tickets", "conversion", "revenue", "churn")):
        return "business"
    if any(k in low for k in ("users can", "user", "complete", "successfully", "satisfaction")):
        return "usability"
    return "other"


def _analyze_success_criteria(spec_text: str, tasks_text: str) -> dict:
    scs = _parse_sc_from_spec(spec_text)

    mapping: dict[str, list[str]] = defaultdict(list)
    for line_no, line in enumerate(tasks_text.splitlines(), start=1):
        m = CHECKBOX_RE.match(line)
        if not m:
            continue

        text = m.group("text")
        task_id_match = TASK_ID_RE.match(text)
        task_id = task_id_match.group(1) if task_id_match else f"L{line_no}"

        sc_ids = sorted(set(SC_ID_RE.findall(text)))
        for sc_id in sc_ids:
            mapping[sc_id].append(task_id)

    total = len(scs)
    covered = sum(1 for sc_id in scs.keys() if mapping.get(sc_id))
    uncovered = [sc_id for sc_id in scs.keys() if not mapping.get(sc_id)]
    unmeasurable = [sc_id for sc_id, text in scs.items() if text and not _is_measurable(text)]

    by_type: dict[str, list[str]] = defaultdict(list)
    for sc_id, text in scs.items():
        by_type[_metric_type(text)].append(sc_id)

    coverage_pct = 0.0 if total == 0 else (covered * 100.0) / total

    normalized_mapping: dict[str, list[str]] = OrderedDict()
    for sc_id in scs.keys():
        normalized_mapping[sc_id] = mapping.get(sc_id, [])
    for sc_id in sorted(mapping.keys() - set(scs.keys())):
        normalized_mapping[sc_id] = mapping[sc_id]

    return {
        "criteria_total": total,
        "verified_criteria": covered,
        "coverage_pct": round(coverage_pct, 1),
        "unverified_criteria": uncovered,
        "unmeasurable_criteria": unmeasurable,
        "by_metric_type": dict(sorted(by_type.items())),
        "mapping": normalized_mapping,
    }


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Analyze SC verification coverage across spec.md and tasks.md")
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
        **_analyze_success_criteria(spec_text, tasks_text),
    }

    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
