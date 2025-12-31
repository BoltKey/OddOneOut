import { useState } from "react";
import { api } from "../services/api";

interface Props {
  onLoginSuccess: () => void;
}

export default function LoginPage({ onLoginSuccess }: Props) {
  const [isRegistering, setIsRegistering] = useState(false);
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

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
        margin: "50px auto",
        padding: 20,
        border: "1px solid #ccc",
        borderRadius: 8,
      }}
    >
      <h2>{isRegistering ? "Create Account" : "Sign In"}</h2>

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
            style={{ width: "100%", padding: 8, marginTop: 5 }}
          />
        </div>

        <div>
          <label>Password</label>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: "100%", padding: 8, marginTop: 5 }}
          />
        </div>
        {isRegistering && (
          <div>
            <label>Email (optional)</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              style={{ width: "100%", padding: 8, marginTop: 5 }}
            />
          </div>
        )}
        {error && (
          <div style={{ color: "red", fontSize: "0.9em" }}>{error}</div>
        )}

        <button
          type="submit"
          disabled={loading}
          style={{ padding: 10, cursor: "pointer" }}
        >
          {loading ? "Processing..." : isRegistering ? "Register" : "Login"}
        </button>
      </form>

      <p style={{ marginTop: 20, fontSize: "0.9em", textAlign: "center" }}>
        {isRegistering ? "Already have an account?" : "Need an account?"}{" "}
        <button
          onClick={() => setIsRegistering(!isRegistering)}
          style={{
            background: "none",
            border: "none",
            color: "blue",
            textDecoration: "underline",
            cursor: "pointer",
          }}
        >
          {isRegistering ? "Login here" : "Register here"}
        </button>
      </p>
    </div>
  );
}
