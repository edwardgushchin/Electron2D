<p align="right"><a href="README.md">English</a> · <a href="README.ru.md">Русский</a> · 简体中文 · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a></p>

<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-dark.svg">
    <source media="(prefers-color-scheme: light) and (max-width: 480px)" srcset="docs/design/assets/sprite/logo-compact-light.svg">
    <source media="(prefers-color-scheme: dark)" srcset="docs/design/assets/sprite/logo-primary-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/design/assets/sprite/logo-primary-light.svg">
    <img alt="Electron2D — Agent-native cross-platform 2D game engine" src="docs/design/assets/sprite/logo-primary-light.svg" width="640" height="148">
  </picture>
</h1>

<p align="center">
  <a href="#installation"><img alt="构建所需的 .NET 版本" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2FElectron2D.csproj&amp;query=substring-after%28%2FProject%2FPropertyGroup%2FTargetFramework%5Bnot%28%40Condition%29%5D%5B1%5D%2C+%27net%27%29&amp;label=.NET&amp;suffix=+SDK&amp;color=A63B75" height="28"></a>
  <a href="#license"><img alt="引擎许可证" src="https://img.shields.io/badge/dynamic/regex?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fraw.githubusercontent.com%2Fedwardgushchin%2FElectron2D%2Fmain%2Flicence%2FElectron2D-LICENSE.txt&amp;search=%5E%5Cs%2A%28%5CS%2B%29%5Cs%2BLicense&amp;replace=%241&amp;label=%E8%AE%B8%E5%8F%AF%E8%AF%81&amp;color=A63B75" height="28"></a>
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="最新发布版本" src="https://img.shields.io/badge/dynamic/xml?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;url=https%3A%2F%2Fgithub.com%2Fedwardgushchin%2FElectron2D%2Freleases.atom&amp;query=concat%28substring-after%28string%28%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%5B1%5D%2F%2A%5Blocal-name%28%29%3D%22link%22%5D%2F%40href%29%5B1%5D%29%2C+%22%2Ftag%2F%22%29%2C+substring%28%22%E6%9A%82%E6%97%A0%E7%89%88%E6%9C%AC%22%2C+1+div+not%28%2F%2F%2A%5Blocal-name%28%29%3D%22entry%22%5D%29%29%29&amp;label=%E7%89%88%E6%9C%AC&amp;color=A63B75" height="28"></a>
</p>

<p align="center">
  <a href="https://github.com/edwardgushchin/Electron2D/commits/main"><img alt="main 分支的最近提交" src="https://img.shields.io/github/last-commit/edwardgushchin/Electron2D/main?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%E6%9C%80%E5%90%8E%E6%8F%90%E4%BA%A4&amp;display_timestamp=committer&amp;color=A63B75" height="28"></a>
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

## <img src="docs/design/assets/sprite/readme-about.svg" width="24" height="24" align="absmiddle" alt=""> 关于项目

Electron2D 是一款**开源、跨平台的 C# 2D 游戏引擎，供开发者与 AI 智能体共同开发游戏**。

使用熟悉的 .NET 工具，以及 Codex、Claude Code 等 AI 助手，创作游戏世界和玩法。

<a id="features"></a>

## <img src="docs/design/assets/sprite/readme-features.svg" width="24" height="24" align="absmiddle" alt=""> 功能

- [图形](docs/domains/rendering.md)。精灵、纹理图集、摄像机、视差，以及图形和文字绘制。支持为材质导入 HLSL 和 GLSL 着色器。
- [场景与动画](docs/domains/scene.md)。可复用的对象和关卡、逐帧动画、属性动画和计时器。
- [物理](docs/domains/physics.md)。刚体、碰撞、区域、相交查询、铰链和弹簧。
- [游戏界面](docs/domains/scene.md)。按钮、输入框、滚动、布局容器、字体和主题。
- [音频](docs/domains/audio.md)。WAV、MP3 和 Ogg Vorbis、位置音效、混音、效果和录音。
- [输入](docs/domains/input.md)。键盘、鼠标、触控和控制器。将输入映射到游戏操作。
- [寻路](docs/domains/navigation.md)。在网格或指定点之间寻找路径，并考虑障碍物与移动代价。
- [资源](docs/domains/resources.md)。加载图像、字体和音频。支持渐变、曲线和程序化纹理。
- [本地化](docs/domains/localization.md)。翻译、复数形式和语言选择。
- [网络](docs/domains/networking.md)。TCP、UDP、本地套接字和 TLS 加密连接。

着色器材质需要 GPU 渲染器。兼容渲染器支持基础 2D 图形。详细说明和限制请参阅上方链接中的文档。

<a id="quick-start"></a>

## <img src="docs/design/assets/sprite/readme-quick-start.svg" width="24" height="24" align="absmiddle" alt=""> 快速开始

先运行“角色移动”示例。你会看到一个角色，并能用方向键移动它。以下 .NET 命令适用于 Windows、Linux 和 macOS；已完成的运行验证见[平台表](#platforms)。

<a id="installation"></a>

### 所需工具

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 和 Git。

通过 NuGet 安装 `Electron2D` 以及游戏目标平台的包，例如 `Electron2D.Windows`、`Electron2D.Linux` 或 `Electron2D.MacOS`。原生依赖会自动还原。[平台包及版本规则](docs/native-packaging.md)。

当前的 `0.1.0-alpha.1` 包仍在准备发布。

### 构建与运行

克隆仓库并构建库：

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

运行示例：

```bash
dotnet run --project examples/CharacterMovement
```

程序会打开一个场景，网格上有一个粉色角色。方向键让角色在区域内移动，按 Escape 或关闭窗口可退出。试着修改 `Player.cs` 中的移动速度，然后重新运行示例。

![Electron2D 角色移动：网格上的角色和键盘操作提示](docs/images/character-movement.png)

[示例源码](examples/CharacterMovement/CharacterMovementScene.cs) · [运行说明](examples/CharacterMovement/README.md)

### 在自己的游戏中使用 Electron2D

在 `Electron2D` 目录旁创建一个 .NET 10 控制台项目，并添加引擎项目引用。在仓库根目录执行以下命令：

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

通过 NuGet 添加目标平台的包，选择以下一条命令：

| 平台 | 命令 |
| --- | --- |
| Windows | `dotnet add ../MyGame/MyGame.csproj package Electron2D.Windows --prerelease` |
| Linux | `dotnet add ../MyGame/MyGame.csproj package Electron2D.Linux --prerelease` |
| macOS | `dotnet add ../MyGame/MyGame.csproj package Electron2D.MacOS --prerelease` |

将 `MyGame/Program.cs` 的内容替换为以下代码：

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
dotnet run --project ../MyGame/MyGame.csproj -c Release
```

使用 `AddChild` 将游戏对象添加到窗口。上方示例展示了帧更新和键盘输入处理。

构建引擎会生成 `Electron2D.dll`。发布游戏时会包含引擎库和原生依赖；自包含发布还会包含 .NET 运行时。

<a id="platforms"></a>

## <img src="docs/design/assets/sprite/readme-platforms.svg" width="24" height="24" align="absmiddle" alt=""> 平台

下表列出各游戏目标平台已完成的验证。可视化编辑器面向 Windows、Linux 和 macOS。

| 游戏目标平台 | 本仓库已完成的验证 |
| --- | --- |
| Windows，x86 / x64 / ARM64 | 尚未验证运行 |
| Linux，x64 / ARM64 | 已在 x64 上验证 Wayland 窗口、输入和渲染，也验证了 XWayland 渲染。尚未验证 ARM64 |
| macOS，x64 / ARM64 | 尚未验证运行 |
| Android，手机和平板 | 已在一台 ARM64 手机上验证渲染、着色器材质和物理 |
| Android TV | 已在一台 32 位电视上验证兼容渲染器和物理 |
| iOS 和 tvOS，设备与模拟器 | 已准备自动化库构建，尚未进行设备验证 |
| 浏览器 | 已在独立测试程序中验证渲染和物理。尚未实现浏览器游戏运行支持 |

Android 和浏览器验证目前仅涵盖特定场景。各目标平台的原生库可用情况见[原生库交付说明](docs/native-packaging.md)。

构建和测试徽章显示代码自动检查及测试应用的状态。真实设备上的运行需要单独验证。[自动检查详情](docs/platform-verification.md#automated-rid-checks)。

设备型号、命令和验证范围见[平台报告](docs/platform-verification.md)。

<a id="development"></a>

## <img src="docs/design/assets/sprite/readme-development.svg" width="24" height="24" align="absmiddle" alt=""> 引擎开发

场景可以保存到文件中重复使用，也可以在游戏下次启动时加载。这由 `PackedScene` 和[类型化资源及场景文件](docs/components/resource-files.md)提供。目前可视化编辑器仅显示启动画面，游戏项目编辑和项目管理命令尚未实现。

[引擎架构](docs/decisions/agent-native.md#adr-0090)规划了供 AI 助手使用的项目操作命令，以及带图像验证的游戏场景运行工具。完整工具集仍待实现。

下一步任务和各方法的状态见[开发路线图](docs/coverage/index.md)。已实现的行为在 API 参考中说明。

<a id="documentation"></a>

## <img src="docs/design/assets/sprite/readme-documentation.svg" width="24" height="24" align="absmiddle" alt=""> 文档

| 需求 | 文档 |
| --- | --- |
| 查找类或方法 | [API 参考](docs/inventory.md) |
| 了解子系统 | [文档目录](docs/README.md) |
| 准备着色器 | [HLSL 和 GLSL 导入工具](tools/shaders/README.md) |
| 了解架构与设计决策 | [架构决策](docs/decisions/index.md) |
| 选择开发任务 | [开发路线图](docs/coverage/index.md) |

<a id="examples"></a>

复现验证的说明分别见 [Android](tests/Electron2D.AndroidProbe/README.md) 和 [WebGPU](tests/Electron2D.WebGpuProbe/README.md) 文档。WebGPU 测试独立于引擎，用于验证浏览器能力。

<a id="feedback-and-contributing"></a>

## <img src="docs/design/assets/sprite/readme-contributing.svg" width="24" height="24" align="absmiddle" alt=""> 参与项目

[提问](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) · [报告问题](https://github.com/edwardgushchin/Electron2D/issues/new/choose) · [贡献指南](CONTRIBUTING.md) · [获取帮助](SUPPORT.md) · [行为准则](CODE_OF_CONDUCT.md) · [安全政策](SECURITY.md)

通过 [GitHub Issues](https://github.com/edwardgushchin/Electron2D/issues) 报告错误或提出功能建议。报告错误时，请注明引擎版本或提交、操作系统和渲染器，并附上最小复现示例及错误输出。

通过 [Pull Request](https://github.com/edwardgushchin/Electron2D/pulls) 提交修复。开始前请阅读[维护指南](docs/maintaining.md)及相关架构决策。代码、测试和文档应一起更新。

在仓库根目录运行主要检查：

```bash
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

<a id="contributors"></a>

项目由 [Eduard Gushchin](https://github.com/edwardgushchin) 维护。所有贡献者均列于 [GitHub 贡献者页面](https://github.com/edwardgushchin/Electron2D/graphs/contributors)。

<a id="license"></a>

## <img src="docs/design/assets/sprite/readme-license.svg" width="24" height="24" align="absmiddle" alt=""> 许可证

Electron2D 采用 [MIT 许可证](licence/Electron2D-LICENSE.txt)。可用于商业游戏，但须保留版权声明和许可证文本。

依赖项的许可证列于[第三方声明](licence/THIRD_PARTY_NOTICES.md)。分发游戏时，请附上这些依赖项要求的许可证文本，文件位于 `licence/` 目录。
