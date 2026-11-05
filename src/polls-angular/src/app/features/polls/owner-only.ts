import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

/** Shown instead of an owner tab when a participant opens its URL directly (D26, D45); no API call is made. */
@Component({
  selector: 'app-owner-only',
  imports: [RouterLink, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state" role="alert">
      <p>Tylko właściciel ankiety ma dostęp do tej sekcji.</p>
      <a mat-stroked-button routerLink="../answers">Moje odpowiedzi</a>
    </div>
  `,
})
export class OwnerOnly {}
