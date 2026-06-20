# Performance budgets и soak-критерии `0.1.0 Preview`

Статус: текущая release-gate документация.
Задача: `T-0007`.
Обновлено: 2026-06-20.

## Эталонная матрица

Текущие Tier 1 ориентиры:

- Windows 11 x64, 4-core CPU, 8 GB RAM, integrated GPU уровня Intel Iris Xe.
- Linux Ubuntu 24.04 x64, 4-core CPU, 8 GB RAM, Vulkan-capable integrated GPU.
- macOS 14+, Apple Silicon M1, 8 GB RAM.
- Android 12+, Snapdragon 720G / Adreno 618, 4 GB RAM.
- iOS 16+, iPhone 11 / A13, 4 GB RAM.

## 60 FPS и frame-time

Базовая цель: `60 FPS`.

- Empty project: p95 <= `16.67 ms`, p99 <= `25 ms`.
- Small 2D game scene: p95 <= `16.67 ms`, p99 <= `33 ms`.
- Editor play mode: p95 <= `16.67 ms`.

## memory budget

- Empty project runtime: desktop <= 128 MB RSS, mobile <= 160 MB RSS.
- Small 2D game scene: desktop <= 512 MB RSS, mobile <= 350 MB RSS.
- 30-minute soak: memory growth <= 5% после warm-up.

## 30-minute soak

30-minute soak проверяет scene load, input loop, render loop, fixed physics loop и resource load/unload cycles по мере появления подсистем.

Критерии:

- нет crash/hang;
- p95 frame time остаётся в пределах `60 FPS`;
- memory growth <= 5%;
- нет unhandled exception в логах.

## mobile background/foreground

Android и iOS smoke должен включать 20 background/foreground cycles. Проверяются pause/resume, render/input recovery, сохранение main scene state и memory growth <= 5%.

Сейчас mobile export smoke остаётся documented release gap до задач экспортного пайплайна.
