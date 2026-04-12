import { useState } from "react";
import { api } from "../services/api";
import GoogleLoginButton from "./GoogleLoginButton";
import { FaReddit } from "react-icons/fa";
import { SiItchdotio } from "react-icons/si";
import type { DevvitContext } from "../services/devvit";
import type { ItchioContext } from "../services/itchio";
import "./LoginPage.css";

interface Props {
  onLoginSuccess: () => void;
  isGuest?: boolean;
  isRedditContext?: boolean;
  devvitContext?: DevvitContext | null;
  isItchioContext?: boolean;
  itchioContext?: ItchioContext | null;
}

export default function LoginPage({
  onLoginSuccess,
  isGuest,
  isRedditContext,
  devvitContext,
  isItchioContext,
  itchioContext,
}: Props) {
  const [isRegistering, setIsRegistering] = useState(isGuest || false);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [redditLoading, setRedditLoading] = useState(false);
  const [itchioLoading, setItchioLoading] = useState(false);

  // Get guest display name for button text
  const getGuestButtonText = () => {
    const storedGuestId = localStorage.getItem("guestUserId");
    if (storedGuestId) {
      const guestDisplayName = localStorage.getItem("guestDisplayName");
      if (guestDisplayName) {
        // Display name format is "Guest_xxxx" - use it directly
        return `Continue as ${guestDisplayName}`;
      }
      // Fallback: extract short ID from user ID (last 8 chars)
      const shortId =
        storedGuestId.length > 8
          ? storedGuestId.substring(storedGuestId.length - 8)
          : storedGuestId.substring(0, 8);
      return `Continue as Guest${shortId}`;
    }
    return "Play as Guest";
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      if (isRegistering) {
        await api.register(username, password);
        // After register, auto-login
        await api.login(username, password);
      } else {
        await api.login(username, password);
      }
      onLoginSuccess(); // Tell App.tsx we are done
    } catch (err: any) {
      setError(err.message || "An error occurred");
    } finally {
      setLoading(false);
    }
  };

  // Handle Reddit login when user clicks the button
  const handleRedditLogin = async () => {
    if (!devvitContext?.userId) {
      setError("Unable to detect Reddit account. Please try refreshing.");
      return;
    }

    setRedditLoading(true);
    setError("");

    try {
      await api.redditLogin(devvitContext.userId);
      onLoginSuccess();
    } catch (err: any) {
      setError(err.message || "Reddit login failed. Please try again.");
    } finally {
      setRedditLoading(false);
    }
  };

  const handleItchioLogin = async () => {
    const clientId = import.meta.env.VITE_ITCH_OAUTH_CLIENT_ID;
    if (!clientId) {
      setError(
        "Itch.io OAuth client ID is missing. Set VITE_ITCH_OAUTH_CLIENT_ID.",
      );
      return;
    }

    setItchioLoading(true);
    setError("");

    try {
      const state =
        typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
          ? crypto.randomUUID()
          : `${Date.now()}`;
      const callbackPath = window.location.pathname.endsWith("/")
        ? `${window.location.pathname}index.html`
        : window.location.pathname;
      const redirectUri = `${window.location.origin}${callbackPath}`;
      const oauthUrl =
        `https://itch.io/user/oauth` +
        `?client_id=${encodeURIComponent(clientId)}` +
        `&scope=${encodeURIComponent("profile:me")}` +
        `&redirect_uri=${encodeURIComponent(redirectUri)}` +
        `&response_type=token` +
        `&state=${encodeURIComponent(state)}`;

      const popup = window.open(
        oauthUrl,
        "itchio-oauth",
        "width=560,height=720,scrollbars=yes,resizable=yes",
      );

      if (!popup) {
        throw new Error("Popup blocked. Please allow popups for this page.");
      }

      const accessToken = await new Promise<string>((resolve, reject) => {
        const timeout = window.setTimeout(() => {
          cleanup();
          reject(new Error("Itch.io login timed out."));
        }, 120000);

        const closePoll = window.setInterval(() => {
          if (popup.closed) {
            cleanup();
            reject(new Error("Itch.io login was cancelled."));
          }
        }, 500);

        const onMessage = (event: MessageEvent) => {
          if (event.origin !== window.location.origin) return;
          if (!event.data || event.data.type !== "itch-oauth-success") return;
          if (event.data.state !== state) return;

          cleanup();

          if (event.data.error) {
            reject(new Error(`Itch.io login failed: ${event.data.error}`));
            return;
          }

          if (!event.data.accessToken) {
            reject(new Error("Itch.io login did not return an access token."));
            return;
          }

          resolve(event.data.accessToken);
        };

        const cleanup = () => {
          window.clearTimeout(timeout);
          window.clearInterval(closePoll);
          window.removeEventListener("message", onMessage);
        };

        window.addEventListener("message", onMessage);
      });

      const result = await api.itchioOAuthLogin(accessToken);
      if (result?.token) {
        localStorage.setItem("authToken", result.token);
      }
      onLoginSuccess();
    } catch (err: any) {
      setError(err.message || "Itch.io login failed. Please try again.");
    } finally {
      setItchioLoading(false);
    }
  };

  if (isItchioContext) {
    return (
      <div className="login-container">
        <div className="logo splashscreen"></div>

        <div className="itchio-login-section">
          <h2>Welcome to Misfit!</h2>
          <p>
            Sign in with itch.io to link your account and keep your progress.
          </p>

          {error && <div className="error-message">{error}</div>}

          <button
            className="itchio-login-button"
            onClick={handleItchioLogin}
            disabled={itchioLoading}
          >
            <SiItchdotio />
            <span>
              {itchioLoading ? "Connecting..." : "Continue with itch.io"}
            </span>
          </button>

          <div className="guest-option">
            <p>Or play without linking your account:</p>
            <button
              className="guest-button"
              onClick={async () => {
                try {
                  const storedGuestId = localStorage.getItem("guestUserId");
                  const result = await api.createGuest(
                    storedGuestId || undefined,
                  );
                  if (result?.userId) {
                    localStorage.setItem("guestUserId", result.userId);
                  }
                  onLoginSuccess();
                } catch (err: any) {
                  setError(err.message || "An error occurred");
                }
              }}
            >
              {getGuestButtonText()}
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Show Reddit-specific login UI when accessed from Reddit/Devvit
  if (isRedditContext && devvitContext?.userId) {
    return (
      <div className="login-container">
        <div className="logo splashscreen"></div>

        <div className="reddit-login-section">
          <h2>Welcome to Misfit!</h2>
          <p>Click below to start playing with your Reddit account.</p>

          {error && <div className="error-message">{error}</div>}

          <button
            className="reddit-login-button"
            onClick={handleRedditLogin}
            disabled={redditLoading}
          >
            <FaReddit />
            <span>{redditLoading ? "Connecting..." : "Play with Reddit"}</span>
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="login-container">
      {!isGuest && <div className="logo splashscreen"></div>}
      {isGuest && (
        <div className="guest-info">
          <p>
            You are currently playing as a Guest. Creating an account allows you
            to:
          </p>
          <ul>
            <li>Save and track your stats over time</li>
            <li>Play from multiple devices</li>
            <li>Compete on leaderboards</li>
            <li>Access your guess history</li>
          </ul>
        </div>
      )}
      Misfit is an async-multiplayer game, meaning you are guessing based on
      clues given by other real players. It is recommended to create an account,
      but you can play as guest if you want.
      <div className="divider"></div>
      <div className="login-tabs">
        <button
          className={`login-tab ${!isRegistering ? "active" : ""}`}
          onClick={() => {
            setIsRegistering(false);
            setError("");
          }}
        >
          Sign In
        </button>
        <button
          className={`login-tab ${isRegistering ? "active" : ""}`}
          onClick={() => {
            setIsRegistering(true);
            setError("");
          }}
        >
          Create Account
        </button>
      </div>
      <div className="login-content">
        <div className="google-login-section">
          <GoogleLoginButton onSuccess={onLoginSuccess} />
        </div>

        <div className="divider">
          <span>or</span>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              required
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              autoComplete="username"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              autoComplete={isRegistering ? "new-password" : "current-password"}
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" className="submit-button" disabled={loading}>
            {loading
              ? "Processing..."
              : isRegistering
                ? "Create Account"
                : "Sign In"}
          </button>
        </form>

        {!isGuest && (
          <div className="guest-option">
            <p>Don't want to create an account?</p>
            <button
              className="guest-button"
              onClick={async () => {
                try {
                  const storedGuestId = localStorage.getItem("guestUserId");
                  const result = await api.createGuest(
                    storedGuestId || undefined,
                  );
                  if (result?.userId) {
                    localStorage.setItem("guestUserId", result.userId);
                  }
                  onLoginSuccess();
                } catch (err: any) {
                  setError(err.message || "An error occurred");
                }
              }}
            >
              {getGuestButtonText()}
            </button>
          </div>
        )}
      </div>
      <a
        href="https://www.reddit.com/r/misfitgame/"
        target="_blank"
        rel="noopener noreferrer"
        className="reddit-link"
        title="Join us on Reddit"
      >
        <FaReddit />
        <span>r/misfitgame</span>
      </a>
    </div>
  );
}
