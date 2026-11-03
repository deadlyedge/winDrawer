<#
.SYNOPSIS
    Computes the next winDrawer version.

.DESCRIPTION
    Versions use the yy.M.patch format (for example 26.11.1): the last two digits of the year,
    the month without a leading zero, and a release counter that restarts every month. The
    counter is derived from the existing v<yy>.<M>.<patch> git tags, so consecutive releases in
    the same month produce 26.11.1, 26.11.2, and so on.

    Pass -Version to validate and return an explicit version instead (the release workflow does
    this for manual runs).

.PARAMETER Version
    Optional explicit version, with or without the leading "v" (for example 26.11.2).

.PARAMETER Date
    Date used to build the yy.M prefix. Defaults to the current date; useful for tests.

.EXAMPLE
    ./scripts/Get-NextWinDrawerVersion.ps1
    26.11.1
#>
[CmdletBinding()]
param(
    [string] $Version,
    [datetime] $Date = (Get-Date)
)

$ErrorActionPreference = 'Stop'

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $requested = $Version.Trim()
    if ($requested.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase)) {
        $requested = $requested.Substring(1)
    }

    if ($requested -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version '$Version' is invalid. Use the yy.M.patch format, for example 26.11.1."
    }

    Write-Output $requested
    return
}

$prefix = '{0}.{1}.' -f ($Date.Year % 100), $Date.Month
$pattern = '^v' + [regex]::Escape($prefix) + '(\d+)$'
$nextPatch = 1

$tags = @()
try {
    $gitTags = & git tag --list "v$prefix*" 2>$null
    if ($LASTEXITCODE -eq 0) {
        $tags = @($gitTags)
    }
}
catch {
    Write-Verbose "Git tags are unavailable; starting at patch 1. $($_.Exception.Message)"
}

foreach ($tag in $tags) {
    if ($tag -match $pattern -and [int] $Matches[1] -ge $nextPatch) {
        $nextPatch = [int] $Matches[1] + 1
    }
}

Write-Output ('{0}{1}' -f $prefix, $nextPatch)
