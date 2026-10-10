# Incidente Gemini no Coach - 2026-10-10

## Evidencia e causa

O seletor do Coach encaminhou `provider=gemini` corretamente. No Render, as
chamadas ao endpoint `generateContent` do Gemini retornaram HTTP 404 para
`gemini-2.5-flash`. O provider convertia toda excecao do SDK em `None`; o
roteador, por isso, classificava o evento como `invalid_response` e o Coach
mostrava sua resposta deterministica de apoio local.

A documentacao do Google informa que o acesso aos modelos Gemini 2.5 esta
limitado a projetos que ja os utilizavam e recomenda, para projetos novos,
Gemini 3.5 Flash-Lite ou Gemini 3.8 Flash.

## Correcao

- modelo padrao atualizado para `gemini-3.5-flash-lite`;
- escolha explicita OpenAI/Gemini do Coach preservada;
- demais funcionalidades continuam configuradas para Gemini;
- falhas 400, 401, 403, 404, 429, 5xx, transporte e timeout classificadas sem
  registrar corpo do provedor, prompt, chave ou mensagem potencialmente
  sensivel;
- resposta local segura e revisao nutricional obrigatoria preservadas;
- imagens Base64 invalidas sao rejeitadas antes de chamar o provedor;
- nenhum fallback automatico entre OpenAI e Gemini foi habilitado.

## Publicacao necessaria

Esta alteracao de repositorio nao muda o Render sozinha. Antes de validar no
Android, publicar o servico Python com o `render.yaml` atualizado ou configurar
privadamente `EQUILIBRAFIT_AI_GEMINI_MODEL=gemini-3.5-flash-lite` no servico
`equilibrafit-plusplus-ai` e reinicia-lo. Nao e necessario alterar a API key.

Validar no Coach uma mensagem com Gemini e confirmar nos logs:

```text
provider=gemini
model=gemini-3.5-flash-lite
fallback=false
```

Se houver falha, usar `error_type` e `upstream_status_code`; nao copiar o corpo
da resposta nem segredos para logs ou chamados.

## Validacao local

- `python -m pytest -q`: 142 aprovados;
- `ruff check app tests`: aprovado;
- `python -m compileall -q app tests`: aprovado;
- `python -m pip check`: nenhuma dependencia quebrada;
- `dotnet test EquilibraFitPlusPlus.sln -c Release --no-restore`: 336
  aprovados e 2 ignorados;
- os 2 testes ignorados exigem, respectivamente, PostgreSQL vazio dedicado e
  projeto Supabase dedicado, e nao receberam connection strings de producao;
- `docker compose config --no-interpolate --quiet`: nao executado porque Docker
  nao esta instalado nesta maquina.

Todos os testes de provedor usam mocks. Nao houve chamada paga, alteracao de
segredo, deploy, commit ou push durante esta correcao. A validacao funcional no
Android permanece pendente ate a nova configuracao chegar ao Render.
