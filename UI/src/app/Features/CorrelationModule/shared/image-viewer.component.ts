import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';

/** Image to show in the full-screen viewer. */
export interface CrViewerImage {
  src: string;
  title?: string;
  subtitle?: string;
}

/**
 * Full-screen image viewer. The image is fitted to the screen (any size or
 * format the browser supports); Esc, the close button or a click on the
 * backdrop closes it.
 */
@Component({
  selector: 'app-cr-image-viewer',
  standalone: true,
  template: `
    <div
      class="fixed inset-0 z-[60] flex flex-col bg-slate-950/90 backdrop-blur-sm"
      (click)="close()"
      role="dialog"
      aria-modal="true"
    >
      <!-- Header -->
      <div class="flex items-start justify-between gap-4 px-4 py-3 sm:px-6" (click)="$event.stopPropagation()">
        <div class="min-w-0 text-white">
          @if (image.subtitle) {
          <p class="text-xs font-medium uppercase tracking-wider text-slate-400">{{ image.subtitle }}</p>
          }
          <h3 class="mt-0.5 break-words text-sm font-semibold sm:text-base">{{ image.title || 'Image' }}</h3>
        </div>
        <button
          type="button"
          (click)="close()"
          class="shrink-0 rounded-lg p-2 text-slate-300 transition hover:bg-white/10 hover:text-white"
          title="Close (Esc)"
        >
          <svg class="h-6 w-6" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Image, fitted to the screen -->
      <div class="relative flex min-h-0 flex-1 items-center justify-center p-3 pb-6 sm:p-6">
        @if (state === 'loading') {
        <svg class="absolute h-8 w-8 animate-spin text-white/70" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
        </svg>
        }
        @if (state === 'error') {
        <div class="text-center text-sm text-slate-300" (click)="$event.stopPropagation()">
          <svg class="mx-auto mb-2 h-10 w-10 text-slate-500" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="m2.25 15.75 5.159-5.159a2.25 2.25 0 0 1 3.182 0l5.159 5.159m-1.5-1.5 1.409-1.409a2.25 2.25 0 0 1 3.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 0 0 1.5-1.5V6a1.5 1.5 0 0 0-1.5-1.5H3.75A1.5 1.5 0 0 0 2.25 6v12a1.5 1.5 0 0 0 1.5 1.5Zm10.5-11.25h.008v.008h-.008V8.25Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z" />
          </svg>
          No image available for this item.
        </div>
        }
        <img
          [src]="image.src"
          [alt]="image.title || 'Image'"
          (load)="state = 'loaded'"
          (error)="state = 'error'"
          (click)="$event.stopPropagation()"
          class="max-h-full max-w-full rounded-lg bg-white object-contain shadow-2xl"
          [class.hidden]="state !== 'loaded'"
        />
      </div>
    </div>
  `,
})
export class ImageViewerComponent implements OnInit, OnChanges, OnDestroy {
  @Input({ required: true }) image!: CrViewerImage;
  @Output() closed = new EventEmitter<void>();

  state: 'loading' | 'loaded' | 'error' = 'loading';

  private previousOverflow = '';

  ngOnInit(): void {
    // Keep the page behind from scrolling while the viewer is open
    this.previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
  }

  ngOnChanges(): void {
    this.state = 'loading';
  }

  ngOnDestroy(): void {
    document.body.style.overflow = this.previousOverflow;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }

  close(): void {
    this.closed.emit();
  }
}
