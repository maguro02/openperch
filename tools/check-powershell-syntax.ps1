<#
.SYNOPSIS
Fails when any PowerShell script under tools/ has a syntax error.
#>
$ErrorActionPreference = 'Stop'
$failed = $false
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' | ForEach-Object {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors) | Out-Null
    foreach ($e in $errors) {
        Write-Host ('{0}:{1}: {2}' -f $_.Name, $e.Extent.StartLineNumber, $e.Message)
        $failed = $true
    }
}
if ($failed) { exit 1 }
Write-Host 'PowerShell scripts parse.'
