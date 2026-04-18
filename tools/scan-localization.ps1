<#
.SYNOPSIS
    Scan the repository for hard-coded UI string literals in .razor and .cs files.

.DESCRIPTION
    Heuristic scanner that searches for double-quoted string literals that look like
    UI text (contain spaces or punctuation) and are not using resource accessors
    (e.g. DtoResources, UiResources, ResourceManager, GetString).

.EXITCODE
    0 - no candidate hard-coded UI strings found
    1 - one or more candidate hard-coded UI strings found

.USAGE
    pwsh.exe ./tools/scan-localization.ps1 -Root .
#>

[CmdletBinding()]
param(
    [string]$Root = '.',
    [bool]$WebClientOnly = $true
)

Set-StrictMode -Version Latest

$extensions = @('*.razor','*.cs')
$resourcePatterns = 'DtoResources|UiResources|ResourceManager|GetString\(|nameof\(|ProtectedLocalStorage|\bResource\b'
$stylePatterns = 'var\(--|px|rem|em|#|;|:|border|background|color|gradient|display:|^\s*<'
$technicalLiteralPatterns = '^(\/|_framework\/|@|[A-Za-z0-9_\.\-\/\[\]]+)$|^\@\(|^\@typeof\(|^\@Assets\[|^\(\)\s*=>|^new\s+Icons\.'

$results = [System.Collections.Generic.List[psobject]]::new()

Get-ChildItem -Path $Root -Recurse -Include $extensions -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\.git\\'
} | ForEach-Object {
    $file = $_
    if ($WebClientOnly -and $file.FullName -notmatch '\\OpenMES\.WebClient\\') { return }

    $lines = Get-Content -LiteralPath $file.FullName -ErrorAction SilentlyContinue
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match $resourcePatterns) { continue }
        if ($line -match 'Logger\.Log|LogDebug\(|LogWarning\(|LogError\(') { continue }
        # find double-quoted string literals
        $regex = '"([^"\\]*(?:\\.[^"\\]*)*)"'
        $matches = [regex]::Matches($line, $regex)
        foreach ($m in $matches) {
            $value = $m.Groups[1].Value
            if ([string]::IsNullOrWhiteSpace($value)) { continue }
            if ($value.Length -lt 3) { continue }
            # skip likely CSS/style values or tokens
            if ($value -match $stylePatterns) { continue }
            # skip technical literals (routes, asset paths, Razor expressions, enum tokens)
            if ($value -match $technicalLiteralPatterns) { continue }
            # skip Razor expression bindings
            if ($value.StartsWith('@')) { continue }
            # skip technical viewport metadata
            if ($line -match '<meta\s+name="viewport"' -and $value -match 'width=device-width|initial-scale|user-scalable') { continue }
            # skip interpolation-like fragments and format expressions
            if ($value.Contains('{') -or $value.Contains('}')) { continue }
            # skip simple identifiers (no spaces, only word chars)
            if ($value -match '^[A-Za-z0-9_]+$') { continue }

            # heuristic: candidate UI string
            $results.Add([PSCustomObject]@{
                File = $file.FullName
                Line = $i + 1
                Text = $value
            }) | Out-Null
        }
    }
}

if ($results.Count -gt 0) {
    Write-Host "Found $($results.Count) candidate hard-coded UI strings:`n" -ForegroundColor Yellow
    foreach ($r in $results) {
        Write-Host "$($r.File):$($r.Line) -> \"$($r.Text)\""
    }
    Write-Host "`nNote: this is a heuristic scanner. If a reported string is intentionally hard-coded, consider moving it to resources or whitelist it in the script." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host 'No candidate hard-coded UI strings found.' -ForegroundColor Green
    exit 0
}
