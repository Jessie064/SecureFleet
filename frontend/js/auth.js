// Minimal auth helper. In demo mode it issues a local "token" + role and stores
// in sessionStorage. In production, replace with Supabase Auth and store the
// real JWT — every API call sends Authorization: Bearer <token>.

const SF_AUTH_KEY = "sf.auth";

const Auth = {
  async signIn({ email }) {
    const res = await fetch(`${window.SF_CONFIG.apiBase}/api/auth/demo-login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email })
    });
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      throw new Error(body.error || `Sign in failed (${res.status})`);
    }
    const data = await res.json();
    const session = {
      email: data.email,
      role: data.role,
      name: data.name || data.email.split("@")[0],
      token: data.token,
      issuedAt: Date.now()
    };
    sessionStorage.setItem(SF_AUTH_KEY, JSON.stringify(session));
    return session;
  },
  signOut() {
    sessionStorage.removeItem(SF_AUTH_KEY);
    window.location.href = "login.html";
  },
  current() {
    try { return JSON.parse(sessionStorage.getItem(SF_AUTH_KEY)); }
    catch { return null; }
  },
  requireRole(allowed) {
    const s = this.current();
    if (!s) { window.location.href = "login.html"; return null; }
    if (allowed && !allowed.includes(s.role)) {
      window.location.href = `${s.role}.html`;
      return null;
    }
    return s;
  },
  authHeaders() {
    const s = this.current();
    return s ? { "Authorization": `Bearer ${s.token}` } : {};
  }
};

window.Auth = Auth;

// Wire up logout button if present
document.addEventListener("DOMContentLoaded", () => {
  const btn = document.getElementById("logout");
  if (btn) btn.addEventListener("click", () => Auth.signOut());

  const chip = document.getElementById("user-chip");
  if (chip) {
    const s = Auth.current();
    if (s) chip.innerHTML = `<strong>${s.name}</strong><span>${s.email}</span>`;
  }
});
