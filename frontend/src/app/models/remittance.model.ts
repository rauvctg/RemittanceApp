export type RemittanceStatus = 'Pending' | 'Sent' | 'Completed' | 'Cancelled';
export const REMITTANCE_STATUSES: RemittanceStatus[] = ['Pending', 'Sent', 'Completed', 'Cancelled'];

export type IdDocumentType = 'CC' | 'CE' | 'Passport' | 'DNI';
export const ID_DOCUMENT_TYPES: IdDocumentType[] = ['CC', 'CE', 'Passport', 'DNI'];

export interface Remittance {
  id: string;
  userId: string;
  senderName: string;
  senderIdType: IdDocumentType;
  senderIdNumber: string;
  beneficiaryName: string;
  beneficiaryIdType: IdDocumentType;
  beneficiaryIdNumber: string;
  amount: number;
  currency: string;
  status: RemittanceStatus;
  date: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  trackingCode: string;
}

export interface TrackingResult {
  trackingCode: string;
  senderName: string;
  senderIdType: IdDocumentType;
  senderIdNumber: string;
  beneficiaryName: string;
  beneficiaryIdType: IdDocumentType;
  beneficiaryIdNumber: string;
  amount: number;
  currency: string;
  status: RemittanceStatus;
  date: string;
}

export interface CreateRemittanceRequest {
  senderName: string;
  senderIdType: IdDocumentType;
  senderIdNumber: string;
  beneficiaryName: string;
  beneficiaryIdType: IdDocumentType;
  beneficiaryIdNumber: string;
  amount: number;
  currency: string;
}

export interface UpdateRemittanceRequest {
  senderName: string;
  senderIdType: IdDocumentType;
  senderIdNumber: string;
  beneficiaryName: string;
  beneficiaryIdType: IdDocumentType;
  beneficiaryIdNumber: string;
  amount: number;
  currency: string;
  status: RemittanceStatus;
}
