<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · 简体中文 · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — 面向 AI 智能体的跨平台 2D 游戏引擎" src="docs/design/assets/sprite/logo-primary-light.svg" width="900">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/graphs/contributors">贡献者</a> ·
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main">提交记录</a> ·
  <a href="licence/Electron2D-LICENSE.txt">MIT 许可证</a>
</p>

<p align="center">
  <img alt="面向智能体 · 跨平台 · 2D · 开发中" src="docs/design/assets/sprite/readme-badges.svg" width="405">
</p>

<p align="center">
  <a href="#about">简介</a> ·
  <a href="#features">功能</a> ·
  <a href="#platforms">平台</a> ·
  <a href="#installation">安装</a> ·
  <a href="#quick-start">快速开始</a> ·
  <a href="#documentation">文档</a> ·
  <a href="#examples">示例</a> ·
  <a href="#feedback-and-contributing">反馈</a> ·
  <a href="#license">许可证</a>
</p>

<p align="center">
  ⭐ <a href="https://github.com/edwardgushchin/Electron2D">在 GitHub 上为项目点星</a>，关注开发进展。
</p>

<a id="about"></a>

## 🧭 简介

Electron2D 是一款**面向 AI 智能体的跨平台 2D 游戏引擎**。

它旨在让开发者与编程智能体通过有文档说明的程序化操作共同开发游戏：创建和修改项目、构建、运行并验证结果。开发者应能够审阅这些改动和验证结果。[智能体原生架构](docs/decisions/agent-native.md#adr-0090)规定了 CLI 与编辑器共用的创作操作、无图形界面的模拟，以及对渲染结果的批量验证。

统一的公共运行时 API 面向桌面、移动设备、电视和浏览器游戏。你可以用节点和资源搭建场景，并使用同一引擎提供的渲染、输入、物理、音频和 GUI 功能。

**开发状态：**运行时、示例、测试和 API 文档仍在积极开发中。可视化编辑器、统一的项目 CLI、将场景持久化到文件以及公开的画面捕获工作流程尚未实现。下文分别列出了目标平台和已完成的运行验证。

<a id="features"></a>

## ✨ 功能

- **智能体原生工作流程** — 设计目标是让编程智能体通过有文档说明的程序化操作查看和编辑场景、资源及项目设置，再构建、运行和验证游戏。尚缺的工具列在[实现路线图](docs/coverage/index.md)中。
- **跨平台运行时** — 为桌面、移动设备、电视和浏览器游戏提供统一的引擎 API。请参阅[目标平台和当前验证情况](#platforms)。
- **基于节点的场景** — `Node`、`CanvasItem` 和 `Entity` 层级结构，场景调度、计时器、补间动画，以及可在内存中复用的 `PackedScene` 实例。
- **2D 渲染** — 精灵、动画、摄像机、纹理、文字、画布绘制，以及采用强类型 API 的 HLSL/GLSL 着色器材质。请参阅[渲染能力与限制](docs/domains/rendering.md)。
- **GUI 组件** — 控件、容器、标签、按钮、文字输入、焦点导航和强类型主题。请参阅[场景领域文档](docs/domains/scene.md)。
- **2D 物理** — 刚体、区域、碰撞形状、查询和关节，采用固定时间步模拟。请参阅[物理领域文档](docs/domains/physics.md)。
- **音频** — WAV、MP3 和 Ogg Vorbis 播放、程序化音频流、输出总线及录音 API。请参阅[音频行为和验证范围](docs/domains/audio.md)。
- **资源与 I/O** — 图像、字体、资源加载、强类型配置、文件访问和本地化。请参阅[资源](docs/domains/resources.md)和[核心](docs/domains/core.md)领域文档。
- **C# 游戏逻辑** — 使用普通的 C# 类、属性、资源和事件，以及熟悉的 .NET 工具。

以上既包含已实现的功能领域，也包含产品的发展方向。各链接文档记录了 API 和后端仍存在的缺口。

<a id="platforms"></a>

## 🖥️ 平台

| 平台 | 编辑器目标 | 运行时目标 | 当前验证情况 |
| --- | --- | --- | --- |
| Windows | 计划支持 | x86、x64、ARM64 | 已映射原生软件包；尚未验证 Windows 上的运行 |
| Linux | 计划支持 X11 和 Wayland | x64、ARM64 | 已检查 Linux x64 Wayland 宿主和渲染，以及 XWayland 渲染器；尚未验证 ARM64 |
| macOS | 计划支持 | x64、ARM64 | 已映射原生软件包；尚未验证 macOS 上的运行 |
| Android | — | 手机和平板的 ABI | 已在 ARM64 手机上检查画布、GPU 着色器和 CPU 物理；其他设备和宿主场景仍待验证 |
| Android TV | — | Android ABI | 已在 32 位电视上检查画布兼容渲染和 CPU 物理；该设备没有 Vulkan 后端 |
| iOS | — | 设备与模拟器 RID | 已映射原生软件包；用于 macOS 库构建的 CI 已准备，但尚未运行 |
| tvOS | — | 设备与模拟器 RID | 已映射原生软件包；用于 macOS 库构建的 CI 已准备，但尚未运行 |
| Web | — | `browser-wasm` | 已分别验证画布/物理和独立的 WebGPU 探针；产品级浏览器宿主和后端尚未完成 |

编辑器面向桌面系统。仅有原生软件包或成功构建，并不意味着该平台已获得支持。Android 验证仅覆盖所列设备及图形、物理路径；应用生命周期、输入、音频、存储和发布打包仍需单独验证。

[平台验证矩阵](docs/platform-verification.md)记录设备、渲染器、命令和限制。目前 Linux Wayland 是必需的原生验证环境。着色器材质需要 GPU 路径；兼容渲染器会明确拒绝不支持的材质。

<a id="installation"></a>

## 📦 安装

安装 **.NET 10 SDK**，然后在仓库根目录克隆并构建运行时：

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

运行时会构建为 **`Electron2D.dll`**。请在应用项目中引用 `Electron2D.csproj`。项目会在还原和发布时提供相应的平台依赖。自包含应用发布包包含游戏可执行文件、`Electron2D.dll`、.NET 运行时及适用的原生库。

<a id="quick-start"></a>

## 🚀 快速开始

在 Linux Wayland 上运行现有的**窗口与输入**示例：

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

按住方向键可移动场景节点；按 Escape 或关闭窗口可退出。此示例会在终端报告移动情况，但不会绘制场景。

其[入口代码](examples/HostExample/Program.cs)配置 `Window`、添加场景并调用 `Engine.Run`。事件循环、帧计时和关闭流程由引擎负责。完整流程请参阅[示例指南](examples/HostExample/README.md)。

<a id="documentation"></a>

## 📚 文档

- **[文档索引](docs/README.md)** — 领域和组件指南。
- **[API 参考](docs/inventory.md)** — 已实现的生产代码类型及其类文档。
- **[架构](docs/decisions/index.md)** — 当前的产品与运行时决策。
- **[实现路线图](docs/coverage/index.md)** — 已实现、已适配和缺失的能力。
- **[平台验证](docs/platform-verification.md)** — 运行证据与平台限制。
- **[视觉识别](docs/design/identity.md)** — 已批准的 Sprite 方向、标志文件、色彩和字体。

<a id="examples"></a>

## 🎮 示例

- **[窗口与输入](examples/HostExample/README.md)** — 仅使用公共 API 的可运行示例，包含窗口、场景节点、键盘输入和正常退出。

[Android 设备探针](tests/Electron2D.AndroidProbe/README.md)和[浏览器着色器探针](tests/Electron2D.WebGpuProbe/README.md)是 `tests/` 下的验证工具，不是游戏示例。它们分别记录已验证的图形和物理路径。

<a id="feedback-and-contributing"></a>

## 💬 反馈与贡献

请通过 [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) 报告错误、提出功能需求或讨论设计。报告运行时问题时，请注明引擎版本、平台、渲染器及最小复现步骤。

欢迎提交 [Pull Request](https://github.com/edwardgushchin/Electron2D/pulls)。修改行为前请阅读[维护指南](docs/maintaining.md)和相关架构决策，并同时更新受影响的 API 文档与验证。

从仓库根目录运行可执行检查：

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

## 👥 贡献者

Electron2D 由 Eduard Gushchin 维护。仓库贡献者列表请参阅 [GitHub 页面](https://github.com/edwardgushchin/Electron2D/graphs/contributors)。

<a id="license"></a>

## 📄 许可证

Electron2D 自有代码采用 [MIT 许可证](licence/Electron2D-LICENSE.txt)。依赖项保留各自的许可证；请参阅[第三方声明](licence/THIRD_PARTY_NOTICES.md)。许可证文本保存在 `licence/`，并按适用情况随应用发布包提供。
