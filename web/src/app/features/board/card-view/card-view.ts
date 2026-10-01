import {
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import { MatIcon } from '@angular/material/icon';
import { CARD_CONTENT_EXTENSIONS, ContentDoc, isEmptyContent, renderContent } from '../../../core/content/card-content';
import { clampSize } from '../../../core/data/card-size';
import { Card, Size } from '../../../core/data/models';
import { readableTextColor } from '../../../shared/ui/color/readable-text-color';

/** Uma ação de teclado pedida sobre o cartão (esquema "explorador de arquivos"). */
export type CardKeyAction = 'open' | 'rename' | 'delete';

/** Um cartão no canvas. Só apresenta e avisa; quem decide o que fazer é o quadro. */
@Component({
  selector: 'app-card-view',
  imports: [MatIcon],
  templateUrl: './card-view.html',
  styleUrl: './card-view.scss',
  host: {
    role: 'button',
    tabindex: '0',
    '[attr.aria-label]': 'card().title',
    '[attr.aria-selected]': 'selected()',
    '[class.selected]': 'selected()',
    '[class.done]': 'card().properties.done === true',
    '[class.light-text]': "textColor() === 'light'",
    '[style.background-color]': 'card().color',
    '[style.width.px]': 'size().width',
    '[style.height.px]': 'size().height',
    '[class.resizing]': 'liveSize() !== null',
    '(click)': 'onClick($event)',
    '(dblclick)': 'onDoubleClick($event)',
    '(contextmenu)': 'onContextMenu($event)',
    '(keydown)': 'onKeydown($event)',
  },
})
export class CardView {
  readonly card = input.required<Card>();
  readonly selected = input(false);
  readonly editing = input(false);
  /** A descrição está sendo editada (no lugar dela aparece o editor). */
  readonly editingDescription = input(false);

  readonly selectedChange = output<void>();
  readonly keyAction = output<CardKeyAction>();
  readonly menuRequested = output<MouseEvent>();
  readonly renamed = output<string>();
  readonly renameCancelled = output<void>();
  readonly resized = output<Size>();
  readonly descriptionEditRequested = output<void>();
  /** Fim da edição da descrição: o documento novo, ou nulo se ficou vazio. */
  readonly descriptionChanged = output<ContentDoc | null>();

  /** Tamanho durante o redimensionamento (antes de soltar); nulo no resto do tempo. */
  protected readonly liveSize = signal<Size | null>(null);
  protected readonly size = computed(() => this.liveSize() ?? this.card().size);

  protected readonly textColor = computed(() => readableTextColor(this.card().color));
  protected readonly due = computed(() => describeDue(this.card().properties.dueOn, this.card().properties.done));

  /** HTML da descrição para exibir; nulo quando o cartão não tem descrição. */
  protected readonly descriptionHtml = computed(() => {
    const content = this.card().content;
    return isEmptyContent(content) ? null : renderContent(content as ContentDoc);
  });

  private readonly titleInput = viewChild<ElementRef<HTMLInputElement>>('titleInput');
  private readonly editorHost = viewChild<ElementRef<HTMLElement>>('editorHost');
  /** O editor existe só enquanto a descrição está sendo editada. */
  private editor: Editor | null = null;

  constructor() {
    effect(() => {
      const input = this.titleInput()?.nativeElement;
      input?.focus();
      input?.select();
    });
    effect(() => {
      const host = this.editorHost()?.nativeElement;
      if (host && !this.editor) {
        this.startEditor(host);
      }
    });
    inject(DestroyRef).onDestroy(() => this.editor?.destroy());
  }

  protected onDescriptionDoubleClick(event: MouseEvent): void {
    event.stopPropagation(); // edita, em vez de abrir o quadro do cartão
    this.descriptionEditRequested.emit();
  }

  private startEditor(host: HTMLElement): void {
    this.editor = new Editor({
      element: host,
      extensions: CARD_CONTENT_EXTENSIONS,
      content: this.card().content ?? '',
      autofocus: 'end',
      editorProps: {
        handleKeyDown: (_view, event) => {
          if (event.key === 'Escape') {
            this.editor?.commands.blur();
            return true;
          }
          return false;
        },
      },
      onBlur: () => this.finishEditor(),
    });
  }

  /** Sair do editor (clicar fora ou Esc) salva. */
  private finishEditor(): void {
    const editor = this.editor;
    if (!editor) {
      return;
    }
    this.editor = null;
    const doc = editor.getJSON();
    editor.destroy();
    this.descriptionChanged.emit(isEmptyContent(doc) ? null : doc);
  }

  protected onClick(event: MouseEvent): void {
    event.stopPropagation(); // não deseleciona pelo canvas
    this.selectedChange.emit();
  }

  protected onDoubleClick(event: MouseEvent): void {
    event.stopPropagation(); // não cria um cartão novo pelo canvas
    if (!this.editing()) {
      this.keyAction.emit('open');
    }
  }

  protected onContextMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedChange.emit();
    this.menuRequested.emit(event);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (this.editing() || this.editingDescription() || (event.target as HTMLElement).isContentEditable) {
      return; // as teclas pertencem ao campo de texto
    }
    const action = KEY_ACTIONS[event.key];
    if (action) {
      event.preventDefault();
      this.keyAction.emit(action);
    }
  }

  /**
   * Arrastar o canto inferior direito redimensiona. O cartão muda de tamanho enquanto o ponteiro se move, e o
   * tamanho final só é avisado (e salvo) ao soltar.
   */
  protected startResize(event: PointerEvent): void {
    event.preventDefault(); // impede o arrastar do cartão inteiro
    event.stopPropagation();
    const handle = event.currentTarget as HTMLElement;
    const start = { x: event.clientX, y: event.clientY, ...this.card().size };
    handle.setPointerCapture(event.pointerId);

    const move = (e: PointerEvent) =>
      this.liveSize.set(
        clampSize({ width: start.width + e.clientX - start.x, height: start.height + e.clientY - start.y }),
      );
    const end = () => {
      handle.removeEventListener('pointermove', move);
      handle.removeEventListener('pointerup', end);
      handle.removeEventListener('pointercancel', end);
      const size = this.liveSize();
      if (size && (size.width !== start.width || size.height !== start.height)) {
        this.resized.emit(size);
      }
      this.liveSize.set(null);
    };
    handle.addEventListener('pointermove', move);
    handle.addEventListener('pointerup', end);
    handle.addEventListener('pointercancel', end);
  }

  protected commit(value: string): void {
    if (this.editing()) {
      this.renamed.emit(value);
    }
  }
}

const KEY_ACTIONS: Record<string, CardKeyAction> = { Enter: 'open', F2: 'rename', Delete: 'delete' };

/** "30/09" e se está atrasado (prazo no passado e não concluído). */
function describeDue(dueOn: string | undefined, done: boolean | undefined) {
  if (!dueOn) {
    return null;
  }
  const [year, month, day] = dueOn.split('-').map(Number);
  const label = new Date(year, month - 1, day).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
  const today = new Date().toLocaleDateString('sv-SE'); // YYYY-MM-DD local
  return { label, overdue: !done && dueOn < today };
}
