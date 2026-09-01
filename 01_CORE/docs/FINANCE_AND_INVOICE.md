# Payments, finance, and invoices

Phase 13 adds operational payment, financial-decision, balance, and invoice foundations. It deliberately excludes accounting ledgers, bank gateways, settlement services, statutory e-invoicing, inventory, purchasing, production, QC, and shipping execution.

## Payments and reversals

An Order may have multiple immutable `Payment` records. Payment method is a technical Lookup value under the global `payment.method` type (`cash`, `bank_transfer`, `card`, `online`, or `other`). An inactive method remains meaningful historically but cannot be selected for a new payment. Amounts use positive `decimal(18,2)` values and must match the finalized Order pricing currency. Reference numbers are company-unique; an identical retry returns the existing payment while conflicting reuse is rejected.

A correction never edits or deletes a completed payment. Reversal creates another immutable Payment row with `RecordType=Reversal`, the original payment ID, positive reversal amount, and mandatory bounded reason. Cumulative reversals cannot exceed the original. A reference number makes reversal retries deterministic. Financial tables have no normal DELETE API or grant.

Overpayment is intentionally allowed and reported rather than silently capped. Underpayment is also allowed. The authoritative balance is derived on every read:

`Net paid = completed payments - reversals`

`Remaining = max(0, finalized Order total - net paid)`

`Overpaid = max(0, net paid - finalized Order total)`

## Financial decision

Each submitted Order has a lazily created mutable `OrderFinancialState`: Pending, Approved, Rejected, or Suspended. `FinancialStatusTransitionService` is the only transition authority. Reject and Suspend require a reason. Mutable state uses optimistic concurrency; stale decisions return HTTP 409. These decisions do not directly implement a later workflow engine.

## Invoices and snapshots

Phase 13 supports one Proforma and one final Invoice per Order. Repeated generation returns the existing record. Proforma requires a submitted Order and finalized price snapshot; final Invoice additionally requires Approved financial state. Numbers come from PostgreSQL `invoice_number_seq` and use `PRO-YYYY-NNNNNNNNNN` or `INV-YYYY-NNNNNNNNNN`. Gaps are acceptable; uniqueness and concurrency safety take priority.

Invoice headers, customer/order identity, monetary breakdown, and item/product pricing data are copied into immutable typed snapshot tables. No client total is accepted. Later Product, Customer, tariff, or pricing changes cannot rewrite an issued invoice.

## Security, history, and integration

Permissions are `finance.read`, `payment.manage`, `payment.reverse`, `finance.approve`, and `invoice.manage`. System administrators receive them idempotently. All six finance tables have EF company filters, same-company foreign keys, and PostgreSQL RLS using transaction-local `barang.company_id`. Pooled connections are covered by integration tests.

`FinanceHistory` is concise business history (`PaymentRegistered`, `PaymentReversed`, finance decisions, and `InvoiceGenerated`). It is separate from Phase 06 technical Audit. Every mutation writes both required history and audit inside the PostgreSQL transaction.

API routes are rooted at `/api/v1/orders/{orderId}/finance` and cover payments, reversals, balance, decisions, invoice generation/listing, and history. There are no DELETE endpoints.

Future accounting or payment-gateway phases may consume these immutable records and invoice snapshots. They must add explicit reconciliation/ledger models rather than mutating Phase 13 history.
