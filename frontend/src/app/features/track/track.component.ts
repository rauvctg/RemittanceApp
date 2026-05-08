import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RemittancesService } from '../../core/remittances.service';
import { AuthService } from '../../core/auth.service';
import { TrackingResult } from '../../models/remittance.model';

@Component({
  selector: 'app-track',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatCardModule, MatChipsModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './track.component.html',
  styleUrl: './track.component.scss'
})
export class TrackComponent {
  private readonly api = inject(RemittancesService);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(3)]]
  });

  readonly loading = signal(false);
  readonly result = signal<TrackingResult | null>(null);
  readonly errorMsg = signal<string | null>(null);

  search(): void {
    if (this.form.invalid) { return; }
    this.loading.set(true);
    this.result.set(null);
    this.errorMsg.set(null);

    this.api.track(this.form.getRawValue().code.trim()).subscribe({
      next: r => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.errorMsg.set(
          err?.status === 404
            ? 'No remittance found with that tracking code.'
            : 'Could not retrieve the remittance status. Please try again.'
        );
      }
    });
  }

  statusColor(status: string): 'primary' | 'accent' | 'warn' {
    if (status === 'Completed') { return 'primary'; }
    if (status === 'Sent') { return 'accent'; }
    if (status === 'Cancelled') { return 'warn'; }
    return 'accent';
  }
}
