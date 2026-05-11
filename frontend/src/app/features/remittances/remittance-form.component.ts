import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RemittancesService } from '../../core/remittances.service';
import { ID_DOCUMENT_TYPES, IdDocumentType, REMITTANCE_STATUSES, RemittanceStatus } from '../../models/remittance.model';

const CURRENCIES = ['USD', 'EUR', 'GBP', 'MXN', 'COP', 'VES'];

interface IdRule {
  minLength: number;
  maxLength: number;
  pattern: RegExp;
  hint: string;
}

const ID_RULES: Record<IdDocumentType, IdRule> = {
  CC:       { minLength: 6,  maxLength: 10, pattern: /^\d+$/,            hint: '6 to 10 digits' },
  CE:       { minLength: 6,  maxLength: 7,  pattern: /^\d+$/,            hint: '6 to 7 digits' },
  Passport: { minLength: 6,  maxLength: 12, pattern: /^[A-Za-z0-9]+$/,   hint: '6 to 12 alphanumeric' },
  DNI:      { minLength: 7,  maxLength: 9,  pattern: /^\d+$/,            hint: '7 to 9 digits' }
};

@Component({
  selector: 'app-remittance-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './remittance-form.component.html',
  styleUrl: './remittance-form.component.scss'
})
export class RemittanceFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(RemittancesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);

  readonly statuses = REMITTANCE_STATUSES;
  readonly currencies = CURRENCIES;
  readonly idDocTypes = ID_DOCUMENT_TYPES;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly originalStatus = signal<RemittanceStatus | null>(null);
  readonly availableStatuses = signal<RemittanceStatus[]>(REMITTANCE_STATUSES);
  readonly businessFieldsLocked = signal(false);

  readonly form = this.fb.nonNullable.group({
    senderName: ['', [Validators.required, Validators.maxLength(200)]],
    senderIdType: ['CC' as IdDocumentType, Validators.required],
    senderIdNumber: ['', this.idNumberValidators('CC')],
    beneficiaryName: ['', [Validators.required, Validators.maxLength(200)]],
    beneficiaryIdType: ['CC' as IdDocumentType, Validators.required],
    beneficiaryIdNumber: ['', this.idNumberValidators('CC')],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currency: ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    status: ['Pending' as RemittanceStatus, Validators.required]
  });

  readonly senderIdRule = signal<IdRule>(ID_RULES['CC']);
  readonly beneficiaryIdRule = signal<IdRule>(ID_RULES['CC']);

  constructor() {
    this.form.controls.senderIdType.valueChanges.subscribe(t => this.applyIdRule('sender', t));
    this.form.controls.beneficiaryIdType.valueChanges.subscribe(t => this.applyIdRule('beneficiary', t));
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { return; }

    this.editingId.set(id);
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: r => {
        this.form.patchValue({
          senderName: r.senderName,
          senderIdType: r.senderIdType,
          senderIdNumber: r.senderIdNumber,
          beneficiaryName: r.beneficiaryName,
          beneficiaryIdType: r.beneficiaryIdType,
          beneficiaryIdNumber: r.beneficiaryIdNumber,
          amount: r.amount,
          currency: r.currency,
          status: r.status
        });
        this.applyStatusRules(r.status);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snack.open('Could not load the remittance.', 'Close', { duration: 4000 });
        this.router.navigate(['/remittances']);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) { return; }
    const v = this.form.getRawValue();
    const editingId = this.editingId();

    this.saving.set(true);
    const op$ = editingId
      ? this.api.update(editingId, {
          senderName: v.senderName,
          senderIdType: v.senderIdType,
          senderIdNumber: v.senderIdNumber,
          beneficiaryName: v.beneficiaryName,
          beneficiaryIdType: v.beneficiaryIdType,
          beneficiaryIdNumber: v.beneficiaryIdNumber,
          amount: v.amount,
          currency: v.currency.toUpperCase(),
          status: v.status
        })
      : this.api.create({
          senderName: v.senderName,
          senderIdType: v.senderIdType,
          senderIdNumber: v.senderIdNumber,
          beneficiaryName: v.beneficiaryName,
          beneficiaryIdType: v.beneficiaryIdType,
          beneficiaryIdNumber: v.beneficiaryIdNumber,
          amount: v.amount,
          currency: v.currency.toUpperCase()
        });

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.snack.open(editingId ? 'Remittance updated.' : 'Remittance created.', 'Close', { duration: 3000 });
        this.router.navigate(['/remittances']);
      },
      error: err => {
        this.saving.set(false);
        const msg = err?.error?.detail ?? 'Failed to save the remittance.';
        this.snack.open(msg, 'Close', { duration: 4000 });
      }
    });
  }

  onAmountFocus(ev: FocusEvent): void {
    const input = ev.target as HTMLInputElement;
    if (this.form.controls.amount.value === 0) {
      input.value = '';
      this.form.controls.amount.setValue(null as unknown as number);
    }
  }

  onAmountBlur(ev: FocusEvent): void {
    const input = ev.target as HTMLInputElement;
    if (input.value === '' || this.form.controls.amount.value === null) {
      this.form.controls.amount.setValue(0);
      this.form.controls.amount.markAsUntouched();
      this.form.controls.amount.markAsPristine();
    }
  }

  onAmountInput(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const cleaned = input.value.replace(/^0+(?=\d)/, '');
    if (cleaned !== input.value) {
      input.value = cleaned;
      this.form.controls.amount.setValue(cleaned === '' ? (null as unknown as number) : Number(cleaned));
    }
  }

  private applyStatusRules(currentStatus: RemittanceStatus): void {
    this.originalStatus.set(currentStatus);
    this.availableStatuses.set(this.allowedNextStatuses(currentStatus));

    if (currentStatus !== 'Pending') {
      this.businessFieldsLocked.set(true);
      const businessControls: Array<keyof typeof this.form.controls> = [
        'senderName', 'senderIdType', 'senderIdNumber',
        'beneficiaryName', 'beneficiaryIdType', 'beneficiaryIdNumber',
        'amount', 'currency'
      ];
      for (const name of businessControls) {
        this.form.controls[name].disable({ emitEvent: false });
      }
    } else {
      this.businessFieldsLocked.set(false);
    }
  }

  private allowedNextStatuses(current: RemittanceStatus): RemittanceStatus[] {
    switch (current) {
      case 'Pending': return ['Pending', 'Sent', 'Cancelled'];
      case 'Sent': return ['Sent', 'Completed', 'Cancelled'];
      case 'Completed': return ['Completed'];
      case 'Cancelled': return ['Cancelled'];
      default: return [current];
    }
  }

  private idNumberValidators(type: IdDocumentType): ValidatorFn[] {
    const rule = ID_RULES[type];
    return [
      Validators.required,
      Validators.minLength(rule.minLength),
      Validators.maxLength(rule.maxLength),
      Validators.pattern(rule.pattern)
    ];
  }

  private applyIdRule(side: 'sender' | 'beneficiary', type: IdDocumentType): void {
    const rule = ID_RULES[type];
    const control: AbstractControl = side === 'sender'
      ? this.form.controls.senderIdNumber
      : this.form.controls.beneficiaryIdNumber;
    control.setValidators(this.idNumberValidators(type));
    control.updateValueAndValidity();
    if (side === 'sender') {
      this.senderIdRule.set(rule);
    } else {
      this.beneficiaryIdRule.set(rule);
    }
  }
}
