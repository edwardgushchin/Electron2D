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


CHECKBOX_RE = re.compile(r"^\s*-\s*\[(?P<state>[ xX])\]\s*(?P<text>.*)$")
TASK_ID_RE = re.compile(r"^(T\d+)\b")

EDGE_SECTION_RE = re.compile(r"^###\s+Edge\s+Cases\s*$", re.IGNORECASE)
HEADING_RE = re.compile(r"^#{1,3}\s+")

TOKEN_RE = re.compile(r"[a-zA-Z0-9']+")

STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "as",
    "at",
    "be",
    "but",
    "by",
    "can",
    "do",
    "does",
    "for",
    "from",
    "handle",
    "happens",
    "how",
    "if",
    "in",
    "is",
    "it",
    "of",
    "on",
    "or",
    "system",
    "the",
    "then",
    "to",
    "user",
    "users",
    "when",
    "what",
    "with",
}


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


def _extract_edge_cases(spec_text: str) -> OrderedDict[str, str]:
    lines = spec_text.splitlines()

    start_idx: int | None = None
    for i, line in enumerate(lines):
        if EDGE_SECTION_RE.match(line.strip()):
            start_idx = i + 1
            break

    if start_idx is None:
        return OrderedDict()

    edge_lines: list[str] = []
    for line in lines[start_idx:]:
        if HEADING_RE.match(line) and not line.strip().startswith("-"):
            break
        if line.strip().startswith("-"):
            edge_lines.append(line.strip().lstrip("-").strip())

    edge_cases: OrderedDict[str, str] = OrderedDict()
    for idx, text in enumerate(edge_lines, start=1):
        edge_cases[f"EC-{idx:03d}"] = text

    return edge_cases


def _tokenize(text: str) -> set[str]:
    tokens = {t.lower() for t in TOKEN_RE.findall(text)}
    filtered: set[str] = set()
    for t in tokens:
        if t in STOPWORDS:
            continue
        if len(t) < 3 and not t.isdigit():
            continue
        filtered.add(t)
    return filtered


def _parse_tasks(tasks_text: str) -> list[tuple[str, str]]:
    tasks: list[tuple[str, str]] = []
    for line_no, line in enumerate(tasks_text.splitlines(), start=1):
        m = CHECKBOX_RE.match(line)
        if not m:
            continue
        text = m.group("text")
        task_id_match = TASK_ID_RE.match(text)
        task_id = task_id_match.group(1) if task_id_match else f"L{line_no}"
        tasks.append((task_id, text))
    return tasks


def _analyze_edge_cases(spec_text: str, tasks_text: str) -> dict:
    edge_cases = _extract_edge_cases(spec_text)
    tasks = _parse_tasks(tasks_text)

    results: list[dict] = []
    counts = {"EXPLICIT": 0, "IMPLICIT": 0, "UNCOVERED": 0}

    task_tokens = [(task_id, _tokenize(text)) for task_id, text in tasks]
    tasks_text_all = tasks_text

    for ec_id, ec_text in edge_cases.items():
        explicit = ec_id in tasks_text_all
        if explicit:
            counts["EXPLICIT"] += 1
            results.append(
                {
                    "edge_case_id": ec_id,
                    "text": ec_text,
                    "coverage": "EXPLICIT",
                    "matched_tasks": [task_id for task_id, text in tasks if ec_id in text],
                }
            )
            continue

        ec_tokens = _tokenize(ec_text)
        matched: list[str] = []
        for task_id, toks in task_tokens:
            if len(ec_tokens & toks) >= 2:
                matched.append(task_id)

        if matched:
            counts["IMPLICIT"] += 1
            results.append(
                {
                    "edge_case_id": ec_id,
                    "text": ec_text,
                    "coverage": "IMPLICIT",
                    "matched_tasks": matched,
                }
            )
        else:
            counts["UNCOVERED"] += 1
            results.append(
                {
                    "edge_case_id": ec_id,
                    "text": ec_text,
                    "coverage": "UNCOVERED",
                    "matched_tasks": [],
                }
            )

    total = len(edge_cases)
    covered = counts["EXPLICIT"] + counts["IMPLICIT"]
    coverage_pct = 0.0 if total == 0 else (covered * 100.0) / total

    return {
        "edge_cases_total": total,
        "coverage_pct": round(coverage_pct, 1),
        "breakdown": counts,
        "edge_cases": results,
    }


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Analyze edge case coverage across spec.md and tasks.md")
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
        **_analyze_edge_cases(spec_text, tasks_text),
    }

    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
