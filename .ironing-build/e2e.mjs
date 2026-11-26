// E2E: drive a test order through workflow v18 and verify اتو (IRON) sits after پرس (PRESS).
const BASE = 'http://localhost:8080';
const COMPANY = '42f0e459-6095-45bb-aa13-f38126d91d9c';
const BRANCH = '2509b4d4-aef0-4d80-8569-b001c6c41190';
const CUSTOMER = '12000000-0000-0000-0000-000000000001';
const PRODUCT = '66b64de1-5a31-4217-b268-ceead022f9c0';
const VARIANT = 'b3fb01c3-25b1-4baf-9548-5fb1d4656d80';
const WAREHOUSE = '1623ffb7-7374-4598-9b79-fff2832b9fec'; // RAWTEE
const ADMIN_ID = '2226b36f-32bb-405e-aa8a-0be102f9d434';
const HEAT_PRESS_EQUIPMENT = '2bb1a834-058b-4809-bc5e-24e42c1c5f8e';
const IRON_EQUIPMENT = '92500fca-d929-4e5f-902c-528a28f32b5b';
import fs from 'fs';

let TOKEN = fs.readFileSync('D:/00_BARANG/.ironing-build/admin-token.txt', 'utf8').trim();
async function api(method, path, body, extraHeaders) {
  const res = await fetch(BASE + path, {
    method,
    headers: { 'Authorization': `Bearer ${TOKEN}`, 'X-Company-ID': COMPANY, 'Content-Type': 'application/json', ...(extraHeaders || {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null; try { json = text ? JSON.parse(text) : null; } catch { }
  if (!res.ok) { console.error(`FAIL ${method} ${path} -> ${res.status}`, text.slice(0, 400)); process.exit(1); }
  return json;
}
const log = (...a) => console.log(...a);

// ---- 1. Create draft order + item ----
const order = await api('POST', '/api/v1/orders', { branchId: BRANCH, customerId: CUSTOMER, source: 'Internal', orderDate: new Date().toISOString().slice(0, 10) });
const orderId = order.id; log('order draft', orderId);
let version = order.version;
const item = await api('POST', `/api/v1/orders/${orderId}/items`, { productId: PRODUCT, productVariantId: VARIANT, quantity: 2, notes: 'اتو E2E' });
version = item.version;
log('item added', item.id, 'version', version);

// ---- 2. Checkout (gateway mock): prices, approves finance, submits, starts workflow v18, auto-issues raw tee ----
const checkout = await api('POST', `/api/v1/client/orders/${orderId}/checkout-gateway-mock`, { version });
log('checkout ok, order number', checkout.order?.orderNumber ?? checkout.orderNumber, 'version', checkout.order?.version);

// ---- helpers ----
async function currentTasks() {
  const r = await api('GET', '/api/v1/workflows/definition/runtime/tasks/eligible?page=1&pageSize=100');
  return r.items.filter(t => t.orderId === orderId);
}
async function completeTask(taskId, version, resultJson) {
  const claimed = await api('POST', `/api/v1/workflows/definition/runtime/tasks/${taskId}/claim`, { version });
  return api('POST', `/api/v1/workflows/definition/runtime/tasks/${taskId}/complete`, { resultJson: JSON.stringify(resultJson ?? { outcome: 'Approve' }), attachments: [], version: claimed.version });
}
async function completeJob(j) {
  let v = j.version;
  const planned = await api('PUT', `/api/v1/production/jobs/${j.id}/plan`, { priority: 'Normal', plannedStartUtc: null, plannedEndUtc: null, queuePosition: 1, version: v }); v = planned.version;
  const op = await api('PUT', `/api/v1/production/jobs/${j.id}/operator`, { operatorUserId: ADMIN_ID, version: v }); v = op.version;
  if (j.jobType !== 'DesignPreparation') { const eq = await api('PUT', `/api/v1/production/jobs/${j.id}/equipment`, { equipmentId: j.jobType === 'HeatPress' ? HEAT_PRESS_EQUIPMENT : IRON_EQUIPMENT, version: v }); v = eq.version; }
  const started = await api('POST', `/api/v1/production/jobs/${j.id}/start`, { version: v }); v = started.version;
  await api('POST', `/api/v1/production/jobs/${j.id}/complete`, { quantityCompleted: 2, notes: 'e2e', version: v });
  log('  job completed:', j.jobType, j.id.slice(0, 8));
}
async function orderJobs() {
  const r = await api('GET', '/api/v1/production/jobs?page=1&pageSize=100');
  return r.items.filter(j => j.orderId === orderId);
}

// ---- 3. ADMIN task ----
let tasks = await currentTasks();
let t = tasks.find(x => x.nodeKey === 'ADMIN');
await completeTask(t.id, t.version);
log('ADMIN completed');

// ---- 4. FINANCE task ----
tasks = await currentTasks();
t = tasks.find(x => x.nodeKey === 'FINANCE');
await completeTask(t.id, t.version);
log('FINANCE completed');

// ---- 5. PRODUCTION task (generates jobs incl. Ironing) ----
tasks = await currentTasks();
t = tasks.find(x => x.nodeKey === 'PRODUCTION');
await completeTask(t.id, t.version);
log('PRODUCTION completed');
let jobs = await orderJobs();
log('generated jobs:', jobs.map(j => j.jobType).join(', '));
if (!jobs.some(j => j.jobType === 'Ironing')) { console.error('FATAL: no Ironing production job generated'); process.exit(1); }
const ITEM_ID = jobs[0].orderItemId;

// ---- 6. Reserve + issue stock (satisfies warehouse-issue gate for job start) ----
await api('POST', `/api/v1/inventory/orders/${orderId}/reservations`, { warehouseId: WAREHOUSE, idempotencyKey: `e2e-iron-${orderId}` });
await api('POST', `/api/v1/inventory/orders/${orderId}/issues`, { warehouseId: WAREHOUSE, reference: `E2E-IRON:${orderId.slice(0, 8)}`, items: [{ orderItemId: ITEM_ID, quantity: 2, uomCode: 'PCS' }] });
log('stock reserved + issued');

// ---- 7. Complete design jobs then DESIGN task ----
for (const j of jobs.filter(x => x.jobType === 'DesignPreparation')) await completeJob(j);
tasks = await currentTasks();
t = tasks.find(x => x.nodeKey === 'DESIGN');
await completeTask(t.id, t.version);
log('DESIGN completed');

// ---- 8. WAREHOUSE + PRINT tasks (no print ops -> print gate passes trivially) ----
for (const key of ['WAREHOUSE', 'PRINT']) {
  tasks = await currentTasks();
  t = tasks.find(x => x.nodeKey === key);
  if (!t) { console.error(`FATAL: ${key} task not found; open:`, (await currentTasks()).map(x => x.nodeKey)); process.exit(1); }
  await completeTask(t.id, t.version);
  log(key, 'completed');
}

// ---- 9. PRESS: heat press jobs then task ----
jobs = await orderJobs();
for (const j of jobs.filter(x => x.jobType === 'HeatPress')) await completeJob(j);
tasks = await currentTasks();
t = tasks.find(x => x.nodeKey === 'PRESS');
if (!t) { console.error('FATAL: PRESS task not found; open:', tasks.map(x => x.nodeKey)); process.exit(1); }
await completeTask(t.id, t.version);
log('PRESS completed');

// ---- 10. THE KEY CHECK: IRON task must now be open ----
tasks = await currentTasks();
t = tasks.find(x => x.nodeKey === 'IRON');
if (!t) { console.error('FATAL: IRON task did NOT open after PRESS; open:', tasks.map(x => x.nodeKey)); process.exit(1); }
log('*** IRON task OPEN after PRESS *** id', t.id, 'position', t.positionTitle);

// ---- 11. Complete ironing jobs then IRON task -> QUALITY_CONTROL must open ----
jobs = await orderJobs();
for (const j of jobs.filter(x => x.jobType === 'Ironing')) await completeJob(j);
await completeTask(t.id, t.version);
log('IRON completed');
tasks = await currentTasks();
const qc = tasks.find(x => x.nodeKey === 'QUALITY_CONTROL');
if (!qc) { console.error('FATAL: QUALITY_CONTROL did not open after IRON; open:', tasks.map(x => x.nodeKey)); process.exit(1); }
log('*** QUALITY_CONTROL open after IRON — chain verified ***', qc.id);
fs.writeFileSync('D:/00_BARANG/.ironing-build/e2e-order.json', JSON.stringify({ orderId, orderNumber: checkout.order?.orderNumber ?? checkout.orderNumber, workflowInstanceId: t.workflowInstanceId, ironTaskId: t.id, qcTaskId: qc.id }, null, 1));
log('saved e2e-order.json');
