import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PlannerDataError } from '../data/planner-data';

/** Mensagens em português para cada código de erro estável da API (docs/api-design.md, seção Errors). */
const MESSAGES: Record<string, string> = {
  'category.invalid-name': 'O nome da categoria precisa ter entre 1 e 100 caracteres.',
  'category.name-taken': 'Já existe uma categoria com esse nome.',
  'card.invalid-title': 'O título do cartão precisa ter entre 1 e 200 caracteres.',
  'card.invalid-color': 'Cor inválida.',
  'card.invalid-position': 'Posição inválida.',
  'card.invalid-size': 'Tamanho fora dos limites permitidos.',
  'card.invalid-content': 'Descrição inválida.',
  'card.cycle': 'Não é possível colocar um cartão dentro dele mesmo ou de um cartão que está dentro dele.',
  'card.invalid-location': 'Destino inválido para o cartão.',
  validation: 'Algum dado enviado é inválido.',
  'not-found': 'Esse item não existe mais.',
  'concurrency.stale': 'Isso foi alterado em outro lugar. A versão atual foi carregada; tente de novo.',
  network: 'Sem conexão com o servidor. Confira se a API está rodando.',
};

/** Avisos rápidos no canto da tela (snack bar do Material). */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  error(error: unknown): void {
    const message = error instanceof PlannerDataError ? (MESSAGES[error.code] ?? error.message) : 'Algo deu errado.';
    this.snackBar.open(message, 'OK', { duration: 6000, horizontalPosition: 'end' });
    if (!(error instanceof PlannerDataError)) {
      console.error(error);
    }
  }
}
