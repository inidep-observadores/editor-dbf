#!/usr/bin/env pwsh
# Gestor de Versionamiento Semantico para OBSArrastre2026

param(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$Command,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$OtherArgs
)

function Show-Help {
    Write-Host "Ayuda: Versionamiento Semantico" -ForegroundColor Cyan
    Write-Host "`nComandos disponibles:" -ForegroundColor Cyan
    Write-Host "  next                - Determinar siguiente version" -ForegroundColor Gray
    Write-Host "  commits             - Listar commits desde ultima version" -ForegroundColor Gray
    Write-Host "  changelog           - Generar changelog" -ForegroundColor Gray
    Write-Host "  tag                 - Crear tag de version" -ForegroundColor Gray
    Write-Host "  validate            - Validar formato de version" -ForegroundColor Gray
    Write-Host "  help                - Mostrar esta ayuda" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

function Get-LastTag {
    try {
        $tag = git describe --tags --match "v*" --abbrev=0 2>$null
        if ($tag) { return $tag }
    } catch { }
    return "v0.0.0"
}

function Get-CommitsSinceTag {
    param([string]$FromTag)
    $commits = @(git log "$FromTag..HEAD" --pretty=format:"%h|%s" 2>$null | Where-Object { $_ })
    return $commits
}

# Validar version
function Test-ValidVersion {
    param([string]$Version)
    $v = $Version -replace "^v", ""
    # Simple validation: X.Y.Z format
    return $v -match "^[0-9]+\.[0-9]+\.[0-9]+"
}

# ============================================================================
# COMANDO: next
# ============================================================================
if ($Command -eq 'next') {
    Write-Host "Determinando siguiente version..." -ForegroundColor Cyan

    $lastTag = Get-LastTag
    Write-Host "Version actual: $lastTag" -ForegroundColor Gray

    $commits = Get-CommitsSinceTag $lastTag

    if ($commits.Count -eq 0) {
        Write-Host "No hay commits nuevos" -ForegroundColor Yellow
        exit 0
    }

    Write-Host "Commits nuevos: $($commits.Count)" -ForegroundColor Gray

    # Analizar commits
    $hasBreaking = $false
    $hasFeatures = 0
    $hasFixes = 0

    foreach ($commit in $commits) {
        $msg = $commit -split '\|' | Select-Object -Index 1
        if ($msg -like '*feat*') { $hasFeatures++ }
        if ($msg -like '*fix*' -or $msg -like '*perf*') { $hasFixes++ }
        if ($msg -like '*!:*' -or $msg -like '*BREAKING*') { $hasBreaking = $true }
    }

    # Calcular siguiente version
    $v = $lastTag -replace "^v", ""
    $parts = $v -split '\.'
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]($parts[2] -replace '[^0-9]', '')

    if ($hasBreaking) {
        $major++; $minor = 0; $patch = 0
    } elseif ($hasFeatures -gt 0) {
        $minor++; $patch = 0
    } elseif ($hasFixes -gt 0) {
        $patch++
    }

    $nextVersion = "v$major.$minor.$patch"

    Write-Host "`nSiguiente version: " -NoNewline
    Write-Host $nextVersion -ForegroundColor Green -Bold

    Write-Host "`nResumen:" -ForegroundColor Cyan
    if ($hasBreaking) { Write-Host "  WARNING - BREAKING CHANGES" -ForegroundColor Red }
    if ($hasFeatures -gt 0) { Write-Host "  Features: $hasFeatures" -ForegroundColor Green }
    if ($hasFixes -gt 0) { Write-Host "  Fixes: $hasFixes" -ForegroundColor Yellow }

    exit 0
}

# ============================================================================
# COMANDO: commits
# ============================================================================
elseif ($Command -eq 'commits') {
    Write-Host "Commits desde ultima version..." -ForegroundColor Cyan

    $lastTag = Get-LastTag
    $commits = Get-CommitsSinceTag $lastTag

    if ($commits.Count -eq 0) {
        Write-Host "No hay commits nuevos" -ForegroundColor Yellow
        exit 0
    }

    Write-Host "`nTotal: $($commits.Count) commits`n" -ForegroundColor Gray

    foreach ($commit in $commits) {
        Write-Host $commit -ForegroundColor Gray
    }

    exit 0
}

# ============================================================================
# COMANDO: changelog
# ============================================================================
elseif ($Command -eq 'changelog') {
    Write-Host "Generando changelog..." -ForegroundColor Cyan

    $lastTag = Get-LastTag
    $commits = Get-CommitsSinceTag $lastTag

    if ($commits.Count -eq 0) {
        Write-Host "No hay commits nuevos" -ForegroundColor Yellow
        exit 0
    }

    # Agrupar commits
    $added = @()
    $fixed = @()
    $breaking = @()

    foreach ($commit in $commits) {
        $msg = $commit -split '\|' | Select-Object -Index 1

        if ($msg -like '*!:*' -or $msg -like '*BREAKING*') {
            $breaking += $msg
        } elseif ($msg -like '*feat*') {
            $added += $msg
        } elseif ($msg -like '*fix*' -or $msg -like '*perf*') {
            $fixed += $msg
        }
    }

    # Calcular siguiente version
    $v = $lastTag -replace "^v", ""
    $parts = $v -split '\.'
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]($parts[2] -replace '[^0-9]', '')

    if ($breaking.Count -gt 0) { $major++; $minor = 0; $patch = 0 }
    elseif ($added.Count -gt 0) { $minor++; $patch = 0 }
    elseif ($fixed.Count -gt 0) { $patch++ }

    $nextVersion = "v$major.$minor.$patch"
    $date = Get-Date -Format "yyyy-MM-dd"

    Write-Host "`n## [$nextVersion] - $date`n" -ForegroundColor Cyan

    if ($breaking.Count -gt 0) {
        Write-Host "### WARNING - BREAKING CHANGES" -ForegroundColor Red
        foreach ($c in $breaking) { Write-Host "- $c" }
        Write-Host ""
    }

    if ($added.Count -gt 0) {
        Write-Host "### Added" -ForegroundColor Green
        foreach ($c in $added) { Write-Host "- $c" }
        Write-Host ""
    }

    if ($fixed.Count -gt 0) {
        Write-Host "### Fixed" -ForegroundColor Yellow
        foreach ($c in $fixed) { Write-Host "- $c" }
        Write-Host ""
    }

    exit 0
}

# ============================================================================
# COMANDO: tag
# ============================================================================
elseif ($Command -eq 'tag') {
    if ($OtherArgs.Count -eq 0) {
        Write-Host "ERROR: Especifica version. Uso: semver.ps1 tag v1.2.3" -ForegroundColor Red
        exit 1
    }

    $version = $OtherArgs[0]
    $message = if ($OtherArgs.Count -gt 1) { $OtherArgs[1] } else { "Release: $version" }

    if (-not (Test-ValidVersion $version)) {
        Write-Host "ERROR: Version invalida: $version" -ForegroundColor Red
        exit 1
    }

    Write-Host "Creando tag: $version" -ForegroundColor Cyan

    try {
        git tag -a $version -m $message
        Write-Host "Tag creado exitosamente" -ForegroundColor Green
        Write-Host "Proximo paso: git push origin --tags" -ForegroundColor Gray
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
        exit 1
    }

    exit 0
}

# ============================================================================
# COMANDO: validate
# ============================================================================
elseif ($Command -eq 'validate') {
    if ($OtherArgs.Count -eq 0) {
        Write-Host "ERROR: Especifica version. Uso: semver.ps1 validate v1.2.3" -ForegroundColor Red
        exit 1
    }

    $version = $OtherArgs[0]

    if (Test-ValidVersion $version) {
        Write-Host "OK: '$version' es valida" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "ERROR: '$version' NO es valida" -ForegroundColor Red
        Write-Host "Formato esperado: vMAJOR.MINOR.PATCH" -ForegroundColor Gray
        exit 1
    }
}

# ============================================================================
# COMANDO: help
# ============================================================================
elseif ($Command -eq 'help' -or $Command -eq '--help' -or $Command -eq '-h') {
    Show-Help
}

# ============================================================================
# Comando desconocido
# ============================================================================
else {
    Write-Host "ERROR: Comando desconocido: $Command" -ForegroundColor Red
    Show-Help
}
