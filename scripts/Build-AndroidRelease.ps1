param(
    [uri]$ApiUrl = 'https://equilibrafit-plusplus-api-4lkw.onrender.com',
    [string]$FlutterCommand = 'flutter'
)

$ErrorActionPreference = 'Stop'
if (-not $ApiUrl.IsAbsoluteUri -or $ApiUrl.Scheme -ne 'https' -or
    $ApiUrl.IsLoopback -or $ApiUrl.UserInfo -or
    [Uri]::CheckHostName($ApiUrl.Host) -ne [UriHostNameType]::Dns -or
    $ApiUrl.Query -or $ApiUrl.Fragment -or $ApiUrl.AbsolutePath -ne '/') {
    throw 'API_BASE_URL must be a public HTTPS origin, without credentials, query or fragment.'
}

$mobile = Join-Path (Split-Path $PSScriptRoot -Parent) 'src/mobile/equilibrafit_plusplus_app'
$properties = Join-Path $mobile 'android/key.properties'
if (-not (Test-Path -LiteralPath $properties -PathType Leaf)) {
    foreach ($name in @('ANDROID_KEYSTORE_PATH', 'ANDROID_KEYSTORE_PASSWORD', 'ANDROID_KEY_ALIAS', 'ANDROID_KEY_PASSWORD')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
            throw "Configure ignored android/key.properties or $name locally before generating release. See docs/android-release.md."
        }
    }
    if (-not (Test-Path -LiteralPath $env:ANDROID_KEYSTORE_PATH -PathType Leaf)) {
        throw 'The configured Android release keystore does not exist.'
    }
}
$flutter = (Get-Command $FlutterCommand -ErrorAction Stop).Source
function Invoke-Flutter([string[]]$Arguments) {
    & $flutter @Arguments
    if ($LASTEXITCODE -ne 0) { throw 'Flutter failed; no successful release is being reported.' }
}

Push-Location $mobile
try {
    Invoke-Flutter -Arguments @('pub', 'get')
    Invoke-Flutter -Arguments @('analyze')
    Invoke-Flutter -Arguments @('test')
    $apiDefine = '--dart-define=API_BASE_URL=' + $ApiUrl.AbsoluteUri.TrimEnd('/')
    Invoke-Flutter -Arguments @('build', 'apk', '--release', $apiDefine)
    Invoke-Flutter -Arguments @('build', 'appbundle', '--release', $apiDefine)
    foreach ($relative in @('build/app/outputs/flutter-apk/app-release.apk', 'build/app/outputs/bundle/release/app-release.aab')) {
        $artifact = Join-Path $mobile $relative
        if (-not (Test-Path -LiteralPath $artifact -PathType Leaf)) { throw 'Expected release artifact is missing.' }
        Get-FileHash -LiteralPath $artifact -Algorithm SHA256 | Select-Object Path, Hash
    }
}
finally { Pop-Location }
