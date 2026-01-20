param(
    [string]$GlobalJsonPath = "global.json",
    [string]$VersionPropsPath = "Version.props",
    [string]$NugetSource = "https://api.nuget.org/v3-flatcontainer"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-LatestStableVersion {
    param([string]$PackageId)
    $lower = $PackageId.ToLowerInvariant()
    $url = "$NugetSource/$lower/index.json"
    $data = Invoke-RestMethod -Uri $url -UseBasicParsing
    $versions = @($data.versions)
    if ($versions.Count -eq 0) {
        throw "No versions found for $PackageId."
    }
    $stable = $versions | Where-Object { $_ -notmatch '-' }
    if ($stable.Count -eq 0) {
        $stable = $versions
    }
    return ($stable | Sort-Object { [version]$_ })[-1]
}

$cache = @{}
function Get-Latest {
    param([string]$PackageId)
    if (-not $cache.ContainsKey($PackageId)) {
        $cache[$PackageId] = Get-LatestStableVersion -PackageId $PackageId
    }
    return $cache[$PackageId]
}

$errors = New-Object System.Collections.Generic.List[string]

if (Test-Path $GlobalJsonPath) {
    $global = Get-Content $GlobalJsonPath -Raw | ConvertFrom-Json
    $sdkSection = $global.'msbuild-sdks'
    if ($sdkSection) {
        foreach ($id in @("ANcpLua.NET.Sdk", "ANcpLua.NET.Sdk.Web", "ANcpLua.NET.Sdk.Test")) {
            $pinned = $sdkSection.$id
            if ($pinned) {
                $latest = Get-Latest -PackageId $id
                if ($pinned -ne $latest) {
                    $errors.Add("global.json pins $id to $pinned but latest is $latest.")
                }
            }
        }
    }
}

$versionPropsPathResolved = $VersionPropsPath
if (-not (Test-Path $versionPropsPathResolved) -and (Test-Path "version.props")) {
    $versionPropsPathResolved = "version.props"
}

if (Test-Path $versionPropsPathResolved) {
    [xml]$xml = Get-Content $versionPropsPathResolved -Raw
    $props = @($xml.Project.PropertyGroup)

    $sdkVersion = ($props | ForEach-Object { $_.ANcpSdkPackageVersion } | Where-Object { $_ -and $_.Trim() -ne "" }) | Select-Object -First 1
    if ($sdkVersion) {
        $latest = Get-Latest -PackageId "ANcpLua.NET.Sdk"
        if ($sdkVersion -ne $latest) {
            $errors.Add("$versionPropsPathResolved has ANcpSdkPackageVersion=$sdkVersion but latest ANcpLua.NET.Sdk is $latest.")
        }
    }

    $analyzersVersion = ($props | ForEach-Object { $_.ANcpLuaAnalyzersVersion } | Where-Object { $_ -and $_.Trim() -ne "" }) | Select-Object -First 1
    if ($analyzersVersion) {
        $latest = Get-Latest -PackageId "ANcpLua.Analyzers"
        if ($analyzersVersion -ne $latest) {
            $errors.Add("$versionPropsPathResolved has ANcpLuaAnalyzersVersion=$analyzersVersion but latest ANcpLua.Analyzers is $latest.")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Error ("Version pin check failed:`n" + ($errors -join "`n"))
    exit 1
}

Write-Host "Version pin check OK."
