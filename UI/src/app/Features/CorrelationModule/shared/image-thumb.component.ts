import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';

/**
 * Small image preview (lazy loaded). Clicking a loaded image emits "open";
 * when the item has no image a quiet placeholder is shown instead.
 */
@Component({
  selector: 'app-cr-image-thumb',
  standalone: true,
  template: `
    <button
      type="button"
      (click)="state === 'loaded' && open.emit()"
      [disabled]="state !== 'loaded'"
      class="group relative flex shrink-0 items-center justify-center overflow-hidden rounded-lg border border-slate-200 bg-slate-50 transition enabled:hover:border-indigo-300 enabled:hover:shadow-sm"
      [style.width.px]="width"
      [style.height.px]="height"
      [title]="state === 'loaded' ? 'View image' : state === 'error' ? 'No image' : ''"
    >
      <!-- Kept in the layout while loading (opacity, not display:none):
           browsers never fetch a lazy image that has no layout box -->
      <img
        [src]="src"
        [alt]="alt"
        loading="lazy"
        decoding="async"
        (load)="state = 'loaded'"
        (error)="state = 'error'"
        class="h-full w-full object-contain transition-opacity duration-200"
        [class.opacity-0]="state !== 'loaded'"
        [class.hidden]="state === 'error'"
      />
      @if (state === 'loading') {
      <span class="absolute inset-0 animate-pulse bg-slate-100"></span>
      }
      @if (state === 'error') {
      <svg class="h-5 w-5 text-slate-300" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" d="m2.25 15.75 5.159-5.159a2.25 2.25 0 0 1 3.182 0l5.159 5.159m-1.5-1.5 1.409-1.409a2.25 2.25 0 0 1 3.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 0 0 1.5-1.5V6a1.5 1.5 0 0 0-1.5-1.5H3.75A1.5 1.5 0 0 0 2.25 6v12a1.5 1.5 0 0 0 1.5 1.5Z" />
      </svg>
      }
      @if (state === 'loaded') {
      <span class="absolute inset-0 flex items-center justify-center bg-slate-900/0 text-white opacity-0 transition group-hover:bg-slate-900/40 group-hover:opacity-100">
        <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3.75v4.5m0-4.5h4.5m-4.5 0L9 9M3.75 20.25v-4.5m0 4.5h4.5m-4.5 0L9 15M20.25 3.75h-4.5m4.5 0v4.5m0-4.5L15 9m5.25 11.25h-4.5m4.5 0v-4.5m0 4.5L15 15" />
        </svg>
      </span>
      }
    </button>
  `,
})
export class ImageThumbComponent implements OnChanges {
  @Input({ required: true }) src!: string;
  @Input() alt = 'Image';
  @Input() width = 72;
  @Input() height = 56;
  @Output() open = new EventEmitter<void>();

  state: 'loading' | 'loaded' | 'error' = 'loading';

  ngOnChanges(): void {
    this.state = 'loading';
  }
}
