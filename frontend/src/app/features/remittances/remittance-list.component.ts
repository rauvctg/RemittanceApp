import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RemittancesService } from '../../core/remittances.service';
import { Remittance, RemittanceStatus } from '../../models/remittance.model';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';

@Component({
  selector: 'app-remittance-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './remittance-list.component.html',
  styleUrl: './remittance-list.component.scss'
})
export class RemittanceListComponent implements OnInit {
  private readonly api = inject(RemittancesService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly items = signal<Remittance[]>([]);
  readonly displayedColumns = ['date', 'beneficiary', 'amount', 'status', 'tracking', 'actions'];

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: data => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snack.open('Failed to load remittances.', 'Close', { duration: 4000 });
      }
    });
  }

  edit(r: Remittance): void {
    this.router.navigate(['/remittances', r.id, 'edit']);
  }

  canDelete(r: Remittance): boolean {
    return r.status === 'Pending' || r.status === 'Cancelled';
  }

  remove(r: Remittance): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete remittance',
        message: `Delete the remittance to "${r.beneficiaryName}" for ${r.amount} ${r.currency}? This cannot be undone.`,
        confirmLabel: 'Delete'
      }
    });
    ref.afterClosed().subscribe(ok => {
      if (!ok) return;
      this.api.delete(r.id).subscribe({
        next: () => {
          this.snack.open('Remittance deleted.', 'Close', { duration: 3000 });
          this.refresh();
        },
        error: () => this.snack.open('Failed to delete.', 'Close', { duration: 4000 })
      });
    });
  }

  statusColor(status: RemittanceStatus): string {
    switch (status) {
      case 'Completed': return 'primary';
      case 'Sent': return 'accent';
      case 'Cancelled': return 'warn';
      default: return '';
    }
  }
}
