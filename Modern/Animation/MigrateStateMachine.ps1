param([string]$SourcePath = "$PSScriptRoot\..\..\Scmpoo\Scmpoo.c")
$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Resolve-Path $SourcePath))
$names = @{
    word_A15A='NormalActions'; word_A1FA='GravityActions'; word_A29A='SpecialActions'
    word_A2B4='BlinkFrames'; word_A314='HangFrames'; word_A324='CollisionFrames'
    word_A34C='YawnFrames'; word_A362='BaaFrames'; word_A372='SneezeFrames'
    word_A38C='AmazedFrames'; word_A398='EatFrames'; word_A3DE='BurnFrames'
    word_A422='RolloverFrames'; word_A43C='GetUpLeftFrames'; word_A44C='GetUpRightFrames'
    word_A45C='MerryFrames'; word_A494='SplashFrames'; word_A49E='BathExitFrames'
    word_A50C='BlushFrames'; word_A524='RollFrames'; word_A536='SpinFrames'
    word_A2AA='_direction'; word_A2AC='_companionDirection'; word_A7FC='_gravityEnabled'
    word_A7FE='_collideWithWindows'; word_A800='_x'; word_A802='_y'; word_A804='_sprite'
    word_A806='_verticalSpeed'; word_A808='_horizontalSpeed'; word_A80C='_previousY'
    word_A80E='_companionX'; word_A810='_companionY'; word_A812='_companionSprite'
    word_A81C='_landingWindow'; stru_A81E='_landingRect'; word_A826='_frameCounter'
    word_A828='_durationCounter'; word_A82A='_actionVariant'; word_A82C='_movementLocked'
    word_A830='_chimeHour'; word_A832='_chimesRemaining'; word_A838='_clockPollCounter'
    word_A83A='_periodCounter'; word_A83C='_targetX'; word_A83E='_targetY'
    word_A840='_hasBounced'; word_A842='_fallVariant'; word_A844='_collisionHeight'
    word_A846='_collisionSpinCounter'; word_A848='_collisionFrame'; word_A84A='_peerRefreshAge'
    word_A8A0='_state'; word_C0AE='_mainAboveCompanion'; word_CA3C='_preventSpecial'
    word_CA42='GravityOption'; word_CA46='_fadeStep'; word_CA54='_pendingSleepState'
    word_CA56='_retainCompanion'; word_CA5C='_companionBeamHeight'; word_CA72='_beamHeight'
    word_CA76='_sleeping'; word_C0AC='ChimeOption'; word_C0B0='MainWindowHandle'
    scmpoo_subwindow='CompanionWindowHandle'
    sub_4CF8='AdvanceState'; sub_4559='TurnAtBoundary'; sub_4614='CheckRunningBoundary'
    sub_46D2='FinishTimedAction'; sub_496F='CheckSupport'; sub_4B3B='CheckClimbingWindow'
    sub_4C21='CheckSheepCollision'; sub_4C91='FindSheepCollision'; sub_3A36='FindPeerEdge'
    sub_8FD7='React'; sub_2A21='ShowCompanion'; sub_2A96='HideCompanion'
    sub_2ABF='RaiseWindow'; sub_2B01='PlaceWindowBehind'; sub_39D6='IsSheepWindow'
    sub_3DF0='RefreshWindowSnapshot'; sub_3E7C='FindSideEdge'; sub_408C='FindLandingEdge'
    sub_419E='FindSpecificLandingEdge'; sub_428E='StopSound'; sub_42C8='PlaySound'
    sub_46F7='UpdateChime'; sub_4807='PresentMain'; sub_488C='PresentCompanion'
    sub_48F3='IsSupportValid'; sub_491D='GetSupportRect'
    scmpoo_should_trigger_special='ShouldTriggerSpecial'; scmpoo_sprite_on_monitor='SpriteOnMonitor'
    scmpoo_random_window_x='RandomWindowX'; scmpoo_get_walk_bounds='GetWalkBounds'
    scmpoo_get_monitor_rect='GetMonitorRect'; SCREEN_LEFT='SceneLeft'; SCREEN_TOP='SceneTop'
    SCREEN_RIGHT='SceneRight'; SCREEN_BOTTOM='SceneBottom'; SCREEN_WIDTH='SceneWidth'
    SCREEN_HEIGHT='SceneHeight'; SCREEN_CENTER_X='SceneCenterX'; SCREEN_POINT_Y='ScenePointY'
    SCREEN_POINT_X='ScenePointX'; SCMPOO_NO_COLLISION='NoCollision'; SCMPOO_BELOW_FLOOR='BelowFloor'
    rand='NextRandom'; max='Math.Max'; min='Math.Min'; GetActiveWindow='GetForegroundWindow'
    WindowFromPoint='WindowAt'; TRUE='true'; FALSE='false'; NULL='IntPtr.Zero'; SND_LOOP='8'
    RECT='EngineRect'; POINT='Point'; HWND='IntPtr'; BOOL='bool'; loc_4D33='DispatchState'
}
function Rename([string]$text) {
    [regex]::Replace($text, '\b[A-Za-z_]\w*\b', { param($m)
        if ($names.ContainsKey($m.Value)) { $names[$m.Value] } else { $m.Value }
    })
}
function ReadFunction([string]$name) {
    $match = [regex]::Match($source, '(?ms)^(?:void|int|BOOL) '+$name+'\([^;]*?\)\s*\{.*?^\}')
    if (!$match.Success) { throw "Function not found: $name" }
    $match.Value
}
$functionNames = 'sub_4CF8','sub_4559','sub_4614','sub_46D2','sub_496F','sub_4B3B','sub_4C21','sub_4C91','sub_8FD7'
$blocks = New-Object 'Collections.Generic.List[string]'
foreach ($name in $functionNames) {
    $body = ReadFunction $name
    $body = [regex]::Replace($body, '(?s)    if \(word_A84A\+\+ > 100\) \{.*?\n    \}', '')
    $body = [regex]::Replace($body, '(?s)        if \(!scmpoo_random_seeded\) \{.*?scmpoo_random_seeded = TRUE;\s*\}', '')
    $body = [regex]::Replace($body, '(?s)(    case 30:.*?goto loc_4D33;\s*\}\s*)word_A83A = 0;.*?(?=    case 31:)', '$1')
    $body = [regex]::Replace($body, '(?s)(    case 55:\s*break;)\s*if.*?(?=    case 56:)', ('$1' + "`n"))
    $body = [regex]::Replace($body, '(?m)^[ \t]+case (\d+):', '    case $1:')
    $body = $body.Replace('scmpoo_monitor_work_areas[rand() % scmpoo_monitor_count]', 'RandomMonitorRect()')
    $body = $body.Replace('*(HWND *)&var_10', 'hitWindow')
    $body = [regex]::Replace($body, 'stru_A8A2\[[^\]]+\]\.(?:width|height)', '40')
    $body = $body.Replace('(word_A7FC & !(rand() & 1))', '(word_A7FC & ((rand() & 1) == 0 ? 1 : 0))')
    $body = [regex]::Replace($body, 'SetWindowPos\(word_C0B0, word_A81C, 0, 0, 0, 0, SWP_NOSIZE \| SWP_NOMOVE \| SWP_NOACTIVATE\)', 'sub_2B01(word_C0B0, word_A81C)')
    $body = [regex]::Replace($body, '\b([0-9]+)U\b', '$1')
    $body = [regex]::Replace($body, '\b(WORD|UINT)\b', 'int')
    $body = [regex]::Replace($body, '(word_A2B4|word_A314)\[([^\]]+)\]\[([^\]]+)\]', '$1[$2, $3]')
    $body = [regex]::Replace($body, '(sub_491D\([^,]+, )&', '${1}out ')
    $body = [regex]::Replace($body, '(sub_3E7C\(|sub_408C\()&', '${1}out ')
    $body = [regex]::Replace($body, '(scmpoo_get_(?:walk_bounds|monitor_rect)\([^,]+, [^,]+, )&', '${1}out ')
    $body = $body.Replace('scmpoo_random_window_x(&', 'scmpoo_random_window_x(')
    $body = Rename $body
    $body = [regex]::Replace($body, '\.(left|top|right|bottom|x|y)\b', { param($m) '.' + $m.Groups[1].Value.Substring(0,1).ToUpperInvariant() + $m.Groups[1].Value.Substring(1) })
    $body = [regex]::Replace($body, '^(void|int|bool) ', 'private $1 ')
    $body = $body.Replace('(void)', '()')
    $body = [regex]::Replace($body, '(?m)^    (IntPtr|EngineRect|Point) (\w+);', '    $1 $2 = default;')
    if ($name -eq 'sub_4CF8') { $body = $body.Replace('    int var_2;', "    IntPtr hitWindow = IntPtr.Zero;`n    int var_2;") }
    # C# requires explicit fallthrough; preserve the original C control flow.
    $labels = [regex]::Matches($body, '(?m)^    case (\d+):')
    for ($i = $labels.Count - 1; $i -gt 0; $i--) {
        $previous = $body.Substring($labels[$i-1].Index, $labels[$i].Index - $labels[$i-1].Index).TrimEnd()
        if ($previous -notmatch '(?:break;|return(?: [^;]+)?;|goto [^;]+;)$' -and $previous -notmatch '^\s*case 30:') {
            $body = $body.Insert($labels[$i].Index, '        goto case ' + $labels[$i].Groups[1].Value + ";`n")
        }
    }
    $blocks.Add($body)
}
$originalBodies = ($functionNames | ForEach-Object { ReadFunction $_ }) -join "`n"
$fields = New-Object 'Collections.Generic.List[string]'
foreach ($match in [regex]::Matches($source, '(?m)^(int|WORD|UINT) (word_\w+) = ([0-9]+)U?;[^\r\n]*')) {
    $identifier = $match.Groups[2].Value
    if ($originalBodies -notmatch "\b$identifier\b") { continue }
    if ($identifier -in @('word_CA42','word_C0AC','word_A84A')) { continue }
    $field = Rename ('private int ' + $identifier + ' = ' + $match.Groups[3].Value + ';')
    $fields.Add($field)
}
$arrays = New-Object 'Collections.Generic.List[string]'
foreach ($match in [regex]::Matches($source, '(?ms)^WORD (word_\w+)(\[\d+\])(?:\[\d+\])? = \{.*?^\};')) {
    if ($originalBodies -notmatch ('\b'+$match.Groups[1].Value+'\b')) { continue }
    $declaration = [regex]::Replace($match.Value, '^WORD (\w+)\[\d+\]\[\d+\]', 'private static readonly int[,] $1')
    $declaration = [regex]::Replace($declaration, '^WORD (\w+)\[\d+\]', 'private static readonly int[] $1')
    $arrays.Add((Rename $declaration))
}
$members = (($fields + $arrays + $blocks) -join "`n`n")
$members = [regex]::Replace($members, '(?m)^(?=\S| +\S)', '    ')
$contents = @(
    'using System;', 'using System.Drawing;', '', 'namespace Scmpoo.Modern.Animation;', '',
    '// Translated from the reconstructed original C state machine. State numbers',
    '// and frame tables are retained for traceability and animation parity.',
    'public sealed partial class SheepActor', '{',
    $members, '}'
) -join "`n"
# This file is a mechanical migration output; the actor/platform boundary is handwritten.
[IO.File]::WriteAllText("$PSScriptRoot\AnimationMachine.cs", $contents.Replace("`r`n", "`n"), (New-Object Text.UTF8Encoding $false))
Write-Output ('Translated {0} functions and {1} states.' -f $blocks.Count, ([regex]::Matches($blocks[0], '(?m)^    case \d+:').Count))
