param(
    [string]$OutputDirectory = "$PSScriptRoot\..\build\win98",
    [string]$FrameworkDirectory = "$env:WINDIR\Microsoft.NET\Framework\v2.0.50727",
    [string]$RoslynCompiler = '',
    [switch]$RunSelfTests
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot\..").Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
if (!$RoslynCompiler) {
    $sdkRoot = Join-Path (Split-Path $dotnet) 'sdk'
    $candidates = @(Get-ChildItem -LiteralPath $sdkRoot -Directory | Where-Object {
        $_.Name -match '^\d+\.\d+\.\d+$' -and (Test-Path "$($_.FullName)\Roslyn\bincore\csc.dll")
    } | Sort-Object { [version]$_.Name } -Descending)
    if (!$candidates.Count) { throw 'A modern .NET SDK with Roslyn is required on the build computer.' }
    $RoslynCompiler = Join-Path $candidates[0].FullName 'Roslyn\bincore\csc.dll'
}
$references = 'mscorlib.dll','System.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xml.dll'
foreach ($reference in $references) {
    if (!(Test-Path -LiteralPath (Join-Path $FrameworkDirectory $reference))) {
        throw "Missing CLR 2.0 reference: $reference. Enable the Windows .NET Framework 3.5 feature or supply -FrameworkDirectory."
    }
}
$sources = @(Get-ChildItem -LiteralPath "$project\Modern" -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](?:obj|bin)[\\/]' } | Sort-Object FullName | ForEach-Object FullName)
$sources += @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.cs' | ForEach-Object FullName)
if (!$sources.Count) { throw 'Modern C# application sources are missing.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$executable = Join-Path $output 'Scmpoo.Win98.exe'
$arguments = @(
    '/nologo', '/noconfig', '/nostdlib+', '/langversion:latest', '/nullable:enable',
    '/optimize+', '/debug-', '/target:winexe', '/platform:x86', '/subsystemversion:4.0',
    '/define:LEGACY_WINDOWS', '/nowin32manifest', "/win32icon:$project\Scmpoo\100.ico", "/out:$executable"
)
$arguments += @($references | ForEach-Object { '/reference:' + (Join-Path $FrameworkDirectory $_) })
foreach ($asset in Get-ChildItem -LiteralPath "$project\Scmpoo" -File | Where-Object { $_.Extension -in '.bmp','.wav','.ico' }) {
    $arguments += ('/resource:' + $asset.FullName + ',Scmpoo.Assets.' + $asset.Name)
}
$arguments += $sources
& $dotnet $RoslynCompiler $arguments
if ($LASTEXITCODE -ne 0) { throw "CLR 2.0 compilation failed with exit code $LASTEXITCODE." }
Copy-Item -LiteralPath "$PSScriptRoot\Scmpoo.Win98.exe.config" -Destination "$executable.config"
Copy-Item -LiteralPath "$PSScriptRoot\README.md" -Destination (Join-Path $output 'WIN98-README.md')
& "$PSScriptRoot\audit-win98.ps1" -Executable $executable -ReportPath (Join-Path $output 'compatibility-audit.json')
if ($RunSelfTests) {
    $stdout = Join-Path $output 'self-test.stdout.txt'
    $stderr = Join-Path $output 'self-test.stderr.txt'
    $testOutput = Join-Path $output 'self-test'
    $report = Join-Path $testOutput 'self-test.txt'
    $process = Start-Process -FilePath $executable -ArgumentList @('--self-test', '--output', ('"' + $testOutput + '"')) -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    if ($process.ExitCode -ne 0) {
        throw "CLR 2.0 application self-test failed ($($process.ExitCode)); see $stderr and $report."
    }
    if (!(Test-Path -LiteralPath $report)) { throw "Self-test did not produce its success report: $report" }
    Write-Output "Compatibility application self-test passed; actual runtime version is recorded in: $report"
}
Get-Item -LiteralPath $executable | Select-Object FullName, Length
Write-Output 'Compiled for CLR 2.0 and audited. This does not establish execution compatibility on an actual Windows 98 installation.'
