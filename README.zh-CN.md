<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · 简体中文 · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

Linux 原生无障碍功能通过 AT-SPI 发布场景名称、角色、状态、按钮和焦点操作以及 Range 数值。可编辑的 Slider、ScrollBar 和 SpinBox 接受原生数值请求；进度指示器为只读。请在打开窗口前设置 `ProjectSettings.AccessibilitySupport`；表格协议以及其他平台适配器仍在开发中。 AT-SPI 还提供可保存的标签、描述和控件关系、单选组、验证错误、活动后代及内部链接锚点。关系会随节点路径和目标生命周期更新。

原生 Text/EditableText 支持读取和编辑 LineEdit/TextEdit/CodeEdit，包括 Unicode 选择、文本几何信息和密码掩码。具体约定与验证范围见 [AccessibilityServer](docs/classes/AccessibilityServer.md#native-text)。

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D - Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="640" height="148">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="构建所需的 .NET 版本" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=brightgreen" height="28"></a>
  <a href="#license"><img alt="引擎许可证" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=%E8%AE%B8%E5%8F%AF%E8%AF%81&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="最新发布版本" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22%E6%9A%82%E6%97%A0%E7%89%88%E6%9C%AC%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=%E7%89%88%E6%9C%AC&amp;color=brightgreen" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="main 分支的最近提交" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%E6%9C%80%E5%90%8E%E6%8F%90%E4%BA%A4&amp;display_timestamp=committer&amp;color=brightgreen" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="自动构建状态" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%E6%9E%84%E5%BB%BA&amp;nameFilter=Build" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/actions/workflows/ci.yml"><img alt="测试 (GitHub Actions)" src="https://img.shields.io/github/check-runs/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%E6%B5%8B%E8%AF%95&amp;nameFilter=Tests" height="28"></a>
</p>

<p align="center">
  <a href="#quick-start">开始使用</a> ·
  <a href="#features">功能</a> ·
  <a href="#platforms">平台</a> ·
  <a href="#documentation">文档</a> ·
  <a href="#feedback-and-contributing">参与</a>
</p>

<p align="center">⭐ <a href="https://github.com/edwardgushchin/Electron2D">在 GitHub 上为我们点亮 Star</a> - 这会给我们很大的动力！</p>

<a id="about"></a>

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="28" align="absmiddle" alt=""> 关于项目

Electron2D 是一款**开源、跨平台的 C# 2D 游戏引擎，供开发者与 AI 智能体共同开发游戏**。

使用熟悉的 .NET 工具创作游戏世界和玩法。Codex、Claude Code 和其他 AI 助手可以协助编写代码。

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="28" align="absmiddle" alt=""> 功能

这里概述引擎已经实现的主要功能。各子系统的 API 仍在开发中，具体范围和限制见对应文档。

- [图形](docs/domains/rendering.md)。精灵和纹理图集、摄像机与视差、图形和文字、裁剪、离屏视口、光照、阴影及 HDR。支持 HLSL/GLSL 材质、可视化着色器图和 CPU/GPU 粒子。
- [场景](docs/domains/scene.md)。节点层级、可复用和链接场景、分组、事件、延迟调用及计时器。通过 C# 创建场景，连同资源一起保存，并再次实例化。
- [动画](docs/components/scene-animation.md)。逐帧和属性轨道、贝塞尔曲线、补间、混合图及状态机。[骨骼与 IK](docs/components/skeletal-animation.md) 驱动骨骼，按顶点权重变形多边形和网格。
- [网格](docs/components/meshes.md)与[瓦片地图](docs/components/tiles.md)。索引和程序化几何、网格实例化、动画图集、地形连接，以及带碰撞和导航层的方形、等距和六边形网格。
- [物理](docs/domains/physics.md)。CPU 和独立 GPU 后端、刚体和角色运动、区域、力、关节及弹簧。射线和形状查询、连续碰撞检测及[状态检查点](docs/components/physics-space-checkpoints.md)。
- [游戏界面](docs/domains/scene.md)与[文本](docs/components/text.md)。按钮、菜单、列表、树、布局容器、滚动、文本和代码编辑器及富文本。支持主题、Unicode 字形整形、双向排版和后备字体。
- [音频](docs/domains/audio.md)。WAV、MP3 和 Ogg Vorbis、程序化音频流、麦克风输入及位置音效。支持音频总线、混音、效果和录音。
- [视频播放](docs/components/video-playback.md)与[影片录制](docs/components/movie-writing.md)。播放带音频的 Ogg Theora 视频；以非实时方式将实际渲染帧和混合音频录制为 PNG/WAV、AVI 或 OGV。
- [输入](docs/domains/input.md)。键盘、鼠标、触控、控制器、操作映射、快捷键，以及带 IME 组合输入的文本输入。
- [窗口与显示](docs/domains/display.md)。窗口和全屏模式、多视口、嵌入式及原生弹出窗口、显示信息查询、缩放和画面呈现控制。
- [导航](docs/domains/navigation.md)。点图和网格 A-star 寻路、多边形区域、导航链接、从几何数据烘焙导航、代理沿路径移动及相互避障。
- [资源](docs/domains/resources.md)。类型化加载与保存、后台加载、图像、字体、音频、渐变、曲线及噪声纹理。[PCK/ZIP 资源包](docs/components/resource-packs.md)将文件挂载到资源文件系统。 [缺失材质恢复](docs/components/material-placeholders.md)保留场景资源数据，供原始类型恢复后使用。
- [核心服务与文件](docs/domains/core.md)。项目设置、JSON/XML 和配置文件、目录操作、压缩、加密及 ZIP 归档。数学、随机数、后台任务和运行时诊断。
- [本地化](docs/domains/localization.md)。翻译域与上下文、PO/MO 目录、语言回退、整数复数规则、伪本地化及本地化资源选择。
- [网络](docs/domains/networking.md)。TCP/UDP 和本地套接字、TLS/DTLS、HTTP/HTTPS、WebSocket 及 ENet。[WebRTC 数据通道](docs/components/webrtc.md)、类型化 RPC、权限控制、联网对象生成及[属性复制](docs/components/scene-replication.md)。
- [共享项目工作流](docs/components/project-jobs.md)。创建或打开项目，共享编辑未保存的场景与 C# 代码，构建和运行不可变快照，并检查带有过期状态的实际截图。任务目前需要 Linux；截图需要原生窗口和渲染器。
- [编辑器中的智能体](docs/components/project-agent.md)。原生任务与对话面板通过经过身份验证的 MCP 操作，将已验证版本的 Codex CLI 连接到共享的未保存文档，支持受保护的历史记录、取消、重新连接和实际运行截图。当前执行器配置需要 Linux 和已有 CLI 登录。

着色器材质、GPU 粒子、光照与阴影及 HDR 需要 GPU 渲染器；兼容渲染器提供其文档所述的 2D 功能。物理后端与渲染器分别选择。具体支持范围和验证限制见各子系统文档及[平台报告](docs/platform-verification.md)。

<a id="agent-support"></a>

## 智能体提供商与客户端

支持范围仅限于经过验证的客户端版本和平台。详见[支持矩阵及限制](docs/components/project-agent.md#provider-and-client-support)。

- **OpenAI / Codex：** 已验证有限工作流的编辑器执行器，使用 Linux 上的 Codex CLI 0.163.0。外部 CLI 的真实模型验收仍需单独进行。
- **Anthropic / Claude Code：** 计划提供编辑器执行器及操作/skill 配置，尚未验证。
- **Google / Gemini CLI：** 计划提供编辑器执行器及操作/skill 配置，尚未验证。
- **DeepSeek：** 计划通过兼容执行器接入模型提供商，尚未验证。
- **Moonshot AI / Kimi：** 计划提供 Kimi Code CLI 的编辑器、MCP 和 skill 配置，尚未验证。
- **Alibaba / Qwen：** 计划提供 Qwen Code 的编辑器、MCP 和 skill 配置，尚未验证。
- **Z.ai / GLM：** 计划通过兼容执行器接入模型提供商，尚未验证。
- **MiniMax / M-series：** 计划通过兼容执行器接入模型提供商，尚未验证。
- **GitHub / Copilot CLI：** 计划作为操作/skill 的外部客户端，尚未验证。
- **Cursor / Cursor Agent：** 计划作为操作/skill 的外部客户端，尚未验证。

随附的开发 skill、按需限量加载文档上下文，以及支持分支、提交、push 和 worktree 的[编辑器 Git 工作区](docs/decisions/agent-native.md#git-workspace-in-the-editor-and-operation-host)，均已纳入架构决策，目前尚未实现。手动开发和常规 .NET 游戏构建无需智能体提供商。

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="28" align="absmiddle" alt=""> 快速开始

从 [CharacterMovement](examples/CharacterMovement/README.md) 开始。在电脑上用方向键移动角色，在手机上用手指拖动，在电视上用遥控器的方向按钮。同一个示例面向引擎的所有目标平台。

<a id="installation"></a>

### 所需工具

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。仅从源码构建时需要 Git。

通过 NuGet 安装主包 `Electron2D` 和目标平台的包，命令见[下方表格](#use-electron2d-in-your-game)。NuGet 会自动还原原生依赖。[平台包版本规则](docs/native-packaging.md)。

安装预发布包时需要 `--prerelease`。请使用版本匹配的引擎包和平台包。已发布的版本见 [NuGet](https://www.nuget.org/packages/Electron2D)。

### 构建与运行

创建自己的项目，请参阅[创建游戏项目](#use-electron2d-in-your-game)。以下命令用于构建和运行仓库中的现有示例。

从源码构建时，克隆仓库并构建库：

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

在电脑上运行示例：

```bash
dotnet run --project examples/CharacterMovement
```

程序会打开一个固定为 800 × 600 的窗口，网格上有一个粉色角色。方向键让角色在区域内移动，按 Escape 或关闭窗口可退出。窗口和角色的大小保持不变。试着修改 `Player.cs` 中的移动速度，然后重新运行示例。

macOS 请使用[应用包](examples/CharacterMovement/README.md#macos)。示例 README 中提供了 [Android](examples/CharacterMovement/README.md#android-phone-or-tablet)、[Android TV](examples/CharacterMovement/README.md#android-tv)、[Web](examples/CharacterMovement/README.md#web)、[iOS](examples/CharacterMovement/README.md#ios) 和 [Apple TV](examples/CharacterMovement/README.md#apple-tv-tvos) 的操作步骤。

![CharacterMovement：网格上的角色](docs/images/character-movement.png)

[示例源码](examples/CharacterMovement/CharacterMovementScene.cs) · [运行说明](examples/CharacterMovement/README.md)

<a id="use-electron2d-in-your-game"></a>

### 创建游戏项目

为面向 Windows、Linux 或 macOS 的游戏创建一个 .NET 10 控制台项目，并添加引擎包：

```bash
dotnet new console -n MyGame --framework net10.0
cd MyGame
dotnet add package Electron2D --prerelease
```

也可以使用引擎源码。此时改为在仓库根目录执行以下命令：

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
cd ../MyGame
```

然后安装目标平台的包：

| 平台 | 命令 |
| --- | --- |
| Windows | `dotnet add package Electron2D.Windows --prerelease` |
| Linux | `dotnet add package Electron2D.Linux --prerelease` |
| macOS | `dotnet add package Electron2D.MacOS --prerelease` |
| Web | `dotnet add package Electron2D.Web --prerelease` |
| Android 和 Android TV | `dotnet add package Electron2D.Android --prerelease` |
| iOS | `dotnet add package Electron2D.iOS --prerelease` |
| Apple TV (tvOS) | `dotnet add package Electron2D.tvOS --prerelease` |

面向多个平台时，请添加每个所需的平台包。Web、Android、iOS 和 tvOS 需要对应的 .NET 工作负载和平台应用入口。平台包不包含现成的应用项目。下方控制台示例适用于 Windows、Linux 和 macOS。

将 `Program.cs` 的内容替换为以下代码：

```csharp
using Electron2D;

var window = new Window
{
    Title = "我的游戏",
    Size = new Vector2i(800, 600)
};

return Engine.Run(window);
```

运行程序：

```bash
dotnet build -c Release
dotnet run -c Release
```

使用 `AddChild` 将游戏对象添加到窗口。上方示例展示了帧更新和键盘输入处理。

构建引擎会生成 `Electron2D.dll`。发布游戏时会包含引擎库和原生依赖；自包含发布还会包含 .NET 运行时。

#### 手机、电视和浏览器

以 [CharacterMovement](examples/CharacterMovement/README.md#start-your-own-project) 为起点。将其源码和平台文件复制到 `examples/MyGame`，将项目重命名为 `MyGame.csproj`，并设置自己的应用标识符。示例 README 说明了复制步骤、应用图标和设备安装方法。

然后选择目标：

| 目标 | 在仓库根目录执行的命令 | 安装与运行 |
| --- | --- | --- |
| Android，ARM64 手机或平板 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm64` | [USB、APK 和触摸](examples/CharacterMovement/README.md#android-phone-or-tablet) |
| Android TV，32 位 ARM | `dotnet build examples/MyGame/MyGame.csproj -c Release -r android-arm` | [网络 ADB 和遥控器](examples/CharacterMovement/README.md#android-tv) |
| Web | `dotnet publish examples/MyGame/MyGame.csproj -c Release -r browser-wasm -o bin/my-game/web` | [HTTP 服务器和 Chrome](examples/CharacterMovement/README.md#web) |
| iOS，ARM64 Mac 上的模拟器 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r iossimulator-arm64 -p:EnableCodeSigning=false` | [模拟器和已签名应用](examples/CharacterMovement/README.md#ios) |
| Apple TV，ARM64 Mac 上的模拟器 | `dotnet build examples/MyGame/MyGame.csproj -c Release -r tvossimulator-arm64 -p:EnableCodeSigning=false` | [tvOS 模拟器和设备](examples/CharacterMovement/README.md#apple-tv-tvos) |

手机和电视也有其他架构，示例 README 列出了全部选项。构建 iOS 和 tvOS 需要 macOS 和 Xcode。

手机和电视上的应用占满屏幕。浏览器提供全屏按钮。游戏区域适应屏幕宽高比，角色、文字和网格统一缩放。

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="28" align="absmiddle" alt=""> 平台

下表列出各游戏目标平台已完成的验证。可视化编辑器面向 Windows、Linux 和 macOS。

| 游戏目标平台 | 本仓库已完成的验证 |
| --- | --- |
| Windows，x86 / x64 / ARM64 | 完整无界面测试及可用的裁剪/AOT 检查已通过 CI。尚未验证渲染 |
| Linux，x64 / ARM64 | 两种架构的完整无界面测试和裁剪/AOT 检查均已通过。已在 x64 上验证 Wayland 窗口、输入和渲染，也验证了 XWayland 渲染。尚未验证 ARM64 渲染 |
| macOS，x64 / ARM64 | 完整无界面测试和裁剪/AOT 检查已通过 CI。尚未验证渲染 |
| Android，手机和平板 | 已在一台 ARM64 手机上验证 CharacterMovement 启动、全屏布局和触摸拖动。着色器材质和物理已单独验证 |
| Android TV | 已在一台 32 位电视上验证 CharacterMovement 和遥控器方向按钮。兼容渲染器和物理已单独验证 |
| iOS 和 tvOS，设备与模拟器 | 四种模拟器配置的原生契约检查均已通过。设备应用包可进行未签名构建；尚未验证真机运行或渲染 |
| 浏览器 | 已在 Chrome 中验证 CharacterMovement、自适应布局和方向键。竖屏布局和触摸拖动通过手机模拟验证，尚未在真实移动浏览器中验证。渲染和物理已单独验证 |

Android 和浏览器验证涵盖指定设备及 Chrome 中的示例。各目标平台的原生库可用情况见[原生库交付说明](docs/native-packaging.md)。

构建和测试徽章显示代码自动检查及测试应用的状态。真实设备上的运行需要单独验证。[自动检查详情](docs/platform-verification.md#automated-rid-checks)。

设备型号、命令和验证范围见[平台报告](docs/platform-verification.md)。

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="28" align="absmiddle" alt=""> 项目状态

Electron2D 处于 alpha 阶段。公共 API 仍在开发中，可能随版本变化。

可以在代码中创建场景，通过 `PackedScene` 保存，再从[资源及场景文件](docs/components/resource-files.md)中加载。目前可视化编辑器仅显示启动画面，游戏项目编辑尚未实现。

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="28" align="absmiddle" alt=""> 文档

| 需求 | 文档 |
| --- | --- |
| 查找类或方法 | [API 参考](docs/inventory.md) |
| 了解子系统 | [文档目录](docs/README.md) |
| 准备着色器 | [HLSL 和 GLSL 导入工具](tools/shaders/README.md) |
| 构建和运行未保存的项目并检查截图 | [共享项目任务](docs/components/project-jobs.md) |
| 准备发布 | [发布流程](docs/releasing.md) |

<a id="examples"></a>

复现验证的说明分别见 [Android](tests/Electron2D.AndroidProbe/README.md) 和 [WebGPU](tests/Electron2D.WebGpuProbe/README.md) 文档。WebGPU 测试独立于引擎，用于验证浏览器能力。

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="28" align="absmiddle" alt=""> 参与项目

[提问](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [报告问题](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [贡献指南](CONTRIBUTING.md) · [获取帮助](SUPPORT.md) · [行为准则](CODE_OF_CONDUCT.md) · [安全政策](SECURITY.md)

通过 [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) 报告错误或提出功能建议。报告错误时，请注明引擎版本或提交、操作系统和渲染器，并附上最小复现示例及错误输出。

通过 [Pull Request](https://github.com/edwardgushchin/Electron2D/pulls) 提交修复。工作流程见[贡献指南](CONTRIBUTING.md)。

<a id="contributors"></a>

项目由 [Eduard Gushchin](https://github.com/edwardgushchin) 维护。所有贡献者均列于 [GitHub 贡献者页面](https://github.com/edwardgushchin/Electron2D/graphs/contributors)。

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="28" align="absmiddle" alt=""> 许可证

Electron2D 采用 [MIT 许可证](licence/Electron2D-LICENSE.txt)。可用于商业游戏，但须保留版权声明和许可证文本。

依赖项的许可证列于[第三方声明](licence/THIRD_PARTY_NOTICES.md)。分发游戏时，请附上这些依赖项要求的许可证文本，文件位于 `licence/` 目录。
