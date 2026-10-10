# Roteamento de IA

## Escolha no Coach

O Coach oferece um seletor OpenAI/Gemini por mensagem. A escolha acompanha o
pedido Flutter -> API .NET -> FastAPI e fica no payload SQLite de pedidos
pendentes, sem depender de estado compartilhado do roteador.

O contrato da API aceita `provedor: "openai"` ou `provedor: "gemini"`.
A API envia `provider` ao Python. Valores desconhecidos sao rejeitados antes
de chamar um provedor. O campo e opcional: clientes anteriores continuam
usando `EQUILIBRAFIT_AI_COACH_PROVIDER`. A conversa pode continuar com outro
provedor sem substituir as mensagens anteriores.

Uma escolha explicita nunca habilita troca automatica para o outro provedor.
Se a chamada falhar, continuam as respostas locais seguras, identificadas por
`fallbackUsed`, e os erros estruturados de indisponibilidade de infraestrutura.
O modelo da resposta continua sendo o efetivamente retornado pelo Python.
Os controles clinicos e a revisao nutricional obrigatoria nao foram removidos.

## Demais funcionalidades

Geracao de treinos, planos alimentares, imagens de refeicoes, estimativas por
texto e rotulos usam Gemini por padrao. Nao possuem seletor no aplicativo.
Variaveis de ambiente podem sobrescrever os padroes; revisar o servico Python
existente no Render para que corresponda a esta configuracao:

```dotenv
EQUILIBRAFIT_AI_COACH_PROVIDER=openai
EQUILIBRAFIT_AI_WORKOUTS_PROVIDER=gemini
EQUILIBRAFIT_AI_PLANS_PROVIDER=gemini
EQUILIBRAFIT_AI_MEALS_PROVIDER=gemini
EQUILIBRAFIT_AI_MEAL_TEXT_PROVIDER=gemini
EQUILIBRAFIT_AI_LABELS_PROVIDER=gemini
EQUILIBRAFIT_AI_OPENAI_MODEL=gpt-4.1-mini
EQUILIBRAFIT_AI_GEMINI_MODEL=gemini-3.5-flash-lite
EQUILIBRAFIT_AI_FALLBACK_ENABLED=false
```

O modelo 2.5 tem acesso limitado pelo Google a projetos com uso anterior. Para
projetos novos, a configuracao usa `gemini-3.5-flash-lite`, modelo estavel com
entrada multimodal e saida estruturada. Um HTTP 404 do provedor e registrado
como `model_not_found`, com o status e o modelo, sem corpo, prompt ou segredo.
Erros de autenticacao, permissao, limite, transporte e timeout tambem possuem
categorias seguras. Nenhum deles ativa troca automatica de provedor.

As chaves OpenAI/Gemini e a chave interna da IA permanecem somente no ambiente
confiavel do backend/Python. Nao existem chaves de provedor no seletor Flutter.

## Publicacao e verificacao

As alteracoes locais nao atualizam os servicos publicados automaticamente.
Publicar primeiro o Python, depois a API e por ultimo atualizar o APK. Um
Python antigo pode ignorar `provider`, e uma API antiga pode ignorar `provedor`;
portanto nao validar a escolha somente pela presenca do seletor no aplicativo.
Verificar o modelo da resposta e os logs `AI request completed` por funcionalidade,
sem registrar tokens, prompts pessoais ou chaves.

No Android, enviar uma mensagem com OpenAI e outra com Gemini na mesma conversa.
Confirmar persistencia, ausencia de conflito e o provedor/modelo nos logs.
Repetir offline: selecionar Gemini, enviar, reiniciar e reconectar; o pedido
pendente deve conservar `provedor: "gemini"`.
Falhas devem continuar distinguindo resposta local, indisponibilidade e bloqueio
clinico. Health checks nao certificam acesso aos provedores.

Os testes automatizados usam mocks e nao efetuam chamadas pagas nem validam
as credenciais reais. Nao houve deploy automatico, alteracao de segredos ou
migration de banco nesta implementacao.
