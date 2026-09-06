<meta charset="UTF-8">

# STRAY SHEEP The Screen Mate

[![Build Windows executables](https://github.com/star9platinum/Scmpoo/actions/workflows/build-windows.yml/badge.svg)](https://github.com/star9platinum/Scmpoo/actions/workflows/build-windows.yml)

Modern Windows version 2.0 by CVTS.

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
