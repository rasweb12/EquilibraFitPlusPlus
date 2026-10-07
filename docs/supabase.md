# Supabase

Para resolver startup sem connection string no Visual Studio e configurar
email SMTP separado do Vero, leia [configuracao local e Resend](supabase-resend-setup.md).

## Projeto Independente

Crie um projeto exclusivo do EquilibraFit++. Nao execute migrations/testes na
origem ou em producao. PostgreSQL e fonte de verdade remota; SQLite nao e banco
do Render. Copie host/usuario/porta reais do painel Connect, nunca deduza o host.

Para backend persistente, use conexao direta quando houver conectividade IPv6
ou session pooler compativel com sua rede. Evite transaction pooler para
migrations/session state. Limite o pool para os tres servicos e demais clientes.
Veja [conexoes oficiais](https://supabase.com/docs/guides/database/connecting-to-postgres).

```dotenv
SUPABASE_URL=https://SEU_PROJETO.supabase.co
SUPABASE_PUBLISHABLE_KEY=SUA_CHAVE_PUBLICA
SUPABASE_DB_CONNECTION_STRING="Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=VerifyFull;Maximum Pool Size=10"
```

A API e a factory aceitam alternativamente `ConnectionStrings__Default`.
A variavel Supabase nao vazia tem prioridade. `SSL Mode=VerifyFull` valida
certificado e host; Require sozinho nao certifica identidade do servidor.
Use CA confiavel/Root Certificate quando requerido, nunca Trust Server Certificate
para contornar validacao. [Npgsql TLS](https://www.npgsql.org/doc/security.html).

Nenhuma service_role/secret key e necessaria ao mobile/Admin. A connection string
e um secret exclusivo do backend; publishable key nao autoriza acesso irrestrito.

## Migrations Novas

Em `Infrastructure/Data/Migrations/PostgreSql`:

1. 20261004165529_InitialPostgreSql: baseline completo e seeds.
2. 20261004165930_SupabaseAuthProfiles: remove stores locais Identity e cria AuthSessions.
3. 20261004170424_SupabaseRowLevelSecurity: habilita RLS e policies de leitura propria.
4. 20261004182432_GooglePlayBilling: estado, protecao de token e eventos de cobranca.
5. 20261004211837_ProtectBillingData: RLS em BillingEvents e billing privado para clientes.
6. 20261004215736_GlobalSessionRevocation: marco global por usuario para rejeitar autenticacoes antigas.
7. 20261007042719_ProtectMigrationHistory: RLS/default-deny no historico EF em public.

As migrations SQL Server da origem nao foram copiadas/apagadas/modificadas.
O primeiro baseline foi gerado durante a transicao e possui stores Identity
temporarios; a segunda migration os remove. O estado final usa somente Supabase
Auth. Preserve esta sequencia; nao aplique apenas uma migration intermediaria.

```powershell
dotnet tool restore
dotnet ef database update --project src/backend/EquilibraFitPlusPlus.Infrastructure --startup-project src/backend/EquilibraFitPlusPlus.Api
dotnet ef migrations has-pending-model-changes --project src/backend/EquilibraFitPlusPlus.Infrastructure --startup-project src/backend/EquilibraFitPlusPlus.Api
```

O shell precisa das variaveis de conexao. Factory nao le appsettings/.env.
Docker gera um bundle PostgreSQL; `RUN_DB_MIGRATIONS=true` aplica antes do startup,
com `./efbundle --connection "$SUPABASE_DB_CONNECTION_STRING"`.
A reaplicacao usa historico EF e nao recria banco nem apaga dados.
Nunca use EnsureCreated no PostgreSQL nem misture EnsureCreated/migrations.
Mudancas futuras usam migrations forward e backup/revisao antes do deploy.
Down de RLS e bloqueado para impedir exposicao acidental; rollback de seguranca
exige migration revisada, nao disable global de RLS.

## Auth E Email

Habilite email/senha, confirmacao de email e signing key ES256 ou RS256.
Configure Site URL/redirects para endereco publico seu e SMTP adequado.
Cadastro sem sessao retorna email_confirmation_required, nao cria perfil
com base apenas em resposta anonima. Apos confirmar, o login provisiona perfil.

O Flutter usa endpoints existentes via adapter da API, sem supabase_flutter.
Tokens persistem no secure storage; refresh e logout pertencem ao Supabase.
Google OAuth nao foi implementado nesta etapa.

Signup/login/refresh tem timeout HTTP de 15 segundos (Flutter: 20), sem retry
automatico. Timeout/408/504 do provider retornam auth.provider_timeout (504);
rede/5xx restantes/JSON invalido retornam auth.provider_unavailable (503).
Credenciais invalidas continuam distintas de indisponibilidade temporaria.
Cancelamento pelo cliente e propagado e nao tenta escrever uma resposta 500
em uma conexao encerrada. Confirmacao de email permanece obrigatoria.
Veja o diagnostico e as limitacoes em [Supabase/Resend](supabase-resend-setup.md).

O template Reset Password deve conter link Android:
```html
<a href="equilibrafitplusplus://auth/recovery?token_hash={{ .TokenHash }}">Redefinir senha</a>
```

A API verifica token_hash com type recovery, troca senha e revoga sessoes locais.
Este hash e uma credencial de uso unico: nao logar links, enviar screenshots ou
registrar query strings. O aplicativo nao recebe token administrativo.
[Templates oficiais](https://supabase.com/docs/guides/auth/auth-email-templates).
Deep link precisa de teste em Android frio/quente; iOS/Universal Links e
recuperacao pelo navegador Admin nao estao integrados ainda.

Revogacao global usa iat e o momento original de autenticacao em amr, nao somente
o momento de emissao de um token renovado. Apos revogacao, amr ausente/invalido
falha fechado. Novo login deve ocorrer depois do segundo UTC do marco; hooks
customizados nao devem remover amr. Validar isto com refresh real do Supabase.
[Claims oficiais](https://supabase.com/docs/guides/auth/jwt-fields).

## RLS

Todas as tabelas app possuem RLS. Perfil e dados pessoais autorizados possuem
SELECT somente do dono (`auth.uid()`) e tenant correspondente. INSERT/UPDATE/DELETE
do cliente ficam negados: escritas passam pela API. Auditoria, sessoes,
idempotencia, configuracoes e billing nao sao acessiveis por anon/authenticated.
Nao existem policies globais para todos os usuarios autenticados.
[Referencia RLS](https://supabase.com/docs/guides/database/postgres/row-level-security).

Nao exponha schema app na Data API enquanto nao houver uso concreto aprovado.
Nao use tabela Assinaturas para entregar purchase token ao app.
Um administrador usa Admin -> API; seu navegador nunca recebe service role.

## Testes Remotos

Variaveis para bancos dedicados, fora de producao:

```powershell
# Configure TEST_POSTGRES_CONNECTION_STRING e/ou TEST_SUPABASE_DB_CONNECTION_STRING
dotnet test tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests -c Release
```

O teste PostgreSQL aplica migrations, reaplica, verifica seed e RLS habilitada,
constraints/indices e soft delete. O teste Supabase usa SET LOCAL ROLE authenticated
e JWT claims A/B dentro de transacao para provar isolamento, negar CRUD de escrita
e negar leitura de billing. Dados de teste sao revertidos, migrations permanecem.
Sem essas variaveis os testes sao SKIP explicito.
Para testar banco existente, rode novamente no mesmo projeto de teste e confirme
preservacao dos dados/ausencia de migrations pendentes.

## Administrador Inicial

Cadastre/ confirme email/entre pela API com a conta administrativa pretendida.
Configure temporariamente no backend:

```dotenv
AdminBootstrap__Enabled=true
AdminBootstrap__SupabaseUserId=UUID_DO_USUARIO_CONFIRMADO
```

Reinicie, verifique auditoria admin.bootstrap, desabilite bootstrap e reinicie.
Nao existe senha administrativa hardcoded ou seed de credenciais.

Conta local verificada em 2026-10-07: admin@equilibrafit.local, criada e confirmada
manualmente pelo operador no projeto knaubynyytqnyyrdrkfi. UUID Auth/perfil:
b6e9b851-bed3-4262-987f-45084849d38b. O primeiro login provisionou app.Usuarios;
o bootstrap promoveu somente esse perfil a Administrador e registrou exatamente
uma auditoria admin.bootstrap. AdminBootstrap:Enabled esta false nos User Secrets
e a API foi reiniciada com o bootstrap desabilitado.

Teste real pela API: login 200, refresh 200, dashboard administrativo 200,
logout 204 e dashboard com a mesma sessao revogada 401. O JWT ES256 foi validado
via JWKS e a role veio do perfil, nao de metadata editavel do usuario.
A pagina Admin esta disponivel em http://localhost:5029/login; o teste acima
valida seus endpoints, nao uma automacao de login pela interface do navegador.

Refresh tokens sao opacos: o validador aceita valores nao vazios ate 4096
caracteres, sem minimo herdado do emissor anterior. A autenticidade permanece
responsabilidade do Supabase. Sete testes de regressao cobrem essa regra.

Como .local nao recebe emails, a confirmacao deve ser administrativa/manual para
essa conta de desenvolvimento; nao desabilite confirmacao global nem use esse
endereco na Beta. Para recuperacao por email, use um endereco real verificado.
Nunca habilite o bootstrap antes de existir o perfil; isso interrompe o startup.
Senha temporaria fornecida pelo usuario foi usada somente em memoria, com prompt
protegido, para esse teste; nao foi gravada em arquivos ou Git nem exibida em logs.
Substitua-a diretamente no painel Supabase antes de uso definitivo.
