import { v7 } from 'uuid';

/**
 * Id para um item novo, gerado no navegador: UUID versão 7, como a API exige. Saber o id antes de salvar torna a
 * criação idempotente (repetir o pedido não duplica) e permite mostrar o item antes da resposta.
 */
export function newId(): string {
  return v7();
}
