param([int]$ApiPort = 52158, [int]$AdminPort = 52080, [int]$AiPort = 58001)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$run = Join-Path $root ('artifacts/smoke-' + [Guid]::NewGuid().ToString('N'))
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
$saved = @{}
function Set-TestEnvironment([string]$Name, [string]$Value) {
    if (!$saved.ContainsKey($Name)) { $saved[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process') }
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}
function Wait-Healthy([string]$Url) {
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        try {
            $response = Invoke-WebRequest $Url -TimeoutSec 3
            if ($response.StatusCode -eq 200) { Write-Output "PASS $Url"; return }
        } catch { Start-Sleep -Seconds 1 }
    }
    throw "Service did not become healthy: $Url. Logs: $run"
}
try {
    foreach ($port in @($ApiPort, $AdminPort, $AiPort)) {
        $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $port)
        try { $listener.Start() } finally { $listener.Stop() }
    }
    New-Item -ItemType Directory -Path $run | Out-Null
    $api = Join-Path $root 'src/backend/EquilibraFitPlusPlus.Api'
    $admin = Join-Path $root 'src/admin/EquilibraFitPlusPlus.Admin'
    $ai = Join-Path $root 'src/ai/equilibrafit_plusplus_ai'
    $python = Join-Path $ai '.venv/Scripts/python.exe'
    foreach ($file in @($python, "$api/bin/Release/net10.0/EquilibraFitPlusPlus.Api.dll", "$admin/bin/Release/net10.0/EquilibraFitPlusPlus.Admin.dll")) {
        if (!(Test-Path -LiteralPath $file)) { throw "Missing prerequisite: $file" }
    }
    $key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    Set-TestEnvironment ASPNETCORE_ENVIRONMENT Development
    Set-TestEnvironment ASPNETCORE_FORWARDEDHEADERS_ENABLED false
    Set-TestEnvironment DATABASE_PROVIDER Sqlite
    Set-TestEnvironment SUPABASE_DB_CONNECTION_STRING ''
    Set-TestEnvironment ConnectionStrings__Default ("Data Source=" + (Join-Path $run 'health.db'))
    Set-TestEnvironment SUPABASE_URL https://supabase.example.test
    Set-TestEnvironment SUPABASE_PUBLISHABLE_KEY public-test-placeholder
    Set-TestEnvironment ConnectionStrings__Redis ''
    Set-TestEnvironment AdminBootstrap__Enabled false
    Set-TestEnvironment GooglePlay__Enabled false
    Set-TestEnvironment AiCoach__BaseUrl "http://127.0.0.1:$AiPort"
    Set-TestEnvironment AiCoach__ApiKey $key
    Set-TestEnvironment AdminApi__BaseUrl "http://127.0.0.1:$ApiPort"
    Set-TestEnvironment EQUILIBRAFIT_AI_ENVIRONMENT Production
    Set-TestEnvironment EQUILIBRAFIT_AI_API_KEY $key
    Set-TestEnvironment EQUILIBRAFIT_AI_OPENAI_API_KEY ''
    Set-TestEnvironment EQUILIBRAFIT_AI_ENABLE_DOCS false
    $processes.Add((Start-Process -FilePath $python -ArgumentList @('-m', 'uvicorn', 'app.main:app', '--host', '127.0.0.1', '--port', "$AiPort", '--no-access-log') -WorkingDirectory $ai -WindowStyle Hidden -PassThru -RedirectStandardOutput "$run/ai.out.log" -RedirectStandardError "$run/ai.err.log"))
    $processes.Add((Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('bin/Release/net10.0/EquilibraFitPlusPlus.Api.dll', '--urls', "http://127.0.0.1:$ApiPort") -WorkingDirectory $api -WindowStyle Hidden -PassThru -RedirectStandardOutput "$run/api.out.log" -RedirectStandardError "$run/api.err.log"))
    $processes.Add((Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('bin/Release/net10.0/EquilibraFitPlusPlus.Admin.dll', '--urls', "http://127.0.0.1:$AdminPort") -WorkingDirectory $admin -WindowStyle Hidden -PassThru -RedirectStandardOutput "$run/admin.out.log" -RedirectStandardError "$run/admin.err.log"))
    Wait-Healthy "http://127.0.0.1:$AiPort/health"
    Wait-Healthy "http://127.0.0.1:$ApiPort/health"
    Wait-Healthy "http://127.0.0.1:$ApiPort/health/ready"
    Wait-Healthy "http://127.0.0.1:$AdminPort/health"
    $swagger = Invoke-RestMethod "http://127.0.0.1:$ApiPort/swagger/v1/swagger.json"
    if ($swagger.info.title -ne 'EquilibraFit++ API') { throw 'Swagger branding failed' }
    $login = Invoke-WebRequest "http://127.0.0.1:$AdminPort/login"
    if ($login.Content -notmatch 'EquilibraFit') { throw 'Admin login failed' }
    $body = @{ mensagem = 'Como ajustar meu jantar?' } | ConvertTo-Json
    $denied = Invoke-WebRequest "http://127.0.0.1:$AiPort/api/v1/coach/chat" -Method Post -Body $body -ContentType application/json -SkipHttpErrorCheck
    if ($denied.StatusCode -ne 401) { throw 'AI internal authorization failed' }
    $fallback = Invoke-RestMethod "http://127.0.0.1:$AiPort/api/v1/coach/chat" -Method Post -Body $body -ContentType application/json -Headers @{ 'X-API-Key' = $key }
    if (!$fallback.fallback_used) { throw 'AI fallback failed' }
    Write-Output 'PASS Swagger, Admin login, AI unauthorized and authenticated fallback'
    Write-Output 'This smoke uses disposable SQLite and fake public Supabase configuration; it does not validate real Auth/PostgreSQL.'
} finally {
    foreach ($process in $processes) {
        if (!$process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
        $process.Dispose()
    }
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process') }
}
