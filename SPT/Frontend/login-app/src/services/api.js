// ─── API Base URL ─────────────────────────────────────────────────────────────
// Use HTTP port 5273 (from launchSettings.json) to avoid SSL cert issues in dev
const BASE_URL = "http://localhost:5273/api";

// ─── Token Helpers ────────────────────────────────────────────────────────────
export const saveToken = (token) => localStorage.setItem("token", token);
export const getToken  = ()      => localStorage.getItem("token");
export const removeToken = ()    => localStorage.removeItem("token");

export const saveUser = (user)   => localStorage.setItem("user", JSON.stringify(user));
export const getUser  = ()       => {
  const u = localStorage.getItem("user");
  return u ? JSON.parse(u) : null;
};
export const removeUser = ()     => localStorage.removeItem("user");

export const isAuthenticated = () => !!getToken();

// ─── Fetch Wrapper ────────────────────────────────────────────────────────────
const apiFetch = async (endpoint, options = {}) => {
  const token = getToken();
  const headers = {
    "Content-Type": "application/json",
    ...(token && { Authorization: `Bearer ${token}` }),
    ...options.headers,
  };

  let response;
  try {
    response = await fetch(`${BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });
  } catch (networkError) {
    // Network-level failure (API not running, CORS preflight blocked, SSL error)
    console.error("[API] Network error:", networkError.message);
    throw new Error("Cannot connect to server. Is the .NET API running on port 5273?");
  }

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    // ── 401: Token expired or invalid — auto-logout and redirect to login ──
    if (response.status === 401) {
      console.warn("[API] 401 received — clearing session and redirecting to login.");
      localStorage.removeItem("token");
      localStorage.removeItem("user");
      // Full page redirect so React state is fully reset
      window.location.href = "/";
      throw new Error("Session expired. Redirecting to login…");
    }

    const errMsg =
      response.status === 403 ? "Access denied." :
      response.status === 500 ? `Server error: ${data.message || data.error || "Internal server error"}` :
      data.message || data.error || `HTTP error ${response.status}`;
    console.error(`[API] ${response.status} on ${endpoint}:`, data);
    throw new Error(errMsg);
  }
  return data;
};

// ─── Auth API ─────────────────────────────────────────────────────────────────
export const authAPI = {
  login: (email, password) =>
    apiFetch("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),

  register: (fullName, email, password) =>
    apiFetch("/auth/register", {
      method: "POST",
      body: JSON.stringify({ fullName, email, password }),
    }),

  // Google OAuth — sends the credential (ID token) returned by @react-oauth/google
  googleLogin: (credential) =>
    apiFetch("/auth/google", {
      method: "POST",
      body: JSON.stringify({ credential }),
    }),

  logout: () =>
    apiFetch("/auth/logout", { method: "POST" }),
};

// ─── Chat API ─────────────────────────────────────────────────────────────────
export const chatAPI = {
  sendMessage: (message, email, sessionId = null) =>
    apiFetch("/chat", {
      method: "POST",
      body: JSON.stringify({ message, email, sessionId }),
    }),
};

// ─── Chat Session API (history sidebar) ──────────────────────────────────────
export const chatSessionAPI = {
  list: () =>
    apiFetch("/chatsession"),

  create: () =>
    apiFetch("/chatsession", { method: "POST" }),

  get: (id) =>
    apiFetch(`/chatsession/${id}`),

  delete: (id) =>
    apiFetch(`/chatsession/${id}`, { method: "DELETE" }),

  appendMessage: (id, msg) =>
    apiFetch(`/chatsession/${id}/message`, {
      method: "POST",
      body: JSON.stringify(msg),
    }),
};

// ─── Ticket API ───────────────────────────────────────────────────────────────
export const ticketAPI = {
  getAll: () =>
    apiFetch("/ticket"),

  getById: (id) =>
    apiFetch(`/ticket/${id}`),

  getAnalytics: () =>
    apiFetch("/ticket/analytics"),

  create: (ticket) =>
    apiFetch("/ticket", {
      method: "POST",
      body: JSON.stringify(ticket),
    }),

  updateStatus: (id, status, adminReply = null) =>
    apiFetch(`/ticket/${id}`, {
      method: "PUT",
      body: JSON.stringify({ status, adminReply }),
    }),

  updatePriority: (id, priority) =>
    apiFetch(`/ticket/${id}/priority`, {
      method: "PATCH",
      body: JSON.stringify({ priority }),
    }),

  addUserReply: (id, text) =>
    apiFetch(`/ticket/${id}/reply`, {
      method: "POST",
      body: JSON.stringify({ text }),
    }),
};


