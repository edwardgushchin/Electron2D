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
  <a href="https://github.com/edwardgushchin/Electron2D/releases"><img alt="最新发布版本" src="https://img.shields.io/github/v/release/edwardgushchin/Electron2D?style=flat&amp;labelColor=3D2749&amp;cacheSeconds=300&amp;label=%E7%89%88%E6%9C%AC&amp;color=A63B75" height="28"></a>
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

## 🧭 关于项目

Electron2D 是一款**开源、跨平台的 C# 2D 游戏引擎，供开发者与 AI 智能体共同开发游戏**。

使用熟悉的 .NET 工具，以及 Codex、Claude Code 等 AI 助手，创作游戏世界和玩法。

<a id="features"></a>

## ✨ 功能

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

## 🚀 快速开始

先运行“窗口与输入”示例。以下命令适用于使用 Wayland 的 Linux x64 环境。

<a id="installation"></a>

### 所需工具

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 和 Git。

私有原生组件作为 NuGet 依赖项还原。首个包的可用性和完整原生重建说明见[原生包交付](docs/native-packaging.md)。

### 构建与运行

克隆仓库并构建库：

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build Electron2D.csproj -c Release
```

发布包含独立 .NET 运行时的示例并启动：

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

程序会打开一个窗口。方向键移动游戏对象，其坐标会显示在终端中。按 Escape 或关闭窗口可退出程序。此示例不绘制场景，而是展示引擎启动和输入处理。

[示例源码](examples/HostExample/Program.cs) · [运行说明](examples/HostExample/README.md)

### 在自己的游戏中使用 Electron2D

在 `Electron2D` 目录旁创建一个 .NET 10 控制台项目，并添加引擎项目引用。在仓库根目录执行以下命令：

```bash
dotnet new console -n MyGame -o ../MyGame --framework net10.0
dotnet add ../MyGame/MyGame.csproj reference Electron2D.csproj
```

将 `MyGame/Program.cs` 的内容替换为以下代码：

```csharp
using Electron2D;

var window = new Window
{
    Title = "我的游戏",
    Size = new Vector2i(960, 540)
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

## 🖥️ 平台

游戏的目标平台和已完成的验证分列如下。可视化编辑器面向 Windows、Linux 和 macOS。

| 游戏目标平台 | 本仓库已完成的验证 |
| --- | --- |
| Windows，x86 / x64 / ARM64 | 尚未验证运行 |
| Linux，x64 / ARM64 | 已在 x64 上验证 Wayland 窗口、输入和渲染，也验证了 XWayland 渲染。尚未验证 ARM64 |
| macOS，x64 / ARM64 | 尚未验证运行 |
| Android，手机和平板 | 已在一台 ARM64 手机上验证渲染、着色器材质和物理 |
| Android TV | 已在一台 32 位电视上验证兼容渲染器和物理 |
| iOS 和 tvOS，设备与模拟器 | 已准备自动化库构建，尚未进行设备验证 |
| 浏览器 | 已在独立测试程序中验证渲染和物理。尚未实现浏览器游戏运行支持 |

Linux x64 的完整原生文本和音频库已构建。其他平台的构建与集成仍是独立任务。Android 和浏览器验证仅涵盖特定场景。

构建和测试徽章覆盖全部 18 个 RID：启用分析器的库构建、Linux 完整无窗口测试或桌面可移植测试，以及 trimmed/AOT、Android、Apple 模拟器和浏览器契约测试应用。Apple 实机及其他平台的完整原生运行验收仍需单独完成。参见 [CI 验证范围](docs/platform-verification.md#automated-rid-checks)。

设备型号、命令和验证范围见[平台报告](docs/platform-verification.md)。

<a id="development"></a>

## 🔧 引擎开发

目前可通过 C# 和 .NET 使用 Electron2D。`PackedScene` 模板支持[类型化资源和场景文件](docs/components/resource-files.md)，并可在新进程中加载。可视化编辑器和游戏项目管理命令仍在计划中。

与 AI 协作已纳入[引擎架构](docs/decisions/agent-native.md#adr-0090)：项目操作、游戏场景运行和图像验证需要通过有文档说明的工具完成。完整工具集仍待实现。

下一步任务和各方法的状态见[开发路线图](docs/coverage/index.md)。已实现的行为在 API 参考中说明。

<a id="documentation"></a>

## 📚 文档

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

## 💬 参与项目

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

## 📄 许可证

Electron2D 采用 [MIT 许可证](licence/Electron2D-LICENSE.txt)。可用于商业游戏，但须保留版权声明和许可证文本。

依赖项的许可证列于[第三方声明](licence/THIRD_PARTY_NOTICES.md)。分发游戏时，请附上这些依赖项要求的许可证文本，文件位于 `licence/` 目录。
