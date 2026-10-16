import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { GoogleLogin } from "@react-oauth/google";
import { authAPI, saveToken, saveUser } from "../services/api";
import "./Login.css";

function Login() {
  const [email, setEmail]       = useState("");
  const [password, setPassword] = useState("");
  const [error, setError]       = useState("");
  const [loading, setLoading]   = useState(false);
  const [googleLoading, setGoogleLoading] = useState(false);

  const navigate = useNavigate();

  const [showPassword, setShowPassword] = useState(false);

  // ── Helper: save token/user and navigate by role ──────────────────────────
  const handleLoginSuccess = (data) => {
    saveToken(data.token);
    saveUser({
      fullName: data.fullName,
      email:    data.email,
      role:     data.role,
      picture:  data.picture || null,
    });
    if (data.role === "Admin") {
      navigate("/admin");
    } else {
      navigate("/dashboard");
    }
  };

  // ── Email + Password Login ────────────────────────────────────────────────
  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const data = await authAPI.login(email, password);
      handleLoginSuccess(data);
    } catch (err) {
      setError(err.message || "Login failed. Please check your credentials.");
    } finally {
      setLoading(false);
    }
  };

  // ── Google Login ──────────────────────────────────────────────────────────
  const handleGoogleSuccess = async (credentialResponse) => {
    setError("");
    setGoogleLoading(true);
    try {
      const data = await authAPI.googleLogin(credentialResponse.credential);
      handleLoginSuccess(data);
    } catch (err) {
      setError(err.message || "Google sign-in failed. Please try again.");
    } finally {
      setGoogleLoading(false);
    }
  };

  return (
    <div className="container">
      {/* ── Left Branding Panel ─────────────────────────── */}
      <div className="auth-left-panel">
        <div className="auth-brand">
          <div className="auth-brand-icon">
            <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
            </svg>
          </div>
          <span className="auth-brand-name">AI Smart <span>Support</span></span>
        </div>

        <div className="auth-hero">
          <div className="auth-hero-tag">AI-Powered · Live Now</div>
          <h1>
            AI Smart <br />
            <span>Customer Support</span><br />
            Platform
          </h1>
          <p>
            Resolve tickets faster with Groq AI — intelligent routing,
            sentiment analysis, and real-time conversation support.
          </p>

          <div className="auth-features">
            {[
              "Groq AI auto-classification",
              "Real-time sentiment analysis",
              "Smart ticket prioritization",
              "Threaded conversation support",
            ].map((f) => (
              <div className="auth-feature-item" key={f}>
                <div className="auth-feature-dot">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                </div>
                {f}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Right Auth Panel ────────────────────────────── */}
      <div className="auth-right-panel">
        <div className="card">
          <div className="avatar">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
          </div>

          <h2>Welcome back</h2>
          <p className="subtitle">Sign in to your AI Smart Support account</p>

          {error && (
            <p className="error-msg">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {error}
            </p>
          )}

          {/* ── Google Sign-In Button ── */}
          <div className="google-btn-wrapper">
            {googleLoading ? (
              <button className="google-btn" disabled>
                <span className="google-spinner" />
                Signing in with Google…
              </button>
            ) : (
              <GoogleLogin
                onSuccess={handleGoogleSuccess}
                onError={() => setError("Google sign-in failed. Please try again.")}
                useOneTap={false}
                theme="filled_black"
                size="large"
                shape="rectangular"
                width="360"
                text="continue_with"
                logo_alignment="left"
              />
            )}
          </div>

          {/* ── Divider ── */}
          <div className="divider"><span>or sign in with email</span></div>

          {/* ── Email/Password Form ── */}
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label">Email Address</label>
              <input
                id="login-email"
                className="input"
                type="email"
                placeholder="you@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label className="form-label">Password</label>
              <div className="password-wrapper">
                <input
                  id="login-password"
                  className="input"
                  type={showPassword ? "text" : "password"}
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={loading}
                />
                <button
                  type="button"
                  className="toggle-password-btn"
                  onClick={() => setShowPassword(!showPassword)}
                  title={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
                      <line x1="1" y1="1" x2="23" y2="23" />
                    </svg>
                  ) : (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                      <circle cx="12" cy="12" r="3" />
                    </svg>
                  )}
                </button>
              </div>
            </div>

            <button id="login-submit" className="button" type="submit" disabled={loading}>
              {loading ? "Authenticating…" : "Sign in →"}
            </button>
          </form>

          <p className="register-text">
            New to AI Smart Support?
            <span id="go-register" onClick={() => navigate("/register")} className="link">
              Create account
            </span>
          </p>
        </div>
      </div>
    </div>
  );
}

export default Login;
