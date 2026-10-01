/**
 * Formatos dos dados trocados com a API, seguindo docs/api-design.md (camelCase, instantes em ISO 8601 UTC).
 * Na fase 2 estes tipos passam a ser gerados a partir do OpenAPI.
 */

export interface Category {
  id: string;
  name: string;
  /** Posição da aba no rodapé (menor vem antes). */
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}
