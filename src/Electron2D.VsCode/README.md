# Electron2D Task Board for VS Code

Откройте Electron2D-проект с `.taskboard/board.e2tasks` в доверенном workspace и нажмите значок `Electron2D Task Board` в Activity Bar слева. Sidebar покажет активные задачи отдельными строками; клик мгновенно откроет выбранную задачу из compact snapshot, после чего полные details догрузятся в фоне. Доску также можно открыть командой `Electron2D: Open Task Board`.

Расширение не содержит `e2d` и не редактирует taskboard JSON. Оно использует путь из `electron2d.taskboard.cliPath`, а при пустом значении — `e2d` из `PATH`. Все изменения выполняются через CLI с revision guards.

Локальные проверки:

```text
npm ci
npm run typecheck
npm run test:unit
npm run test:extension
npm run bundle
npm run package:vsix
```
