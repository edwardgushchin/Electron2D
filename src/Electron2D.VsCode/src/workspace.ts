/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import path from "node:path";

export interface WorkspaceCandidate {
  readonly name: string;
  readonly path: string;
  readonly hasBoard: boolean;
}

export function selectTaskboardCandidates(candidates: readonly WorkspaceCandidate[]): WorkspaceCandidate[] {
  return candidates.filter(candidate => candidate.hasBoard);
}

export function resolveWorkspaceFilePath(workspaceRoot: string, candidate: string): string | undefined {
  if (!isProjectRelativeFilePath(candidate)) {
    return undefined;
  }

  const resolvedRoot = path.resolve(workspaceRoot);
  const resolvedCandidate = path.resolve(resolvedRoot, ...candidate.split("/"));
  return isPathInsideWorkspace(resolvedRoot, resolvedCandidate) ? resolvedCandidate : undefined;
}

export function isPathInsideWorkspace(workspaceRoot: string, candidate: string): boolean {
  const relative = path.relative(path.resolve(workspaceRoot), path.resolve(candidate));
  return relative.length > 0 && relative !== ".." && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
}

export function isProjectRelativeFilePath(candidate: unknown): candidate is string {
  if (typeof candidate !== "string" || candidate.length === 0 || candidate.length > 4_096 ||
      candidate.startsWith("/") || candidate.startsWith("\\") || candidate.endsWith("/") ||
      candidate.includes("\\") || candidate.includes(":") || /[\u0000-\u001f]/.test(candidate)) {
    return false;
  }

  return candidate.split("/").every(segment => segment.length > 0 && segment !== "." && segment !== "..");
}
