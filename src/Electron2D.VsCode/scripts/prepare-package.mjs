/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { mkdir } from "node:fs/promises";

await mkdir(new URL("../artifacts", import.meta.url), { recursive: true });
