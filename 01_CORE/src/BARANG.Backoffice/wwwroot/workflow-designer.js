/* ─────────────────────────────────────────────────────────────────────────────
   BARANG workflow designer canvas engine (plain JS + SVG, no dependencies).

   Owns everything visual/interactive on the designer canvas:
   - edge routing from REAL node DOM geometry (anchors on node borders, never
     center-to-center), orthogonal (rounded elbows) and bezier styles,
     fan-out spreading along a shared side, parallel-pair perpendicular
     spreading, cheap obstacle avoidance, arrowheads, hover/selected states,
     hover endpoint dots, edge labels
   - smooth client-side node dragging with live edge re-routing (no server
     round trip per pointermove) committed back to Blazor on drop
   - connect gesture from node hover handles, edge endpoint re-anchoring
   - wheel zoom (scale transform), space/middle/pan-tool panning, minimap
   - keyboard: Del (delete w/ confirmation), Ctrl+Z/Y, arrow nudges

   Contract with WorkflowWorkspace.razor (data attributes on the surface):
     data-zoom, data-snap-grid, data-edge-style("orthogonal"|"bezier"),
     data-connect-mode, data-readonly, data-dir
   Server-rendered <path class="wf-edge-src" data-idx data-from data-to
   data-a-from data-a-to data-label data-selected> elements are the data
   source; this engine renders the actual visuals into an overlay <g>.
   Bridge methods (JSInvokable on the page): SelectNodeAsync, SelectEdgeAsync,
   CanvasClickedAsync, BeginNodeDragAsync, CommitNodePositionAsync,
   NudgeSelectionAsync, CommitZoomAsync, ConnectNodesAsync, BeginEdgeEditAsync,
   CommitEdgeAnchorAsync, DeleteSelectionAsync, RequestDeleteEdgeAsync,
   AddNodeAtAsync, UndoAsync, RedoAsync.
   ──────────────────────────────────────────────────────────────────────────── */
(() => {
  'use strict';
  const SVG_NS = 'http://www.w3.org/2000/svg';
  const PARALLEL_GAP = 30;      // px between parallel same-pair edges
  const FAN_MIN_GAP = 26;       // min spread along a shared node side
  const DRAG_THRESHOLD = 3;     // px before a pointerdown becomes a drag
  const ANCHOR_SNAP = 90;       // px snap radius for edge endpoint re-anchoring

  const svgEl = (name, attrs) => {
    const el = document.createElementNS(SVG_NS, name);
    if (attrs) for (const k in attrs) el.setAttribute(k, attrs[k]);
    return el;
  };
  const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
  const fmt = (n) => (Math.round(n * 100) / 100).toString();

  function mount(viewport, dotnet) {
    if (!viewport || !dotnet) return () => { };
    const state = {
      viewport, dotnet, surface: viewport.querySelector('.workflow-canvas-surface'),
      sizer: null, svg: null, overlay: null, labels: null, temp: null,
      minimap: null, mmNodes: null, mmView: null,
      scale: 1, worldW: 0, worldH: 0,
      observer: null, raf: 0, disposed: false, autofitDone: false,
      drag: null, connect: null, anchorDrag: null, pan: null,
      spaceDown: false, panTool: false,
      invoke(method, ...args) {
        try { return this.dotnet.invokeMethodAsync(method, ...args); } catch (e) { /* circuit gone */ }
      },
    };
    if (!state.surface) return () => { };

    /* ── DOM scaffolding: viewport > sizer > world(surface) ────────────────── */
    const world = state.surface;
    world.classList.add('wf-world');
    const sizer = document.createElement('div');
    sizer.className = 'wf-sizer';
    world.parentNode.insertBefore(sizer, world);
    sizer.appendChild(world);
    state.sizer = sizer;

    /* svg defs for arrowheads */
    const defs = svgEl('defs');
    defs.innerHTML =
      '<marker id="wfd-arrow" viewBox="0 0 10 10" markerWidth="9" markerHeight="9" refX="8.5" refY="5" orient="auto-start-reverse" markerUnits="userSpaceOnUse">' +
      '<path d="M0,1.2 L9,5 L0,8.8 L2.4,5 z" class="wfd-arrow-fill"/></marker>' +
      '<marker id="wfd-arrow-hot" viewBox="0 0 10 10" markerWidth="10" markerHeight="10" refX="8.5" refY="5" orient="auto-start-reverse" markerUnits="userSpaceOnUse">' +
      '<path d="M0,1 L9.6,5 L0,9 L2.6,5 z" class="wfd-arrow-fill-hot"/></marker>' +
      '<marker id="wfd-arrow-sel" viewBox="0 0 10 10" markerWidth="10" markerHeight="10" refX="8.5" refY="5" orient="auto-start-reverse" markerUnits="userSpaceOnUse">' +
      '<path d="M0,1 L9.6,5 L0,9 L2.6,5 z" class="wfd-arrow-fill-sel"/></marker>';
    world.appendChild(defs);

    state.svg = world.querySelector('svg.workflow-connections') || (() => {
      const s = svgEl('svg', { class: 'workflow-connections', width: '100%', height: '100%' });
      world.insertBefore(s, world.firstChild);
      return s;
    })();
    state.overlay = svgEl('g', { class: 'wf-overlay' });
    state.labels = svgEl('g', { class: 'wf-labels' });
    state.svg.appendChild(state.overlay);
    state.svg.appendChild(state.labels);
    state.temp = svgEl('path', { class: 'wf-temp-edge', d: '', fill: 'none' });
    state.svg.appendChild(state.temp);
    state.temp.style.display = 'none';

    /* ── geometry helpers ─────────────────────────────────────────────────── */
    const nodeEls = () => [...world.querySelectorAll('.workflow-node')];
    const readNodes = () => {
      const map = new Map();
      for (const el of nodeEls()) {
        const key = el.getAttribute('data-node-key');
        if (!key) continue;
        map.set(key, {
          key, el,
          x: el.offsetLeft, y: el.offsetTop, w: el.offsetWidth, h: el.offsetHeight,
          type: el.getAttribute('data-node-type') || 'node-usertask',
          cx: el.offsetLeft + el.offsetWidth / 2, cy: el.offsetTop + el.offsetHeight / 2,
        });
      }
      return map;
    };
    const normalizeSide = (a) => a === 'Auto' || !a ? null
      : a.toLowerCase().startsWith('top') ? 'top'
        : a.toLowerCase().startsWith('bottom') ? 'bottom'
          : a.toLowerCase().startsWith('left') ? 'left'
            : a.toLowerCase().startsWith('right') ? 'right' : null;
    const autoSide = (from, to) => {
      const dx = to.cx - from.cx, dy = to.cy - from.cy;
      if (Math.abs(dx) >= Math.abs(dy) * 0.9) return dx >= 0 ? 'right' : 'left';
      return dy >= 0 ? 'bottom' : 'top';
    };
    const sidePoint = (n, side, t) => {
      const tt = clamp(t, 0.06, 0.94);
      switch (side) {
        case 'top': return [n.x + n.w * tt, n.y];
        case 'bottom': return [n.x + n.w * tt, n.y + n.h];
        case 'left': return [n.x, n.y + n.h * tt];
        default: return [n.x + n.w, n.y + n.h * tt];
      }
    };
    const segHitsRect = (x1, y1, x2, y2, r) => {
      // Liang-Barsky clip of segment against rect
      let t0 = 0, t1 = 1; const dx = x2 - x1, dy = y2 - y1;
      const p = [-dx, dx, -dy, dy], q = [x1 - r.x, r.x + r.w - x1, y1 - r.y, r.y + r.h - y1];
      for (let i = 0; i < 4; i++) {
        if (p[i] === 0) { if (q[i] < 0) return false; }
        else {
          const t = q[i] / p[i];
          if (p[i] < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
          else { if (t < t0) return false; if (t < t1) t1 = t; }
        }
      }
      return true;
    };
    const obstaclesBetween = (nodes, a, b, x1, y1, x2, y2) => {
      for (const n of nodes.values()) {
        if (n === a || n === b) continue;
        if (segHitsRect(x1, y1, x2, y2, n)) return n;
      }
      return null;
    };
    const roundedPath = (pts, r) => {
      let d = `M ${fmt(pts[0][0])} ${fmt(pts[0][1])}`;
      for (let i = 1; i < pts.length - 1; i++) {
        const [px, py] = pts[i - 1], [cx, cy] = pts[i], [nx, ny] = pts[i + 1];
        const d1 = Math.hypot(cx - px, cy - py), d2 = Math.hypot(nx - cx, ny - cy);
        const rr = Math.min(r, d1 / 2, d2 / 2);
        if (rr < 2) { d += ` L ${fmt(cx)} ${fmt(cy)}`; continue; }
        const p1 = [cx - (cx - px) / d1 * rr, cy - (cy - py) / d1 * rr];
        const p2 = [cx + (nx - cx) / d2 * rr, cy + (ny - cy) / d2 * rr];
        d += ` L ${fmt(p1[0])} ${fmt(p1[1])} Q ${fmt(cx)} ${fmt(cy)} ${fmt(p2[0])} ${fmt(p2[1])}`;
      }
      const last = pts[pts.length - 1];
      d += ` L ${fmt(last[0])} ${fmt(last[1])}`;
      return d;
    };

    /* ── edge model from server-rendered source paths ─────────────────────── */
    const readEdges = (nodes) => {
      const list = [];
      for (const p of state.svg.querySelectorAll('path.wf-edge-src')) {
        const from = nodes.get(p.getAttribute('data-from'));
        const to = nodes.get(p.getAttribute('data-to'));
        if (!from || !to) continue;
        list.push({
          idx: p.getAttribute('data-idx'),
          from, to,
          aFrom: normalizeSide(p.getAttribute('data-a-from')),
          aTo: normalizeSide(p.getAttribute('data-a-to')),
          label: p.getAttribute('data-label') || '',
          selected: p.getAttribute('data-selected') === 'true',
          el: p,
        });
      }
      return list;
    };

    /* ── routing: sides, fan spread, parallel spread, path build ──────────── */
    const layoutEdges = (nodes, edges) => {
      // fan spread: group by (nodeKey, side)
      const fanFrom = new Map(), fanTo = new Map();
      const pairCount = new Map();
      for (const e of edges) {
        e.sFrom = e.aFrom || autoSide(e.from, e.to);
        e.sTo = e.aTo || autoSide(e.to, e.from);
        const kf = e.from.key + '|' + e.sFrom, kt = e.to.key + '|' + e.sTo;
        fanFrom.set(kf, (fanFrom.get(kf) || 0) + 1);
        fanTo.set(kt, (fanTo.get(kt) || 0) + 1);
        const pk = [e.from.key, e.to.key].sort().join('→');
        pairCount.set(pk, (pairCount.get(pk) || 0) + 1);
      }
      const fanSeenFrom = new Map(), fanSeenTo = new Map(), pairSeen = new Map();
      for (const e of edges) {
        const kf = e.from.key + '|' + e.sFrom, kt = e.to.key + '|' + e.sTo;
        const iF = fanSeenFrom.get(kf) || 0, iT = fanSeenTo.get(kt) || 0;
        fanSeenFrom.set(kf, iF + 1); fanSeenTo.set(kt, iT + 1);
        const nF = fanFrom.get(kf), nT = fanTo.get(kt);
        // fan t position — center single edges, spread multiples (min gap enforced by clamped t)
        e.tFrom = nF > 1 ? (iF + 1) / (nF + 1) : 0.5;
        e.tTo = nT > 1 ? (iT + 1) / (nT + 1) : 0.5;

        const pk = [e.from.key, e.to.key].sort().join('→');
        const iP = pairSeen.get(pk) || 0; pairSeen.set(pk, iP + 1);
        const nP = pairCount.get(pk);
        const slot = iP - (nP - 1) / 2;                    // -1, 0, +1 …
        e.parallel = nP > 1;
        if (e.parallel) { e.tFrom = clamp(0.5 + slot * 0.15, 0.15, 0.85); e.tTo = clamp(0.5 + slot * 0.15, 0.15, 0.85); }
        e.off = nP > 1 || nF > 1 || nT > 1 ? slot * PARALLEL_GAP : 0;
      }

      for (const e of edges) {
        const p0 = sidePoint(e.from, e.sFrom, e.tFrom);
        const p3 = sidePoint(e.to, e.sTo, e.tTo);
        const dx = p3[0] - p0[0], dy = p3[1] - p0[1];
        const dist = Math.hypot(dx, dy) || 1;
        // perpendicular direction (of the overall flow) for pair offsetting
        const ux = dx / dist, uy = dy / dist;
        const px = -uy, py = ux;
        const horizontal = e.sFrom === 'right' || e.sFrom === 'left';

        if (state.edgeStyle === 'bezier') {
          let off = e.off;
          if (!e.parallel && obstaclesBetween(nodes, e.from, e.to, p0[0], p0[1], p3[0], p3[1])) off += 46 * (e.idx % 2 === 0 ? 1 : -1);
          const len = clamp(dist * 0.42, 46, 240);
          const f = sideNormal(e.sFrom), t = sideNormal(e.sTo);
          const c1 = [p0[0] + f[0] * len + px * off * 0.7, p0[1] + f[1] * len + py * off * 0.7];
          const c2 = [p3[0] + t[0] * len + px * off * 0.7, p3[1] + t[1] * len + py * off * 0.7];
          e.d = `M ${fmt(p0[0])} ${fmt(p0[1])} C ${fmt(c1[0])} ${fmt(c1[1])}, ${fmt(c2[0])} ${fmt(c2[1])}, ${fmt(p3[0])} ${fmt(p3[1])}`;
          e.mid = cubicAt(p0, c1, c2, p3, 0.5);
        } else {
          // orthogonal elbow with exit leads, obstacle-aware mid placement and
          // perpendicular parallel-offset that keeps every segment axis-aligned
          let pts;
          let midOff = e.off;
          const sgn = Math.sign(dx || 1);
          const lead = clamp(Math.abs(dx) / 3, 18, 44);
          for (let attempt = 0; attempt < 5; attempt++) {
            if (horizontal) {
              const mx = (p0[0] + p3[0]) / 2;
              const y0 = p0[1] + midOff, y3 = p3[1] + midOff;
              pts = [[p0[0], p0[1]], [p0[0] + lead * sgn, p0[1]], [p0[0] + lead * sgn, y0], [mx, y0], [mx, y3], [p3[0] - lead * sgn, y3], [p3[0] - lead * sgn, p3[1]], [p3[0], p3[1]]];
              const hit = obstaclesBetween(nodes, e.from, e.to, mx, Math.min(y0, y3), mx, Math.max(y0, y3));
              if (!hit) break;
              midOff += (hit.cx >= mx ? -1 : 1) * (Math.abs(hit.cx - mx) + hit.w / 2 + PARALLEL_GAP + 14);
            } else {
              const my = (p0[1] + p3[1]) / 2;
              const sgnY = Math.sign(dy || 1);
              const leadY = clamp(Math.abs(dy) / 3, 18, 44);
              const x0 = p0[0] + midOff, x3 = p3[0] + midOff;
              pts = [[p0[0], p0[1]], [p0[0], p0[1] + leadY * sgnY], [x0, p0[1] + leadY * sgnY], [x0, my], [x3, my], [x3, p3[1] - leadY * sgnY], [p3[0], p3[1] - leadY * sgnY], [p3[0], p3[1]]];
              const hit = obstaclesBetween(nodes, e.from, e.to, Math.min(x0, x3), my, Math.max(x0, x3), my);
              if (!hit) break;
              midOff += (hit.cy >= my ? -1 : 1) * (Math.abs(hit.cy - my) + hit.h / 2 + PARALLEL_GAP + 14);
            }
          }
          e.d = roundedPath(pts, 9);
          const n = pts.length;
          e.mid = [(pts[Math.floor(n / 2) - 1][0] + pts[Math.floor(n / 2)][0]) / 2, (pts[Math.floor(n / 2) - 1][1] + pts[Math.floor(n / 2)][1]) / 2];
        }
        e.p0 = p0; e.p3 = p3;
      }
    };
    const sideNormal = (s) => s === 'right' ? [1, 0] : s === 'left' ? [-1, 0] : s === 'bottom' ? [0, 1] : [0, -1];
    const cubicAt = (p0, c1, c2, p3, t) => {
      const mt = 1 - t;
      const x = mt * mt * mt * p0[0] + 3 * mt * mt * t * c1[0] + 3 * mt * t * t * c2[0] + t * t * t * p3[0];
      const y = mt * mt * mt * p0[1] + 3 * mt * mt * t * c1[1] + 3 * mt * t * t * c2[1] + t * t * t * p3[1];
      return [x, y];
    };

    /* ── overlay rendering (reuses groups by edge idx) ─────────────────────── */
    const groups = new Map();
    const renderOverlay = (edges) => {
      const seen = new Set();
      for (const e of edges) {
        seen.add(e.idx);
        let g = groups.get(e.idx);
        if (!g || !state.overlay.contains(g)) {
          g = buildEdgeGroup(e.idx);
          groups.set(e.idx, g);
          state.overlay.appendChild(g);
        }
        g.__wf = e;
        g.hit.setAttribute('d', e.d);
        g.vis.setAttribute('d', e.d);
        g.vis.classList.toggle('selected', e.selected);
        // endpoint dots (visible on hover/selected via CSS)
        g.d1.setAttribute('cx', fmt(e.p0[0])); g.d1.setAttribute('cy', fmt(e.p0[1]));
        g.d2.setAttribute('cx', fmt(e.p3[0])); g.d2.setAttribute('cy', fmt(e.p3[1]));
        // hover delete badge at path midpoint
        g.del.setAttribute('transform', `translate(${fmt(e.mid[0])},${fmt(e.mid[1])})`);
        g.del.style.display = state.readOnly ? 'none' : '';
        // label
        if (e.label) {
          g.label.textContent = e.label;
          g.labelG.style.display = '';
          positionLabel(g, e);
        } else g.labelG.style.display = 'none';
      }
      for (const [idx, g] of groups) {
        if (!seen.has(idx)) { g.remove(); groups.delete(idx); }
      }
    };
    const positionLabel = (g, e) => {
      const mid = e.mid;
      g.labelG.setAttribute('transform', `translate(${fmt(mid[0])},${fmt(mid[1])})`);
    };
    const buildEdgeGroup = (idx) => {
      const g = svgEl('g', { class: 'wf-edge-g', 'data-idx': idx });
      const hit = svgEl('path', { class: 'wf-edge-hit', d: '' });
      const vis = svgEl('path', { class: 'wf-edge', d: '' });
      const d1 = svgEl('circle', { class: 'wf-end-dot wf-end-from', r: 4.5 });
      const d2 = svgEl('circle', { class: 'wf-end-dot wf-end-to', r: 4.5 });
      const labelG = svgEl('g', { class: 'wf-edge-labelg' });
      const labelRect = svgEl('rect', { class: 'wf-edge-labelrect', x: 0, y: 0, rx: 4 });
      const label = svgEl('text', { class: 'wf-edge-lbl', 'text-anchor': 'middle', y: -8 });
      labelG.appendChild(labelRect); labelG.appendChild(label);
      const del = svgEl('g', { class: 'wf-edge-del', role: 'button', 'aria-label': 'delete edge' });
      del.innerHTML = '<circle r="9"/><path d="M-3.4,-3.4 L3.4,3.4 M3.4,-3.4 L-3.4,3.4"/>';
      g.appendChild(hit); g.appendChild(vis); g.appendChild(d1); g.appendChild(d2);
      g.appendChild(labelG); g.appendChild(del);
      g.hit = hit; g.vis = vis; g.d1 = d1; g.d2 = d2; g.labelG = labelG; g.label = label; g.labelRect = labelRect; g.del = del;
      // interactions
      hit.addEventListener('pointerenter', () => g.classList.add('hot'));
      hit.addEventListener('pointerleave', () => g.classList.remove('hot'));
      hit.addEventListener('click', (ev) => { ev.stopPropagation(); state.invoke('SelectEdgeAsync', Number(idx)); });
      del.addEventListener('click', (ev) => { ev.stopPropagation(); state.invoke('RequestDeleteEdgeAsync', Number(idx)); });
      d1.addEventListener('pointerdown', (ev) => startAnchorDrag(ev, Number(idx), true));
      d2.addEventListener('pointerdown', (ev) => startAnchorDrag(ev, Number(idx), false));
      // size the label rect after text lands
      requestAnimationFrame(() => {
        try {
          const bb = label.getBBox();
          labelRect.setAttribute('x', bb.x - 5); labelRect.setAttribute('y', bb.y - 2.5);
          labelRect.setAttribute('width', bb.width + 10); labelRect.setAttribute('height', bb.height + 5);
        } catch (e) { /* not rendered */ }
      });
      return g;
    };

    /* ── scale / zoom ─────────────────────────────────────────────────────── */
    const applyScale = (s, anchorClient) => {
      s = clamp(s, 0.4, 2.5);
      const vp = state.viewport;
      let ax = null, ay = null;
      if (anchorClient) {
        const r = vp.getBoundingClientRect();
        ax = anchorClient[0] - r.left + vp.scrollLeft;
        ay = anchorClient[1] - r.top + vp.scrollTop;
      }
      const old = state.scale;
      state.scale = s;
      world.style.transform = `scale(${s})`;
      world.style.transformOrigin = '0 0';
      measureWorld();
      sizer.style.width = Math.max(state.worldW * s, vp.clientWidth) + 'px';
      sizer.style.height = Math.max(state.worldH * s, vp.clientHeight) + 'px';
      if (ax !== null) {
        const k = s / old;
        vp.scrollLeft = ax * k - (ax - vp.scrollLeft);
        vp.scrollTop = ay * k - (ay - vp.scrollTop);
      }
      updateMinimap();
    };
    const measureWorld = () => {
      // world content bounds + server-provided minimums
      let maxX = 400, maxY = 300;
      for (const el of nodeEls()) maxX = Math.max(maxX, el.offsetLeft + el.offsetWidth + 160), maxY = Math.max(maxY, el.offsetTop + el.offsetHeight + 160);
      state.worldW = maxX; state.worldH = maxY;
      world.style.minWidth = ''; world.style.minHeight = '';
      world.style.width = maxX + 'px'; world.style.height = maxY + 'px';
    };

    /* ── minimap ──────────────────────────────────────────────────────────── */
    const buildMinimap = () => {
      const mm = document.createElement('div');
      mm.className = 'wf-minimap';
      mm.innerHTML = '<div class="wf-mm-canvas"><div class="wf-mm-nodes"></div><div class="wf-mm-view"></div></div>';
      state.viewport.parentNode.appendChild(mm);
      state.minimap = mm;
      state.mmNodes = mm.querySelector('.wf-mm-nodes');
      state.mmView = mm.querySelector('.wf-mm-view');
      let dragging = false;
      const jump = (ev) => {
        const r = state.mmNodes.getBoundingClientRect();
        const t = clamp((ev.clientX - r.left) / r.width, 0, 1), u = clamp((ev.clientY - r.top) / r.height, 0, 1);
        const s = state.scale;
        state.viewport.scrollLeft = t * state.worldW * s - state.viewport.clientWidth / 2;
        state.viewport.scrollTop = u * state.worldH * s - state.viewport.clientHeight / 2;
        updateMinimap();
      };
      mm.addEventListener('pointerdown', (ev) => { dragging = true; try { mm.setPointerCapture(ev.pointerId); } catch (e) { } jump(ev); });
      mm.addEventListener('pointermove', (ev) => { if (dragging) jump(ev); });
      mm.addEventListener('pointerup', () => { dragging = false; });
    };
    const updateMinimap = () => {
      if (!state.minimap) return;
      const nodes = readNodes();
      const W = state.worldW || 1, H = state.worldH || 1;
      const kx = 100 / W, ky = 100 / H;
      let html = '';
      for (const n of nodes.values()) html += `<i class="${cssType(n.type)}" style="left:${(n.x * kx).toFixed(2)}%;top:${(n.y * ky).toFixed(2)}%;width:${Math.max(1, n.w * kx).toFixed(2)}%;height:${Math.max(1, n.h * ky).toFixed(2)}%"></i>`;
      state.mmNodes.innerHTML = html;
      const vp = state.viewport, s = state.scale;
      state.mmView.style.left = (vp.scrollLeft / (W * s) * 100) + '%';
      state.mmView.style.top = (vp.scrollTop / (H * s) * 100) + '%';
      state.mmView.style.width = clamp(vp.clientWidth / (W * s) * 100, 0, 100) + '%';
      state.mmView.style.height = clamp(vp.clientHeight / (H * s) * 100, 0, 100) + '%';
    };
    const cssType = (t) => t || 'node-usertask';

    /* ── relayout pipeline ────────────────────────────────────────────────── */
    const syncFromServer = () => {
      const flag = (name) => {
        const v = world.getAttribute(name);
        return v != null && v.toLowerCase() === 'true';
      };
      state.readOnly = flag('data-readonly');
      state.connectMode = flag('data-connect-mode');
      state.edgeStyle = world.getAttribute('data-edge-style') === 'bezier' ? 'bezier' : 'orthogonal';
      state.snap = flag('data-snap-grid') ? 20 : 0;
      state.dir = world.getAttribute('data-dir') || 'ltr';
      const z = parseFloat(world.getAttribute('data-zoom') || '1') || 1;
      // Only adopt the server zoom when it actually CHANGED server-side; otherwise a
      // client wheel-zoom (scale applied instantly, data-zoom attribute still stale)
      // would be reverted by the observer relayout before the debounced commit lands.
      if (z !== state.lastServerZoom) { state.lastServerZoom = z; applyScale(z); }
      world.classList.toggle('grid-visible', state.snap > 0);
    };
    const relayout = () => {
      if (state.disposed) return;
      syncFromServer();
      const nodes = readNodes();
      const edges = readEdges(nodes);
      layoutEdges(nodes, edges);
      renderOverlay(edges);
      measureWorld();
      sizer.style.width = Math.max(state.worldW * state.scale, state.viewport.clientWidth) + 'px';
      sizer.style.height = Math.max(state.worldH * state.scale, state.viewport.clientHeight) + 'px';
      updateMinimap();
      if (!state.autofitDone && nodes.size > 0) {
        state.autofitDone = true;
        const need = Math.max(state.worldW / state.viewport.clientWidth, state.worldH / state.viewport.clientHeight);
        if (need > 1.04) {
          const z = clamp(Math.min(1 / need, state.scale), 0.55, 1);
          applyScale(z);
          clearTimeout(state._fitT);
          state._fitT = setTimeout(() => state.invoke('CommitZoomAsync', z), 400);
        }
      }
    };
    const scheduleRelayout = () => {
      if (state.raf) return;
      state.raf = requestAnimationFrame(() => { state.raf = 0; relayout(); });
    };
    const isOwnLayer = (t) => t && (t.classList?.contains('wf-temp-edge') || !!t.closest?.('.wf-overlay') || !!t.closest?.('.wf-labels') || !!t.closest?.('defs') || !!t.closest?.('.wf-minimap'));
    const onMutation = (records) => {
      for (const m of records) {
        if (isOwnLayer(m.target)) continue;
        if (m.type === 'childList' && m.addedNodes.length + m.removedNodes.length === 0) continue;
        if (m.type === 'childList') {
          let relevant = false;
          for (const n of [...m.addedNodes, ...m.removedNodes]) if (n.nodeType === 1 && !isOwnLayer(n)) { relevant = true; break; }
          if (!relevant) continue;
        }
        scheduleRelayout();
        return;
      }
    };
    state.observer = new MutationObserver(onMutation);
    state.observer.observe(world, { childList: true, subtree: true, attributes: true, attributeFilter: ['style', 'class', 'data-zoom', 'data-readonly', 'data-connect-mode', 'data-edge-style', 'data-snap-grid', 'data-selected', 'data-a-from', 'data-a-to', 'data-label'] });

    /* ── screen<->world conversion ────────────────────────────────────────── */
    const toWorld = (clientX, clientY) => {
      const r = world.getBoundingClientRect();
      return [(clientX - r.left) / state.scale, (clientY - r.top) / state.scale];
    };

    /* ── node dragging (client-side, committed on drop) ───────────────────── */
    const startNodeDrag = (ev, nodeEl) => {
      if (state.readOnly || state.connectMode || state.panTool || state.spaceDown) return;
      if (ev.button !== 0) return;
      const key = nodeEl.getAttribute('data-node-key');
      state.drag = { key, nodeEl, startX: ev.clientX, startY: ev.clientY, origX: nodeEl.offsetLeft, origY: nodeEl.offsetTop, moved: false, undoPushed: false };
      try { nodeEl.setPointerCapture(ev.pointerId); } catch (e) { }
      nodeEl.classList.add('wf-dragging');
      ev.preventDefault();
    };
    const moveNodeDrag = (ev) => {
      const d = state.drag; if (!d) return;
      const dx = (ev.clientX - d.startX) / state.scale, dy = (ev.clientY - d.startY) / state.scale;
      if (!d.moved && Math.hypot(ev.clientX - d.startX, ev.clientY - d.startY) < DRAG_THRESHOLD) return;
      if (!d.moved) {
        d.moved = true;
        if (!d.undoPushed) { d.undoPushed = true; state.invoke('BeginNodeDragAsync', d.key); }
      }
      let x = Math.max(0, d.origX + dx), y = Math.max(0, d.origY + dy);
      if (state.snap) { x = Math.round(x / state.snap) * state.snap; y = Math.round(y / state.snap) * state.snap; }
      d.nodeEl.style.left = x + 'px'; d.nodeEl.style.top = y + 'px';
      scheduleRelayout();
    };
    const endNodeDrag = (ev) => {
      const d = state.drag; if (!d) return;
      state.drag = null;
      d.nodeEl.classList.remove('wf-dragging');
      if (d.moved) {
        state.invoke('CommitNodePositionAsync', d.key, d.nodeEl.offsetLeft, d.nodeEl.offsetTop);
      } else {
        state.invoke('SelectNodeAsync', d.key);
      }
    };

    /* ── connect gesture (from node hover handle) ─────────────────────────── */
    const startConnect = (ev, nodeEl) => {
      if (state.readOnly || state.connectMode) return;
      const key = nodeEl.getAttribute('data-node-key');
      const nodes = readNodes(); const n = nodes.get(key); if (!n) return;
      state.connect = { from: n, nodeEl };
      ev.preventDefault(); ev.stopPropagation();
      highlightTargets(n);
    };
    const highlightTargets = (source) => {
      for (const el of nodeEls()) {
        const k = el.getAttribute('data-node-key'); const n = { el };
        const type = el.getAttribute('data-node-type') || '';
        const invalid = type === 'node-start' || k === source.key
          || (source.type === 'node-end')
          || !!state.svg.querySelector(`path.wf-edge-src[data-from="${cssEscape(source.key)}"][data-to="${cssEscape(k)}"]`);
        el.classList.toggle('wf-connect-target', !invalid);
      }
    };
    const cssEscape = (s) => (window.CSS && CSS.escape) ? CSS.escape(s) : s.replace(/["\\]/g, '\\$&');
    const clearTargetHighlights = () => { for (const el of nodeEls()) el.classList.remove('wf-connect-target'); };
    const moveConnect = (ev) => {
      const c = state.connect; if (!c) return;
      const [wx, wy] = toWorld(ev.clientX, ev.clientY);
      const p0 = sidePoint(c.from, autoSide(c.from, { cx: wx, cy: wy }), 0.5);
      state.temp.style.display = '';
      state.temp.setAttribute('d', `M ${fmt(p0[0])} ${fmt(p0[1])} L ${fmt(wx)} ${fmt(wy)}`);
    };
    const endConnect = (ev) => {
      const c = state.connect; if (!c) return;
      state.connect = null;
      state.temp.style.display = 'none';
      clearTargetHighlights();
      const el = document.elementFromPoint(ev.clientX, ev.clientY)?.closest?.('.workflow-node');
      if (el) {
        const k = el.getAttribute('data-node-key');
        if (k && k !== c.from.key) state.invoke('ConnectNodesAsync', c.from.key, k);
      }
    };

    /* ── edge endpoint re-anchoring ───────────────────────────────────────── */
    const startAnchorDrag = (ev, idx, isSource) => {
      if (state.readOnly) return;
      ev.preventDefault(); ev.stopPropagation();
      state.anchorDrag = { idx, isSource };
      state.svg.classList.add('wf-anchor-dragging');
    };
    const moveAnchorDrag = (ev) => {
      const a = state.anchorDrag; if (!a) return;
      const g = groups.get(String(a.idx)); if (!g || !g.__wf) return;
      const [wx, wy] = toWorld(ev.clientX, ev.clientY);
      const e = g.__wf;
      const fixed = a.isSource ? e.p3 : e.p0;
      const moving = [wx, wy];
      const path = a.isSource
        ? `M ${fmt(moving[0])} ${fmt(moving[1])} L ${fmt(fixed[0])} ${fmt(fixed[1])}`
        : `M ${fmt(fixed[0])} ${fmt(fixed[1])} L ${fmt(moving[0])} ${fmt(moving[1])}`;
      state.temp.style.display = '';
      state.temp.setAttribute('d', path);
      // snap preview on nearest node border
      const near = nearestAnchorPoint(wx, wy, a.isSource);
      for (const el of nodeEls()) el.classList.toggle('wf-connect-target', near && el.getAttribute('data-node-key') === near.key);
    };
    const nearestAnchorPoint = (wx, wy, isSource) => {
      let best = null, bestD = ANCHOR_SNAP;
      for (const el of nodeEls()) {
        const key = el.getAttribute('data-node-key');
        const type = el.getAttribute('data-node-type');
        if (isSource && type === 'node-end') continue;
        if (!isSource && type === 'node-start') continue;
        const x = el.offsetLeft, y = el.offsetTop, w = el.offsetWidth, h = el.offsetHeight;
        const cands = [[x + w / 2, y], [x + w, y + h / 2], [x + w / 2, y + h], [x, y + h / 2]];
        for (const [ax, ay] of cands) {
          const d = Math.hypot(ax - wx, ay - wy);
          if (d < bestD) { bestD = d; best = { key, x: ax, y: ay, side: ay === y ? 'Top' : ax === x + w ? 'Right' : ay === y + h ? 'Bottom' : 'Left' }; }
        }
      }
      return best;
    };
    const endAnchorDrag = (ev) => {
      const a = state.anchorDrag; if (!a) return;
      state.anchorDrag = null;
      state.temp.style.display = 'none';
      state.svg.classList.remove('wf-anchor-dragging');
      clearTargetHighlights();
      const [wx, wy] = toWorld(ev.clientX, ev.clientY);
      const near = nearestAnchorPoint(wx, wy, a.isSource);
      if (near) {
        state.invoke('BeginEdgeEditAsync', a.idx);
        state.invoke('CommitEdgeAnchorAsync', a.idx, a.isSource, near.key, near.side);
      }
    };

    /* ── panning ──────────────────────────────────────────────────────────── */
    const startPan = (ev, mode) => {
      state.pan = { x: ev.clientX, y: ev.clientY, sl: state.viewport.scrollLeft, st: state.viewport.scrollTop };
      state.viewport.classList.add('wf-panning');
      if (ev.pointerId !== undefined && mode === 'capture') { try { state.viewport.setPointerCapture(ev.pointerId); } catch (e) { } }
      ev.preventDefault();
    };
    const movePan = (ev) => {
      const p = state.pan; if (!p) return;
      state.viewport.scrollLeft = p.sl - (ev.clientX - p.x);
      state.viewport.scrollTop = p.st - (ev.clientY - p.y);
      updateMinimap();
    };
    const endPan = () => { state.pan = null; state.viewport.classList.remove('wf-panning'); };

    /* ── global pointer routing on the viewport ───────────────────────────── */
    const onPointerDown = (ev) => {
      if (ev.button === 1) { startPan(ev, 'capture'); return; }
      if (ev.button !== 0) return;
      const nodeEl = ev.target.closest?.('.workflow-node');
      const handle = ev.target.closest?.('.wf-out-handle');
      const dot = ev.target.closest?.('.wf-end-dot');
      const edgeHit = ev.target.closest?.('.wf-edge-g');
      if (handle && nodeEl) { startConnect(ev, nodeEl); return; }
      if (dot) return; // handled by dot's own pointerdown
      if (nodeEl) {
        if (state.connectMode || state.readOnly) {
          // no dragging in connect/readonly mode — plain click selects (or connects) on up
          state._nodeClick = { key: nodeEl.getAttribute('data-node-key'), x: ev.clientX, y: ev.clientY };
          return;
        }
        startNodeDrag(ev, nodeEl); return;
      }
      if (edgeHit) return;
      if (state.spaceDown || state.panTool) { startPan(ev, 'capture'); return; }
      // click on empty canvas: deselect (on up, if not panned)
      state._emptyDown = { x: ev.clientX, y: ev.clientY };
    };
    const onPointerMove = (ev) => {
      if (state.pan) { movePan(ev); return; }
      if (state.drag) { moveNodeDrag(ev); return; }
      if (state.connect) { moveConnect(ev); return; }
      if (state.anchorDrag) { moveAnchorDrag(ev); return; }
    };
    const onPointerUp = (ev) => {
      if (state.pan) { endPan(); return; }
      if (state.drag) { endNodeDrag(ev); return; }
      if (state.connect) { endConnect(ev); return; }
      if (state.anchorDrag) { endAnchorDrag(ev); return; }
      if (state._nodeClick && Math.hypot(ev.clientX - state._nodeClick.x, ev.clientY - state._nodeClick.y) < DRAG_THRESHOLD) {
        state.invoke(state.connectMode ? 'ConnectModeNodeClickAsync' : 'SelectNodeAsync', state._nodeClick.key);
      }
      state._nodeClick = null;
      if (state._emptyDown && Math.hypot(ev.clientX - state._emptyDown.x, ev.clientY - state._emptyDown.y) < DRAG_THRESHOLD) {
        if (!ev.target.closest?.('.workflow-node') && !ev.target.closest?.('.wf-edge-g') && !ev.target.closest?.('.wf-minimap')) {
          state.invoke('CanvasClickedAsync');
        }
      }
      state._emptyDown = null;
    };
    state.viewport.addEventListener('pointerdown', onPointerDown);
    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    state.viewport.addEventListener('pointercancel', onPointerUp);

    /* ── wheel zoom ───────────────────────────────────────────────────────── */
    state.viewport.addEventListener('wheel', (ev) => {
      if (ev.ctrlKey || !ev.shiftKey) {
        // plain wheel + ctrl+wheel zoom; shift+wheel lets vertical scroll happen
        ev.preventDefault();
        const factor = Math.pow(0.998, ev.deltaY * (ev.deltaMode === 1 ? 16 : 1));
        const z = clamp(state.scale * factor, 0.4, 2.5);
        applyScale(z, [ev.clientX, ev.clientY]);
        clearTimeout(state._zoomT);
        state._zoomT = setTimeout(() => state.invoke('CommitZoomAsync', state.scale), 450);
      }
    }, { passive: false });

    /* ── keyboard ─────────────────────────────────────────────────────────── */
    const isTypingTarget = (t) => !!(t && t.closest && t.closest('input,textarea,select,[contenteditable="true"]'));
    const onKeyDown = (ev) => {
      if (state.disposed) return;
      if (ev.code === 'Space' && !isTypingTarget(ev.target) && !state.spaceDown) { state.spaceDown = true; state.viewport.classList.add('wf-space'); ev.preventDefault(); return; }
      if (isTypingTarget(ev.target)) return;
      if (!state.viewport.parentNode?.contains?.(ev.target) && ev.target !== document.body && ev.target !== state.viewport && !state.viewport.contains(ev.target)) {
        // keep global shortcuts only when canvas area or body focused
        if (ev.target !== document.body) { /* still allow below when fired from viewport */ }
      }
      const inCanvas = state.viewport.contains(ev.target) || ev.target === state.viewport || ev.target === document.body;
      if (!inCanvas) return;
      if (ev.key === 'Delete' || ev.key === 'Backspace') { ev.preventDefault(); state.invoke('DeleteSelectionAsync'); }
      else if (ev.ctrlKey && (ev.key === 'z' || ev.key === 'Z')) { ev.preventDefault(); state.invoke('UndoAsync'); }
      else if ((ev.ctrlKey && (ev.key === 'y' || ev.key === 'Y')) || (ev.ctrlKey && ev.shiftKey && (ev.key === 'z' || ev.key === 'Z'))) { ev.preventDefault(); state.invoke('RedoAsync'); }
      else if (ev.key.startsWith('Arrow') && !state.readOnly) {
        ev.preventDefault();
        const step = ev.shiftKey ? 40 : (state.snap || 5);
        const dx = ev.key === 'ArrowLeft' ? -step : ev.key === 'ArrowRight' ? step : 0;
        const dy = ev.key === 'ArrowUp' ? -step : ev.key === 'ArrowDown' ? step : 0;
        state.invoke('NudgeSelectionAsync', dx, dy);
      }
    };
    const onKeyUp = (ev) => { if (ev.code === 'Space') { state.spaceDown = false; state.viewport.classList.remove('wf-space'); } };
    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('keyup', onKeyUp);

    /* ── dblclick → properties ────────────────────────────────────────────── */
    state.viewport.addEventListener('dblclick', (ev) => {
      const nodeEl = ev.target.closest?.('.workflow-node');
      const edgeHit = ev.target.closest?.('.wf-edge-g');
      if (nodeEl) {
        state.invoke('SelectNodeAsync', nodeEl.getAttribute('data-node-key'));
        const props = state.viewport.closest('.workflow-designer-page')?.querySelector('.workflow-properties');
        if (props) { props.classList.remove('wf-flash'); void props.offsetWidth; props.classList.add('wf-flash'); }
      } else if (edgeHit) {
        state.invoke('SelectEdgeAsync', Number(edgeHit.getAttribute('data-idx')));
      }
    });

    /* ── pan tool button + zoom reset (plain HTML in toolbar) ─────────────── */
    const bindToolbar = () => {
      const page = state.viewport.closest('.workflow-designer-page'); if (!page) return;
      const panBtn = page.querySelector('#wf-pan-btn');
      if (panBtn) {
        panBtn.addEventListener('click', () => {
          state.panTool = !state.panTool;
          panBtn.classList.toggle('active', state.panTool);
          panBtn.setAttribute('aria-pressed', state.panTool ? 'true' : 'false');
        });
      }
      const z100 = page.querySelector('#wf-zoom-reset');
      if (z100) z100.addEventListener('click', () => {
        applyScale(1);
        state.invoke('CommitZoomAsync', 1);
      });
    };

    /* ── palette HTML5 drop (world coords computed here) ──────────────────── */
    world.addEventListener('dragover', (ev) => { ev.preventDefault(); world.classList.add('drop-active'); });
    world.addEventListener('dragleave', () => world.classList.remove('drop-active'));
    world.addEventListener('drop', (ev) => {
      ev.preventDefault();
      world.classList.remove('drop-active');
      const type = ev.dataTransfer.getData('text/wf-type');
      if (type === '' || state.readOnly) return;
      const [wx, wy] = toWorld(ev.clientX, ev.clientY);
      state.invoke('AddNodeAtAsync', Number(type), Math.max(0, wx - 70), Math.max(0, wy - 32));
    });
    // tag palette items so the drag payload carries the type
    for (const item of document.querySelectorAll('.workflow-palette-item')) {
      item.addEventListener('dragstart', (ev) => {
        const t = item.getAttribute('data-wf-type');
        if (t !== null && ev.dataTransfer) { ev.dataTransfer.setData('text/wf-type', t); ev.dataTransfer.effectAllowed = 'copy'; }
      });
    }

    /* ── window resize ────────────────────────────────────────────────────── */
    const onResize = () => { sizer.style.width = Math.max(state.worldW * state.scale, state.viewport.clientWidth) + 'px'; sizer.style.height = Math.max(state.worldH * state.scale, state.viewport.clientHeight) + 'px'; updateMinimap(); };
    window.addEventListener('resize', onResize);

    /* ── boot ─────────────────────────────────────────────────────────────── */
    buildMinimap();
    bindToolbar();
    relayout();
    window.__wfDebug = state; // test hook
    // palette items may render later (Blazor) — retag periodically short-term
    const tagInterval = setInterval(() => {
      for (const item of document.querySelectorAll('.workflow-palette-item:not([data-wf-tagged])')) {
        item.setAttribute('data-wf-tagged', '1');
        item.addEventListener('dragstart', (ev) => {
          const t = item.getAttribute('data-wf-type');
          if (t !== null && ev.dataTransfer) { ev.dataTransfer.setData('text/wf-type', t); ev.dataTransfer.effectAllowed = 'copy'; }
        });
      }
    }, 1200);
    setTimeout(() => clearInterval(tagInterval), 30000);

    return () => {  // dispose
      state.disposed = true;
      clearInterval(tagInterval);
      state.observer?.disconnect();
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
      window.removeEventListener('resize', onResize);
      document.removeEventListener('keydown', onKeyDown);
      document.removeEventListener('keyup', onKeyUp);
      state.minimap?.remove();
      state.dotnet = null;
    };
  }

  window.barangWorkflowDesigner = { mount };
})();
