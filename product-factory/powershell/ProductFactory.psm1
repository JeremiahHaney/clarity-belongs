Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-ProductDirectory
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop

    if (-not (Test-Path -LiteralPath $resolved.Path -PathType Container))
    {
        throw "Directory not found: $Path"
    }

    return $resolved.Path
}

function Ensure-ProductOutputDirectory
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path))
    {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Start-ProductRun
{
    param(
        [Parameter(Mandatory)]
        [string]$Product,

        [hashtable]$Inputs = @{}
    )

    return [pscustomobject]@{
        product = $Product
        startedUtc = [DateTimeOffset]::UtcNow
        inputs = $Inputs
    }
}

function Complete-ProductRun
{
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$Run,

        [Parameter(Mandatory)]
        [object]$Result
    )

    $completed = [DateTimeOffset]::UtcNow

    return [pscustomobject]@{
        product = $Run.product
        status = 'Succeeded'
        startedUtc = $Run.startedUtc
        completedUtc = $completed
        durationMs = [math]::Round(($completed - $Run.startedUtc).TotalMilliseconds)
        inputs = $Run.inputs
        result = $Result
    }
}

function Save-ProductJson
{
    param(
        [Parameter(Mandatory)]
        [object]$Value,

        [Parameter(Mandatory)]
        [string]$Path
    )

    $parent = Split-Path -Parent $Path

    if ($parent)
    {
        Ensure-ProductOutputDirectory -Path $parent | Out-Null
    }

    $Value |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $Path -Encoding UTF8

    return (Resolve-Path -LiteralPath $Path).Path
}

function Get-ProductFiles
{
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$Recurse,

        [string[]]$ExcludeDirectoryNames = @('.git', 'bin', 'obj', 'node_modules')
    )

    $root = Resolve-ProductDirectory -Path $Path
    $items = Get-ChildItem -LiteralPath $root -File -Recurse:$Recurse

    return @($items | Where-Object {
        $fullName = $_.FullName
        -not ($ExcludeDirectoryNames | Where-Object {
            $separator = [IO.Path]::DirectorySeparatorChar
            $fullName -like "*$separator$_$separator*"
        })
    })
}

Export-ModuleMember -Function @(
    'Resolve-ProductDirectory',
    'Ensure-ProductOutputDirectory',
    'Start-ProductRun',
    'Complete-ProductRun',
    'Save-ProductJson',
    'Get-ProductFiles'
)
