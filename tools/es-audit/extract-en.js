const fs = require('fs'), path = require('path');
// Extract string literals that look like English UI text from the given files.
const files = process.argv.slice(2);
const seen = new Map(); // text -> [file:line]
for (const f of files) {
  if (!fs.existsSync(f)) { console.error('MISSING ' + f); continue; }
  const src = fs.readFileSync(f, 'utf8');
  let line = 1, str = null, cur = '';
  for (let i = 0; i < src.length; i++) {
    const c = src[i];
    if (c === '\n') line++;
    if (str) {
      cur += c;
      if (c === str && src[i - 1] !== '\\') {
        // unescape basic
        const raw = cur;
        const text = raw.slice(1, -1).replace(/\\"/g, '"').replace(/\\\\/g, '\\').replace(/\\n/g, '\n').replace(/\\t/g, '\t');
        // English-like: has 2+ letters, no Persian/Arabic chars
        if (/[A-Za-z]{2}/.test(text) && !/[\u0600-\u06FF]/.test(text) && text.length <= 120 && !/^[A-Za-z0-9_.\-/ {}():]+$/.test(text) === false) {
          // keep only if it contains a space or is a known word-ish token (filter pure codes later)
          if (/ /.test(text) || /^[A-Z][a-z]{2,}$/.test(text)) {
            if (!seen.has(text)) seen.set(text, []);
            seen.get(text).push(path.basename(f) + ':' + line);
          }
        }
        str = null; cur = '';
      }
      continue;
    }
    if (c === '"' || c === "'") { str = c; cur = c; }
  }
}
const list = [...seen.keys()].sort((a, b) => a.localeCompare(b));
for (const t of list) console.log(JSON.stringify(t) + '  <= ' + seen.get(t).slice(0, 2).join(' '));
console.error('COUNT=' + list.length);
