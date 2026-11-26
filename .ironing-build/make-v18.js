// Build workflow definition v18: insert IRON (اتو) UserTask + DECIDE_IRON after PRESS.
const fs = require('fs');
const d = require('D:/00_BARANG/.ironing-build/wf-current.json');
const typeInt = { Start: 0, End: 1, UserTask: 2, ExclusiveGateway: 3, ParallelGateway: 4 };
const IRON_POSITION_ID = '39cdcc85-2b94-4dd3-9334-1fb104f28e28';
const SHIFT = 360; // room for IRON + DECIDE_IRON after DECIDE_PRESS (x=2280)

let nodes = d.current.nodes.map(n => ({
  key: n.key,
  type: typeInt[n.type] ?? 2,
  title: n.title,
  description: n.description,
  positionId: n.positionId,
  x: n.x > 2280 ? n.x + SHIFT : n.x, // shift QUALITY_CONTROL..END rightward
  y: n.y,
}));

nodes.push({
  key: 'IRON',
  type: 2,
  title: { fa: 'اتو', en: 'Ironing', ar: 'الكوي' },
  description: { fa: 'اتو پس از پرس، پیش از کنترل کیفیت', en: 'Ironing after press, before quality control', ar: 'الكوي بعد الكبس وقبل مراقبة الجودة' },
  positionId: IRON_POSITION_ID,
  x: 2420,
  y: 300,
});
nodes.push({
  key: 'DECIDE_IRON',
  type: 3,
  title: { fa: 'تصمیم', en: 'Decision', ar: 'قرار' },
  description: { fa: '', en: '', ar: '' },
  positionId: null,
  x: 2600,
  y: 300,
});

let transitions = d.current.transitions.map(t => {
  const copy = { ...t };
  delete copy.id; delete copy.version;
  return copy;
});
// Rewire: DECIDE_PRESS --Approve--> IRON (was QUALITY_CONTROL)
for (const t of transitions) {
  if (t.fromKey === 'DECIDE_PRESS' && t.toKey === 'QUALITY_CONTROL' && t.outcomeCode === 'Approve') t.toKey = 'IRON';
}
const T = (fromKey, toKey, outcomeCode, sourceAnchor, targetAnchor, title) => ({
  fromKey, toKey, fieldCode: '', operator: null, value: null, isDefault: false, sortOrder: 10,
  outcomeCode, sourceAnchor, targetAnchor, title,
});
transitions.push(T('IRON', 'DECIDE_IRON', '', 'RightCenter', 'Left', { fa: '', en: '', ar: '' }));
transitions.push(T('DECIDE_IRON', 'QUALITY_CONTROL', 'Approve', 'Right', 'LeftCenter', { fa: 'تأیید', en: 'Approve', ar: 'موافقة' }));
transitions.push(T('DECIDE_IRON', 'PRESS', 'Reject', 'Top', 'TopCenter', { fa: 'رد', en: 'Reject', ar: 'رفض' }));
transitions.push(T('DECIDE_IRON', 'AUDIT', 'AuditReject', 'Bottom', 'TopCenter', { fa: 'رد ممیزی', en: 'Audit Reject', ar: 'رفض التدقيق' }));

const command = {
  versionId: d.current.id,
  name: d.definition.name,
  changeNote: { fa: 'افزودن مرحله اتو پس از پرس و پیش از کنترل کیفیت', en: 'Add ironing stage after press and before quality control', ar: 'إضافة مرحلة الكوي بعد الكبس وقبل مراقبة الجودة' },
  nodes,
  transitions,
};
fs.writeFileSync('D:/00_BARANG/.ironing-build/wf-v18-draft.json', JSON.stringify(command, null, 1));
console.log('nodes:', nodes.length, 'transitions:', transitions.length);
const order = nodes.slice().sort((a, b) => a.x - b.x).map(n => `${n.key}@${n.x}`).join(' ');
console.log('x-order:', order);
