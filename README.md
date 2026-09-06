<meta charset="UTF-8">

# STRAY SHEEP The Screen Mate

[![Build Windows executables](https://github.com/star9platinum/Scmpoo/actions/workflows/build-windows.yml/badge.svg)](https://github.com/star9platinum/Scmpoo/actions/workflows/build-windows.yml)

Modern C# version 3.0, based on the reconstructed Windows version 2.0 by CVTS.

## Windows 98 兼容分支

本分支从现代 C# 分支 `1008e5f` 派生，加入 `Legacy/` 构建工具。同一套扩展程序源码使用现代 Roslyn 编译器生成 **x86、PE32、CLR 2.0、OS/Subsystem 4.0** 的 `Scmpoo.Win98.exe`，不依赖 .NET 10。原始素材全部内嵌，设置、共享调度、动作、数量、暂停、预设等功能保留。

```powershell
./Legacy/build-win98.ps1 -RunSelfTests
# 输出：build/win98/Scmpoo.Win98.exe 和同名 .config
```

构建机需要现代 .NET SDK 和 .NET 2.0 参考程序集；可使用本机 .NET 3.5 可选组件提供的 CLR2 文件，或通过 `-FrameworkDirectory` 指定独立参考程序集。CI 使用固定版本 `Microsoft.NETFramework.ReferenceAssemblies.net20` 1.0.3，无须在构建机安装 Win98。运行环境配置优先 CLR2，现代 Windows 上可回退 CLR4；报告记录实际运行时版本。

**目标环境为 Windows 98 SE 加 .NET Framework 2.0 RTM。** 不要使用不支持 Win98 的 .NET 2.0 SP1/SP2、3.5 或更高运行库。已在现代 Windows 的真实 CLR `2.0.50727.9179` 上通过完整自检及 32 只窗口压力测试，并检查了 PE、程序集依赖、原生导入和资源；**尚未在 Win98 真机或虚拟机运行，不能宣称已完成 Win98 实机兼容验证**。

当前完整可执行文件约 372 KiB，连同 `.config` 即可分发，不要把构建用的参考程序集当作运行库一起复制。CLR2 的实际设置窗口测试也通过了从 3 只扩展到 32 只并立即应用、单只设置隔离、窗口未提交修改保留和暂停同步。

兼容分支禁用现代 DPI 初始化和 Win9x 不支持的进程 CPU/内存计数，相关测试结果明确标为不可用。XML 保存改用可恢复的重命名流程，避免 `File.Replace`；字符串互操作让 CLR 在 Win9x 选择 ANSI API。两种缺失的 `Action` 委托签名由兼容层补充。Windows 98 的共享 GDI/USER 资源较少，建议从一只、1 倍大小开始验证。详细前提、技术实现、构建参数和验证限制见 [Legacy/README.md](Legacy/README.md)。

## C# 3.0 分支

本分支以 `Modern/Scmpoo.Modern.csproj` 为主程序，使用现代 C#、.NET 10 和 WinForms。动画、设置、窗口管理、音频和绘制全部由 C# 实现，不需要调用原生版 EXE 或 DLL。原来的 `Scmpoo/Scmpoo.c` 保留为可构建的历史实现及行为参考；`master` 上的修复版本为 `176aa39`。

双击小羊打开设置；右键菜单可调用剧情、补齐 32 只或关闭全部。再次运行现代版会通知同一桌面的现有进程添加小羊，不会再启动一整套资源。`--settings` 打开现有小羊的设置，`--count 32` 在首次启动时建立 32 只。托盘图标提供设置、数量和暂停全部操作。

现代扩展包括每只小羊的 50%–200% 速度、0%–500% 特殊动作频率、1–4 倍像素显示、指定活动显示器、跟随鼠标、暂停、静音时段（支持跨午夜）、可关闭的休息提醒和 XML 预设导入导出。设置窗口列出全部 30 种原版动作入口，包括黑羊、浴盆和三种 UFO 剧情。**立即应用到所有小羊**直接为当前进程内的全部对象应用独立配置副本，不需要等待磁盘轮询或重启。另有“应用到当前小羊”；其他设置窗口中未提交的修改不会因此被覆盖。

### 构建与运行现代版

```powershell
dotnet build Modern/Scmpoo.Modern.csproj -c Release
./Modern/build.ps1 -Runtime win-x64
# 同时打包 .NET 运行库，目标机器无须预先安装 .NET：
./Modern/build.ps1 -Runtime win-x86 -SelfContained
./artifacts/modern-win-x64/Scmpoo.Modern.exe --count 32 --settings
```

开发需要 .NET 10 SDK；未带运行库的发布目录需要对应架构的 .NET 10 Desktop Runtime。请分发整个发布目录。CI 对现代分支构建 x64 和 x86 的自包含包，并运行确定性回归测试。原生版的 CMake 构建方法保留在下文。

### 现代实现细节

| 模块 | 责任 |
| --- | --- |
| `Animation/SheepActor.cs` | 每只小羊独立状态、随机源、报时生命周期、拖动、显示器恢复、平台接口 |
| `Animation/AnimationMachine.cs` | 原 C 状态机的全部 0–154 状态标签、原始动作帧表与剧情子窗口状态；显式保留原来的跳转顺序 |
| `Platform/DesktopSnapshot.cs` | 全群共用的每 250 ms 系统窗口快照、工作区和指针信息，排除本进程窗口 |
| `FlockContext.cs` | 一个 UI 线程、一个 27 ms 调度器、每只小羊独立更新期限；落后时跳过积压，不反复追帧；全部暂停时降至 250 ms |
| `Rendering/SpriteAtlas.cs` | 用 GDI+ 解码原版 11 张 RLE8 BMP，统一缓存 176 帧、镜像、最近邻缩放、原掩码淡出和透明区域 |
| `UI/SpriteWindow.cs` | 每只小羊一个无边框窗口，剧情时按需创建伴随窗口；仅换帧时重建形状，未变帧仅更新位置 |
| `Services/SoundService.cs` | 全群唯一音频工作线程和请求槽；200 ms 播放请求间隔，合并瞬时重复音效；仅接受播放请求后才转移声音归属 |
| `Settings/AppSettings.cs` | 配置校验、禁用 DTD 的 XML 解析、完整文件保存、跨午夜静音时段；每只小羊使用独立配置副本 |

小羊之间直接读取对象坐标，不再通过跨进程窗口数据交换位置。显示器热拔插刷新工作区并恢复丢失的小羊；特殊剧情限定到事件开始时的真实屏幕工作区，避免将剧情中心放在虚拟桌面空洞中。所有声音调用均在工作线程运行，声音设备阻塞不会阻塞动作状态推进。停用报时会立即退出相应等待状态，声音关闭不影响报时动作的正常结束。

缩放是保留原版运动参数的像素显示倍率：按脚底中点放大，物理碰撞和剧情间距仍使用原始 40 像素逻辑尺寸。在高倍率、屏幕边缘或密集碰撞时，视觉轮廓可能超出逻辑边界；这不是高分辨率物理引擎。UFO 光束保留原来的桌面像素重着色，临时缓冲按 128 像素增长，仅光束存在时捕获该区域。

配置位于 `%LOCALAPPDATA%\Scmpoo\modern-settings.xml`，与原生版 INI 分开。首次启动现代版使用默认值；预设导入先填充当前设置窗口，点击应用后生效。静音时段起止相同时视为全天静音。按用户登录会话限制为一个现代进程；原生版与现代版分别管理各自的羊群。

### 回归与性能

```powershell
./Modern/bin/Release/net10.0-windows/Scmpoo.Modern.exe --self-test --output artifacts/modern-tests
./Modern/bin/Release/net10.0-windows/Scmpoo.Modern.exe --ui-test --output artifacts/modern-ui
./Modern/bin/Release/net10.0-windows/Scmpoo.Modern.exe --stress-test --output artifacts/modern-stress
```

命令完成后，输出目录中的 `self-test.txt`、`ui-test.txt` 或 `modern-stress.xml` 是结果；失败写入 `error.txt` 并返回非零退出码。界面测试会暂存并恢复现代设置文件，运行前应关闭现有现代羊群。

动作测试覆盖全部 30 种动作入口、重力开关、长时间 32 只模拟、报时取消、睡眠唤醒及零坐标窗口/地面，合计至少 246,000 次模拟更新。精灵测试逐像素核对原素材、镜像、透明区域、缩放和淡出，并生成素材联系图。真实窗口测试核验设置按钮和全群应用；压力测试在真实 32 只窗口上执行叫声及浴盆，输出 CPU、内存、动画步数、窗口扫描次数和缓存项数。

本机一次 12 秒压力运行中，原生 32 进程合计工作集约 670 MiB，现代单进程约 86 MiB，现代私有提交约 39 MiB。两轮包含相似但不完全相同的剧情负载，CPU 约为单个逻辑核心的 54% 与 57%，不能据此宣称 CPU 加速比；主要已验证收益是共享资源、减少进程和桌面扫描。总工作集含共享页，这些数值也不是精确的独占内存对比。具体开销取决于显示器、剧情、倍率、速度、音频驱动和桌面窗口数量。

## 原生版说明与历史

![Windows 3.1](/../images/Windows3.1.png?raw=true) ![Windows 10](/../images/Windows10.png?raw=true)

_STRAY SHEEP The Screen Mate_ (Japanese: STRAY SHEEP スクリーンメイト) is a [digital pet](https://en.wikipedia.org/wiki/Digital_pet) based on Fuji TV's multimedia project _STRAY SHEEP_ (Japanese: ストレイシープ). The application includes notable character animations in Fuji TV's late night animation series _The Adventure of STRAY SHEEP_ (1994) (Japanese: ストレイシープの大冒険), featuring main characters such as the sheep Poe and Merry, along with the alien Hue.

This repository aims to reconstruct C source code from disassembly of the original 16-bit New Executable and add compatibility with 32-bit and 64-bit Windows (NT) operating systems.

Compiled executable files are available for download on this repository's [Releases](https://github.com/star9platinum/Scmpoo/releases) page.

## Usage

Poe appears on desktop and normally chooses random actions (most likely run or walk) with certain probabilities. Some of the actions will play his voice. Seven special actions may occur at times only when there is only one instance running.

Poe will react to visible windows under certain circumstances. For example, Poe may collide with a visible window when running and switch to collision actions, or may fall onto the top edge of a visible window when gravity is enabled.

Up to 32 instances can be run simultaneously. Instances discover one another through top-level windows and exchange window handles with `WM_USER` messages; their positions are then used by the original collision state machine. An error message will appear when trying to run another instance exceeding that number. When there are multiple instances, one may collide with another and both will switch to collision actions.

Drag Poe with left/right mouse button to move him to an arbitrary position in the screen view.

Drag a file onto Poe and he will "eat" the file. If the file is a Waveform Audio (.wav), the sound contained will be played.

Double-click with left mouse button to bring up a configuration window. The original application stored four options in an INI file under the Windows directory and consequently required administrator privileges on Windows Vista and above. You can exit the instance from the configuration window.

This modernized branch uses a Unicode Simplified Chinese settings window and stores configuration in `%LOCALAPPDATA%\Scmpoo\settings.ini`, so administrator privileges are no longer required. In addition to the four original options, it provides animation speed, an adjustable 0%–500% random special-animation frequency, optional always-on-top behavior, an owner name, rest reminders, speech bubbles, and buttons for previewing the original flower, burn/bathtub, black sheep, UFO, and fall sequences. The settings window shows the current live instance count, and both it and the right-click menu can fill the desktop to 32 running sheep or close every running sheep with one command. Run `Scmpoo.exe --settings` to open the settings window immediately.

The animation engine now uses the complete Windows virtual desktop, including monitors positioned to the left or above the primary display, and declares Per-Monitor V2 DPI awareness. Each monitor's usable work area is treated as the floor, so Poe lands above its taskbar instead of falling behind it. Original 40-by-40 sprite frames and the reconstructed 153-state behavior machine remain unchanged.

Sprite windows are clipped with the original monochrome frame masks, so unused pixels are genuinely transparent and do not intercept mouse input. This applies to Poe, Merry, the black-sheep sequence, and the UFO effect while preserving the original crisp pixel edges.

The modern renderer converts each sprite sheet mask into cached frame-region data in one pass, reuses its GDI device contexts and render buffers, and moves unchanged frames without repainting them. Instance timers are phase-staggered across all 32 process slots so a batch launch does not produce synchronized redraw spikes.

The settings dialog also provides **立即应用到所有小羊** (apply immediately to all sheep). It validates and saves the current controls, updates this sheep, and queues a settings reload in every running sheep on the current desktop. The dialog stays open, and other open settings dialogs refresh their controls. Newly launched sheep read the same saved settings.

Double-click with right mouse button exits the instance.

Double-click with left mouse button with Ctrl and Shift buttons pressed down to bring up a debug window. Click on the 30 radio buttons to choose Poe's current action, which will take effect instantly. Click the four control buttons to move Poe instantly for 20 pixels by corresponding direction. Click "OK" button to close the debug window.

## Build from source

You can use CMake to generate Visual Studio projects and MinGW Makefiles. A minimum version of 3.16.0 is required.

Every push to `master` is built automatically for Windows x64 and x86. Open the latest successful [Build Windows executables](https://github.com/star9platinum/Scmpoo/actions/workflows/build-windows.yml) run and download either the `Scmpoo-Windows-x64` or `Scmpoo-Windows-x86` artifact to get the corresponding `Scmpoo.exe`.

```powershell
cmake -S . -B build/native-x64 -A x64
cmake --build build/native-x64 --config Release --parallel
ctest --test-dir build/native-x64 -C Release --output-on-failure
# Repeat with -A Win32 and a different build directory for x86.
./tests/stress.ps1 -Executable ./build/native-x64/Scmpoo/Release/Scmpoo.exe
```

## 动画、声音与 32 实例修复

原版整点报时进入状态 81/82，依靠剩余报时次数计数退出。如果报时期间关闭报时选项，旧实现会停止调用整个报时处理器，导致永远停留在叫声等待状态。现在每次动画更新都会处理取消或完成；关闭报时立即取消未完成的报时，并回到普通动作或睡眠状态。计时使用无符号时间差，兼容 `GetTickCount` 约 49.7 天回绕。

旧声音代码在 UI 线程调用 `sndPlaySound(NULL, SND_SYNC)`，切换音效时还反复装载资源。32 只整点同时发声时，一个慢音频设备就可能阻塞动画线程。现在所有 WinMM 调用，包括播放、循环和停止，都在延迟创建的后台线程上执行。一个加锁的待处理槽只保留最新请求，不建立无限声音队列；锁内仅复制小型命令，驱动调用在锁外。内嵌 WAVE 通过 `SND_RESOURCE` 播放，资源生命周期覆盖整个进程。退出只投递关闭请求，不等待音频驱动，事件和锁由进程退出回收。

实例加入和退出通知由同步 `SendMessage` 改为 `PostMessage`。发现和通知同时核验 `ScreenMatePoo` 窗口类及 `Screen Mate` 标题，定期 `EnumWindows` 修复失效的实例列表。随机数只在初始化时播种一次，混合进程 ID 和高精度计数器，避免重新初始化动作时重复播种。

## 多显示器实现

- 缓存每个显示器的工作区，保留负 X/Y 坐标；显示器、工作区和 DPI 变化时刷新。正常出生位置从真实显示器选取，不落在虚拟桌面的空洞中。
- 行走边界合并当前高度实际连通的显示器，允许跨相邻屏幕，阻止走进错位显示器之间的空白。气泡也约束到所在屏幕的工作区。
- 显示器断开时，把不可见的小羊恢复到最近的有效工作区，重新建立落脚状态。40 像素原版素材保持物理像素大小，DPI 改变不会模糊放大素材。
- 碰撞查询使用 `INT_MIN` 表示无碰撞，`INT_MIN + 1` 表示超出地面；坐标 0 和 -1 都是有效的实际碰撞位置。旧代码把它们当作哨兵，导致左侧或上方屏幕的边界判断错误。
- 随机落在窗口上的 X 坐标按窗口宽度计算。修复了以窗口绝对右边缘取模的问题，窗口右边缘为 0 时不再发生除零。

原版特殊剧情仍使用部分虚拟桌面坐标，因此某些跨屏剧情可能短暂经过显示器间空隙；普通行走、出生、落脚和热拔插恢复使用真实工作区。

## 多实例效率和设置传播

| 路径 | 实现与边界 |
| --- | --- |
| 精灵渲染 | 共享本进程的帧掩码缓存、GDI DC 和缓冲位图；未换帧时只移动窗口 |
| 系统窗口扫描 | 每个进程最多每 250 ms 更新一次 128 项快照，排除最小化窗口和小羊窗口；使用前验证候选窗口是否仍有效 |
| 显示器查询 | 从缓存工作区查找最近屏幕，避免每帧调用显示器 API |
| UI 消息 | 实例通知与设置广播异步投递，其他实例打开模态设置窗口也不会阻塞发送者 |
| 音频 | 每个进程一个延迟创建的工作线程、一个待处理请求；过时请求合并 |
| 动画调度 | 每只小羊保留原版 108 ms 基础节奏，按速度调整，启动相位按实例槽错开 |

设置保存先生成包含全部字段的 UTF-16 INI 临时文件，成功写入和刷新后用 `MoveFileExW` 替换正式文件。失败会显示错误并保留原来的正式配置。临时文件包含进程 ID，避免同时保存时共用文件。通过注册消息 `Scmpoo.SettingsChanged.v1` 通知同桌面的已运行小羊重新读取最新设置；发送的消息中没有跨进程指针。接收方立即更新速度、主窗口和剧情子窗口置顶状态，并同步打开的设置窗口。关闭声音会投递停止请求，关闭气泡会移除现有气泡，开启持续活动会唤醒自动睡眠的小羊。广播使用同一用户的设置路径，不能跨登录会话修改其他用户的配置。

这仍是每只小羊一个进程的原生版本，位图、运行库和窗口缓存按进程各自持有。降低进程数和共享所有精灵资源需要架构改造，不能仅靠缩短计时器间隔获得性能收益。

## 验证方法

`tests/animation_tests.c` 覆盖报时中取消、睡眠报时恢复、计时回绕、零坐标实例碰撞、同名非小羊窗口过滤，以及人为阻塞音频驱动时动画仍能完成。压力请求发送 10,000 次，验证只保留最新声音请求。

`tests/monitor_tests.c` 使用合成多屏拓扑覆盖负坐标、屏幕错位与间隙、0/-1 地面、右边缘为 0 的窗口、窗口快照复用，以及移除显示器后睡眠小羊恢复。测试不依赖机器必须连接特定数量的屏幕。

`tests/stress.ps1` 启动 32 个真实进程，打开全部设置窗口，从其中一个窗口点击“立即应用到所有小羊”，核验全部 32 个窗口中的速度、特殊动作频率和称呼；然后执行同步剧情压力并反复检查窗口响应，输出 CPU 时间和总工作集。脚本只关闭自己启动的进程，并在结束时恢复原设置。运行前必须关闭已有小羊，测试会暂时修改当前用户配置。CPU 百分比按单个逻辑核心为 100% 计算，总工作集包含共享页，不能直接视为独占内存。真实异构 DPI 屏幕及物理热拔插仍应在目标设备上进行交互验收。

## Copyright information and credits

Original codebase owned by Village Center, Inc. (defunct)

All character sprites in bitmap images owned by Fuji Television Network, Inc. and Robot Communications Inc.

Artwork: NOMURA Tatsutoshi (Robot)

Producer: SAITŌ Akimi (Fuji TV)

Poe's voice: HARA Masumi
