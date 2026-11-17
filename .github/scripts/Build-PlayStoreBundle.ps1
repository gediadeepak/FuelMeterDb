param(
    [Parameter(Mandatory)] [string] $VersionName,
    [Parameter(Mandatory)] [string] $VersionCode,
    [Parameter(Mandatory)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Test-NativeLibraryAlignment([string] $Bundle) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Bundle)
    try {
        $libraries = @($archive.Entries | Where-Object { $_.FullName -match '/lib/(arm64-v8a|x86_64)/[^/]+\.so$' })
        if (-not ($libraries.FullName -match '/lib/arm64-v8a/')) { throw 'The bundle has no ARM64 libraries.' }

        foreach ($library in $libraries) {
            $reader = [IO.BinaryReader]::new($library.Open())
            try {
                $header = $reader.ReadBytes(64)
                # The supported Android 64-bit ABIs use little-endian ELF64.
                if ($header.Length -ne 64 -or [BitConverter]::ToString($header, 0, 6) -ne '7F-45-4C-46-02-01') {
                    throw "Invalid ELF64 header in $($library.FullName)."
                }
                $offset = [BitConverter]::ToUInt64($header, 32)
                $size = [BitConverter]::ToUInt16($header, 54)
                $count = [BitConverter]::ToUInt16($header, 56)
                if ($offset -lt 64 -or $offset -gt 65536 -or $size -ne 56 -or $count -lt 1) {
                    throw "Invalid ELF64 program header table in $($library.FullName)."
                }
                $null = $reader.ReadBytes([int]$offset - 64)
                for ($i = 0; $i -lt $count; $i++) {
                    $segment = $reader.ReadBytes($size)
                    if ($segment.Length -ne $size) { throw "Truncated ELF64 header in $($library.FullName)." }
                    # PT_LOAD segments must be aligned to at least 0x4000 (16 KB).
                    if ([BitConverter]::ToUInt32($segment, 0) -eq 1 -and
                        [BitConverter]::ToUInt64($segment, 48) -lt 0x4000) {
                        throw "Native library $($library.FullName) is not 16 KB aligned."
                    }
                }
            }
            finally { $reader.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}

if ($VersionName -notmatch '^([0-9]+\.){1,3}[0-9]+$' -or $VersionName.Length -gt 50) {
    throw 'Use a dotted numeric version name, such as 1.0.0.'
}
if ($VersionCode -notmatch '^[1-9][0-9]{0,9}$' -or [long]$VersionCode -gt 2100000000) {
    throw 'Use a version code from 1 through 2100000000.'
}
foreach ($variable in 'ANDROID_KEYSTORE_BASE64', 'ANDROID_KEY_ALIAS', 'ANDROID_KEY_PASSWORD', 'ANDROID_KEYSTORE_PASSWORD') {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($variable))) {
        throw "Missing GitHub Actions signing secret $variable."
    }
}

$project = Join-Path $PSScriptRoot '../../FuelMeter.csproj'
$java = Join-Path $env:JAVA_HOME 'bin/java.exe'
$jarsigner = Join-Path $env:JAVA_HOME 'bin/jarsigner.exe'
$dotnetRoot = Split-Path (Get-Command dotnet -CommandType Application).Source
$androidPacks = Join-Path $dotnetRoot 'packs/Microsoft.Android.Sdk.Windows'
$bundletool = Get-ChildItem $androidPacks -Filter bundletool.jar -File -Recurse |
    Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
    Select-Object -First 1
if (!$bundletool) { throw 'The .NET Android workload did not provide bundletool.jar.' }

$tempRoot = [IO.Path]::TrimEndingDirectorySeparator((Resolve-Path -LiteralPath ([IO.Path]::GetTempPath())).Path)
$work = Join-Path $tempRoot "fuelmeter-play-$([Guid]::NewGuid())"
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $keystore = Join-Path $work 'upload.keystore'
    $keyPassword = Join-Path $work 'key-password.txt'
    $storePassword = Join-Path $work 'store-password.txt'
    $publish = Join-Path $work 'publish'
    [IO.File]::WriteAllBytes($keystore, [Convert]::FromBase64String($env:ANDROID_KEYSTORE_BASE64))
    [IO.File]::WriteAllText($keyPassword, $env:ANDROID_KEY_PASSWORD)
    [IO.File]::WriteAllText($storePassword, $env:ANDROID_KEYSTORE_PASSWORD)

    $publishArgs = @(
        'publish', $project, '-f', 'net10.0-android', '-c', 'Release', '-o', $publish,
        '-p:AndroidPackageFormats=aab', '-p:CompressionEnabled=false', "-p:ApplicationVersion=$VersionCode",
        "-p:ApplicationDisplayVersion=$VersionName", "-p:JavaSdkDirectory=$env:JAVA_HOME",
        '-p:AndroidKeyStore=true', "-p:AndroidSigningKeyStore=$keystore",
        "-p:AndroidSigningKeyAlias=$env:ANDROID_KEY_ALIAS",
        "-p:AndroidSigningKeyPass=file:$keyPassword", "-p:AndroidSigningStorePass=file:$storePassword"
    )
    dotnet @publishArgs

    $bundles = @(Get-ChildItem $publish -Filter '*-Signed.aab' -File)
    if ($bundles.Count -ne 1) { throw 'Expected exactly one signed App Bundle.' }
    $bundle = $bundles[0].FullName

    $signature = & $jarsigner -J-Duser.language=en -verify $bundle
    if (-not ($signature -match '^jar verified\.$')) { throw 'The App Bundle signature could not be verified.' }
    & $java -jar $bundletool.FullName validate "--bundle=$bundle"

    [xml]$manifest = (& $java -jar $bundletool.FullName dump manifest "--bundle=$bundle" '--module=base') -join "`n"
    $android = 'http://schemas.android.com/apk/res/android'
    $root = $manifest.DocumentElement
    $sdk = $root.SelectSingleNode('uses-sdk')
    $app = $root.SelectSingleNode('application')
    if ($root.GetAttribute('package') -ne 'com.fuelmeter.app' -or
        $app.GetAttribute('label', $android) -ne 'FuelMeter' -or
        $root.GetAttribute('versionCode', $android) -ne $VersionCode -or
        $root.GetAttribute('versionName', $android) -ne $VersionName -or
        [int]$sdk.GetAttribute('targetSdkVersion', $android) -lt 36 -or
        $app.GetAttribute('allowBackup', $android) -ne 'false' -or
        $app.GetAttribute('usesCleartextTraffic', $android) -ne 'false' -or
        $app.GetAttribute('debuggable', $android) -eq 'true') {
        throw 'The bundle manifest does not match the FuelMeter Play release configuration.'
    }

    if ($app.GetAttribute('extractNativeLibs', $android) -eq 'false') {
        $config = & $java -jar $bundletool.FullName dump config "--bundle=$bundle"
        if (-not ($config -match 'PAGE_ALIGNMENT_16K')) { throw 'Uncompressed native libraries require 16 KB APK ZIP alignment.' }
    }
    Test-NativeLibraryAlignment $bundle

    $restrictedPermissions = @(
        'android.permission.USE_EXACT_ALARM', 'android.permission.SCHEDULE_EXACT_ALARM',
        'android.permission.READ_EXTERNAL_STORAGE', 'android.permission.WRITE_EXTERNAL_STORAGE',
        'android.permission.READ_MEDIA_IMAGES', 'android.permission.READ_MEDIA_VIDEO',
        'android.permission.MANAGE_EXTERNAL_STORAGE', 'com.google.android.gms.permission.AD_ID'
    )
    foreach ($permission in $root.SelectNodes('uses-permission | uses-permission-sdk-23')) {
        $name = $permission.GetAttribute('name', $android)
        if ($name -in $restrictedPermissions) { throw "Unexpected release permission: $name." }
    }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    Copy-Item -LiteralPath $bundle -Destination (Join-Path $OutputDirectory 'FuelMeter.aab')
    Write-Output "Verified FuelMeter $VersionName (version code $VersionCode), target SDK $($sdk.GetAttribute('targetSdkVersion', $android))."
}
finally {
    $resolvedWork = (Resolve-Path -LiteralPath $work).Path
    if ((Split-Path $resolvedWork) -eq $tempRoot) {
        Remove-Item -LiteralPath $resolvedWork -Recurse -Force
    }
}
