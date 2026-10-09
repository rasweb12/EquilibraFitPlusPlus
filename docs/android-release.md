# Android Release - EquilibraFit++

Application ID autorizado: `br.com.equilibrafit.app.plusplus`.
Debug: `br.com.equilibrafit.app.plusplus.dev`; original inalterado.
Versao inicial: `1.0.0+1`. Aumentar build number a cada novo upload Play.

## Chave De Upload Propria

`Certificate/prod-ca-2021.crt` valida TLS do banco: NAO assina APK/AAB.
O release exige um keystore Android independente. Nao usar a chave do
original nem assinatura debug. Keystore e senhas nao vao para Git/chat.

Caso ainda nao exista, execute no seu terminal (keytool solicita a senha
localmente). Escolha um diretorio existente fora do repositorio e mantenha
backup privado da chave e senha. Nao sobrescreva uma chave existente:

```powershell
keytool -genkeypair -v -keystore "$env:USERPROFILE/keys/equilibrafit-plusplus-upload.jks" -storetype JKS -keyalg RSA -keysize 2048 -validity 10000 -alias upload
```

Configure `src/mobile/equilibrafit_plusplus_app/android/key.properties`, ignorado
por Git e Docker, a partir de `key.properties.example`. `storeFile` deve ser
absoluto, com `/` no Windows. As outras propriedades sao storePassword,
keyAlias e keyPassword. Alternativamente configure as quatro variaveis
ANDROID_KEYSTORE_PATH, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS e
ANDROID_KEY_PASSWORD no ambiente do processo de build. Nao preencha
`.env.example` com credenciais. O arquivo `.env` nao e carregado pelo Gradle.

## Build Verificado

Da raiz do novo projeto, com Flutter no PATH:

```powershell
./scripts/Build-AndroidRelease.ps1
# Para Flutter instalado fora do PATH:
./scripts/Build-AndroidRelease.ps1 -FlutterCommand C:/develop/flutter/bin/flutter.bat
```

O script verifica assinatura configurada e URL HTTPS publica; executa
pub get, analyze e test antes de gerar APK e AAB, interrompendo na falha.
Gradle continua exigindo assinatura real, sem fallback para chave debug.
O build nao envia arquivos para Google Play nem habilita Premium no Render.

Artefatos, relativos ao diretorio Flutter:

```text
build/app/outputs/flutter-apk/app-release.apk
build/app/outputs/bundle/release/app-release.aab
```

Conferir assinatura com `apksigner verify --verbose --print-certs` do SDK Android
e package/version com `apkanalyzer manifest application-id`/`version-code`.
Validar hash SHA256 antes de distribuir. Ambos os artefatos sao ignorados pelo Git.
Enviar AAB ao teste interno do NOVO app Play; configurar Play App Signing.
Teste real de assinatura exige package correspondente, produtos ativos e
license testers. O debug com sufixo `.dev` NAO testa produtos do app publicado.

Usuarios comuns no teste interno podem ser cobrados: usar license testers
e pagamento de teste identificado pela Google Play. Nao distribuir a Beta
como pronta para monetizacao antes dos cenarios em `google-play-billing.md`.

Em 2026-10-07, nao havia keystore/key.properties/variaveis de assinatura
configuradas neste ambiente. Release assinado e validacao Play permanecem
pendentes dessas configuracoes, nao de uma senha de administrador.

[Flutter Android deploy](https://docs.flutter.dev/deployment/android),
[testes Play Billing](https://developer.android.com/google/play/billing/test).
