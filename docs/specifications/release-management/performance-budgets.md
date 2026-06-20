# Performance budgets и soak-критерии `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0007`.
Обновлено: 2026-06-20.

## Цель

`0.1.0 Preview` должен иметь проверяемые performance expectations до появления полноценного runtime. Эта матрица задаёт release gate для будущих задач рендеринга, физики, ввода, аудио, редактора и экспорта.

## Эталонные устройства

| Tier | Платформа | Минимальный ориентир | Назначение |
| --- | --- | --- | --- |
| Desktop Tier 1 | Windows 11 x64 | 4-core CPU, 8 GB RAM, integrated GPU уровня Intel Iris Xe | Основной developer/runtime gate |
| Desktop Tier 1 | Linux Ubuntu 24.04 x64 | 4-core CPU, 8 GB RAM, Vulkan-capable integrated GPU | Linux runtime/export gate |
| Desktop Tier 1 | macOS 14+ | Apple Silicon M1, 8 GB RAM | macOS runtime/export gate |
| Mobile Tier 1 | Android 12+ | Snapdragon 720G / Adreno 618, 4 GB RAM | Android export smoke gate |
| Mobile Tier 1 | iOS 16+ | iPhone 11 / A13, 4 GB RAM | iOS export smoke gate |

## Frame-time budget

Цель первой версии: `60 FPS`.

| Сценарий | Budget |
| --- | --- |
| Empty project | p95 frame time <= `16.67 ms`, p99 <= `25 ms` |
| Small 2D game scene | p95 frame time <= `16.67 ms`, p99 <= `33 ms` |
| Editor play mode | p95 frame time <= `16.67 ms` без долгих stalls при старте сцены |

## memory budget

| Сценарий | Desktop budget | Mobile budget |
| --- | --- | --- |
| Empty project runtime | <= 128 MB RSS | <= 160 MB RSS |
| Small 2D game scene | <= 512 MB RSS | <= 350 MB RSS |
| 30-minute soak growth | <= 5% RSS growth after warm-up | <= 5% RSS growth after warm-up |

## 30-minute soak checks

Каждая стабильная runtime milestone должна иметь 30-minute soak сценарий:

- scene load;
- input loop;
- render loop;
- fixed physics loop, когда физика появится;
- resource load/unload cycle, когда появится importer/runtime resource system.

Критерии soak:

- нет crash/hang;
- p95 frame time остаётся в пределах 60 FPS budget;
- memory growth после warm-up не выше 5%;
- logs не содержат unhandled exception.

## mobile background/foreground cycles

Для Android и iOS export gate нужен отдельный mobile smoke:

- 20 циклов background/foreground;
- audio/render/input pause-resume без crash;
- сохранение main scene state, когда появится scene runtime;
- отсутствие роста памяти выше 5% после цикла.

До появления export pipeline эти проверки остаются documented release gap, но не считаются выполненными.
