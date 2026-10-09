param(
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\ClarityBelongs.Web\ClarityBelongs.Web.csproj"
$publishRoot = Join-Path $repoRoot "artifacts\publish\clarity-belongs"

# Shared portfolio Web Deploy credential.
# Software Belongs, AutoPilot IT, and Clarity Belongs all use this same
# Windows DPAPI-encrypted PSCredential for the IIS VPS administrator account.
$credentialRoot = Join-Path $env:LOCALAPPDATA "SoftwareBelongs\Publish"
$credentialPath = Join-Path $credentialRoot "webdeploy-credential.xml"

$DeployServer = "https://69.164.255.30:8172/msdeploy.axd"
$AllowUntrusted = $true
$SiteName = "ClarityBelongs"
$BaseUrl = "https://claritybelongs.com/"

function Get-PublishCredential
{
    if (-not (Test-Path $credentialPath))
    {
        throw "Shared secure Web Deploy credential not found. Run C:\Projects\SoftwareBelongs\publish\setup-credential.ps1 first."
    }

    try
    {
        $credential = Import-Clixml -Path $credentialPath

        if ($credential -isnot [System.Management.Automation.PSCredential])
        {
            throw "Stored credential is not a PSCredential."
        }

        return $credential
    }
    catch
    {
        throw "Could not decrypt the shared Web Deploy credential. Run C:\Projects\SoftwareBelongs\publish\setup-credential.ps1 again while signed in as this Windows user. $($_.Exception.Message)"
    }
}

$credential = Get-PublishCredential
$DeployUsername = $credential.UserName
$DeployPassword = $credential.GetNetworkCredential().Password

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

$migrationRoot = Join-Path $repoRoot "database\migrations"
$latestMigration = Get-ChildItem -Path $migrationRoot -Filter "*.sql" -File |
    Sort-Object Name |
    Select-Object -Last 1

if ($null -ne $latestMigration)
{
    Write-Host ""
    Write-Host "DATABASE: apply production SQL through $($latestMigration.Name) before publishing this build." -ForegroundColor Yellow
    Write-Host "Path    : $($latestMigration.FullName)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================================"
Write-Host "Clarity Belongs" -ForegroundColor Cyan
Write-Host "Project: $projectPath"
Write-Host "Target : $SiteName"
Write-Host "URL    : $BaseUrl"
Write-Host "============================================================"

try
{
    Write-Host "Credential: shared DPAPI Web Deploy credential" -ForegroundColor Green
    Write-Host "User      : $DeployUsername"

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

        $mobileApiUrl = ([Uri]::new([Uri]$BaseUrl, "api/mobile/account")).AbsoluteUri
        $mobileStatus = 0

        try
        {
            Invoke-WebRequest -Uri $mobileApiUrl -UseBasicParsing -TimeoutSec 30 | Out-Null
            $mobileStatus = 200
        }
        catch
        {
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode)
            {
                $mobileStatus = [int]$_.Exception.Response.StatusCode
            }
        }

        if ($mobileStatus -ne 401)
        {
            throw "Mobile API smoke test expected HTTP 401 but received HTTP $mobileStatus for $mobileApiUrl"
        }

        Write-Host "Mobile API smoke test: HTTP 401 (protected endpoint available)" -ForegroundColor Green
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
finally
{
    $DeployPassword = $null
    $credential = $null
}
