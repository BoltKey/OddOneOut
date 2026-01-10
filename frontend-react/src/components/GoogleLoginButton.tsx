import { useCallback, useEffect } from "react";
import styles from "./GoogleLoginButton.module.css";

interface GoogleLoginButtonProps {
  onSuccess?: () => void;
}

const GoogleLoginButton = ({ onSuccess }: GoogleLoginButtonProps) => {
  const handleGoogleLogin = useCallback(() => {
    // Calculate popup dimensions and position (centered)
    const width = 500;
    const height = 600;
    const left = window.screenX + (window.outerWidth - width) / 2;
    const top = window.screenY + (window.outerHeight - height) / 2;

    // Open popup for Google OAuth - add popup=true query param so backend knows
    const popup = window.open(
      `${import.meta.env.VITE_API_URL}/user/login-google?popup=true`,
      "google-login",
      `width=${width},height=${height},left=${left},top=${top},popup=yes`
    );

    // Focus the popup
    popup?.focus();
  }, []);

  // Listen for message from popup when auth completes
  useEffect(() => {
    const handleMessage = (event: MessageEvent) => {
      // Verify the message origin matches our API URL
      const apiUrl = new URL(import.meta.env.VITE_API_URL);
      if (event.origin !== apiUrl.origin) return;

      if (event.data?.type === "google-auth-success") {
        // If a JWT token is provided (for iframe/itch.io contexts where cookies don't work),
        // store it in localStorage for use in API requests
        if (event.data.token) {
          localStorage.setItem("authToken", event.data.token);
          console.log("Stored auth token for iframe context");
        }
        // Auth succeeded - small delay to ensure cookies have synced in PWA context
        setTimeout(() => {
          onSuccess?.();
        }, 100);
      } else if (event.data?.type === "google-auth-error") {
        console.error("Google auth failed:", event.data.error);
      }
    };

    window.addEventListener("message", handleMessage);
    return () => window.removeEventListener("message", handleMessage);
  }, [onSuccess]);

  return (
    <button
      onClick={handleGoogleLogin}
      className={
        styles.googleBtn + " button" + " button-highlight button-white"
      }
      type="button"
    >
      <div className={styles.googleIconWrapper}>
        <svg className={styles.googleIcon} viewBox="0 0 48 48">
          <path
            fill="#EA4335"
            d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"
          />
          <path
            fill="#4285F4"
            d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"
          />
          <path
            fill="#FBBC05"
            d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"
          />
          <path
            fill="#34A853"
            d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"
          />
          <path fill="none" d="M0 0h48v48H0z" />
        </svg>
      </div>
      <span className={styles.btnText}>Sign in with Google</span>
    </button>
  );
};

export default GoogleLoginButton;
