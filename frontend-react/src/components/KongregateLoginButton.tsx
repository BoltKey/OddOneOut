import { useState } from "react";
import { api } from "../services/api";
import type { KongregateUser } from "../services/kongregate";
import { showKongregateLoginDialog } from "../services/kongregate";
import styles from "./KongregateLoginButton.module.css";

interface KongregateLoginButtonProps {
  kongregateUser: KongregateUser | null;
  onSuccess?: () => void;
}

const KongregateLoginButton = ({ kongregateUser, onSuccess }: KongregateLoginButtonProps) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleKongregateLogin = async () => {
    // If user is a guest on Kongregate, show the registration dialog
    if (!kongregateUser) {
      showKongregateLoginDialog();
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await api.kongregateLogin(
        kongregateUser.userId,
        kongregateUser.username,
        kongregateUser.gameAuthToken
      );
      onSuccess?.();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "Kongregate login failed";
      setError(errorMessage);
      console.error("Kongregate login error:", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <button
        onClick={handleKongregateLogin}
        className={styles.kongregateBtn + " button button-highlight"}
        type="button"
        disabled={loading}
      >
        <div className={styles.kongregateIconWrapper}>
          <svg className={styles.kongregateIcon} viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"/>
          </svg>
        </div>
        <span className={styles.btnText}>
          {loading ? "Connecting..." : kongregateUser ? `Play as ${kongregateUser.username}` : "Sign in to Kongregate"}
        </span>
      </button>
      {error && <div className={styles.error}>{error}</div>}
    </div>
  );
};

export default KongregateLoginButton;
