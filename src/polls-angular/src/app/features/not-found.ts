import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state">
      <h1>Nie znaleziono strony</h1>
      <a mat-stroked-button routerLink="/">Strona główna</a>
    </div>
  `,
})
export class NotFound {}
