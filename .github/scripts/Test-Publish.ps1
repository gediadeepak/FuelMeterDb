param(
    [Parameter(Mandatory)][ValidateSet('API', 'Website')][string]$Application,
    [Parameter(Mandatory)][string]$Directory
)

$ErrorActionPreference = 'Stop'
$publishDirectory = (Resolve-Path -LiteralPath $Directory).Path
$assembly = if ($Application -eq 'API') { 'FuelMeter.Api' } else { 'FuelMeter.Web' }

foreach ($file in "$assembly.dll", "$assembly.runtimeconfig.json", 'web.config', 'appsettings.json') {
    if (!(Test-Path -LiteralPath (Join-Path $publishDirectory $file) -PathType Leaf)) {
        throw "The publish directory is missing $file."
    }
}

$developmentSettings = Join-Path $publishDirectory 'appsettings.Development.json'
if (Test-Path -LiteralPath $developmentSettings -PathType Leaf) {
    Remove-Item -LiteralPath $developmentSettings
}

# These startup checks do not need a working database or any production settings.
$testEnvironment = @{
    'ASPNETCORE_ENVIRONMENT' = 'Production'
    'DOTNET_ENVIRONMENT' = 'Production'
    'ASPNETCORE_URLS' = 'http://127.0.0.1:0'
    'Jwt__Key' = 'fuelmeter-ci-smoke-key-at-least-32-bytes'
    'Jwt__Issuer' = 'FuelMeterCI'
    'Jwt__Audience' = 'FuelMeterCI'
    'ConnectionStrings__FuelMeterDb' = 'Server=127.0.0.1,1;Database=FuelMeterSmoke;Integrated Security=true'
    'ApiSettings__BaseUrl' = 'http://127.0.0.1:1/'
}
$originalEnvironment = @{}
$stdout = [IO.Path]::GetTempFileName()
$stderr = [IO.Path]::GetTempFileName()
$server = $null

try {
    foreach ($name in $testEnvironment.Keys) {
        $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $testEnvironment[$name])
    }

    $server = Start-Process -FilePath dotnet -ArgumentList "$assembly.dll" -WorkingDirectory $publishDirectory `
        -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru

    $url = $null
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $output = (Get-Content -LiteralPath $stdout) -join ' '
        if ($output -match 'Now listening on: (http://127\.0\.0\.1:\d+)') {
            $url = $Matches[1]
            break
        }
        if ($server.HasExited) { break }
        Start-Sleep -Milliseconds 250
    }
    if (!$url) { throw "$Application did not start in Production mode." }

    if ($Application -eq 'API') {
        $openApi = Invoke-RestMethod "$url/openapi/v1.json" -TimeoutSec 10
        if (!$openApi.openapi) { throw 'The API response was not an OpenAPI document.' }

        $dashboard = Invoke-WebRequest "$url/api/meterreadings/dashboard?year=2026&month=1" -SkipHttpErrorCheck -TimeoutSec 10
        if ($dashboard.StatusCode -ne 401) { throw 'The dashboard endpoint did not require authentication.' }
    }
    else {
        $homePage = Invoke-WebRequest "$url/" -TimeoutSec 10
        $blazor = Invoke-WebRequest "$url/_framework/blazor.web.js" -TimeoutSec 10
        if ($homePage.Content -notmatch 'FuelMeter' -or $blazor.Headers['Content-Type'] -notmatch 'javascript') {
            throw 'The website did not serve the FuelMeter page and the Blazor script.'
        }
        $association = Invoke-WebRequest "$url/.well-known/assetlinks.json" -MaximumRedirection 0 -TimeoutSec 10
        $statements = $association.Content | ConvertFrom-Json
        if ($association.Headers['Content-Type'] -notmatch 'application/json' -or
            !($statements | Where-Object {
                $_.target.package_name -eq 'com.fuelmeter.app' -and
                'delegate_permission/common.handle_all_urls' -in $_.relation -and
                $_.target.sha256_cert_fingerprints.Count -gt 0
            })) {
            throw 'The website did not serve its Android App Links association.'
        }
    }

    Write-Output "$Application publish passed its smoke test."
}
finally {
    if ($server -and !$server.HasExited) {
        Stop-Process -Id $server.Id
        $server.WaitForExit(3000) | Out-Null
    }
    foreach ($name in $originalEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name])
    }
    Remove-Item -LiteralPath $stdout,$stderr -ErrorAction SilentlyContinue
}
