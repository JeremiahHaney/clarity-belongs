param(
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\ClarityBelongs.Web\ClarityBelongs.Web.csproj"
$publishRoot = Join-Path $repoRoot "artifacts\publish\clarity-belongs"
$settingsPath = Join-Path $repoRoot "deployment\publish-settings.local.ps1"
$exampleSettingsPath = Join-Path $repoRoot "deployment\publish-settings.example.ps1"

if (-not (Test-Path $settingsPath))
{
    Write-Host "Missing local publish settings:" -ForegroundColor Yellow
    Write-Host "  $settingsPath"
    Write-Host ""
    Write-Host "Copy this template and set the Web Deploy password:" -ForegroundColor Yellow
    Write-Host "  $exampleSettingsPath"
    exit 2
}

. $settingsPath

if ([string]::IsNullOrWhiteSpace($DeployServer) -or
    [string]::IsNullOrWhiteSpace($DeployUsername) -or
    [string]::IsNullOrWhiteSpace($DeployPassword) -or
    [string]::IsNullOrWhiteSpace($SiteName) -or
    [string]::IsNullOrWhiteSpace($BaseUrl) -or
    $DeployPassword -eq "CHANGE_ME")
{
    throw "Publish settings are incomplete. Update deployment\publish-settings.local.ps1."
}

$msDeployCandidates = @(
    (Get-Command msdeploy.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "$env:ProgramFiles\IIS\Microsoft Web Deploy V3\msdeploy.exe",
    "${env:ProgramFiles(x86)}\IIS\Microsoft Web Deploy V3\msdeploy.exe"
) | Where-Object { $_ -and (Test-Path $_) }

$msDeploy = $msDeployCandidates | Select-Object -First 1

if (-not $msDeploy)
{
    throw "msdeploy.exe was not found. Install Microsoft Web Deploy 3.x or add msdeploy.exe to PATH."
}

if (Test-Path $publishRoot)
{
    Remove-Item $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null

Write-Host ""
Write-Host "============================================================"
Write-Host "Clarity Belongs" -ForegroundColor Cyan
Write-Host "Project: $projectPath"
Write-Host "Target : $SiteName"
Write-Host "URL    : $BaseUrl"
Write-Host "============================================================"

try
{
    dotnet publish $projectPath -c Release -o $publishRoot --nologo

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $arguments = @(
        "-verb:sync",
        "-source:contentPath=$publishRoot",
        "-dest:contentPath=$SiteName,computerName=$DeployServer,userName=$DeployUsername,password=$DeployPassword,authType=Basic,includeAcls=False",
        "-enableRule:DoNotDeleteRule",
        "-enableRule:AppOffline"
    )

    if ($AllowUntrusted)
    {
        $arguments += "-allowUntrusted"
    }

    & $msDeploy @arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "Web Deploy failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipSmokeTest)
    {
        $statusCode = 0

        for ($attempt = 1; $attempt -le 10; $attempt++)
        {
            try
            {
                $response = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -MaximumRedirection 5 -TimeoutSec 30
                $statusCode = [int]$response.StatusCode

                if ($statusCode -ge 200 -and $statusCode -lt 400)
                {
                    break
                }
            }
            catch
            {
                if ($_.Exception.Response -and $_.Exception.Response.StatusCode)
                {
                    $statusCode = [int]$_.Exception.Response.StatusCode
                }
                else
                {
                    $statusCode = 0
                }
            }

            if ($attempt -lt 10)
            {
                Start-Sleep -Seconds 2
            }
        }

        if ($statusCode -lt 200 -or $statusCode -ge 400)
        {
            throw "Deployment completed, but smoke test returned HTTP $statusCode for $BaseUrl"
        }

        Write-Host "Smoke test: HTTP $statusCode" -ForegroundColor Green
    }

    Write-Host "PASS  Clarity Belongs" -ForegroundColor Green
    exit 0
}
catch
{
    Write-Host "FAIL  Clarity Belongs" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
