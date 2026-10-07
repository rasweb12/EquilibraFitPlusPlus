param([string]$Source = 'D:\Curso\APP\EQUILIBRAFIT')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot = [IO.Path]::GetFullPath($Source)
if ($root -eq $sourceRoot) { throw 'Source must be independent from destination.' }
$files = @(git -C $root ls-files --cached --others --exclude-standard | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0) { throw 'Unable to list prospective repository files.' }
$rows = foreach ($relative in $files) {
    if ($relative -eq 'docs/port-inventory.csv') { continue }
    $originRelative = $relative.Replace('EquilibraFitPlusPlus', 'EquilibraFit').Replace('equilibrafit_plusplus_app', 'equilibrafit_app').Replace('equilibrafit_plusplus_ai', 'equilibrafit_ai')
    $origin = Join-Path $sourceRoot $originRelative
    $destination = Join-Path $root $relative
    $isCode = $relative.StartsWith('src/') -or $relative.StartsWith('tests/')
    $action = 'Criado'
    if ($isCode -and (Test-Path -LiteralPath $origin -PathType Leaf)) {
        $action = if ((Get-FileHash -LiteralPath $origin).Hash -eq (Get-FileHash -LiteralPath $destination).Hash) { 'Copiado' } else { 'Adaptado' }
    }
    [pscustomobject]@{ Acao = $action; Arquivo = $relative; Referencia = if ($action -eq 'Criado') { '' } else { $originRelative } }
}
$target = Join-Path $root 'docs/port-inventory.csv'
$rows | Export-Csv -LiteralPath $target -NoTypeInformation -Encoding utf8
$rows | Group-Object Acao | Select-Object Name, Count
Write-Output 'Generated inventory from current files and source comparison; no source files modified.'
