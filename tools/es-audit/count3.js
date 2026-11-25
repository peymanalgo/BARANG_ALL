const fs = require('fs'), path = require('path');
function walk(d, out = []) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (e.name === 'bin' || e.name === 'obj' || e.name === 'node_modules') continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p, out);
    else if (/\.(cshtml|cs|razor)$/.test(e.name)) out.push(p);
  }
  return out;
}
// Finds T( ... ) call sites and returns those with exactly 3 top-level args.
// mode 'en2' = arg[1] is English (T(fa,en,ar)), 'en3' = arg[2] is English (T(fa,ar,en))
function analyze(src) {
  const res = [];
  for (let i = 0; i < src.length; i++) {
    if (src[i] === 'T' && src[i + 1] === '(' && !/[\w.]/.test(src[i - 1] || ' ')) {
      let depth = 0, args = [], cur = '', j = i + 1, str = null;
      for (; j < src.length; j++) {
        const c = src[j];
        if (str) { cur += c; if (c === str && src[j - 1] !== '\\') str = null; continue; }
        if (c === '"' || c === "'") { str = c; cur += c; continue; }
        if (c === '(') { depth++; if (depth === 1) continue; cur += c; continue; }
        if (c === ')') { depth--; if (depth === 0) { args.push(cur); break; } cur += c; continue; }
        if (c === ',' && depth === 1) { args.push(cur); cur = ''; continue; }
        cur += c;
      }
      if (args.length === 3) res.push({ line: src.slice(0, i).split('\n').length, args: args.map(a => a.trim()) });
      i = j;
    }
  }
  return res;
}
const root = process.argv[2];
let total = 0;
for (const f of walk(root)) {
  const src = fs.readFileSync(f, 'utf8');
  const hits = analyze(src);
  if (hits.length) {
    total += hits.length;
    console.log(f.replace(root + path.sep, '') + ' : ' + hits.length);
    hits.slice(0, 2).forEach(h => console.log('   L' + h.line + ' ' + JSON.stringify(h.args.map(a => a.slice(0, 50)))));
  }
}
console.log('TOTAL_3ARG=' + total);
