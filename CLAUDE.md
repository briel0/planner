# Planner

Planner organizado em categorias, com cartões em quadros de canvas livre; cada cartão abre seu próprio quadro, no estilo Notion.
Os dados precisam ser consumíveis por outros clientes no futuro (scripts, agentes de IA etc.), não só pela UI.

> Status: stack, modelagem (`docs/data-modeling/`), convenções da API e arquitetura decididas. Domínio (`Card`, `Category`, `User`) e persistência (EF Core + Postgres, primeira migration aplicada) implementados e testados; próximo passo: os primeiros casos de uso e endpoints.

## Visão do produto

- **Categorias**: cada categoria é um quadro. Navegação entre categorias por abas no rodapé, como as planilhas de uma pasta de trabalho do Excel.
- **Cartões**: cada quadro é um canvas livre com cartões arrastáveis e personalizáveis (cor etc.). Não há tipos de cartão: todo cartão tem o mesmo formato, com **propriedades** flexíveis (ex.: prazo, feito) vindas de um catálogo do sistema, como no Notion.
- **Quadro recursivo**: abrir um cartão mostra o conteúdo rico dele e o seu próprio quadro, que pode conter outros cartões, sem limite de profundidade.
- **Multiusuário**: cada usuário só vê os próprios dados.
- **Dados abertos**: o planner é a primeira peça de um futuro ecossistema próprio e integrado de ferramentas. Outros serviços (de qualquer linguagem, incluindo agentes de IA) devem consumir esses dados sem passar pela UI.

## MVP (objetivo atual)

Dentro:
1. Abas de categoria no rodapé: criar, renomear, reordenar, apagar.
2. Canvas com cartões: criar, arrastar, renomear, mudar cor, apagar.
3. Quadro recursivo: abrir um cartão mostra o quadro dele, com trilha de navegação de volta.
4. Propriedades prazo (`dueOn`) e feito (`done`), visíveis no cartão.
5. Um usuário fixo de desenvolvimento (sem login).

Fora (depois do MVP): editor de conteúdo (Tiptap), camadas (trazer para frente/trás), login com Google, hospedagem, CI.

Caminho: **fase 1** — front Angular com dados falsos em memória, já no formato de `docs/api-design.md`; **fase 2** — ligar à API uma funcionalidade por vez (cada uma com seu endpoint), levantando as decisões adiadas conforme os gatilhos.

## Princípios

- **O contrato é a API, não o banco nem a UI.** Toda leitura e escrita passa por uma camada de domínio/API com schemas validados. A UI, scripts e agentes (ex.: servidor MCP) são clientes dessa camada.
- **Separar dado de domínio e dado de apresentação.** Título, datas e status são domínio. Cor, posição e ícone são apresentação. Um agente precisa do primeiro, não do segundo.
- **Campos estruturados para o que for consultável.** Propriedades (prazo, status) ficam no JSON tipado `properties`, validado pela API contra o catálogo — nunca texto solto dentro do conteúdo rico.
- **A API é a guarda da integridade.** Validação no frontend é conveniência de UX; a garantia é sempre a API (e o banco, quando possível), porque outros sistemas escrevem sem passar pela UI.
- **Conteúdo rico com representação legível.** O conteúdo da página é salvo no formato do editor (JSON), com uma versão derivada em texto/Markdown para consumo por LLMs.
- Tipagem estrita dos dois lados, contrato front/back via OpenAPI, migrations versionadas, testes na camada de domínio.

## Decisões tomadas

- **Banco de dados: PostgreSQL.** Usado no trabalho; suporta vários serviços do ecossistema acessando via API.
- **Quadro: canvas livre.** Cada cartão guarda sua posição (x, y) — dado de apresentação, separado do domínio.
- **Modelagem do banco: método clássico** (minimundo → conceitual → lógico → físico), documentado em `docs/data-modeling/`. O minimundo em `docs/data-modeling/01-miniworld.md` é a fonte de verdade dos requisitos de dados.
- **Ambiente: Docker.** Serviços (banco, API, web) rodam em containers via Docker Compose; dados do Postgres em volume. Por enquanto roda **só localmente** (nada exposto à internet).
- **Frontend: Angular.** Usado no trabalho; foco em Angular moderno (standalone, signals, nova sintaxe de controle).
- **Backend: C# com ASP.NET Core (.NET).** Usado no trabalho; injeção de dependência nativa.
- **Arquitetura do backend: Clean Architecture enxuta**, com modelo de domínio rico — ver `docs/backend-architecture.md`.
- **Lint e formatação do backend:** ferramentas nativas do .NET. `.editorconfig` na raiz (estilo inspirado no `.clang-format` do sumo-sdk: chaves na mesma linha, `else` na linha seguinte, `if(` sem espaço, 4 espaços, 120 colunas); `api/Directory.Build.props` liga nullable, analisadores `latest-recommended`, estilo verificado no build e avisos como erros.
- **Testes: xUnit v3** com o `Assert` nativo, em `api/tests/` espelhando os projetos (`Planner.Domain.Tests` primeiro). Testa-se o que tem regra e pode quebrar — cada invariante do domínio e cada bug corrigido —, não código trivial (getters, construtores sem lógica); cobertura não é meta. TDD no domínio.
- **Configuração e segredos:** segredos nunca vão para o Git. Docker Compose lê a senha do Postgres de `.env` (ignorado; modelo em `.env.example`); a API, em desenvolvimento, lê a string de conexão `ConnectionStrings:Planner` do User Secrets; em produção, de variáveis de ambiente. O Postgres só escuta em `127.0.0.1`.
- **Acesso ao banco: Entity Framework Core** com o provedor `Npgsql.EntityFrameworkCore.PostgreSQL`. Consultas em LINQ; migrations geradas pelo EF Core; SQL manual só em casos pontuais de desempenho. Mapeamento em `Planner.Infrastructure/Persistence/` (um arquivo de configuração por entidade, nomes em `snake_case` via `EFCore.NamingConventions`, sem índices automáticos em FKs). O que o EF Core não gera (FK composta, índices de expressão/parciais, triggers) é SQL escrito à mão dentro da migration.
- **Testes de integração:** `Planner.Infrastructure.IntegrationTests`, com Testcontainers (Postgres 18 descartável, migrations aplicadas). Testam as regras que só o banco garante.
- **Projeto Angular (`web/`):** Angular 22, standalone, `strict`, sem zone.js (zoneless), com rotas, sem SSR; testes com Vitest.
- **Lint e formatação do front:** ESLint (angular-eslint, regras recomendadas + acessibilidade de templates) e Prettier, no padrão do ecossistema TypeScript (`if (`, `} else {`), 2 espaços e 120 colunas. O estilo do sumo-sdk vale só para o C#.
- **Estilos: SCSS.**
- **Editor do conteúdo do cartão: Tiptap** (sobre ProseMirror), via `ngx-tiptap`. Conteúdo salvo como JSON do Tiptap (JSONB no Postgres). UI dos blocos (menu `/`, alça de arrastar) construída em componentes Angular. v1 só com parágrafo, título, lista e checklist.
- **API: REST com aninhamento raso.** `docs/api-design.md` define as **convenções** que todo endpoint segue; os endpoints são criados conforme a necessidade, e a lista oficial é o OpenAPI gerado pelo código.
- **Contrato: OpenAPI.** A API em .NET publica a especificação OpenAPI; o cliente TypeScript do Angular é gerado a partir dela (nunca escrito à mão). Outros serviços do ecossistema fazem o mesmo em suas linguagens.

## Estrutura do repositório

Monorepo: backend e frontend no mesmo repositório, cada um com suas ferramentas oficiais (sem Nx).

```
planner/
├── docker-compose.yml   # ambiente de desenvolvimento (hoje: Postgres 18)
├── .env.example         # modelo do .env (segredos locais, fora do Git)
├── docs/data-modeling/  # modelagem do banco, etapa por etapa
├── docs/api-design.md   # convenções da API (estilo, rotas, representações, erros)
├── docs/backend-architecture.md  # camadas, regra da dependência, domínio rico
├── .editorconfig        # estilo de código (lido pelos editores e pelo dotnet format)
├── api/                 # ASP.NET Core: Planner.slnx, global.json, Directory.*.props, src/Planner.{Domain,Application,Infrastructure,Api}, tests/Planner.{Domain.Tests,Infrastructure.IntegrationTests}
└── web/                 # Angular 22 (src/app/api/ = cliente gerado do OpenAPI, não editar à mão)
```

Outras ferramentas do ecossistema vivem em repositórios próprios e consomem o planner apenas pela API.

## Ambiente de desenvolvimento

- Ubuntu 20.04 (sem suporte oficial do .NET 10, mas testado e funcionando).
- .NET SDK 10 em `~/.dotnet` (instalado com `dotnet-install.sh`; atualizar rodando o script de novo).
- `DOTNET_SYSTEM_NET_DISABLEIPV6=1` no `~/.zshrc`: nesta rede, conexões IPv6 do .NET travam (o restore do NuGet ficava parado).
- Docker Engine 28.1.1 + Compose v2 pelo repositório oficial (última versão publicada para o 20.04); usuário no grupo `docker`.
- Node LTS via nvm (`~/.nvm`) e Angular CLI global (`npm install -g @angular/cli`).

## Primeira configuração (máquina nova)

1. `cp .env.example .env` e preencher `POSTGRES_PASSWORD` (ex.: `openssl rand -hex 24`).
2. `docker compose up -d --wait` (na raiz) sobe o Postgres.
3. Em `api/`: `dotnet tool restore` (instala o `dotnet-ef` na versão do projeto).
4. Em `api/`: `dotnet user-secrets set "ConnectionStrings:Planner" "Host=localhost;Port=5432;Database=planner;Username=planner;Password=<a mesma senha>" --project src/Planner.Api`.

## Comandos (backend, dentro de `api/`)

- `dotnet build` — compila; qualquer aviso (incluindo formatação e nulos) quebra o build.
- `dotnet test` — roda os testes (xUnit v3 sobre a Microsoft.Testing.Platform, ativada em `api/global.json`). Os de integração precisam do Docker rodando.
- `dotnet ef migrations add <Nome> --project src/Planner.Infrastructure --output-dir Persistence/Migrations` — gera uma migration a partir do mapeamento (revisar o SQL com `dotnet ef migrations script` antes de aplicar).
- `dotnet ef database update --project src/Planner.Infrastructure --connection "<string de conexão>"` — aplica as migrations pendentes.
- `dotnet format` — corrige a formatação automaticamente; `dotnet format --verify-no-changes` só verifica.
- Versões de pacotes NuGet ficam só em `api/Directory.Packages.props` (os `.csproj` referenciam sem versão).

## Comandos (frontend, dentro de `web/`)

- `npm start` — servidor de desenvolvimento em http://localhost:4200.
- `npm run lint` — ESLint; `npm run format` — Prettier corrige; `npm run format:check` — só verifica.
- `npm test -- --watch=false` — testes (Vitest); `npm run build` — build de produção.

## Decisões adiadas (não esquecer)

O projeto deve seguir as práticas mais profissionais possíveis. Estas decisões foram **adiadas de propósito**, não esquecidas: cada uma tem um **gatilho**. Ao começar uma tarefa que atinja um gatilho, **levantar a decisão com o dono do projeto antes de implementar**. Ao decidir, mover o item para "Decisões tomadas" (ou para o documento correspondente em `docs/`).

| Decisão | Gatilho (decidir antes de...) |
|---|---|
| Ativar o Ubuntu Pro (gratuito para uso pessoal; estende as atualizações de segurança do 20.04 até 2030) | o quanto antes — não bloqueia o código |
| Atualizar o Ubuntu 20.04 → 24.04 (liberar espaço em disco antes: ~17 GB livres) | hospedar o app, ou alguma ferramenta deixar de funcionar no 20.04 |
| Como a Application acessa a persistência (interfaces de repositório por agregado ou uma interface sobre o `DbContext`) e como mapear entidades ↔ DTOs | o primeiro caso de uso |
| "Usuário atual" em desenvolvimento (usuário fixo/semeado até existir autenticação) | o primeiro endpoint que depende do usuário |
| Paginação de listas | um endpoint de lista que possa crescer sem limite (ex.: consultas por prazo) |
| Concorrência otimista (duas abas editando o mesmo card: `updatedAt`/ETag + `If-Match`) | o primeiro endpoint de alteração (`PATCH`) |
| Idempotência de criação (id UUID gerado pelo cliente) | o primeiro endpoint de criação (`POST`) |
| Ferramenta de geração do cliente OpenAPI para o Angular | o Angular chamar a API pela primeira vez |
| CORS / proxy de desenvolvimento entre Angular e API | o Angular chamar a API pela primeira vez |
| CI no GitHub Actions (build, testes, lint e `dotnet ef migrations has-pending-model-changes` a cada push/PR). Gatilho original (existir código com testes) já atingido; adiado pelo dono do projeto para priorizar o MVP | concluir o MVP |
| Fluxo de branches e pull requests | existir código com CI |
| Logs e observabilidade (logs estruturados, correlação de pedidos) | a API rodar fora da máquina de desenvolvimento |
| Backup do banco | existirem dados reais que não podem ser perdidos |
| Versionamento da API | o primeiro cliente além do Angular depender da API |
| Autenticação e autorização. **Requisito já definido: login com a conta do Google** (OpenID Connect). Impacto previsto na modelagem: identificar o usuário pelo id da conta no provedor (não só pelo e-mail), talvez numa tabela de logins externos para permitir outros provedores depois | o app ir para a internet (ou o primeiro cliente externo) |
| Hospedagem, domínio e HTTPS (candidato: VPS com o mesmo Docker Compose; conferir Azure for Students e GitHub Student Developer Pack) | o app ir para a internet |
| Rate limiting (limite de pedidos por cliente) | o app ir para a internet |
| Armazenamento de arquivos de imagem | o conteúdo dos cards aceitar imagens |
| Hook `commit-msg` validando Conventional Commits | commits passarem a ser feitos à mão com frequência |

## Convenções

- **Commits em inglês, no padrão Conventional Commits** (o mesmo do repositório `briel0/sumo-sdk`):
  - Título: `type(scope): summary` — minúsculo, imperativo, sem ponto final, até ~72 caracteres. Ex.: `feat(api): add endpoint to list tasks by date`.
  - Tipos: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `tweak`. Escopos: `api`, `web`, `db`, `docker` etc. (opcional).
  - Corpo: linha em branco após o título; explica o porquê e o que mudou, quebrado em ~72 colunas; listas com `-` quando houver várias mudanças.
  - Sem trailers `Co-Authored-By` nem outras linhas de atribuição.
- Nunca commitar sem aprovação explícita do dono do projeto.
