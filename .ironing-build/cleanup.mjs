// Close leftover mid-flight IRON/ADMIN tasks from earlier failed runs.
const BASE = 'http://localhost:8080';
const COMPANY = '42f0e459-6095-45bb-aa13-f38126d91d9c';
const ADMIN_ID = '2226b36f-32bb-405e-aa8a-0be102f9d434';
async function login(u, p) { const r = await fetch(BASE + '/api/v1/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username: u, password: p }) }); return (await r.json()).accessToken; }
let adminToken = await login('ADMIN', 'BarangDev@2026');
let u12Token = await login('USER12', 'BarangIron@2026');
async function api(tok, method, path, body) {
  const res = await fetch(BASE + path, { method, headers: { Authorization: `Bearer ${tok}`, 'X-Company-ID': COMPANY, 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text(); let j = null; try { j = text ? JSON.parse(text) : null; } catch { }
  return res.ok ? j : { __failed: true, status: res.status, code: j?.code };
}
// leftover order e1995c00: IRON claimed by USER12, ironing job completed -> complete IRON
const r = await api(u12Token, 'GET', '/api/v1/workflows/definition/runtime/tasks/eligible?stageKey=IRON&page=1&pageSize=10');
for (const t of (r.items ?? [])) {
  const c = await api(u12Token, 'POST', `/api/v1/workflows/definition/runtime/tasks/${t.id}/complete`, { resultJson: JSON.stringify({ outcome: 'Approve' }), attachments: [], version: t.version });
  console.log('closed leftover IRON', t.id.slice(0, 8), c.__failed ? 'FAILED ' + c.code : 'ok');
}
// leftover order 7f235f7b: ADMIN task claimed but not completed
const r2 = await api(adminToken, 'GET', '/api/v1/workflows/definition/runtime/tasks/eligible?page=1&pageSize=100');
for (const t of (r2.items ?? []).filter(x => x.claimedByUserId === ADMIN_ID && x.nodeKey === 'ADMIN')) {
  const c = await api(adminToken, 'POST', `/api/v1/workflows/definition/runtime/tasks/${t.id}/complete`, { resultJson: JSON.stringify({ outcome: 'Approve' }), attachments: [], version: t.version });
  console.log('closed leftover ADMIN', t.id.slice(0, 8), c.__failed ? 'FAILED ' + c.code : 'ok (FINANCE now open — expected residue)');
}
