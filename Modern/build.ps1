param(
    [ValidateSet('win-x64', 'win-x86')][string]$Runtime = 'win-x64',
    [switch]$SelfContained,
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $root "artifacts/modern-$Runtime"
& dotnet publish "$PSScriptRoot/Scmpoo.Modern.csproj" -c Release -r $Runtime --self-contained $SelfContained.IsPresent -o $destination
if ($LASTEXITCODE -ne 0) { throw 'Modern publish failed.' }
if (-not $SkipTests) {
    $testOutput = Join-Path $root "artifacts/tests-$Runtime"
    $run = Start-Process -FilePath "$destination/Scmpoo.Modern.exe" -ArgumentList @('--self-test', '--output', ('"' + $testOutput + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($run.ExitCode -ne 0) { throw "Regression checks failed; inspect $testOutput/error.txt" }
}
Get-Item "$destination/Scmpoo.Modern.exe"
