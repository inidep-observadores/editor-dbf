#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Funciones auxiliares para gestión de versionamiento semántico
#>

# ============================================================================
# FUNCIONES: Obtener versiones y tags
# ============================================================================

function Get-CurrentVersion {
    <#
    .SYNOPSIS
        Obtiene la versión actual del repositorio
    #>
    $tag = Get-LastTag
    if ($tag -and $tag -ne "v0.0.0") {
        return $tag
    }
    return "v0.0.0"
}

function Get-LastTag {
    <#
    .SYNOPSIS
        Obtiene el último tag de versión (formato v*)
    #>
    try {
        $tag = git describe --tags --match "v*" --abbrev=0 2>$null
        if ($tag) {
            return $tag
        }
    } catch {
        # No hay tags aún
    }
    return "v0.0.0"
}

function Get-CommitsSinceTag {
    <#
    .SYNOPSIS
        Obtiene commits desde un tag específico (o desde el inicio si no existe)
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$FromTag
    )

    try {
        $range = "$FromTag..HEAD"
        $commits = git log $range --pretty=format:"%H|%h|%s|%b" --no-decorate 2>$null

        if (-not $commits) {
            return @()
        }

        $result = @()

        # Procesar commits
        if ($commits -is [string]) {
            $commits = @($commits -split "`n")
        }

        foreach ($line in $commits) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }

            $parts = $line -split "\|"
            $hash = $parts[0]
            $shortHash = $parts[1]
            $subject = $parts[2]
            $body = if ($parts.Count -gt 3) { $parts[3] } else { "" }

            $commit = Parse-CommitMessage $subject $body
            $commit.Hash = $hash
            $commit.ShortHash = $shortHash

            $result += $commit
        }

        return $result
    } catch {
        Write-Host "⚠️  Error obteniendo commits: $_" -ForegroundColor Yellow
        return @()
    }
}

# ============================================================================
# FUNCIONES: Analizar commits
# ============================================================================

function Parse-CommitMessage {
    <#
    .SYNOPSIS
        Parsea un mensaje de commit en formato Conventional Commits
    #>
    param(
        [string]$Subject,
        [string]$Body
    )

    $commit = @{
        Subject = $Subject
        Body = $Body
        Type = ""
        Scope = ""
        Description = ""
        IsBreakingChange = $false
        Hash = ""
        ShortHash = ""
    }

    # Detectar breaking change
    if ($Subject -like "*!:*" -or $Body -like "*BREAKING CHANGE:*") {
        $commit.IsBreakingChange = $true
    }

    # Parsear tipo(scope): descripción
    $pattern = "^([a-z]+)(\([^)]+\))?!?:\s*(.+)$"
    if ($Subject -match $pattern) {
        $commit.Type = $matches[1]
        $commit.Scope = if ($matches[2]) { $matches[2] -replace "[()]", "" } else { "" }
        $commit.Description = $matches[3]

        # Detectar breaking change por tipo
        if ($Subject -match "^[a-z]+(\([^)]+\))?!:") {
            $commit.IsBreakingChange = $true
        }
    } else {
        # Si no cumple el patrón, asumimos que es inválido
        $commit.Type = "unknown"
        $commit.Description = $Subject
    }

    # Parsear breaking change del body
    if ($Body -match "BREAKING CHANGE:\s*(.+)") {
        $commit.IsBreakingChange = $true
    }

    return $commit
}

function Analyze-Commits {
    <#
    .SYNOPSIS
        Analiza una lista de commits para determinar tipo de versioning needed
    #>
    param(
        [Parameter(Mandatory = $true)]
        [array]$Commits
    )

    $analysis = @{
        HasBreakingChanges = $false
        HasFeatures = $false
        HasFixes = $false
        Features = @()
        Fixes = @()
        Others = @()
        BumpType = "patch"  # patch, minor, major
    }

    foreach ($commit in $Commits) {
        if ($commit.IsBreakingChange) {
            $analysis.HasBreakingChanges = $true
            $analysis.BumpType = "major"
        } elseif ($commit.Type -eq "feat") {
            $analysis.HasFeatures = $true
            if ($analysis.BumpType -ne "major") {
                $analysis.BumpType = "minor"
            }
            $analysis.Features += $commit
        } elseif ($commit.Type -eq "fix" -or $commit.Type -eq "perf") {
            $analysis.HasFixes = $true
            $analysis.Fixes += $commit
        } else {
            $analysis.Others += $commit
        }
    }

    return $analysis
}

function Group-CommitsByType {
    <#
    .SYNOPSIS
        Agrupa commits por tipo
    #>
    param(
        [array]$Commits
    )

    $grouped = @{}

    $types = @('feat', 'fix', 'perf', 'docs', 'refactor', 'test', 'build', 'ci', 'chore', 'unknown')

    foreach ($type in $types) {
        $grouped[$type] = @($Commits | Where-Object { $_.Type -eq $type })
    }

    return $grouped
}

# ============================================================================
# FUNCIONES: Versionamiento
# ============================================================================

function Parse-Version {
    <#
    .SYNOPSIS
        Parsea una versión semántica en componentes
    #>
    param(
        [string]$Version
    )

    # Remover 'v' si existe
    $v = $Version -replace "^v", ""

    # Patrón: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
    $pattern = "^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9.-]+))?(?:\+([a-zA-Z0-9.-]+))?$"

    if ($v -match $pattern) {
        return @{
            Major = [int]$matches[1]
            Minor = [int]$matches[2]
            Patch = [int]$matches[3]
            PreRelease = if ($matches[4]) { $matches[4] } else { "" }
            Build = if ($matches[5]) { $matches[5] } else { "" }
            IsValid = $true
        }
    }

    return @{
        IsValid = $false
    }
}

function Validate-Version {
    <#
    .SYNOPSIS
        Valida que una versión cumple formato SemVer
    #>
    param(
        [string]$Version
    )

    $parsed = Parse-Version $Version
    return $parsed.IsValid
}

function Calculate-NextVersion {
    <#
    .SYNOPSIS
        Calcula la siguiente versión basada en análisis de commits
    #>
    param(
        [string]$CurrentVersion,
        [hashtable]$Analysis,
        [switch]$PreRelease
    )

    $parsed = Parse-Version $CurrentVersion

    $major = $parsed.Major
    $minor = $parsed.Minor
    $patch = $parsed.Patch

    if ($Analysis.BumpType -eq "major") {
        $major++
        $minor = 0
        $patch = 0
    } elseif ($Analysis.BumpType -eq "minor") {
        $minor++
        $patch = 0
    } else {
        $patch++
    }

    $newVersion = "v$major.$minor.$patch"

    if ($PreRelease) {
        # Agregar identificador de pre-release
        $timestamp = Get-Date -Format "yyyyMMdd.HHmmss"
        $newVersion += "-rc.$timestamp"
    }

    return $newVersion
}

# ============================================================================
# FUNCIONES: Validación de commits
# ============================================================================

function Test-CommitMessage {
    <#
    .SYNOPSIS
        Valida que un mensaje de commit sigue Conventional Commits
    #>
    param(
        [string]$Message
    )

    # Patrón: tipo[(scope)][!]: descripción
    $pattern = "^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([^)]*\))?!?:\s*.+"
    return $Message -match $pattern
}

# ============================================================================
# FUNCIONES: Generación de changelog
# ============================================================================

function Generate-Changelog {
    <#
    .SYNOPSIS
        Genera texto de changelog a partir del análisis de commits
    #>
    param(
        [string]$Version,
        [hashtable]$Analysis,
        [string]$BaseVersion
    )

    $date = Get-Date -Format "yyyy-MM-dd"
    $changelog = "## [$Version] - $date`n`n"

    # Breaking changes
    if ($Analysis.HasBreakingChanges) {
        $changelog += "### ⚠️ BREAKING CHANGES`n"
        foreach ($commit in $Analysis.Features + $Analysis.Fixes) {
            if ($commit.IsBreakingChange) {
                $changelog += "- $($commit.Description)`n"
            }
        }
        $changelog += "`n"
    }

    # Added (features)
    if ($Analysis.Features.Count -gt 0) {
        $changelog += "### Added`n"
        foreach ($commit in $Analysis.Features) {
            $scope = if ($commit.Scope) { "**$($commit.Scope):** " } else { "" }
            $changelog += "- $scope$($commit.Description)`n"
        }
        $changelog += "`n"
    }

    # Fixed (fixes)
    if ($Analysis.Fixes.Count -gt 0) {
        $changelog += "### Fixed`n"
        foreach ($commit in $Analysis.Fixes) {
            $scope = if ($commit.Scope) { "**$($commit.Scope):** " } else { "" }
            $changelog += "- $scope$($commit.Description)`n"
        }
        $changelog += "`n"
    }

    # Otros cambios relevantes
    $others = @($Analysis.Others | Where-Object { $_.Type -in @('docs', 'perf') })
    if ($others.Count -gt 0) {
        foreach ($commit in $others) {
            if ($commit.Type -eq 'perf') {
                if (-not ($changelog -like "*### Performance*")) {
                    $changelog += "### Performance`n"
                }
                $scope = if ($commit.Scope) { "**$($commit.Scope):** " } else { "" }
                $changelog += "- $scope$($commit.Description)`n"
            }
        }
    }

    return $changelog.TrimEnd()
}

# ============================================================================
# FUNCIONES: Utilidades
# ============================================================================

function Get-CommitTypeEmoji {
    <#
    .SYNOPSIS
        Retorna emoji para tipo de commit
    #>
    param(
        [string]$Type
    )

    $emojis = @{
        'feat' = '✨'
        'fix' = '🐛'
        'docs' = '📚'
        'style' = '💅'
        'refactor' = '♻️'
        'perf' = '⚡'
        'test' = '✅'
        'build' = '🔨'
        'ci' = '🔄'
        'chore' = '🔧'
        'revert' = '⏮️'
    }

    return $emojis[$Type] -or '📦'
}

Export-ModuleMember -Function @(
    'Get-CurrentVersion',
    'Get-LastTag',
    'Get-CommitsSinceTag',
    'Parse-CommitMessage',
    'Analyze-Commits',
    'Group-CommitsByType',
    'Parse-Version',
    'Validate-Version',
    'Calculate-NextVersion',
    'Test-CommitMessage',
    'Generate-Changelog',
    'Get-CommitTypeEmoji'
)
