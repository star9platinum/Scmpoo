param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [string]$ReportPath = ''
)
$ErrorActionPreference = 'Stop'
$path = (Resolve-Path -LiteralPath $Executable).Path
$stream = [IO.File]::OpenRead($path)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $headers = $pe.PEHeaders
    if (!$pe.HasMetadata) { throw 'The executable has no managed metadata.' }
    $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    $failures = [Collections.Generic.List[string]]::new()
    function Read-PeAscii([int]$rva) {
        $reader = $pe.GetSectionData($rva).GetReader()
        $bytes = [Collections.Generic.List[byte]]::new()
        while ($reader.RemainingBytes -gt 0) {
            $value = $reader.ReadByte()
            if ($value -eq 0) { break }
            $bytes.Add($value)
            if ($bytes.Count -gt 4096) { throw 'An import name is not terminated.' }
        }
        [Text.Encoding]::ASCII.GetString($bytes.ToArray())
    }
    if ($headers.CoffHeader.Machine.ToString() -ne 'I386') { $failures.Add('PE machine must be x86/I386.') }
    if ($headers.PEHeader.Magic.ToString() -ne 'PE32') { $failures.Add('PE format must be PE32.') }
    if ($headers.PEHeader.MajorOperatingSystemVersion -gt 4) { $failures.Add('PE requires an OS newer than version 4.') }
    if ($headers.PEHeader.MajorSubsystemVersion -gt 4) { $failures.Add('PE requires a subsystem newer than version 4.') }
    if ($metadata.MetadataVersion -ne 'v2.0.50727') { $failures.Add('CLR metadata must target v2.0.50727.') }
    if (($headers.CorHeader.Flags -band [System.Reflection.PortableExecutable.CorFlags]::Requires32Bit) -eq 0) {
        $failures.Add('CLR flags must require the 32-bit runtime.')
    }

    $peImports = @(
        $descriptors = $pe.GetSectionData($headers.PEHeader.ImportTableDirectory.RelativeVirtualAddress).GetReader()
        while ($descriptors.RemainingBytes -ge 20) {
            $lookup = $descriptors.ReadUInt32()
            $timestamp = $descriptors.ReadUInt32()
            $forwarder = $descriptors.ReadUInt32()
            $nameRva = $descriptors.ReadUInt32()
            $addressTable = $descriptors.ReadUInt32()
            if ($lookup -eq 0 -and $nameRva -eq 0 -and $addressTable -eq 0) { break }
            $module = Read-PeAscii $nameRva
            if ($lookup -eq 0) { $lookup = $addressTable }
            $thunks = $pe.GetSectionData([int]$lookup).GetReader()
            $symbols = @(
                while ($thunks.RemainingBytes -ge 4) {
                    $symbolRva = $thunks.ReadUInt32()
                    if ($symbolRva -eq 0) { break }
                    if (($symbolRva -band 0x80000000L) -ne 0) { '#' + ($symbolRva -band 0xffff) }
                    else { Read-PeAscii ($symbolRva + 2) }
                }
            )
            if ($module.ToLowerInvariant() -ne 'mscoree.dll' -or $symbols.Count -ne 1 -or $symbols[0] -ne '_CorExeMain') {
                $failures.Add("Unexpected executable import: $module ($($symbols -join ', ')).")
            }
            [ordered]@{ Module = $module; Symbols = $symbols }
        }
    )

    $assemblyReferences = @(
        foreach ($handle in $metadata.AssemblyReferences) {
            $reference = $metadata.GetAssemblyReference($handle)
            $name = $metadata.GetString($reference.Name)
            if ($name -notin @('mscorlib','System','System.Drawing','System.Windows.Forms','System.Xml') -or $reference.Version -ne [version]'2.0.0.0') {
                $failures.Add("Unexpected framework dependency: $name $($reference.Version)")
            }
            [ordered]@{ Name = $name; Version = $reference.Version.ToString() }
        }
    )

    $nativeImports = @(
        foreach ($handle in $metadata.MethodDefinitions) {
            $method = $metadata.GetMethodDefinition($handle)
            $import = $method.GetImport()
            if ($import.Module.IsNil) { continue }
            $module = $metadata.GetString($metadata.GetModuleReference($import.Module).Name)
            $name = $metadata.GetString($import.Name)
            $attributes = $import.Attributes.ToString()
            if ($module.ToLowerInvariant() -notin @('kernel32.dll','user32.dll','gdi32.dll','winmm.dll','shell32.dll','advapi32.dll','ole32.dll','comctl32.dll')) {
                $failures.Add("Native module requires review: $module!$name")
            }
            if ($name -match '^(?:SetProcessDpiAwareness|GetDpiFor|AdjustWindowRectExForDpi|UpdateLayeredWindow|SetLayeredWindowAttributes|GetTickCount64|GetWindowLongPtr|SetWindowLongPtr|GetProcessMemoryInfo|GetProcessTimes|SHGetKnownFolderPath|ReplaceFile)') {
                $failures.Add("Post-Windows-98 native API: $module!$name")
            }
            if (($import.Attributes -band [System.Reflection.MethodImportAttributes]::CharSetMask) -eq [System.Reflection.MethodImportAttributes]::CharSetUnicode -or $name -cmatch 'W$') {
                $failures.Add("Unicode-only native entry point requires Win9x review: $module!$name ($attributes)")
            }
            [ordered]@{ Module = $module; EntryPoint = $name; Attributes = $attributes }
        }
    )

    $managedMembers = @(
        foreach ($handle in $metadata.MemberReferences) {
            $member = $metadata.GetMemberReference($handle)
            if ($member.Parent.Kind.ToString() -ne 'TypeReference') { continue }
            $typeHandle = [System.Reflection.Metadata.TypeReferenceHandle]$member.Parent
            $type = $metadata.GetTypeReference($typeHandle)
            $typeName = $metadata.GetString($type.Namespace) + '.' + $metadata.GetString($type.Name)
            $memberName = $typeName + '::' + $metadata.GetString($member.Name)
            if ($memberName -in @(
                'System.IO.File::Replace', 'System.Xml.XmlTextReader::set_DtdProcessing',
                'System.Windows.Forms.Application::SetHighDpiMode',
                'System.Windows.Forms.Form::set_TransparencyKey', 'System.Windows.Forms.Form::set_Opacity',
                'System.Diagnostics.Process::get_TotalProcessorTime',
                'System.Diagnostics.Process::get_WorkingSet64', 'System.Diagnostics.Process::get_PrivateMemorySize64'
            )) { $failures.Add("Managed API unavailable or unsupported on Win98: $memberName") }
            $memberName
        }
    ) | Sort-Object -Unique

    $resources = @(
        foreach ($handle in $metadata.ManifestResources) {
            $metadata.GetString($metadata.GetManifestResource($handle).Name)
        }
    )
    foreach ($id in 101..111) {
        if ("Scmpoo.Assets.$id.bmp" -notin $resources) { $failures.Add("Missing original bitmap $id.bmp.") }
    }
    foreach ($id in 108..110) {
        if ("Scmpoo.Assets.$id.wav" -notin $resources) { $failures.Add("Missing original sound $id.wav.") }
    }

    $report = [ordered]@{
        Executable = $path
        Machine = $headers.CoffHeader.Machine.ToString()
        Format = $headers.PEHeader.Magic.ToString()
        OperatingSystemVersion = "$($headers.PEHeader.MajorOperatingSystemVersion).$($headers.PEHeader.MinorOperatingSystemVersion)"
        SubsystemVersion = "$($headers.PEHeader.MajorSubsystemVersion).$($headers.PEHeader.MinorSubsystemVersion)"
        ClrMetadataVersion = $metadata.MetadataVersion
        CorFlags = $headers.CorHeader.Flags.ToString()
        NativePeImports = $peImports
        AssemblyReferences = $assemblyReferences
        PInvokeImports = $nativeImports
        ManagedMemberReferences = $managedMembers
        EmbeddedResources = $resources
        Passed = $failures.Count -eq 0
        Failures = @($failures)
        ActualWindows98ExecutionTested = $false
        Limitation = 'Static compatibility audit only. The target requires Windows 98 SE with .NET Framework 2.0 RTM; a real system or VM test remains required.'
    }
    if ($ReportPath) { $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8 }
    if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
    Write-Output ("Compatibility audit passed: x86 PE32, OS/subsystem 4.0, CLR2; {0} framework dependencies, {1} native entry points, {2} embedded resources." -f $assemblyReferences.Count, $nativeImports.Count, $resources.Count)
}
finally {
    $pe.Dispose()
    $stream.Dispose()
}
