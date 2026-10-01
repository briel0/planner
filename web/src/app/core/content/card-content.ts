import { Extensions, JSONContent, generateHTML } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';

/** Documento do Tiptap (JSON), como fica guardado no campo `content` do cartão. */
export type ContentDoc = JSONContent;

/**
 * O que a descrição de um cartão aceita (versão enxuta): parágrafos, listas com marcadores e numeradas,
 * negrito e itálico. Estas mesmas extensões definem o "formato" do documento, para editar e para exibir.
 */
export const CARD_CONTENT_EXTENSIONS: Extensions = [
  StarterKit.configure({
    heading: false,
    blockquote: false,
    code: false,
    codeBlock: false,
    horizontalRule: false,
    strike: false,
    underline: false,
    link: false,
  }),
];

/** HTML para exibir a descrição na face do cartão (sem criar um editor). */
export function renderContent(doc: ContentDoc): string {
  return generateHTML(doc, CARD_CONTENT_EXTENSIONS);
}

/** Um documento sem nenhum texto conta como "sem descrição" (o cartão volta a ter `content` nulo). */
export function isEmptyContent(doc: ContentDoc | null | undefined): boolean {
  return !doc || !hasText(doc);
}

function hasText(node: ContentDoc): boolean {
  return (node.text?.trim().length ?? 0) > 0 || (node.content ?? []).some(hasText);
}
