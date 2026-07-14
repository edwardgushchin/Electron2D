/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import type { TaskStatus } from "./model.js";

export interface TaskListExpansionState {
  expand(status: TaskStatus): void;
  visibleTasks<T>(status: TaskStatus, tasks: readonly T[], previewLimit: number): readonly T[];
}

export function createTaskListExpansionState(): TaskListExpansionState {
  const expandedStatuses = new Set<TaskStatus>();

  return {
    expand(status): void {
      expandedStatuses.add(status);
    },
    visibleTasks<T>(status: TaskStatus, tasks: readonly T[], previewLimit: number): readonly T[] {
      return expandedStatuses.has(status) ? tasks : tasks.slice(0, previewLimit);
    }
  };
}
