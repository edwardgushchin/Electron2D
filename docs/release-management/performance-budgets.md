# Performance budgets и soak-критерии `0.1.0 Preview`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

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
