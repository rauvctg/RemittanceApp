import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatCardModule, MatProgressSpinnerModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './login.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(false);
  readonly errorMsg = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.errorMsg.set(null);

    const payload = this.form.getRawValue();
    this.auth.register(payload).subscribe({
      next: () => {
        // Auto-login after registration for a smoother UX.
        this.auth.login({ email: payload.email, password: payload.password }).subscribe({
          next: () => {
            this.loading.set(false);
            this.snack.open('Welcome!', 'Close', { duration: 3000 });
            this.router.navigate(['/remittances']);
          },
          error: () => {
            this.loading.set(false);
            this.router.navigate(['/login']);
          }
        });
      },
      error: err => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.detail ?? 'Could not register.');
      }
    });
  }
}
