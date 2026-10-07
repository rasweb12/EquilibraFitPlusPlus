# Configuracao Supabase E Resend

## Estado Em 2026-10-07

- Organizacao rasweb; projeto EquilibraFit++ ativo em sa-east-1 (Sao Paulo).
- Referencia confirmada: knaubynyytqnyyrdrkfi. Vero permanece intocado.
- URL: https://knaubynyytqnyyrdrkfi.supabase.co; PostgreSQL 17.
- Sete migrations EF aplicadas no banco vazio pelo MCP e reaplicadas com
  autorizacao explicita, guard do historico e transacao. Dados preservados.
- 39 tabelas app com RLS, 22 policies SELECT, 34 foreign keys, 34 checks,
  tres indices parciais e tenant inicial. Zero usuarios reais importados.
- Teste SQL remoto aprovou isolamento A/B, CRUD cliente negado, billing,
  sessoes/auditoria/historico privados, soft delete, operation ID, FK e checks.
  Todos os dados de teste foram revertidos.
- JWKS publico confirma assinatura ES256, compativel com a API.
- User Secrets locais da API recebem URL publica, provider PostgreSQL e a
  publishable key fornecida pelo usuario, sem gravar a chave no repositorio.
- GET Auth settings com essa chave aprovado: email habilitado, cadastro
  permitido e confirmacao de email obrigatoria. Essa consulta nao cria usuarios.
- Conta admin@equilibrafit.local criada/confirmada manualmente pelo operador.
  Primeiro login provisionou o perfil; bootstrap por UUID promoveu somente essa
  conta e registrou uma auditoria. Bootstrap desabilitado apos a verificacao.
- Auth real pela API: login 200, refresh 200, dashboard Admin 200, logout 204 e
  sessao revogada rejeitada com 401. Interface Admin disponivel na porta 5029;
  login no navegador e fluxos email/mobile completos ainda nao foram automatizados.
- Campos Session pooler fornecidos pelo usuario: aws-0-sa-east-1.pooler.supabase.com,
  porta 5432, usuario postgres.knaubynyytqnyyrdrkfi. Teste TCP aprovado.
- ConnectionStrings:Default preparada nos User Secrets; o usuario substituiu
  o placeholder localmente. CA oficial configurada com SSL Mode=VerifyFull e
  Root Certificate. /health/ready retorna 200 no host e no emulador. Npgsql
  confirmou as sete migrations aplicadas, sem erro de leitura nem pendencias.
- Resend: equilibrafitplus.com.br cadastrado, envio habilitado, verificacao
  failed na consulta mais recente. DKIM e ambos os CNAMEs tambem failed.
  Google DNS e Cloudflare retornaram NXDOMAIN; RDAP Registro.br retornou 404.
- Open/click tracking ja desabilitados; recebimento de email nao foi habilitado.
- Senha administrativa temporaria fornecida pelo usuario foi usada apenas em
  memoria via prompt protegido no smoke Auth; nao foi exibida em saida ou gravada
  em arquivos/Git. O diagnostico Npgsql usa os User Secrets locais em memoria.
  Nenhuma secret key Supabase/Resend foi utilizada; publishable key validada.

A integracao MCP atual permite schema/SQL/advisors Supabase e dominio Resend,
mas nao expoe alteracao de SMTP, templates ou URL configuration do Supabase,
nem o painel DNS. Esses campos precisam ser salvos pelo usuario no painel;
nao foram declarados configurados. Nenhum email de teste foi enviado.

Security advisor: somente avisos INFO de RLS sem policies nas tabelas privadas,
com default-deny intencional. Nao crie policies permissivas para silenciar avisos.
[Explicacao do advisor](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy).
Performance advisor apontou indices ainda nao utilizados no banco novo: manter,
nao remover indices de integridade/consultas sem dados reais de uso.
[Advisor de indices](https://supabase.com/docs/guides/database/database-linter?lint=0005_unused_index).

As ferramentas Resend conectadas gerenciam dominios/templates, nao um projeto
equivalente ao Supabase. Separe o novo envio por dominio/subdominio e por uma
chave de envio dedicada, que o usuario criara no painel. Nao envie campanhas
nem cadastre contatos para configurar autenticacao.

## Erro Da API No Visual Studio

`Configure SUPABASE_DB_CONNECTION_STRING or ConnectionStrings__Default` significa
que a API nao recebeu a conexao. Nao e uma falha de autenticacao PostgreSQL:
o startup ainda nao tentou conectar. O arquivo .env e usado pelo Docker Compose,
nao e carregado automaticamente pelo Visual Studio ou dotnet run.

No Visual Studio, selecione o projeto EquilibraFitPlusPlus.Api e use
Manage User Secrets / Gerenciar Segredos do Usuario. No arquivo LOCAL fora do
repositorio, configure os campos abaixo com valores do NOVO projeto:

```json
{
  "DATABASE_PROVIDER": "PostgreSQL",
  "SUPABASE_URL": "https://knaubynyytqnyyrdrkfi.supabase.co",
  "SUPABASE_PUBLISHABLE_KEY": "SUA_CHAVE_PUBLICA",
  "ConnectionStrings:Default": "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.knaubynyytqnyyrdrkfi;Password=PREENCHA_SUA_SENHA_AQUI;SSL Mode=VerifyFull;Maximum Pool Size=10"
}
```

Copie host/porta/usuario do painel Connect; nao reutilize a conexao do Vero nem
deduza o hostname do pooler. A checagem local nao encontrou IPv6 global; o host
direto confirmado db.knaubynyytqnyyrdrkfi.supabase.co tem IPv6. Use Connect ->
Session pooler (IPv4), nao Transaction pooler, e copie host/porta/usuario desse
dialogo. Nao foi testada conexao Npgsql local sem credenciais.
Nova consulta DNS confirmou ausencia de IPv4 no host direto. A URI fornecida
`postgresql://postgres:[YOUR-PASSWORD]@db.knaubynyytqnyyrdrkfi.supabase.co:5432/postgres`
e Direct connection e ainda tem placeholder; nao foi salva como conexao valida.
Npgsql usa o formato Host/Port/Database/Username/Password demonstrado acima,
nao a URI PostgreSQL bruta. Os campos Session pooler foram fornecidos e o TCP
respondeu na porta 5432. No Visual Studio, Gerenciar Segredos do Usuario da API:
substituir somente PREENCHA_SUA_SENHA_AQUI pela senha do BANCO desse projeto,
nao pela chave publica, senha da conta Supabase ou key Resend. Nao enviar senha
ou connection string completa pelo chat. Reiniciar a API em Development e
validar /health/ready; TCP e dbcontext info nao comprovam autenticacao ou TLS.
[Connect do projeto correto](https://supabase.com/dashboard/project/knaubynyytqnyyrdrkfi?showConnect=true).
TLS deve validar certificado/hostname. Senhas com ponto-e-virgula/aspas exigem
escape correto da connection string Npgsql, alem do escape JSON.

### CA Supabase Configurada

O certificado publico do Session pooler foi inspecionado sem enviar senha:
hostname correto (*.pooler.supabase.com), RemoteCertificateChainErrors e
UntrustedRoot. Nao e evidencia de senha incorreta. Baixe a CA oficial em
Database -> Settings -> SSL Configuration -> Download Certificate no projeto
EquilibraFit++, e configure o caminho local na conexao dos User Secrets:

```text
SSL Mode=VerifyFull;Root Certificate=D:/CAMINHO_LOCAL/prod-ca-2021.crt
```

O exemplo e apenas o sufixo TLS: preserve host/porta/usuario/senha existentes.
Nao confie em um certificado coletado de uma conexao nao validada. Nao reduza
SSL Mode nem adicione callback aceitando qualquer certificado. O caminho no
Render/container precisara corresponder ao arquivo disponibilizado nesse ambiente.
[CA no painel Supabase](https://supabase.com/docs/guides/platform/ssl-enforcement).
[Root Certificate no Npgsql](https://www.npgsql.org/doc/security.html).

Resolvido em 2026-10-07 com o arquivo fornecido pelo usuario:
`D:/Curso/APP/EquilibraFit++/Certificate/prod-ca-2021.crt`.
CA publica Supabase Root 2021 CA, sem chave privada, validade ate 2031-04-26;
thumbprint A4518A0933AF6949482CCA3014C007C369DF9F6F.
Validacao independente da cadeia e hostname aprovada antes de autenticar.
User Secrets receberam apenas o sufixo TLS, preservando as credenciais existentes.
Nao foi instalada CA global no Windows nem adicionado bypass de certificado.
Certificate/ e configuracao local ignorada no Git; nao foi incluida no APK.
Apos reiniciar apenas a API iniciada pelo agente, /health e /health/ready retornam
200 no host, e /health/ready retornou 200 via 10.0.2.2 no emulator-5554.
EF migrations list abriu o banco real via Npgsql: sete IDs, zero pendencias.
Auth/Admin autenticado foram validados separadamente pelo smoke documentado
abaixo. SMTP e fluxos de email continuam pendentes, nao implicitos no health.

API parada tambem causa erro de conexao no mobile. Para este APK debug:

```powershell
dotnet run --no-build -c Release --project src/backend/EquilibraFitPlusPlus.Api --launch-profile http
```

Em 2026-10-07 foi iniciada uma instancia em background na porta 5158; TLS resolvido
e /health/ready 200. Admin local iniciado em http://localhost:5029/login, conectado
a essa API. HTTP 200 da pagina nao comprova login ou role administrativa.
O teste posterior autenticou a conta pelo endpoint da API e conferiu a role
Administradora e o dashboard protegido; login pela UI nao foi automatizado.
Nao inicie outra API nessa porta simultaneamente; encerre a instancia anterior
antes de executar pelo Visual Studio. Para celular fisico, 10.0.2.2 nao vale:
usar perfil http-lan e API_BASE_URL com IP real da maquina na mesma rede.
Nao houve alteracao de firewall nem exposicao nova de rede nesta verificacao.

O ID de User Secrets ja e independente: equilibrafit-plusplus-api-local.
Use o perfil http ou https, ambos Development. Segredos de usuario nao sao
carregados automaticamente em Production. Nao salve valores reais em
launchSettings.json, appsettings.json, README ou no chat.

Em JSON de User Secrets a chave e `ConnectionStrings:Default`, com dois pontos.
Em variavel de ambiente a chave e `ConnectionStrings__Default`, com dois
underscores. Alternativamente, SUPABASE_DB_CONNECTION_STRING funciona nos dois.
Uma variavel SUPABASE_DB_CONNECTION_STRING nao vazia tem prioridade; verifique
se nao existe um valor antigo no processo/IDE. Reinicie a API apos configurar.

## Cancelamento E Timeout No Cadastro

Em 2026-10-07, os logs remotos Auth do projeto correto registraram POST /signup
com HTTP 504 e error_code request_timeout as 08:39:38 (America/Sao_Paulo), seguido
de um registro /signup HTTP 200 as 08:39:49. Esses registros nao comprovam entrega
do email, identidade da conta ou causa SMTP. O GET /auth/v1/settings respondeu
200 em aproximadamente um segundo durante o diagnostico, com confirmacao ativa.
[Erro request_timeout](https://supabase.com/docs/guides/auth/debugging/error-codes).

O HttpClient Auth antes usava o limite padrao de 100 segundos, enquanto o Flutter
aguarda 20. Agora signup/login/refresh usam limite de 15 segundos, sem retries
automaticos que poderiam repetir cadastro ou rotacao de refresh token.
[Timeout HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout).

Erros publicos da API:
- auth.provider_timeout: HTTP 504 quando o provider expira ou retorna 408/504.
- auth.provider_unavailable: HTTP 503 para rede, 5xx restantes ou JSON invalido.
- auth.registration_failed e auth.invalid_credentials: contratos existentes para
  rejeicoes do provider, separados de falhas temporarias.

Logs do adapter registram somente operacao, codigo e status; nao incluem o body
do provider, senha, tokens, headers apikey ou a excecao de transporte.
O cancelamento do chamador e propagado. Se RequestAborted estiver cancelado,
o middleware nao tenta escrever JSON/500 na conexao fechada; usa 499 quando
os headers ainda nao foram enviados. Outras excecoes mantem o tratamento atual.

TaskCanceledException pode ser interrompida pelo debugger antes de um catch se
Exception Settings estiver configurado para break on thrown. Isso por si so nao
prova falha de startup. Nao desabilite protecoes globais nem aumente indiscriminadamente
timeouts. Para carregar esta correcao no Visual Studio, parar depuracao, recompilar
o backend e iniciar novamente; nao precisa regenerar o APK para uma mudanca da API.

Nenhum signup/email real foi disparado pelo agente neste diagnostico. Um timeout
nao prova que o cadastro nao ocorreu: conferir email/estado da conta antes de
repetir. SMTP/DNS/entrega devem ser validados separadamente com email valido;
nao desabilitar confirmacao nem marcar cadastro como sucesso sem sessao verificada.

Validacao final: restore/build Release aprovados; suite .NET completa com 183
PASS e 2 SKIP remotos. Os testes HTTP usam a configuracao real do HttpClient
e apenas substituem o transporte por um provider simulado, verificando o prazo
de 15 segundos, ausencia de retry, erros 503/504 e ausencia de perfis indevidos.
Revogacao JWT e isolamento permanecem testados. Os hosts de teste compartilham
uma collection xUnit para respeitar o logger bootstrap global do entrypoint.

## Diagnostico Do Cadastro Publicado

Consulta somente leitura em 2026-10-07, apos instalar o APK com a API HTTPS.
Os logs filtrados do emulador registram HTTP 504 em POST /api/v1/auth/cadastrar,
cerca de 16 segundos apos cada chamada. Nos logs Auth do projeto correto,
as 19:40:59, 19:41:26 e 19:41:47 UTC, POST /signup retornou 504 com
request_timeout e context deadline exceeded. Cada registro teve um 200 posterior
com o MESMO request_id; isso nao comprova cadastro ou entrega de email.
Nao foram disparadas novas tentativas de cadastro pelo agente.

Inspecao do banco: um usuario confirmado, zero nao confirmados; nenhum trigger
customizado em auth.users/auth.identities. Nao havia conexoes supabase_auth_admin
visiveis no instante da consulta. Essa amostra nao exclui bloqueios transitorios
ou Auth hooks configurados fora do banco e nao identifica a causa do timeout.

Pendencia independente confirmada: o dominio equilibrafitplus.com.br e seus tres
registros Resend retornaram NXDOMAIN em Google DNS e Cloudflare; consulta RDAP
Registro.br retornou 404. O Resend informa failed para dominio, DKIM e CNAMEs.
Confirmar grafia, registro e delegacao no provedor antes de configurar os
[registros DNS exatos abaixo](#dns-exato-do-resend). Cadastrar o dominio apenas
no Resend nao registra o dominio nem publica esses registros no DNS.
[Diagnostico oficial de verificacao](https://resend.com/docs/knowledge-base/what-if-my-domain-is-not-verifying).

As ferramentas conectadas nao leem ou alteram o SMTP efetivamente salvo no
Supabase. O operador informou os campos publicos: smtp.resend.com, porta 461,
username resend e remetente onboarding@resend.dev. A porta 461 diverge da
[integracao oficial](https://resend.com/docs/send-with-supabase-smtp), que orienta
465. Corrigir SOMENTE a porta para 465 em Authentication -> Email -> SMTP
Settings e salvar. Preservar as demais configuracoes e confirmacao de email;
Password deve continuar sendo a API key Resend preenchida pelo operador.
Nao enviar chaves no chat. A porta incorreta e compativel com o timeout observado;
a correcao ainda precisa ser validada com um cadastro controlado pelo operador.

onboarding@resend.dev e um remetente de testes: so permite envio ao email da
propria conta Resend. Nao usar admin@equilibrafit.local como destinatario de
confirmacao, pois .local nao recebe email publico. Para cadastro publico,
regularizar/verificar o dominio proprio e depois configurar seu remetente.
[Restricao oficial resend.dev](https://resend.com/docs/knowledge-base/403-error-resend-dev-domain).
O dominio proprio failed e uma pendencia separada; ele nao e o remetente
informado e, portanto, nao explica sozinho o timeout atual. Nao trocar para
noreply@equilibrafitplus.com.br antes da verificacao. Nao e necessario outro APK
para alterar o SMTP do Supabase.

Sem alteracoes em Auth/SMTP/DNS, confirmacao de email, timeouts ou aplicativo.
Sem retries, envio de emails, criacao de contas, novo APK, commit ou push.

## Migrations Antes Do Readiness

A factory EF usa variaveis de ambiente, nao User Secrets ou .env. Para nao
registrar a senha no historico do shell, digite a connection string completa
em um prompt oculto. O valor so permanece em texto em memoria no processo:

```powershell
Set-Location 'D:\Curso\APP\EquilibraFit++'
$previousConnection = $env:SUPABASE_DB_CONNECTION_STRING
$connection = Read-Host 'Connection string do NOVO projeto Supabase' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($connection)
try {
    $env:SUPABASE_DB_CONNECTION_STRING = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar dotnet-ef.' }
    dotnet ef database update --project src/backend/EquilibraFitPlusPlus.Infrastructure --startup-project src/backend/EquilibraFitPlusPlus.Api
    if ($LASTEXITCODE -ne 0) { throw 'Falha nas migrations; readiness nao validado.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    $connection.Dispose()
    $env:SUPABASE_DB_CONNECTION_STRING = $previousConnection
    Remove-Variable previousConnection, pointer, connection
}
```

Nao execute em Vero. As sete migrations EF ja estao aplicadas neste projeto.
O bootstrap MCP deixou um registro tecnico Supabase; o historico autoritativo
continua public.__EFMigrationsHistory, com as sete IDs EF. Nao manter migrations
SQL manuais Supabase em paralelo para evoluir o modelo: futuras alteracoes devem
ser migrations EF. Nunca usar EnsureCreated no PostgreSQL.
Nao publique logs completos com credenciais; se houver erro, compartilhe apenas
mensagem sanitizada. Confira /health e /health/ready apos iniciar a API.
O startup .NET direto nao aplica migrations por RUN_DB_MIGRATIONS; essa flag e
interpretada pelo entrypoint Docker. Os testes remotos precisam de conexao
dedicada separada, conforme [Supabase](supabase.md).

## Resend Para Supabase Auth

1. O dominio escolhido e equilibrafitplus.com.br, ja cadastrado no Resend.
2. Publicar exatamente os registros DNS retornados, listados abaixo.
3. Confirmar verificacao DKIM/SPF e revisar DMARC sem sobrescrever a politica
   de um dominio existente. Manter click/open tracking desabilitados para Auth.
4. Usuario cria uma API key dedicada ao envio nesse dominio, sem compartilhar
   a chave pelo chat. Nenhuma chave do Vero e reutilizada.
5. No NOVO Supabase, Authentication -> Email -> SMTP Settings, configurar:

| Campo | Valor |
| --- | --- |
| Host | smtp.resend.com |
| Port | 465 |
| Username | resend |
| Password | API key Resend, preenchida pelo usuario |
| Sender Name | EquilibraFit++ |
| Sender Email | noreply@equilibrafitplus.com.br (remetente proposto) |

Os campos informados pelo usuario (`smtp.resend.com`, `465`, `resend`, nome
EquilibraFit++ e valor `60`) sao de SMTP, nao Host/Port/User PostgreSQL.
`onboarding@resend.dev` e remetente de testes, limitado a envio para o endereco
da propria conta Resend; nao atende cadastro publico da Beta.
[Restricao oficial resend.dev](https://resend.com/docs/knowledge-base/403-error-resend-dev-domain).
Nao substituir pelo dominio novo antes de sua verificacao; uma key restrita ao
dominio novo tambem deve usar esse dominio no remetente. Para preencher Password,
o usuario usara sua API key Resend diretamente no painel, nunca pelo chat.

Esses campos seguem a [integracao oficial Resend/Supabase](https://resend.com/docs/send-with-supabase-smtp).
O SMTP padrao Supabase nao e destinado a Beta publica; veja
[SMTP customizado](https://supabase.com/docs/guides/auth/auth-smtp).
Nao desabilite confirmacao de email para contornar falha de entrega.

Os templates de Auth ficam no Supabase quando se usa SMTP: nao e necessario
criar templates duplicados no Resend. Configure email/senha, confirmacao,
signing key ES256/RS256 e URLs permitidas, conforme [Supabase](supabase.md).
ES256 foi observado no endpoint publico JWKS; nenhum algoritmo foi alterado.
Templates prontos para Authentication -> Email -> Templates:
- [Confirm signup](../infra/supabase/templates/confirmation.html), assunto
  `Confirme seu e-mail - EquilibraFit++`, usando ConfirmationURL do Supabase.
- [Reset password](../infra/supabase/templates/recovery.html), assunto
  `Redefina sua senha - EquilibraFit++`, usando o deep link Android token_hash.

### Publicar O Email De Confirmacao

O email encaminhado pelo operador ainda usa o texto padrao em ingles e
redirect_to=http://localhost:3000. Isso indica que template/URL configuration
do Supabase ainda precisam ser atualizados; os arquivos locais nao sao
publicados pelo Docker ou Render. Nao abrir, reutilizar ou registrar o token
desse email para testar a configuracao.

No projeto knaubynyytqnyyrdrkfi, Authentication -> Email -> Templates -> Confirm
sign up, salvar o assunto `Confirme seu e-mail - EquilibraFit++` e o HTML de
[confirmation.html](../infra/supabase/templates/confirmation.html).
Manter `{{ .ConfirmationURL }}` no botao: o Supabase gera um link individual
para cada destinatario. Nao substituir pelo link de um email ja enviado.
[Configuracao oficial de templates](https://supabase.com/docs/guides/auth/auth-email-templates).

Em Authentication -> URL Configuration, salvar:
- Site URL: `equilibrafitplusplus://auth/login`.
- Redirect URLs: `equilibrafitplusplus://auth/login` e
  `equilibrafitplusplus://auth/recovery`, preservando outros destinos legitimos
  ja configurados. Nao adicionar wildcards globais nem localhost para a Beta.

O APK Android atualizado declara o destino /login; a rota Flutter ja existia.
O Supabase verifica o email antes do retorno ao aplicativo. O callback abre
login sem aceitar tokens da URL como uma nova sessao; o usuario entra com
email/senha pela API existente. Recovery continua separado. Essa configuracao
mobile precisa do APK atualizado, nao de um redeploy da API. iOS e retorno em
navegadores sem o app instalado continuam fora desta validacao Android.
[Site URL e destinos mobile oficiais](https://supabase.com/docs/guides/auth/redirect-urls).

O Admin publicado nao e um handler mobile de confirmacao. O dominio proprio
nao responde no DNS; nao usar https://equilibrafitplus.com.br como destino
publicado sem verificacao. Nao desabilitar confirmacao de email.

Depois de salvar no painel, testar com NOVO email de confirmacao solicitado
pelo operador: texto em portugues, branding correto, ausencia de localhost,
confirmacao e retorno ao APK. Links ja enviados nao mudam retroativamente.
O agente nao enviou emails, consumiu tokens, confirmou contas ou salvou esses
campos remotos por MCP; a verificacao real depende dessa publicacao no painel.

## DNS Exato Do Resend

Nao ha acesso ao seu provedor DNS nesta integracao. A consulta local nao
encontrou esses nomes nem NS para o dominio em 2026-10-07; verificar registro,
delegacao e propagacao com o provedor. Nao sobrescrever MX/SPF/DMARC existentes.
Os campos abaixo sao publicos e nao sao API keys.

| Tipo | Nome relativo | Valor | TTL |
| --- | --- | --- | --- |
| TXT | resend._domainkey | Valor DKIM completo abaixo | Auto |
| CNAME | rsend | rsend-sae1.forge.rmta.net | Auto |
| CNAME | send | send.forge.rmta.net | Auto |

```text
p=MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDetrJ9OBzrUCaFwHzF6W+yb+Zh2LP6dkjCPaQcDFfQU9C2Hu+Gs6U7uf2ig9NcFMGJT3X4T8EmNpz+yjQRBa4ovqWuZnqO59BfOTK18rfMfFhxu6dvrcWXn94GvzBl3uH0aVeTKxbQlvyu36XMF0Hv3OVtWbPB049Q7gDQhqinLwIDAQAB
```

Se o provedor exigir nomes completos, adicionar .equilibrafitplus.com.br ao nome
relativo uma unica vez. CNAMEs nao devem ser proxies HTTP (Cloudflare: DNS only).
Na criacao, `send` estava not_started; `rsend` e DKIM estavam pending.
A consulta mais recente em 2026-10-07 retornou failed para os tres registros.
Confirmar os tres no painel Resend antes de salvar e solicitar nova verificacao
apos propagacao. Revisar DMARC com o provedor sem inventar endereco de relatorios.

## Teste Remoto Reproduzivel

[verify-database.sql](../infra/supabase/verify-database.sql) foi executado no
EquilibraFit++ por MCP com PASS e ROLLBACK. Nao cria usuarios auth.users, nao
usa senha/API key e nao substitui E2E de login. Execute apenas em banco dedicado
sem dados reais; exige permissao de SET ROLE e inspecao do historico EF.
Os dois testes xUnit remotos continuam SKIP porque as connection strings de
teste nao foram configuradas; o resultado SQL remoto e registrado separadamente.

## Verificacoes Pendentes

- Dominio Resend verificado e SMTP salvo com a chave preenchida pelo usuario.
- Cadastro publico, entrega de confirmacao e recovery reais com email valido.
- E2E mobile/Admin UI, isolamento A/B por HTTP e revogacao global reais.
- Deep link recovery Android frio/quente.

Conexao PostgreSQL/readiness e login/refresh/Admin/logout por HTTP comprovados.
SQL RLS A/B foi validado separadamente com fixtures revertidos. Essas evidencias
nao substituem os fluxos de email, as interfaces ou os demais gates Beta.
