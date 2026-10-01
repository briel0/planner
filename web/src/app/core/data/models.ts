/**
 * Formatos dos dados trocados com a API, seguindo docs/api-design.md (camelCase, instantes em ISO 8601 UTC,
 * dias em YYYY-MM-DD). Na fase 2 estes tipos passam a ser gerados a partir do OpenAPI.
 */

export interface Category {
  id: string;
  name: string;
  /** Posição da aba no rodapé (menor vem antes). */
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface Position {
  x: number;
  y: number;
}

/** Tamanho de um cartão, em pixels do canvas (limites em card-size.ts). */
export interface Size {
  width: number;
  height: number;
}

/** Propriedades do catálogo. Uma propriedade ausente significa que o cartão não a tem. */
export interface CardProperties {
  /** Prazo: o dia até o qual o cartão precisa estar pronto (YYYY-MM-DD). */
  dueOn?: string;
  /** Se o cartão está concluído. */
  done?: boolean;
}

/** Representação resumida de um cartão, devolvida pelas listas (um quadro inteiro). */
export interface Card {
  id: string;
  categoryId: string;
  /** Cartão que contém este; nulo quando o cartão está na raiz do quadro da categoria. */
  parentId: string | null;
  title: string;
  /** Cor livre, no formato #rrggbb (minúsculas). */
  color: string;
  position: Position;
  size: Size;
  /** Ordem de sobreposição no quadro: maior fica na frente. */
  layer: number;
  properties: CardProperties;
  /** Quantos cartões existem dentro do quadro deste. */
  childCount: number;
  createdAt: string;
  updatedAt: string;
}

/** Referência curta a um cartão, usada na trilha de navegação. */
export interface CardRef {
  id: string;
  title: string;
}

/** Representação completa de um cartão (GET /cards/{id}). */
export interface CardDetails extends Card {
  /** Os cartões acima deste, da raiz até o pai direto. */
  ancestors: CardRef[];
}

/** Onde um cartão novo é criado: na raiz do quadro de uma categoria, ou dentro de outro cartão. */
export type CardLocation = { categoryId: string } | { parentId: string };

export interface CardChanges {
  title?: string;
  color?: string;
  position?: Position;
  size?: Size;
  properties?: CardProperties;
}
