# Coach IA: persistencia de conversas e identificacao de fallback

Data: 2026-10-10. Repositorio independente: `rasweb12/EquilibraFitPlusPlus`.
Base inspecionada: `68bfeed81ffc3329a782f22558f8c4d1257916ce` (IA5).
As correcoes deste relatorio sao locais; nao houve commit, push, deploy ou mudanca de plano.
O projeto original nao foi alterado.

## Evidencias remotas, somente leitura

Workspace confirmado: EquilibraFit++. Servicos ativos com sufixo `-4lkw`.
Horarios abaixo em UTC; nao foram registrados prompts, tokens ou dados nutricionais reais.

- 09:59:57: API -> Python retornou 502 em 165 ms, na URL HTTPS correta.
- 10:01:54: Python concluiu startup apos consulta publica a `/health`, que retornou 200 em 23,4 segundos.
- 10:02:25: Coach sem credenciais retornou 401 JSON; nenhuma chamada paga foi feita pelo agente.
- 10:03:38: primeira mensagem enviada pelo usuario retornou 200 tanto no Python quanto na API.
- 10:04:27 e 10:04:41: Python retornou 200, mas a API lancou `PersistenceConflictException` ao salvar a continuacao da conversa.
- Nessas chamadas o Python registrou `AI request unavailable: feature=coach error=unconfigured`.

A resposta mostrada no app era o fallback deterministico, nao evidencia de sucesso do OpenAI.
Ao falhar uma mensagem seguinte, a tela preservava a resposta anterior e mostrava o erro de persistencia.
O middleware traduz esse conflito para 409; o request logger anterior a captura da excecao registra 500 durante o desenrolamento da pipeline.

## Causa reproduzida e correcao

Mensagens novas possuem GUID atribuido pela aplicacao. Quando apenas adicionadas a colecao de uma sessao ja rastreada,
o EF Core podia inferir `Modified`, tentando atualizar linhas ainda inexistentes. Isso gerava conflito falso de concorrencia.
O teste relacional reproduziu a falha exatamente na segunda mensagem, antes da correcao.
[Comportamento documentado do EF Core para novas entidades em colecoes](https://learn.microsoft.com/en-us/ef/core/change-tracking/relationship-changes).

- Adicionado `IAiCoachRepository.AdicionarMensagem` e implementacao com `ChatMessages.Add`.
- Handler atribui explicitamente a FK da sessao e registra mensagens de conversas existentes como insercoes.
- Novas sessoes continuam sendo inseridas com seu grafo; historico e RowVersion existentes nao sao sobrescritos.
- Concorrencia real, isolamento por tenant/usuario, soft delete, guardas clinicas e aviso de saude permanecem.
- Nenhuma migration, alteracao de esquema ou escrita no banco remoto foi executada.

## Fallback transparente e compativel

- `fallback_used` do Python passa por `AiCoachClientReply` ate `CoachReplyResponse.fallbackUsed`.
- Campo opcional com valor padrao: consumidores antigos continuam compativeis.
- Flutter mostra `Resposta de apoio local` quando o fallback foi usado.
- Para o deploy antigo, sem o novo campo, o Flutter reconhece os nomes conhecidos dos modelos rules/hybrid.
- O aviso de saude permanece e falhas de infraestrutura continuam distintas de bloqueios clinicos.
- Corrigido o reconhecimento de `refeicoes` no plural pelo fallback Python, sem criar prescricao nutricional.
- Nenhum fallback automatico entre provedores ou retry de POST foi habilitado.

## Inventario deste ajuste

Criados:
- `tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/AiCoach/CoachConversationPersistenceTests.cs`
- `docs/audit/2026-10-10-coach-persistencia-fallback.md`

Alterados:
- Application: `Abstractions/AiCoach/IAiCoachRepository.cs`, `IAiCoachClient.cs` e `Features/AiCoach/Commands/EnviarMensagemCoach/EnviarMensagemCoachCommandHandler.cs`.
- Infrastructure: `Repositories/AiCoachRepository.cs` e `AiCoach/AiCoachHttpClient.cs`.
- Contracts: `AiCoach/CoachReplyResponse.cs`.
- Flutter: `features/coach/data/coach_repository.dart`, `domain/coach_reply.dart`, `presentation/pages/coach_page.dart` e `test/features/coach/coach_repository_test.dart`.
- Python: `app/services/coach_service.py` e `tests/test_coach_and_label_services.py`.
- Testes .NET: `AiCoach/AiCoachHttpContractTests.cs`.

Nenhum arquivo removido ou copiado do original. Nenhuma dependencia adicionada, removida ou atualizada.
Alteracoes locais anteriores de filtros EF, licenciamento e auditoria foram preservadas, nao fazem parte do inventario acima.

## Testes e artefatos

- Regressao antes da correcao: falhou com `PersistenceConflictException`/`DbUpdateConcurrencyException` na segunda mensagem.
- Depois: tres trocas na mesma sessao, preservando IDs, versoes e historico, com e sem fallback.
- Tracking SQLite/Npgsql sem abrir conexao PostgreSQL: so mensagens novas ficam `Added`.
- SQLite: outro usuario/tenant nao encontra a conversa; edicao concorrente real continua falhando e nao sobrescreve historico.
- Metadados HTTP: fallback true/false/ausente mantem compatibilidade.
- `dotnet restore`: passou.
- `dotnet build EquilibraFitPlusPlus.sln -c Release --no-restore`: passou; 58 avisos CS1591 de documentacao nesse build incremental, zero erros.
- `dotnet test EquilibraFitPlusPlus.sln -c Release --no-build`: 320 aprovados, 2 ignorados, zero falhas.
- Python `ruff check app tests` e `compileall -q app`: passaram. Ruff precisou de execucao fora do sandbox para escrever no cache local.
- Python `pytest -q`: 68 aprovados, sem chamadas pagas.
- Flutter `pub get`: passou, sem atualizacao de versoes.
- Flutter `analyze`: passou sem avisos, apos corrigir quatro virgulas no teste novo.
- Flutter `test --no-pub --reporter expanded`: 64 aprovados.
- `flutter build apk --debug --no-pub --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api-4lkw.onrender.com`: passou em 52,6 segundos.
- Package verificado: `br.com.equilibrafit.app.plusplus.dev`, versao 1.0.0+1, minSdk 24, targetSdk 36.
- APK: `src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk`.
- SHA256: `769C5BFD9744E143A9E30A7B1C3675E26C877C108C8B2C7F04334DB0BF521EC5`.
- Instalacao com `adb -s emulator-5554 install -r`: Success; nao foi usado uninstall ou limpeza de dados.
- Build avisa sobre futura migracao do Kotlin Gradle Plugin; nao foi feita atualizacao de toolchain nesta correcao.

Os dois testes ignorados exigem bancos dedicados PostgreSQL/Supabase; nao foram apontados para producao.
Docker, Release assinado/AAB, publicacao Google Play, E2E remoto da correcao e execucao real dos provedores nao foram realizados nesta etapa.

## Configuracao do Render: atualizacao de 10:26 UTC

Uma nova instancia iniciada em 2026-10-10 as 10:26 UTC registrou
`openai_configured=True`, `gemini_configured=True` e `fallback_enabled=False`.
Os avisos `unconfigured` acima pertencem a instancia anterior; nao representam
a configuracao atualmente carregada. Esses indicadores comprovam presenca
das chaves, nao validade das credenciais nem sucesso de chamadas pagas.
A configuracao de ambos os provedores foi feita pelo usuario; o agente nao
leu, exibiu ou alterou as chaves. A escolha de provedor no Coach e o novo
padrao Gemini nas demais funcionalidades estao em `../ai-provider-routing.md`.

No **servico Python ativo** `equilibrafit-plusplus-ai-4lkw`, manter:

```dotenv
EQUILIBRAFIT_AI_COACH_PROVIDER=openai
EQUILIBRAFIT_AI_OPENAI_MODEL=gpt-4.1-mini
EQUILIBRAFIT_AI_FALLBACK_ENABLED=false
```

`error=unconfigured` confirma que o provedor selecionado nao estava utilizavel; nao distingue sozinho chave ausente de SDK nao importavel.
Nao foram lidos/exibidos valores secretos nem presumida igualdade entre chaves internas da API e Python.
A chave interna `EQUILIBRAFIT_AI_API_KEY` nao substitui a chave do OpenAI.
Nao colocar nenhuma chave no Flutter ou Git, nem envia-la pelo chat.

O plano Free encerra servicos apos 15 minutos sem trafego, compativel com os ciclos observados.
A disponibilidade continua pendente: consulta manual a health nao e solucao permanente nem prova de sucesso da IA.
Nao houve ativacao de plano pago, keepalive, automacao ou alteracao de cobranca.
[Limites oficiais do Render Free](https://render.com/docs/free).

## Aceite apos publicacao autorizada

1. Revisar o diff local e autorizar separadamente commit/push/deploy; API e Python ainda executam IA5.
2. Manter as chaves ja configuradas e publicar as correcoes aprovadas, na ordem Python -> API.
3. No app, enviar uma mensagem, depois mais duas na mesma conversa: todas devem salvar, sem conflito falso.
4. Reconsultar a sessao via API e confirmar seis mensagens; outro usuario/tenant nao deve acessa-la.
5. Com provedor indisponivel, a resposta deve ser identificada como apoio local, sem medidas nutricionais inventadas.
6. Com provedor configurado, confirmar nos logs modelo/provedor efetivos; HTTP 200 sozinho nao basta.
7. Validar disponibilidade depois de periodo ocioso; Release e Google Play continuam pendentes e nao foram publicados.
