import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h3 class="text-2xl font-bold text-gray-800 mb-4">{{getTitle()}}</h3>
      <p class="text-gray-600 leading-relaxed">
        This page is under development. The {{getTitle()}} functionality will be implemented soon.
      </p>

      <!-- Sample Dashboard Cards (shown only on dashboard) -->

    </div>
  `
})
export class PlaceholderComponent {
  constructor(private route: ActivatedRoute) {}

  getTitle(): string {
    const url = this.route.snapshot.url.join('/');
    const titleMap: { [key: string]: string } = {

      'users': 'All Users',
    };
    return titleMap[url] || 'Page';
  }


}
