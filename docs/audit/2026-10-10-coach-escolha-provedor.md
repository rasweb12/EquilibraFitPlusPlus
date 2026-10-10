# Coach: escolha entre OpenAI e Gemini

Data: 2026-10-10. Repositorio independente `rasweb12/EquilibraFitPlusPlus`.
As alteracoes sao locais: nao houve commit, push, deploy ou escrita no banco
remoto. O projeto original nao foi modificado.

## Comportamento implementado

- Coach: seletor OpenAI/Gemini por mensagem, inicialmente OpenAI.
- Troca de provedor na mesma conversa conserva sessao e mensagens anteriores.
- Seletor desabilitado enquanto uma mensagem esta sendo processada.
- A API aceita `provedor` opcional e valida somente `openai`/`gemini`.
- A API envia `provider` ao Python; o schema Python tambem valida a selecao.
- O roteador nao altera configuracao compartilhada para atender uma escolha.
- Escolha explicita chama somente o provedor escolhido, mesmo em caso de falha.
- Sem selecao, clientes anteriores usam o provedor padrao do Coach.
- Pedidos offline guardam a escolha no payload SQLite existente.
- As respostas locais e erros de infraestrutura continuam identificados;
  guardas clinicas e revisao nutricional permanecem.
- Treinos, planos alimentares, imagens, texto de refeicoes e rotulos usam
  Gemini por padrao. Fallback automatico permanece desabilitado.

## Inventario deste ajuste

Criados:
- `docs/ai-provider-routing.md`
- `docs/audit/2026-10-10-coach-escolha-provedor.md`
- `src/mobile/equilibrafit_plusplus_app/test/features/coach/coach_page_test.dart`

Adaptados:
- Configuracao: `.env.example`, `docker-compose.yml`, `render.yaml`.
- `.env` local ignorado pelo Git: somente `EQUILIBRAFIT_AI_WORKOUTS_PROVIDER`
  passou de `openai` para `gemini`; nenhuma chave foi alterada.
- Python: `app/core/config.py`, `app/providers/ai_provider_router.py`,
  `app/schemas/coach.py`, `app/services/coach_service.py`.
- Testes Python: `test_ai_provider_router.py`, `test_api_security.py`,
  `test_coach_and_label_services.py`.
- Contracts: `AiCoach/EnviarMensagemCoachRequest.cs`.
- Application: `Abstractions/AiCoach/IAiCoachClient.cs`,
  `Features/AiCoach/Commands/EnviarMensagemCoach/EnviarMensagemCoachCommandHandler.cs`
  e `EnviarMensagemCoachCommandValidator.cs`.
- Infrastructure: `AiCoach/AiCoachHttpClient.cs`.
- Testes .NET: `EnviarMensagemCoachCommandValidatorTests.cs`,
  `AiCoachHttpContractTests.cs`, `CoachConversationPersistenceTests.cs`,
  `SupabaseAuthorizationTests.cs`.
- Flutter: `features/coach/data/coach_repository.dart`,
  `presentation/pages/coach_page.dart`, `test/features/coach/coach_repository_test.dart`.
- Documentacao: `docs/architecture.md`, `docs/context/technical-context.md`,
  `docs/render.md`, `docs/audit/2026-10-10-coach-persistencia-fallback.md`.

Nenhum arquivo copiado ou removido. Nenhuma dependencia adicionada, removida
ou atualizada. Nenhuma migration necessaria. Alteracoes locais anteriores
foram preservadas.

## Testes e resultados

| Verificacao | Resultado |
| --- | --- |
| Python, primeiro bloco de roteamento/schema/Coach | 61 testes aprovados |
| Python compileall app | Aprovado |
| Ruff app/tests | Aprovado |
| Python pytest completo | 92 aprovados |
| .NET build Release | Aprovado; zero erros, avisos existentes CS1591 |
| .NET test Release completo | 336 aprovados, 2 ignorados, zero falhas |
| Flutter testes direcionados do Coach | 12 aprovados |
| Flutter analyze | Sem problemas |
| Flutter test completo | 71 aprovados |
| Debug Android | APK gerado em 15,3 segundos |
| Instalacao emulator-5554 com install -r | Success, sem limpeza de dados |
| git diff --check | Aprovado |
| Docker compose config | Nao executado: CLI Docker ausente |

O primeiro build encontrou ambiguidade `CS0121` no construtor implicito de
um teste novo; o tipo foi explicitado e o build/teste seguinte passou.
Os dois testes ignorados exigem PostgreSQL/Supabase dedicados; variaveis de
conexao desses testes foram esvaziadas somente no processo de testes.
Nenhum teste foi apontado para producao.

Cobertura nova: ambos os provedores, escolha explicita sem troca automatica,
timeout, falta de configuracao, resposta invalida, concorrencia entre escolhas,
modelo efetivo, validacao HTTP autenticada, guarda clinica com Gemini,
persistencia de tres mensagens na mesma sessao alternando provedores,
payload offline, loading e layout de 320/360/430 pixels.
Os testes de provedores usam mocks: nao fazem chamadas pagas nem validam as
credenciais reais. Nao foram executados Release/AAB, Docker build ou deploy.

## APK Debug

- Arquivo: `src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk`.
- Package: `br.com.equilibrafit.app.plusplus.dev`.
- Versao: 1.0.0+1; minSdk 24; targetSdk 36.
- API: `https://equilibrafit-plusplus-api-4lkw.onrender.com` via dart-define.
- SHA256: `72865B042AF624B75D17FE43093FDD5F169AC4745BE25525881DB508C8F6885B`.
- Instalado no emulador, nao em celular fisico; o app nao foi iniciado pelo
  agente para evitar processar pedidos reais pendentes.
- A assinatura release e as chaves do aplicativo original nao foram alteradas.
- Permanece aviso de futura migracao do Kotlin Gradle Plugin, fora deste ajuste.

## Pendencias para funcionar no Render

Revisar o diff e autorizar a publicacao separadamente. Publicar Python antes
da API. Depois testar o APK atualizado: servicos antigos podem ignorar os
novos campos de selecao. O seletor sozinho nao comprova qual provedor atendeu.

No servico Python existente, alinhar `EQUILIBRAFIT_AI_WORKOUTS_PROVIDER=gemini`
e manter Gemini em PLANS/MEALS/MEAL_TEXT/LABELS. Nao criar servicos duplicados
nem presumir que a alteracao local de `render.yaml` ja atualizou o painel.
As duas chaves ja carregadas pelo processo do Render nao foram alteradas.

Aceite: duas mensagens na mesma conversa, uma por provedor, ambas persistidas
e com modelo/provedor confirmado nos logs; nova geracao de treino com Gemini;
pedidos offline mantendo a escolha apos reinicio e reconexao; indisponibilidade
sem troca automatica, falsas medidas nutricionais ou falso bloqueio clinico.
Health check e presenca de chaves nao substituem esses testes reais.

Ver [configuracao e publicacao](../ai-provider-routing.md).
