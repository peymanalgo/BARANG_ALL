const fs = require('fs');
// Usage: node extract-enarg.js <enIndex1based> <file...>  → prints JSON list of literal en-args of T(/L( calls
const idx = parseInt(process.argv[2], 10) - 1;
const files = process.argv.slice(3);
const seen = new Map();
for (const f of files) {
  const src = fs.readFileSync(f, 'utf8');
  for (let i = 0; i < src.length; i++) {
    const isT = (src[i] === 'T' || src[i] === 'L') && src[i + 1] === '(' && !/[\w.]/.test(src[i - 1] || ' ');
    if (!isT) continue;
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
    i = j;
    const enArg = (args[idx] || '').trim();
    if (!enArg.startsWith('"')) continue; // skip interpolated/variable
    const text = JSON.parse(enArg);
    if (!/[A-Za-z]/.test(text)) continue;
    if (!seen.has(text)) seen.set(text, []);
    seen.get(text).push(f.split(/[\\/]/).pop());
  }
}
const list = [...seen.keys()].sort((a, b) => a.localeCompare(b));
for (const t of list) console.log(JSON.stringify(t));
console.error('COUNT=' + list.length);
