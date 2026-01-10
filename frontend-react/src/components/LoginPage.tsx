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

export default function LoginPage({ onLoginSuccess, isGuest, isRedditContext, devvitContext, isItchioContext, itchioContext }: Props) {
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
      const shortId = storedGuestId.length > 8 
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

  // Handle itch.io login when user clicks the button
  const handleItchioLogin = async () => {
    if (!itchioContext?.userId) {
      setError("Unable to detect itch.io account. Please make sure you're logged in to itch.io.");
      return;
    }

    setItchioLoading(true);
    setError("");

    try {
      await api.itchioLogin(itchioContext.userId, itchioContext.username);
      onLoginSuccess();
    } catch (err: any) {
      setError(err.message || "Itch.io login failed. Please try again.");
    } finally {
      setItchioLoading(false);
    }
  };

  // Show itch.io-specific login UI when accessed from itch.io with a logged-in user
  if (isItchioContext && itchioContext?.userId) {
    return (
      <div className="login-container">
        <div className="logo splashscreen"></div>
        
        <div className="itchio-login-section">
          <h2>Welcome to Misfit!</h2>
          <p>Click below to start playing with your itch.io account.</p>
          
          {error && <div className="error-message">{error}</div>}
          
          <button
            className="itchio-login-button"
            onClick={handleItchioLogin}
            disabled={itchioLoading}
          >
            <SiItchdotio />
            <span>{itchioLoading ? "Connecting..." : `Play as ${itchioContext.displayName || itchioContext.username}`}</span>
          </button>
          
          <div className="guest-option">
            <p>Or play without linking your account:</p>
            <button
              className="guest-button"
              onClick={async () => {
                try {
                  const storedGuestId = localStorage.getItem("guestUserId");
                  const result = await api.createGuest(storedGuestId || undefined);
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
          <p>You are currently playing as a Guest. Creating an account allows you to:</p>
          <ul>
            <li>Save and track your stats over time</li>
            <li>Play from multiple devices</li>
            <li>Compete on leaderboards</li>
            <li>Access your guess history</li>
          </ul>
        </div>
      )}
      
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

          <button
            type="submit"
            className="submit-button"
            disabled={loading}
          >
            {loading ? "Processing..." : isRegistering ? "Create Account" : "Sign In"}
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
                  const result = await api.createGuest(storedGuestId || undefined);
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
