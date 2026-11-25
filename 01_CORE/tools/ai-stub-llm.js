#!/usr/bin/env node
// Minimal OpenAI-compatible stub LLM for BARANG AI assistant E2E testing (no deps).
// Behavior:
//   - "connection test" system message           -> reply "pong"
//   - no tool message in conversation yet         -> tool_call today_order_summary({})
//   - tool result contains "access_denied"        -> polite refusal in caller's language
//   - tool result otherwise                       -> final markdown + ```chart``` block
// Culture detected from the last user message (Persian/Arabic script -> fa/ar, else en).
// Logs every request line to stdout for the E2E transcript.
const http = require('http');
const PORT = Number(process.env.STUB_PORT || 19734);
const server = http.createServer((req, res) => {
  let body = '';
  req.on('data', c => { body += c; });
  req.on('end', () => {
    let parsed = null;
    try { parsed = JSON.parse(body || '{}'); } catch { }
    const messages = (parsed && parsed.messages) || [];
    const lastUser = [...messages].reverse().find(m => m.role === 'user');
    const userText = lastUser ? String(lastUser.content || '') : '';
    const isFa = /[\u0600-\u06FF]/.test(userText) && !/[\u0621-\u064A].*(order|hello)/i.test(userText) ? true : /[\u0600-\u06FF]/.test(userText);
    const isAr = /[\u0600-\u06FF]/.test(userText) && /(سلام|حالة|الطلبات|أيهما)/.test(userText) && !/وضعیت|سفارش/.test(userText);
    const lang = isFa ? 'fa' : (isAr ? 'ar' : 'en');
    console.log(`[stub] ${new Date().toISOString()} ${req.method} ${req.url} bytes=${body.length} lang=${lang} msgs=${messages.length} lastUser=${JSON.stringify(userText.slice(0, 80))}`);
    const hasAuth = req.headers.authorization || '(none)';
    console.log(`[stub]   auth=${hasAuth} model=${parsed && parsed.model} tools=${parsed && parsed.tools ? parsed.tools.length : 0}`);

    let payload;
    const systemMsg = messages.find(m => m.role === 'system');
    const toolMsgs = messages.filter(m => m.role === 'tool');
    const assistantCalledTool = messages.some(m => m.role === 'assistant' && Array.isArray(m.tool_calls) && m.tool_calls.length > 0);

    if (systemMsg && /connection test/i.test(String(systemMsg.content || ''))) {
      payload = content('pong');
    } else if (toolMsgs.length > 0) {
      const toolContent = String(toolMsgs[toolMsgs.length - 1].content || '');
      if (/"error"\s*:\s*"access_denied"/.test(toolContent)) {
        payload = content(lang === 'fa'
          ? 'متأسفانه شما مجوز مشاهده اطلاعات سفارش‌ها را ندارید. برای استفاده از این بخش با مدیر سیستم تماس بگیرید.'
          : lang === 'ar'
            ? 'للأسف ليس لديك صلاحية الاطلاع على بيانات الطلبات. يُرجى التواصل مع مسؤول النظام.'
            : 'Sorry, you do not have permission to view order information. Please contact your system administrator.');
      } else {
        payload = content(lang === 'fa'
          ? 'بر اساس داده‌های زندهٔ سیستم، وضعیت سفارش‌های امروز به این شرح است:\n\n| وضعیت | تعداد |\n| --- | --- |\n| ثبت‌شده | ۳ |\n| ارسال‌شده | ۵ |\n| لغوشده | ۱ |\n| تحویل‌شده | ۲ |\n\nجمع کل سفارش‌های امروز: ۱۱ سفارش با ارزش ۴۵٬۰۰۰٬۰۰۰ تومان.\n\n```chart\n{"type":"bar","title":"سفارش‌های امروز","labels":["ثبت‌شده","ارسال‌شده","لغوشده","تحویل‌شده"],"series":[{"name":"تعداد","data":[3,5,1,2]}]}\n```'
          : 'Based on live ERP data, here is today\'s order status:\n\n| Status | Count |\n| --- | --- |\n| Draft | 3 |\n| Submitted | 5 |\n| Cancelled | 1 |\n| Delivered | 2 |\n\nTotal today: 11 orders worth 45,000,000 IRT.\n\n```chart\n{"type":"bar","title":"Today\'s orders","labels":["Draft","Submitted","Cancelled","Delivered"],"series":[{"name":"Count","data":[3,5,1,2]}]}\n```');
      }
    } else {
      payload = {
        choices: [{
          message: {
            role: 'assistant',
            content: null,
            tool_calls: [{ id: 'call_stub_1', type: 'function', function: { name: 'today_order_summary', arguments: '{}' } }]
          }
        }],
        usage: { prompt_tokens: 420, completion_tokens: 30 }
      };
    }

    const out = JSON.stringify(Object.assign({ id: 'chatcmpl-stub', object: 'chat.completion', created: Math.floor(Date.now() / 1000), model: parsed && parsed.model || 'stub' }, payload));
    console.log(`[stub]   -> ${payload.choices[0].message.tool_calls ? 'tool_call:' + payload.choices[0].message.tool_calls[0].function.name : 'final(' + (payload.choices[0].message.content || '').length + 'ch)'}`);
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(out);
  });
});
function content(text) {
  return { choices: [{ message: { role: 'assistant', content: text } }], usage: { prompt_tokens: 500, completion_tokens: 180 } };
}
server.listen(PORT, () => console.log(`[stub] OpenAI-compatible stub LLM listening on http://0.0.0.0:${PORT}/v1/chat/completions`));
