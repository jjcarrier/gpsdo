function Get-GpsdoDevices()
{
    @(gpsdo --enumerate --json | ConvertFrom-Json)
}

$gpsdoScriptBlock = {
    param($wordToComplete, $commandAst, $cursorPosition)

    $helpData = Invoke-Expression 'gpsdo -?'
    $paramValueAssign = $wordToComplete.Contains('=') -and $wordToComplete.IndexOf('=') -lt $cursorPosition
    if ($wordToComplete.StartsWith("--") -and -not $paramValueAssign) {
        Get-ParsedHelpOption -HelpData $helpData |
            New-ParsedHelpParamCompletionResult -WordToComplete $wordToComplete
    } elseif ($wordToComplete.StartsWith("-") -and -not $paramValueAssign) {
        Get-ParsedHelpFlag -HelpData $helpData |
            New-ParsedHelpParamCompletionResult -WordToComplete $wordToComplete
    } else {

        # If the previous option flag is "--serial" or "--sn" then create completion results
        # for each item returned in Get-GpsdoDevices().
        $previousFlag = $commandAst.CommandElements |
            Where-Object { $_.Extent.EndOffset -lt $cursorPosition } |
            Select-Object -Last 1
        $previousFlagText = $previousFlag.Extent.Text

        if ($previousFlagText -and ($previousFlagText -in @('--serial', '--sn'))) {
            Get-GpsdoDevices | Where-Object { $_.Serial -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new(
                    $_.Serial,
                    $_.Serial,
                    'ParameterValue',
                    $_.Model)
            }
        } elseif ($previousFlagText -eq '--mode1') {
            @('NORMAL', 'LOW_POWER', 'PPS', 'NMEA') |
                Where-Object { $_ -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        } elseif ($previousFlagText -in @('--mode', '--mode2')) {
            @('NORMAL', 'LOW_POWER') |
                Where-Object { $_ -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        } elseif ($previousFlagText -eq '--pll') {
            @('true', 'false') |
                Where-Object { $_ -like "*$wordToComplete*" } | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        } else {
            $resultPrefix = ''
            $values = $helpData |
                Get-ParsedHelpParamValue `
                    -WordToComplete $wordToComplete `
                    -CommandAst $commandAst `
                    -CursorPosition $cursorPosition `
                    -ParamValueAssignment:$paramValueAssign `
                    -ResultPrefix ([ref]$resultPrefix)
            $values | New-ParsedHelpValueCompletionResult -ResultPrefix $resultPrefix
        }
    }
}

Register-ArgumentCompleter -CommandName 'gpsdo' -Native -ScriptBlock $gpsdoScriptBlock
