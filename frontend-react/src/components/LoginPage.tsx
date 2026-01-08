import { useState } from "react";
import { api } from "../services/api";
import GoogleLoginButton from "./GoogleLoginButton";

interface Props {
  onLoginSuccess: () => void;
  isGuest?: boolean;
}

export default function LoginPage({ onLoginSuccess, isGuest }: Props) {
  const [isRegistering, setIsRegistering] = useState(isGuest || false);
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  
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
        await api.register(
          username,
          password,
          email === "" ? undefined : email
        );
        // After register, usually auto-login or ask to login. Let's auto-login.
        // await api.login(username, password);
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

  return (
    <div
      className="login-container"
      style={{
        maxWidth: 400,
        minWidth: 300,
        margin: "50px auto",
        padding: 20,
        border: "1px solid #ccc",
        borderRadius: 8,
      }}
    >
      {!isGuest && <div className="logo splashscreen"></div>}
      {isGuest && (
        <>
          You are currently playing as a Guest. Creating an account allows you
          to:
          <ul>
            <li>Save and track your stats over time</li>
            <li>Play from multiple devices</li>
            <li>Compete on leaderboards</li>
            <li>Access your guess history</li>
          </ul>
        </>
      )}
      <h2>{isRegistering ? "Create Account" : "Sign In"}</h2>
      <p>
        <GoogleLoginButton />
      </p>
      Or:
      <form
        onSubmit={handleSubmit}
        style={{ display: "flex", flexDirection: "column", gap: 15 }}
      >
        <div>
          <label>Username</label>
          <input
            type="text"
            required
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            style={{ width: "90%", padding: 8, marginTop: 5 }}
          />
        </div>

        <div>
          <label>Password</label>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: "90%", padding: 8, marginTop: 5 }}
          />
        </div>
        {isRegistering && (
          <div>
            <label>Email (optional)</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              style={{ width: "90%", padding: 8, marginTop: 5 }}
            />
          </div>
        )}
        {error && (
          <div style={{ color: "red", fontSize: "0.9em" }}>{error}</div>
        )}

        <button
          type="submit"
          className="button-highlight"
          disabled={loading}
          style={{ padding: 10, cursor: "pointer" }}
        >
          {loading ? "Processing..." : isRegistering ? "Register" : "Login"}
        </button>
      </form>
      {!isGuest && (
        <>
          <p style={{ marginTop: 20 }}>
            {isRegistering
              ? "Already have an account?"
              : "Don't have an account?"}{" "}
            <button onClick={() => setIsRegistering(!isRegistering)}>
              {isRegistering ? "Login" : "Create new account"}
            </button>
          </p>
          <p>
            Don't want to create an account?{" "}
            <button
              onClick={async () => {
                try {
                  // Check for existing guest ID in localStorage
                  const storedGuestId = localStorage.getItem("guestUserId");
                  const result = await api.createGuest(storedGuestId || undefined);
                  // Store the guest user ID for future sessions
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
          </p>
        </>
      )}
    </div>
  );
}
