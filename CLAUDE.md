# Planner

Planner pessoal organizado em categorias, com tarefas em quadros e páginas de detalhe no estilo Notion.
Os dados precisam ser consumíveis por outros clientes no futuro (scripts, agentes de IA etc.), não só pela UI.

> Status: fase de concepção. Stack ainda **não decidida** — ver "Decisões em aberto".

## Visão do produto

- **Categorias**: cada categoria é um quadro. Navegação entre categorias por abas no rodapé, como as planilhas de uma pasta de trabalho do Excel.
- **Tarefas**: cartões arrastáveis dentro do quadro da categoria, personalizáveis (cor etc.).
- **Página da tarefa**: clicar num cartão abre uma página própria com conteúdo rico (blocos, estilo Notion).
- **Dados abertos**: o planner é a primeira peça de um futuro ecossistema próprio e integrado de ferramentas. Outros serviços (de qualquer linguagem, incluindo agentes de IA) devem consumir esses dados sem passar pela UI.

## Princípios

- **O contrato é a API, não o banco nem a UI.** Toda leitura e escrita passa por uma camada de domínio/API com schemas validados. A UI, scripts e agentes (ex.: servidor MCP) são clientes dessa camada.
- **Separar dado de domínio e dado de apresentação.** Título, datas e status são domínio. Cor, posição e ícone são apresentação. Um agente precisa do primeiro, não do segundo.
- **Campos estruturados para o que for consultável.** Datas, status e categoria são colunas tipadas, nunca texto solto dentro do conteúdo rico.
- **Conteúdo rico com representação legível.** O conteúdo da página é salvo no formato do editor (JSON), com uma versão derivada em texto/Markdown para consumo por LLMs.
- Tipagem estrita dos dois lados, contrato front/back via OpenAPI, migrations versionadas, testes na camada de domínio.

## Decisões tomadas

- **Banco de dados: PostgreSQL.** Usado no trabalho; suporta vários serviços do ecossistema acessando via API.
- **Quadro: canvas livre.** Cada cartão guarda sua posição (x, y) — dado de apresentação, separado do domínio.
- **Campos da tarefa (v1): título + data.** Novos campos entram incrementalmente, via migrations.
- **Ambiente: Docker.** Serviços (banco, API, web) rodam em containers via Docker Compose; dados do Postgres em volume. Por enquanto roda **só localmente** (nada exposto à internet).
- **Frontend: Angular.** Usado no trabalho; foco em Angular moderno (standalone, signals, nova sintaxe de controle).
- **Backend: C# com ASP.NET Core (.NET).** Usado no trabalho; injeção de dependência nativa.
- **Acesso ao banco: Entity Framework Core** com o provedor `Npgsql.EntityFrameworkCore.PostgreSQL`. Consultas em LINQ; migrations geradas pelo EF Core; SQL manual só em casos pontuais de desempenho.
- **Editor da página da tarefa: Tiptap** (sobre ProseMirror), via `ngx-tiptap`. Conteúdo salvo como JSON do Tiptap (JSONB no Postgres). UI dos blocos (menu `/`, alça de arrastar) construída em componentes Angular. v1 só com parágrafo, título, lista e checklist.
- **Contrato: OpenAPI.** A API em .NET publica a especificação OpenAPI; o cliente TypeScript do Angular é gerado a partir dela (nunca escrito à mão). Outros serviços do ecossistema fazem o mesmo em suas linguagens.

## Estrutura do repositório

Monorepo: backend e frontend no mesmo repositório, cada um com suas ferramentas oficiais (sem Nx).

```
planner/
├── docker-compose.yml   # Postgres + API + web
├── api/                 # ASP.NET Core (Planner.sln, src/, tests/)
└── web/                 # Angular (src/app/api/ = cliente gerado do OpenAPI, não editar à mão)
```

Outras ferramentas do ecossistema vivem em repositórios próprios e consomem o planner apenas pela API.

## Decisões em aberto

- Ferramenta de geração do cliente OpenAPI para o Angular.
- Onde hospedar (candidato: VPS rodando o mesmo Docker Compose; conferir benefícios de estudante: Azure for Students, GitHub Student Developer Pack).
- Autenticação — só se torna obrigatória quando o app for para a internet.

## Convenções

- **Commits em inglês, no padrão Conventional Commits** (o mesmo do repositório `briel0/sumo-sdk`):
  - Título: `type(scope): summary` — minúsculo, imperativo, sem ponto final, até ~72 caracteres. Ex.: `feat(api): add endpoint to list tasks by date`.
  - Tipos: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `tweak`. Escopos: `api`, `web`, `db`, `docker` etc. (opcional).
  - Corpo: linha em branco após o título; explica o porquê e o que mudou, quebrado em ~72 colunas; listas com `-` quando houver várias mudanças.
  - Sem trailers `Co-Authored-By` nem outras linhas de atribuição.
- Nunca commitar sem aprovação explícita do dono do projeto.
