const fs = require('fs');
const p = 'D:/00_BARANG/01_CORE/src/BARANG.Backoffice/Components/Pages/Cartable.razor';
let s = fs.readFileSync(p, 'utf8');
const es = {
  WorkloadSummary: 'Resumen de carga', TotalReceived: 'Recibidas', Urgent: 'Urgente', NeedsAttention: 'Requiere atención',
  FollowUpReached: 'Seguimiento vencido', ShortcutFocusSearch: 'Enfocar búsqueda', ShortcutReload: 'Recargar bandeja',
  ShortcutPreview: 'Vista rápida', ShortcutView: 'Abrir detalles', ShortcutEscape: 'Cerrar ventana abierta',
  WorkflowContext: 'Contexto del flujo', WorkflowStepper: 'Progreso del flujo', OperationalWorkspace: 'Acciones operativas',
  Ownership: 'Propiedad', Personal: 'Personal', Priority: 'Prioridad', Unclaimed: 'Sin reclamar',
  ClaimedByMe: 'Reclamada por usted', ClaimedByOther: 'Reclamada por otro usuario', SelectOrder: 'Pedido seleccionado',
  MoreFilters: 'Más filtros', HideFilters: 'Ocultar filtros', Modified: 'Modificado',
  SelectOrderHint: 'Seleccione un pedido para ver su resumen.', SelectedOrder: 'Pedido seleccionado',
  Details: 'Detalles', Files: 'Archivos', History: 'Historial', Messages: 'Mensajes',
  DetailTabs: 'Secciones de detalle del pedido', Decision: 'Decisión', WorkflowStarted: 'Flujo iniciado',
  TaskClaimed: 'Tarea reclamada', TaskReleased: 'Tarea liberada', WorkflowEvent: 'Evento de flujo',
  DecisionAttachments: 'Adjuntos de la decisión', NoFiles: 'No hay archivos disponibles.',
  NoHistory: 'No hay historial de flujo disponible.', NoMessages: 'No hay mensajes de decisión disponibles.',
  Open: 'Abrir', Items: 'Artículos', Total: 'Total', PreviousContext: 'Anterior', CurrentContext: 'Actual',
  PossibleNextContext: 'Siguiente posible', DependsOnDecision: 'Depende de la decisión',
  NoNext: 'Sin destino siguiente', Terminal: 'Flujo completado', Close: 'Cerrar',
};
let count = 0;
for (const [k, v] of Object.entries(es)) {
  const re = new RegExp('\\("en", "' + k + '"\\) => ("(?:[^"\\\\]|\\\\.)*")');
  if (!re.test(s)) { console.error('MISS ' + k); continue; }
  s = s.replace(re, `("en", "${k}") => $1, ("es", "${k}") => "${v}"`);
  count++;
}
// Personal() switch — 4 keys
const personal = { PersonalState: 'Estado personal', Star: 'Destacar', Unstar: 'Quitar destacado', New: 'Nuevo' };
for (const [k, v] of Object.entries(personal)) {
  const re = new RegExp('\\("en", "' + k + '"\\) => ("(?:[^"\\\\]|\\\\.)*")');
  if (!re.test(s)) { console.error('MISS personal ' + k); continue; }
  s = s.replace(re, `("en", "${k}") => $1, ("es", "${k}") => "${v}"`);
  count++;
}
// AgingText: map lowercase category codes to proper resx keys (fixes raw "long-waiting" in every language)
const before = '"Filter" => "WaitingTime", "All" => "AllWaiting", "Normal" => "NormalWaiting", "Attention" => "AttentionWaiting", "LongWaiting" => "LongWaiting", "Longest" => "LongestWaiting", "Shortest" => "ShortestWaiting", "Entered" => "StageEntered", _ => key';
const after = '"Filter" => "WaitingTime", "All" => "AllWaiting", "Normal" => "NormalWaiting", "Attention" => "AttentionWaiting", "LongWaiting" => "LongWaiting", "Longest" => "LongestWaiting", "Shortest" => "ShortestWaiting", "Entered" => "StageEntered", "long-waiting" => "LongWaiting", "attention" => "AttentionWaiting", "normal" => "NormalWaiting", _ => key';
if (s.includes(before)) { s = s.replace(before, after); count++; console.log('aging mapped'); } else console.error('MISS aging');
fs.writeFileSync(p, s);
console.log('patched entries=' + count);
