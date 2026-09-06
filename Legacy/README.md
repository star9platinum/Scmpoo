# Windows 98 Compatibility Build

This build compiles the expanded managed application from `Modern/` with the
modern Roslyn compiler against .NET Framework 2.0 libraries. It retains the
original animation machine, shared sprite atlas, multi-sheep scheduler, settings,
presets and supported desktop extensions. It is not a .NET 10 executable renamed
for an older operating system.

## Target Requirements

- Windows 98 SE is the intended target; this artifact has not been run on an
  actual Windows 98 computer or virtual machine.
- Install the original .NET Framework 2.0 RTM runtime and its prerequisites first.
  .NET 2.0 SP1/SP2, .NET 3.5, .NET 4 and modern .NET do not support Windows 98.
- Install working display and audio drivers. Begin with one sheep, 1x scale and
  sound disabled before increasing the count. Windows 98 has small shared GDI
  and USER resource limits; 32 actors require testing on the target hardware.
- UI text outside the system code page needs an appropriate language pack/font.
  The original pixel artwork remains the same on every build.

## Build On A Modern Windows Computer

```powershell
pwsh -File Legacy/build-win98.ps1 -RunSelfTests
```

The build computer needs a modern .NET SDK and the .NET Framework 2.0 reference
libraries. On current Windows, enabling the optional .NET Framework 3.5 feature
provides the latter under `%WINDIR%\Microsoft.NET\Framework\v2.0.50727`. Existing
libraries are used; the build script does not download or install a runtime.
`-FrameworkDirectory` and `-RoslynCompiler` override automatic discovery.

The CI job instead downloads the pinned NuGet package
`Microsoft.NETFramework.ReferenceAssemblies.net20` version `1.0.3` and supplies
its `build/.NETFramework/v2.0` directory. These are build-time reference files,
not a runtime to install on Windows 98. CI runtime tests may use the installed
CLR4 fallback; read the CLR version in `self-test/self-test.txt`.

Outputs are written to `build/win98/`. Distribute `Scmpoo.Win98.exe` together with
`Scmpoo.Win98.exe.config`. All original BMP, WAV and icon resources are embedded.
The config prefers CLR 2.0 and allows a CLR 4 fallback for testing the same
artifact on newer Windows. That fallback is not available on Windows 98.

## Implementation

The compiler uses `/nostdlib+`, explicit CLR 2.0 references, `/platform:x86`,
`/subsystemversion:4.0`, `/define:LEGACY_WINDOWS` and `/nowin32manifest`. Modern C#
syntax is lowered to ordinary CLR 2.0 IL; no newer BCL runtime is shipped or
required. The renderer uses pixel regions and ordinary WinForms/GDI+ drawing,
which avoids NT-only layered windows. Win32 string interop uses `CharSet.Auto`
where needed so CLR 2.0 selects ANSI APIs on Win9x.
Two small delegate declarations supply the zero- and two-argument `Action`
signatures missing from CLR 2.0; the existing generic `Action<T>` is retained.

The compatibility symbol disables modern DPI initialization, replaces XML DTD
configuration with the CLR 2.0 property, and uses a recoverable rename sequence
for settings writes because Win98 does not implement `File.Replace`. A target
file is moved to a unique backup, the completed temporary file is renamed into
place, and failures restore the backup. This is not a filesystem-atomic replace.

The static audit reads PE headers and CLR metadata, checks x86/PE32, OS/subsystem
version 4.0, CLR metadata `v2.0.50727`, framework references `2.0.0.0`, embedded
assets and the compiled native/managed API references. The PE startup import must
be only `mscoree.dll!_CorExeMain`. NT-only CPU/memory diagnostics are excluded from
this build, and stress reports mark those values `UnavailableOnWin98` instead of
inventing zero measurements. The JSON report explicitly
records that actual Windows 98 execution has not been tested.

## Verification Limits

The original animation machine has been compiled with these CLR 2.0 references
and executed on the locally installed CLR 2.0 runtime on modern Windows. Its
deterministic tests cover all 30 user-facing actions, 32 simultaneous sheep,
audio-independent baa completion, alarm cancellation, negative monitor origins,
zero-coordinate collisions, sleep/wake and live option changes. The application
build also runs its complete self-test when `-RunSelfTests` is supplied.

Compilation, metadata checks and CLR 2.0 execution do not prove Win98 hardware,
driver, font or resource-limit compatibility. Validate startup, transparency,
dragging, settings save/reload, original special scenes, sound, display changes
and progressively larger flocks in a Windows 98 SE VM or on the target computer
before treating this as a verified Win98 release.
