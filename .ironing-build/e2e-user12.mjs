// Verify USER12 (مدیر اتو) sees + claims + completes the IRON cartable task.
const BASE = 'http://localhost:8080';
const COMPANY = '42f0e459-6095-45bb-aa13-f38126d91d9c';
const BRANCH = '2509b4d4-aef0-4d80-8569-b001c6c41190';
const CUSTOMER = '12000000-0000-0000-0000-000000000001';
const PRODUCT = '66b64de1-5a31-4217-b268-ceead022f9c0';
const VARIANT = 'b3fb01c3-25b1-4baf-9548-5fb1d4656d80';
const WAREHOUSE = '1623ffb7-7374-4598-9b79-fff2832b9fec';
const ADMIN_ID = '2226b36f-32bb-405e-aa8a-0be102f9d434';
const HEAT_PRESS_EQUIPMENT = '2bb1a834-058b-4809-bc5e-24e42c1c5f8e';
const IRON_EQUIPMENT = '92500fca-d929-4e5f-902c-528a28f32b5b';
import fs from 'fs';

function makeApi(getToken) {
  return async function api(method, path, body) {
    const res = await fetch(BASE + path, { method, headers: { 'Authorization': `Bearer ${getToken()}`, 'X-Company-ID': COMPANY, 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) });
    const text = await res.text();
    let json = null; try { json = text ? JSON.parse(text) : null; } catch { }
    if (!res.ok) return { __failed: true, status: res.status, code: json?.code, text: text.slice(0, 200) };
    return json;
  };
}
const log = (...a) => console.log(...a);

// login helpers
async function login(username, password) {
  const res = await fetch(BASE + '/api/v1/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username, password }) });
  if (!res.ok) throw new Error('login failed ' + username + ' ' + res.status);
  const j = await res.json();
  return j.accessToken;
}
let adminToken = await login('ADMIN', 'BarangDev@2026');
let user12Token = await login('USER12', 'BarangIron@2026');
log('logged in ADMIN + USER12');
const admin = makeApi(() => adminToken);
const u12 = makeApi(() => user12Token);

const order = await admin('POST', '/api/v1/orders', { branchId: BRANCH, customerId: CUSTOMER, source: 'Internal', orderDate: new Date().toISOString().slice(0, 10) });
const orderId = order.id;
await admin('POST', `/api/v1/orders/${orderId}/items`, { productId: PRODUCT, productVariantId: VARIANT, quantity: 1, notes: 'اتو USER12' });
await admin('POST', `/api/v1/client/orders/${orderId}/checkout-gateway-mock`, { version: 1 });
log('order submitted', orderId);

async function tasks(apiFn) {
  const r = await apiFn('GET', '/api/v1/workflows/definition/runtime/tasks/eligible?page=1&pageSize=100');
  return r.items.filter(t => t.orderId === orderId);
}
async function completeTask(apiFn, taskId, version) {
  const c = await apiFn('POST', `/api/v1/workflows/definition/runtime/tasks/${taskId}/claim`, { version });
  if (c.__failed) return c;
  return apiFn('POST', `/api/v1/workflows/definition/runtime/tasks/${taskId}/complete`, { resultJson: JSON.stringify({ outcome: 'Approve' }), attachments: [], version: c.version });
}
async function jobs() { const r = await admin('GET', '/api/v1/production/jobs?page=1&pageSize=100'); return r.items.filter(j => j.orderId === orderId); }
async function completeJob(j, qty) {
  let v = j.version;
  v = (await admin('PUT', `/api/v1/production/jobs/${j.id}/plan`, { priority: 'Normal', plannedStartUtc: null, plannedEndUtc: null, queuePosition: 1, version: v })).version;
  v = (await admin('PUT', `/api/v1/production/jobs/${j.id}/operator`, { operatorUserId: ADMIN_ID, version: v })).version;
  if (j.jobType !== 'DesignPreparation') v = (await admin('PUT', `/api/v1/production/jobs/${j.id}/equipment`, { equipmentId: j.jobType === 'HeatPress' ? HEAT_PRESS_EQUIPMENT : IRON_EQUIPMENT, version: v })).version;
  v = (await admin('POST', `/api/v1/production/jobs/${j.id}/start`, { version: v })).version;
  await admin('POST', `/api/v1/production/jobs/${j.id}/complete`, { quantityCompleted: qty, notes: 'e2e-user12', version: v });
}

for (const key of ['ADMIN', 'FINANCE']) {
  const t = (await tasks(admin)).find(x => x.nodeKey === key);
  await completeTask(admin, t.id, t.version);
}
const prodT = (await tasks(admin)).find(x => x.nodeKey === 'PRODUCTION');
await completeTask(admin, prodT.id, prodT.version);
let allJobs = await jobs();
const ITEM_ID = allJobs[0].orderItemId;
await admin('POST', `/api/v1/inventory/orders/${orderId}/reservations`, { warehouseId: WAREHOUSE, idempotencyKey: `e2e-u12-${orderId}` });
await admin('POST', `/api/v1/inventory/orders/${orderId}/issues`, { warehouseId: WAREHOUSE, reference: `E2E-U12:${orderId.slice(0, 8)}`, items: [{ orderItemId: ITEM_ID, quantity: 1, uomCode: 'PCS' }] });
for (const j of allJobs.filter(x => x.jobType === 'DesignPreparation')) await completeJob(j, 1);
for (const key of ['DESIGN', 'WAREHOUSE', 'PRINT']) {
  const t = (await tasks(admin)).find(x => x.nodeKey === key);
  await completeTask(admin, t.id, t.version);
}
allJobs = await jobs();
for (const j of allJobs.filter(x => x.jobType === 'HeatPress')) await completeJob(j, 1);
const pressT = (await tasks(admin)).find(x => x.nodeKey === 'PRESS');
await completeTask(admin, pressT.id, pressT.version);
log('walked to after PRESS');

// *** USER12 must now see the IRON task ***
let u12Tasks = await tasks(u12);
let iron = u12Tasks.find(x => x.nodeKey === 'IRON');
if (!iron) { console.error('FATAL: USER12 does not see IRON task. Seen:', u12Tasks.map(x => x.nodeKey)); process.exit(1); }
log('*** USER12 sees IRON task ***', iron.id);

// USER12 claims it
const claimed = await u12('POST', `/api/v1/workflows/definition/runtime/tasks/${iron.id}/claim`, { version: iron.version });
if (claimed.__failed) { console.error('FATAL claim failed', claimed); process.exit(1); }
log('*** USER12 claimed IRON task, version', claimed.version, 'claimant', claimed.claimedByUserId);

// USER12 tries to complete BEFORE ironing jobs are done -> must FAIL (gate)
const premature = await u12('POST', `/api/v1/workflows/definition/runtime/tasks/${iron.id}/complete`, { resultJson: JSON.stringify({ outcome: 'Approve' }), attachments: [], version: claimed.version });
log('premature complete blocked:', premature.__failed ? `yes (${premature.code ?? premature.status})` : 'NO — UNEXPECTED');

// ADMIN completes the ironing production job
allJobs = await jobs();
const ironJobs = allJobs.filter(x => x.jobType === 'Ironing');
log('ironing jobs to complete:', ironJobs.length);
for (const j of ironJobs) await completeJob(j, 1);

// USER12 completes the IRON task -> QUALITY_CONTROL must open
const done = await u12('POST', `/api/v1/workflows/definition/runtime/tasks/${iron.id}/complete`, { resultJson: JSON.stringify({ outcome: 'Approve' }), attachments: [], version: claimed.version });
if (done.__failed) { console.error('FATAL complete failed', done); process.exit(1); }
log('*** USER12 completed IRON task ***');
const qc = (await tasks(admin)).find(x => x.nodeKey === 'QUALITY_CONTROL');
if (!qc) { console.error('FATAL: QUALITY_CONTROL not open after USER12 completed IRON'); process.exit(1); }
log('*** QUALITY_CONTROL open — USER12 flow verified ***');

// Stage filter smoke: stageKey=IRON filter on cartable API
const filtered = await admin('GET', '/api/v1/workflows/definition/runtime/tasks/eligible?stageKey=IRON&page=1&pageSize=10');
log('stageKey=IRON filter total:', filtered.total);

// Definition stage list (what the cartable filter dropdown uses)
const def = await admin('GET', '/api/v1/workflows/definition');
const stages = def.current.nodes.filter(n => n.type === 'UserTask').map(n => `${n.key}(${n.title.fa})`).join(' → ');
log('cartable stage list:', stages);
fs.writeFileSync('D:/00_BARANG/.ironing-build/e2e-user12.json', JSON.stringify({ orderId, ironTaskId: iron.id }, null, 1));
