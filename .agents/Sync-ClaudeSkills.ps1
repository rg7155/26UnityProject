[CmdletBinding()]
param()

$projectRoot = Split-Path $PSScriptRoot -Parent
$sourceRoot = Join-Path $projectRoot '.agents\skills'
$claudeRoot = Join-Path $projectRoot '.claude\skills'

Get-ChildItem -LiteralPath $sourceRoot -Directory | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $claudeRoot -Recurse -Force
}

Write-Host 'Claude skill copies are synchronized from .agents/skills.'
