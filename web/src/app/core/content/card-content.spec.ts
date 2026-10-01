import { isEmptyContent, renderContent } from './card-content';

const doc = (...content: object[]) => ({ type: 'doc', content });
const paragraph = (text?: string) => ({ type: 'paragraph', content: text ? [{ type: 'text', text }] : [] });

describe('card content', () => {
  it('treats documents without text as empty', () => {
    expect(isEmptyContent(null)).toBe(true);
    expect(isEmptyContent(doc(paragraph()))).toBe(true);
    expect(isEmptyContent(doc(paragraph('   ')))).toBe(true);
    expect(isEmptyContent(doc(paragraph('Ler o capítulo 4')))).toBe(false);
  });

  it('renders bullet lists and bold text', () => {
    const html = renderContent(
      doc({
        type: 'bulletList',
        content: [
          {
            type: 'listItem',
            content: [{ type: 'paragraph', content: [{ type: 'text', text: 'urgente', marks: [{ type: 'bold' }] }] }],
          },
        ],
      }),
    );

    expect(html).toContain('<ul>');
    expect(html).toContain('<strong>urgente</strong>');
  });
});
