# Seguranca

## Fronteiras

Supabase Auth autentica; API autoriza por perfil/tenant e role do banco.
Claims de usuario/tenant/role recebidas sao substituidas por contexto confiavel.
RLS e habilitada, sem client writes ou acesso cliente a billing/roles/auditoria.
Admin conversa apenas com API. Revogacao local bloqueia session_id conhecido;
revogacao global verifica marco de usuario, iat e timestamp original em amr.

Emissao/rotacao de JWT nao depende de JWT_SECRET_KEY local.
Supabase utiliza signing key assimetrica e JWKS com HTTPS.
Tenants/perfis nao podem ser promovidos por metadata livre de Auth.
Google Play valida entitlement exclusivamente no servidor.

## Secrets

.env e .env.* sao ignorados, com unica excecao .env.example.
Build contexts excluem ambientes, bancos, chaves, caches e credenciais.
Nunca adicionar service_role/secret key, OpenAI key, senha Supabase,
purchase token bruto, credencial ADC ou keystore a Git/Flutter/Admin navegador.
Use gerenciador de secrets/Render secret file e rotacao operacional.

JWT/refresh ficam em secure storage Flutter; Admin usa sessionStorage por circuit.
Isso nao substitui protecao contra XSS. Logout limpa estado mesmo offline,
e refresh atrasado nao pode restaurar usuario anterior.
Dados sensiveis SQLite nao ficam expostos a outra conta pelos repositorios;
criptografia do SQLite e politica de backup do dispositivo requerem avaliacao.

## Logs

API gera correlation/request ID, JSON Serilog e propaga correlation para AI.
AI retorna correlation e logs JSON sem corpo/query.
Billing desliga HttpClient loggers porque a URL Google contem purchase token;
erros de transporte sao convertidos para mensagens sem URL/token.
Nunca ativar EF EnableSensitiveDataLogging em ambientes com dados reais.
Auditoria de Admin/consentimento preservada.

## Rede

TLS VerifyFull PostgreSQL, HTTPS publico no Render, AI com X-API-Key.
Em producao AI recusa startup sem chave interna.
CORS e allowlist explicita, sem wildcard; Flutter nativo nao depende de CORS.
Nao exponha server interno aceitando forwarded headers arbitrarios fora do proxy
confiavel. Release Android exige HTTPS e ID/assinatura independentes.
Storage ainda nao portado: buckets devem ser privados e URLs assinadas curtas.

## Revisao Beta

- Executar teste RLS real e isolamento API A/B, inclusive soft delete e children.
- Validar cadastro/confirmacao/recovery/refresh/revogacao com Supabase real.
- Revisar SMTP, rate limits e abuso de cadastro/login; limiter atual e por processo.
- Revisar retention/export/exclusao LGPD e exclusao correspondente em Supabase Auth.
- Validar Play sandbox, push autenticado e chave AES/ADC.
- Conferir Git status/diff/lista de arquivos e scanner de secrets antes de commit.
- Nao confundir testes simulados com prova de seguranca do ambiente publicado.

LGPD portada mantem solicitacoes/consentimentos/extracao e bloqueio local;
exclusao administrativa definitiva de auth.users via Supabase nao foi automatizada
sem um fluxo revisado de credencial administrativa e retencao.
