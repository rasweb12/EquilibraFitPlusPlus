# Google Play Billing

Google Play Billing e a integracao de assinatura digital Android; nao Google
Pay. Nao ha Stripe/Checkout/webhook Stripe na nova base.

## Fluxo

```text
Flutter in_app_purchase -> Play Billing -> purchase token
  -> API autenticada -> Google Play Developer API subscriptionsv2
  -> assinatura verificada + evento -> entitlement Premium
  -> acknowledge/completion
```

IPaymentProvider isola dominio de GooglePlayBillingProvider.
API e fonte de verdade; aplicativo nao libera Premium por comprovante local.
ObfuscatedExternalAccountId deve ser SHA256 do UUID do usuario (lowercase),
correspondente ao applicationUserName do Flutter. Token so pode pertencer a
um usuario/tenant; purchase token e cifrado AES-GCM no banco e indexado por hash.

Backend registra productId, estado, orderId, plataforma, inicio/expiracao,
auto-renovacao, cancelamento, processamento/evento e ambiente teste/producao.
Nao registra dados de cartao.

## Configuracao

Novo aplicativo Play com applicationId proprio, assinatura independente,
produtos/subscriptions/base plans e contas license testers.
O ID de debug nao e o ID de publicacao e o original nao deve ser substituido.
ID autorizado: `br.com.equilibrafit.app.plusplus`; debug usa sufixo `.dev`.
Veja `android-release.md` para keystore, APK assinado e AAB de teste interno.

```dotenv
GooglePlay__Enabled=true
GOOGLE_PLAY_PACKAGE_NAME=br.com.equilibrafit.app.plusplus
GOOGLE_PLAY_PRODUCT_IDS=SEU_PRODUCT_ID,OUTRO_PRODUCT_ID
GOOGLE_APPLICATION_CREDENTIALS=/etc/secrets/google-play.json
BILLING_TOKEN_ENCRYPTION_KEY=BASE64_DE_32_BYTES_ALEATORIOS
GOOGLE_PLAY_PUBSUB_AUDIENCE=https://SUA_API/api/v1/billing/google-play/notifications
GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL=CONTA_PUSH_AUTORIZADA
```

Endpoint RTDN: POST /api/v1/billing/google-play/notifications.
Audience da API publicada:
`https://equilibrafit-plusplus-api-4lkw.onrender.com/api/v1/billing/google-play/notifications`.
Product IDs devem ser os REAIS e ativos do Play Console; exemplos/fixtures de
testes nao criam produtos. Configuracao habilitada sem package, produtos,
chave AES de 32 bytes ou RTDN HTTPS/conta push interrompe startup, sem expor
valores privados. Erros Google transitorios retornam 503/504; resposta invalida
502; compra/usuario invalidos 400. Cliente nao confirma recibo nao validado.
Conta ADC precisa de Android Publisher API e permissoes Play apropriadas.
Credencial JSON somente em secret file/backend, nunca Flutter/Git/container image.
Guarde chave AES separada com backup: perder/trocar chave sem recriptografar
os tokens impede reconciliacao. SUPABASE_SECRET_KEY nao substitui ADC.
No Render, criar Secret File `google-play.json`, montado em
`/etc/secrets/google-play.json`. Preencher env vars no servico API e habilitar
GooglePlay__Enabled SOMENTE depois de configurar e revisar todos os requisitos.
`sync: false` no Blueprint preserva a escolha manual; em ambiente novo definir
`false` enquanto o Play nao estiver pronto. Este arquivo nao cria a conta de
servico, produtos, topic/subscription Pub/Sub ou configuracoes no painel.

RTDN usa push Pub/Sub autenticado com token Google OIDC: issuer, audience,
email verificado e conta especifica. Mensagem indica mudanca; estado e sempre
consultado novamente no Google. Evento duplicado nao duplica historico.
Restore envia token para API e apenas conclui compra apos validacao do servidor.

## Estados E Recuperacao

Active/grace concedem entitlement ate expiracao; cancelamento preserva periodo
ja pago. Hold/paused/pending/expired/revoked nao liberam Premium.
Reconciliacao consulta compras persistidas periodicamente quando o processo
esta ativo. RTDN/restore atualizam renovacao, recuperacao, revogacao e expiracao.
Compra ainda nao vinculada ao usuario e ignorada no RTDN ate validacao cliente.
Acknowledge e tentado novamente se falhar apos persistencia.

Troca de plano no Play pode chegar por linkedPurchaseToken; o backend valida
dono e encerra entitlement anterior. UI de upgrade/downgrade com politica
de prorata ainda nao foi implementada; nao anunciar esse fluxo como pronto.

## Validacao Obrigatoria Externa

Sandbox precisa provar compra nova, acknowledge, renovacao acelerada, cancelamento,
fim do periodo, recusada/hold/grace, recuperacao, revogacao, restore, troca de conta,
eventos repetidos e outage da API. Nenhuma conta Play real foi acessada nesta etapa.
Nao habilite billing para usuarios Beta antes de concluir estes cenarios.

[Integracao servidor](https://developer.android.com/google/play/billing/backend),
[assinaturas](https://developer.android.com/google/play/billing/subscriptions),
[subscriptionsv2](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.subscriptionsv2/get).
