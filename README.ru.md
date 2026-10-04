<p align="right"><a href="README.md">English</a> · Русский · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D" src="docs/design/assets/sprite/logo-primary-light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors">Участники</a> ·
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main">Коммиты</a> ·
  <a href="licence/Electron2D-LICENSE.txt">Лицензия MIT</a>
</p>

<p align="center">
  <img alt="Разработка с ИИ · Кроссплатформенность · 2D · В разработке" src="docs/design/assets/sprite/readme-badges.svg" width="405">
</p>

<p align="center">
  <a href="#about">О проекте</a> ·
  <a href="#features">Возможности</a> ·
  <a href="#platforms">Платформы</a> ·
  <a href="#installation">Установка</a> ·
  <a href="#quick-start">Быстрый старт</a> ·
  <a href="#documentation">Документация</a> ·
  <a href="#examples">Примеры</a> ·
  <a href="#feedback-and-contributing">Обратная связь</a> ·
  <a href="#license">Лицензия</a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D">Поддержите Electron2D звездой на GitHub</a>.
</p>

<a id="about"></a>

## О проекте

Electron2D - **свободный кроссплатформенный 2D-движок на C# для совместной разработки игр человеком и ИИ-агентами**.

Создавайте игровые миры и механики с привычными инструментами .NET и ИИ-помощниками вроде Codex или Claude Code.

<a id="features"></a>

## Возможности

- [Сцены](docs/domains/scene.md) с узлами `Node`, `CanvasItem` и `Entity`, таймерами и твинами для плавного изменения свойств. `PackedScene` хранит шаблоны сцен в памяти.
- [Графика](docs/domains/rendering.md): спрайты, анимация, камеры, текстуры, текст и рисование на холсте. Материалы шейдеров HLSL и GLSL требуют GPU; совместимый рендерер их не поддерживает.
- Элементы [интерфейса](docs/domains/scene.md): кнопки, подписи, поля ввода, контейнеры, управление фокусом и темы.
- [Физика](docs/domains/physics.md) с фиксированным шагом расчёта: тела, области, формы столкновений, запросы и соединения тел.
- [Звук](docs/domains/audio.md): WAV, MP3 и Ogg Vorbis, процедурные потоки, звуковые шины и запись.
- [Ресурсы](docs/domains/resources.md): изображения, шрифты и загрузка файлов. [Настройки, файловый доступ и локализация](docs/domains/core.md) используют типизированный API.

Состояние API и оставшиеся задачи описаны в [плане разработки](docs/coverage/index.md).

<a id="platforms"></a>

## Платформы

| Платформа | Редактор (план) | Игра (план) | Что проверено |
| --- | --- | --- | --- |
| Windows | Планируется | x86, x64, ARM64 | Подобраны нативные пакеты. Запуск на Windows ещё не проверен |
| Linux | Планируется для X11 и Wayland | x64, ARM64 | На Linux x64 проверены запуск и отрисовка через Wayland, отдельно отрисовка через XWayland. ARM64 ещё не проверен |
| macOS | Планируется | x64, ARM64 | Подобраны нативные пакеты. Запуск на macOS ещё не проверен |
| Android | - | Телефоны и планшеты | На телефоне ARM64 проверены отрисовка холста, GPU-шейдеры и физика на CPU. Другие устройства и сценарии ещё не проверены |
| Android TV | - | Android TV | На 32-битном телевизоре проверены совместимый рендерер и физика на CPU. Устройство не поддерживает Vulkan |
| iOS | - | Устройства и симулятор | Подобраны нативные пакеты. Автоматическая сборка библиотеки на macOS настроена, но ещё не запускалась |
| tvOS | - | Устройства и симулятор | Подобраны нативные пакеты. Автоматическая сборка библиотеки на macOS настроена, но ещё не запускалась |
| Web | - | `browser-wasm` | В тестовых приложениях проверены отрисовка холста и физика. Отдельный тест WebGPU работает вне движка. Запуск игры в браузере и её графический бэкенд ещё не реализованы |

На Android ещё нужно проверить жизненный цикл приложения, ввод, звук, работу с хранилищем и выпуск пакета. Нативные пакеты и успешная сборка сами по себе не подтверждают, что игра работает на платформе.

Устройства, команды запуска и ограничения приведены в [отчёте о проверке платформ](docs/platform-verification.md). Обязательные проверки нативного запуска сейчас проходят на Linux Wayland.

<a id="installation"></a>

## Установка

Для сборки нужен .NET 10 SDK. Клонируйте репозиторий и соберите библиотеку:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

Сборка создаёт `Electron2D.dll`. Подключите `Electron2D.csproj` к своему приложению как ссылку на проект. .NET восстановит зависимости и при публикации выберет нативные библиотеки для нужной платформы.

При автономной публикации приложение включает исполняемый файл игры, `Electron2D.dll`, среду .NET и нативные библиотеки.

<a id="quick-start"></a>

## Быстрый старт

Пример открывает окно и обрабатывает клавиатуру. Он сообщает о перемещении узла в терминале и не рисует сцену. Запустите его на Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

Перемещайте узел стрелками. Для выхода нажмите Escape или закройте окно.

В [Program.cs](examples/HostExample/Program.cs) показано создание `Window`, добавление сцены и вызов `Engine.Instance.Run`. Подробнее о запуске и устройстве примера читайте в [его README](examples/HostExample/README.md).

<a id="documentation"></a>

## Документация

- [Руководства по устройству движка](docs/README.md).
- [Справочник API со ссылками на типы движка](docs/inventory.md).
- [Принятые архитектурные решения](docs/decisions/index.md).
- [Архитектура разработки с ИИ](docs/decisions/agent-native.md#adr-0090).
- [План разработки и состояние API](docs/coverage/index.md).
- [Результаты проверок на разных платформах](docs/platform-verification.md).
- [Логотипы, цвета и шрифты](docs/design/identity.md).

<a id="examples"></a>

## Примеры

[Окно и ввод](examples/HostExample/README.md) показывает работу с публичным API: создание окна и сцены, ввод с клавиатуры и завершение приложения.

В `tests/` есть отдельные инструменты для [проверки графики и физики на Android](tests/Electron2D.AndroidProbe/README.md) и [шейдеров WebGPU в браузере](tests/Electron2D.WebGpuProbe/README.md). В их README описаны устройства, сценарии и ограничения проверок.

<a id="feedback-and-contributing"></a>

## Обратная связь и участие

Сообщайте об ошибках и предлагайте новые возможности в [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues). В сообщении об ошибке укажите ревизию движка, платформу и рендерер. Приложите минимальный пример, на котором её можно воспроизвести.

Присылайте исправления через [пул-реквесты](https://github.com/edwardgushchin/Electron2D/pulls). Перед изменением поведения прочитайте [правила сопровождения](docs/maintaining.md) и архитектурные решения по нужной теме. Обновляйте документацию API и тесты вместе с кодом.

Перед отправкой изменений запустите проверки из корня репозитория:

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

## Участники

Проект ведёт Эдуард Гущин. Все участники перечислены на [странице GitHub](https://github.com/edwardgushchin/Electron2D/graphs/contributors).

<a id="license"></a>

## Лицензия

Код Electron2D распространяется по [лицензии MIT](licence/Electron2D-LICENSE.txt). Зависимости используют собственные лицензии, перечисленные в [уведомлениях о сторонних компонентах](licence/THIRD_PARTY_NOTICES.md). Тексты лицензий хранятся в `licence/` и при необходимости входят в публикацию приложения.
