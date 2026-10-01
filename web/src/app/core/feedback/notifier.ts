import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PlannerDataError } from '../data/planner-data';

/** Mensagens em português para cada código de erro estável da API (docs/api-design.md, seção Errors). */
const MESSAGES: Record<string, string> = {
  'category.invalid-name': 'O nome da categoria precisa ter entre 1 e 100 caracteres.',
  'category.name-taken': 'Já existe uma categoria com esse nome.',
  'not-found': 'Esse item não existe mais.',
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
