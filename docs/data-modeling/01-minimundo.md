# 01 — Minimundo

Primeira etapa da modelagem clássica do banco de dados (levantamento de requisitos).
Descreve, em linguagem comum, o pedaço da realidade que o banco representa.
As etapas seguintes (modelo conceitual, lógico e físico) derivam deste texto.

## Descrição

O planner é usado por **vários usuários**. Cada usuário tem um **nome** e um **e-mail**, e só enxerga os próprios dados.

Cada usuário organiza seus cartões em **categorias**. Cada categoria pertence a **exatamente um usuário**, tem um **nome** — que não pode se repetir entre as categorias de um mesmo usuário (usuários diferentes podem ter categorias de mesmo nome) — e aparece como uma aba no rodapé da tela, numa **ordem** escolhida pelo usuário. Cada categoria é exibida como **um único quadro**: um canvas livre.

Os quadros contêm **cartões**. Todo cartão tem um **título**, uma **cor**, uma **posição** no quadro onde está e, opcionalmente, um **conteúdo** em texto rico. Ao ser aberto, **todo cartão exibe seu próprio quadro**, que pode conter outros cartões, **sem limite de profundidade**. Todo cartão está em **exatamente um lugar**: no quadro de uma categoria ou no quadro de outro cartão. Um cartão **nunca** pode estar dentro de si mesmo, nem dentro de um cartão que esteja dentro dele. Cartões de um mesmo quadro podem se **sobrepor**, e o usuário escolhe qual fica na frente (a **camada** do cartão).

Um cartão pode ser de **um único tipo**. Uma **tarefa** é um cartão que tem, opcionalmente, uma **data**. Um cartão sem tipo específico é uma **nota**: um cartão genérico, que guarda informação no seu título e conteúdo.

Quando uma categoria é apagada, **todos os seus cartões são apagados junto**. Quando um cartão é apagado, **tudo o que está dentro do seu quadro também é apagado**, em todos os níveis.

## Classificação da especialização

| Propriedade | Valor | Significado |
|---|---|---|
| Disjunção | **Disjunta** | Um cartão tem no máximo um tipo. |
| Completude | **Parcial** | Pode existir cartão sem tipo específico (a nota). |

## Fora do escopo por enquanto

- Compartilhar categorias ou cartões entre usuários.
- Mais de um quadro por categoria (o minimundo diz "inicialmente um único quadro"; o modelo lógico deve facilitar essa mudança).
- Senha e login (dependem da decisão de autenticação).
- Outros tipos de cartão além de tarefa e nota.
