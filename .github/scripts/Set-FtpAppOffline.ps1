param(
    [Parameter(Mandatory)]
    [ValidateSet('Enable', 'Disable')]
    [string]$Mode,

    [string]$Server = 'ftp.shrijiitservices.com',
    [int]$Port = 21,
    [uri]$HealthUrl
)

$ErrorActionPreference = 'Stop'
$uri = [uri]::new("ftp://${Server}:$Port/app_offline.htm")
$credential = [System.Net.NetworkCredential]::new($env:FTP_USERNAME, $env:FTP_PASSWORD)
$marker = "<!-- FuelMeter deployment $env:GITHUB_RUN_ID -->"

function New-OfflineRequest([string]$Method) {
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Method = $Method
    $request.Credentials = $credential
    $request.UseBinary = $true
    $request.UsePassive = $true
    $request.KeepAlive = $false
    $request.Timeout = 20000
    $request.ReadWriteTimeout = 20000
    return $request
}

function Get-OfflinePage {
    $request = New-OfflineRequest ([System.Net.WebRequestMethods+Ftp]::DownloadFile)
    try {
        $response = $request.GetResponse()
    } catch [System.Net.WebException] {
        $ftpResponse = $_.Exception.Response -as [System.Net.FtpWebResponse]
        if ($null -eq $ftpResponse) { throw }
        try {
            if ([int]$ftpResponse.StatusCode -eq 550) { return $null }
            throw
        } finally {
            $ftpResponse.Dispose()
        }
    }
    try {
        $reader = [System.IO.StreamReader]::new($response.GetResponseStream())
        try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally {
        $response.Dispose()
    }
}

$existing = Get-OfflinePage
if ($null -ne $existing -and !$existing.StartsWith($marker, [StringComparison]::Ordinal)) {
    if ($Mode -eq 'Enable') { throw 'The site already has a maintenance page from outside this deployment.' }
    Write-Host 'Left the existing maintenance page in place.'
    return
}

if ($Mode -eq 'Enable') {
    if ($null -eq $HealthUrl) { throw 'HealthUrl is required when taking a site offline.' }

    if ($null -eq $existing) {
        $page = @"
$marker
<!doctype html>
<html lang="en"><meta charset="utf-8"><title>FuelMeter maintenance</title>
<h1>FuelMeter is being updated</h1><p>Please try again shortly.</p></html>
"@
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($page)
        $request = New-OfflineRequest ([System.Net.WebRequestMethods+Ftp]::UploadFile)
        $request.ContentLength = $bytes.Length
        $stream = $request.GetRequestStream()
        try { $stream.Write($bytes, 0, $bytes.Length) } finally { $stream.Dispose() }
        $request.GetResponse().Dispose()
    }
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    do {
        try {
            $response = Invoke-WebRequest -Uri $HealthUrl -SkipHttpErrorCheck -TimeoutSec 5 `
                -Headers @{ 'Cache-Control' = 'no-cache' }
            if ($response.StatusCode -eq 503 -and ([string]$response.Content).Contains($marker)) {
                Write-Host 'IIS is serving the maintenance page. Waiting for the app process to exit.'
                Start-Sleep -Seconds 15
                return
            }
        } catch {
            # The site can briefly disconnect while IIS takes it offline.
        }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)

    throw 'IIS did not serve the maintenance page for this deployment. The upload was stopped.'
} elseif ($null -ne $existing) {
    $request = New-OfflineRequest ([System.Net.WebRequestMethods+Ftp]::DeleteFile)
    $request.GetResponse().Dispose()
    Write-Host 'Removed app_offline.htm from the deployment target.'
}
