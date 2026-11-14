// ─── کلاینت API بانک طرح ───────────────────────────────────────────
const API = process.env.BARANG_API ?? "http://localhost:8080";
const USERNAME = process.env.BARANG_USER ?? "admin";
const PASSWORD = process.env.BARANG_PASS ?? "BarangDev@2026";

export class Api {
  constructor() { this.token = null; this.companyId = null; this.tokenAt = 0; }

  async login() {
    const r = await fetch(`${API}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: USERNAME, password: PASSWORD }),
    });
    if (!r.ok) throw new Error(`login failed ${r.status}: ${await r.text()}`);
    const login = await r.json();
    this.token = login.accessToken;
    this.tokenAt = Date.now();
    const companies = await this.req("GET", "/api/v1/auth/me/companies");
    const list = await companies.json();
    if (!Array.isArray(list) || list.length === 0) throw new Error("no company access");
    this.companyId = list[0].companyId;
    const sel = await fetch(`${API}/api/v1/auth/me/company`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${this.token}` },
      body: JSON.stringify({ companyId: this.companyId }),
    });
    if (!sel.ok) throw new Error(`company select failed ${sel.status}`);
    return this;
  }

  async ensureToken() {
    // توکن ۳۰ دقیقه است؛ بعد از ۲۵ دقیقه تازه می‌شود
    if (!this.token || Date.now() - this.tokenAt > 25 * 60 * 1000) await this.login();
  }

  headers(extra = {}) {
    const headers = { Authorization: `Bearer ${this.token}`, ...extra };
    if (this.companyId) headers["X-Company-ID"] = this.companyId;
    return headers;
  }

  async req(method, path, body, extraHeaders = {}, retry = true) {
    await this.ensureToken();
    const headers = this.headers(extraHeaders);
    if (body !== undefined && body !== null && !(body instanceof FormData) && !extraHeaders["Content-Type"]) {
      headers["Content-Type"] = "application/json";
      body = JSON.stringify(body);
    }
    const r = await fetch(`${API}${path}`, { method, headers, body });
    if ((r.status === 401 || r.status === 403) && retry && !path.includes("/auth/login")) {
      await this.login();
      return this.req(method, path, body, extraHeaders, false);
    }
    return r;
  }

  async json(method, path, body) {
    const r = await this.req(method, path, body);
    const text = await r.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = { raw: text }; }
    if (!r.ok) {
      const err = new Error(`${method} ${path} → ${r.status} ${text.slice(0, 400)}`);
      err.status = r.status; err.data = data;
      throw err;
    }
    return data;
  }
}
